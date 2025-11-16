using guideXOS.Misc;
using System;
using static System.ConsoleKey;
namespace guideXOS.Kernel.Drivers {
    public static unsafe class PS2Keyboard {
        private const byte DataPort = 0x60;
        private const byte CommandPort = 0x64;
        
        // Modifier key states
        private static bool _leftShift = false;
        private static bool _rightShift = false;
        private static bool _leftCtrl = false;
        private static bool _rightCtrl = false;
        private static bool _leftAlt = false;
        private static bool _rightAlt = false;
        private static bool _capsLock = false;
        private static bool _numLock = false;
        private static bool _scrollLock = false;
        
        // Extended scancode flag (0xE0 prefix)
        private static bool _extended = false;
        
        /// <summary>
        /// Initialize PS/2 Keyboard
        /// </summary>
        public static void Initialize() {
            // Register IRQ1 (keyboard interrupt)
            Interrupts.EnableInterrupt(0x21, &OnInterrupt);
            
            // Enable keyboard
            Native.Hlt();
            Native.Out8(CommandPort, 0x60); // Send command byte
            Native.Hlt();
            Native.Out8(DataPort, 0x65); // Enable keyboard interrupt
            Native.Hlt();
            
            Console.WriteLine("[PS2KBD] PS/2 Keyboard initialized");
        }
        
        /// <summary>
        /// Keyboard interrupt handler
        /// </summary>
        public static void OnInterrupt() {
            byte scancode = Native.In8(DataPort);
            
            // Handle extended scancodes (0xE0 prefix)
            if (scancode == 0xE0) {
                _extended = true;
                return;
            }
            
            // Check if this is a key release (bit 7 set)
            bool released = (scancode & 0x80) != 0;
            byte makeCode = (byte)(scancode & 0x7F);
            
            // Update modifier states
            UpdateModifiers(makeCode, released);
            
            // Get the ConsoleKey and character
            ConsoleKey key = ScancodeToKey(makeCode, _extended);
            char keyChar = GetChar(makeCode, _extended);
            
            // Build modifiers flags
            ConsoleModifiers mods = ConsoleModifiers.None;
            if (_leftShift || _rightShift) mods |= ConsoleModifiers.Shift;
            if (_leftCtrl || _rightCtrl) mods |= ConsoleModifiers.Control;
            if (_leftAlt || _rightAlt) mods |= ConsoleModifiers.Alt;
            if (_capsLock) mods |= ConsoleModifiers.CapsLock;
            
            // Create ConsoleKeyInfo
            Keyboard.KeyInfo = new ConsoleKeyInfo {
                Key = key,
                KeyChar = keyChar,
                Modifiers = mods,
                KeyState = released ? ConsoleKeyState.Released : ConsoleKeyState.Pressed,
                ScanCode = scancode
            };
            
            // Reset extended flag
            _extended = false;
            
            // Invoke keyboard events
            Keyboard.InvokeOnKeyChanged(Keyboard.KeyInfo);
            Kbd2Mouse.OnKeyChanged(Keyboard.KeyInfo);
        }
        
        /// <summary>
        /// Update modifier key states
        /// </summary>
        private static void UpdateModifiers(byte makeCode, bool released) {
            if (_extended) {
                // Extended keys
                switch (makeCode) {
                    case 0x1D: // Right Ctrl
                        _rightCtrl = !released;
                        break;
                    case 0x38: // Right Alt
                        _rightAlt = !released;
                        break;
                }
            } else {
                // Normal keys
                switch (makeCode) {
                    case 0x2A: // Left Shift
                        _leftShift = !released;
                        break;
                    case 0x36: // Right Shift
                        _rightShift = !released;
                        break;
                    case 0x1D: // Left Ctrl
                        _leftCtrl = !released;
                        break;
                    case 0x38: // Left Alt
                        _leftAlt = !released;
                        break;
                    case 0x3A: // Caps Lock - toggle on press
                        if (!released) _capsLock = !_capsLock;
                        break;
                    case 0x45: // Num Lock - toggle on press
                        if (!released) _numLock = !_numLock;
                        break;
                    case 0x46: // Scroll Lock - toggle on press
                        if (!released) _scrollLock = !_scrollLock;
                        break;
                }
            }
        }
        
        /// <summary>
        /// Convert scancode to ConsoleKey
        /// </summary>
        private static ConsoleKey ScancodeToKey(byte makeCode, bool extended) {
            if (extended) {
                // Extended keys (E0 prefix)
                switch (makeCode) {
                    case 0x1C: return Enter; // Keypad Enter
                    case 0x1D: return ConsoleKey.None; // Right Ctrl (modifier only, no separate key)
                    case 0x35: return Divide; // Keypad /
                    case 0x38: return ConsoleKey.None; // Right Alt (modifier only, no separate key)
                    case 0x47: return Home;
                    case 0x48: return Up;
                    case 0x49: return PageUp;
                    case 0x4B: return Left;
                    case 0x4D: return Right;
                    case 0x4F: return End;
                    case 0x50: return Down;
                    case 0x51: return PageDown;
                    case 0x52: return Insert;
                    case 0x53: return Delete;
                    case 0x5B: return LeftWindows;
                    case 0x5C: return RightWindows;
                    case 0x5D: return Applications;
                    default: return ConsoleKey.None;
                }
            }
            
            // Normal scancodes
            switch (makeCode) {
                case 0x01: return Escape;
                case 0x02: return D1;
                case 0x03: return D2;
                case 0x04: return D3;
                case 0x05: return D4;
                case 0x06: return D5;
                case 0x07: return D6;
                case 0x08: return D7;
                case 0x09: return D8;
                case 0x0A: return D9;
                case 0x0B: return D0;
                case 0x0C: return OemMinus;
                case 0x0D: return OemPlus;
                case 0x0E: return Backspace;
                case 0x0F: return Tab;
                case 0x10: return Q;
                case 0x11: return W;
                case 0x12: return E;
                case 0x13: return R;
                case 0x14: return T;
                case 0x15: return Y;
                case 0x16: return U;
                case 0x17: return I;
                case 0x18: return O;
                case 0x19: return P;
                case 0x1A: return Oem4; // [
                case 0x1B: return Oem6; // ]
                case 0x1C: return Enter;
                case 0x1D: return ConsoleKey.None; // Left Ctrl (modifier only)
                case 0x1E: return A;
                case 0x1F: return S;
                case 0x20: return D;
                case 0x21: return F;
                case 0x22: return G;
                case 0x23: return H;
                case 0x24: return J;
                case 0x25: return K;
                case 0x26: return L;
                case 0x27: return Oem1; // ;
                case 0x28: return Oem7; // '
                case 0x29: return Oem3; // `
                case 0x2A: return ConsoleKey.None; // Left Shift (modifier only)
                case 0x2B: return Oem5; // backslash
                case 0x2C: return Z;
                case 0x2D: return X;
                case 0x2E: return C;
                case 0x2F: return V;
                case 0x30: return B;
                case 0x31: return N;
                case 0x32: return M;
                case 0x33: return OemComma;
                case 0x34: return OemPeriod;
                case 0x35: return Oem2; // /
                case 0x36: return ConsoleKey.None; // Right Shift (modifier only)
                case 0x37: return Multiply; // Keypad *
                case 0x38: return ConsoleKey.None; // Left Alt (modifier only)
                case 0x39: return Space;
                case 0x3A: return CapsLock;
                case 0x3B: return F1;
                case 0x3C: return F2;
                case 0x3D: return F3;
                case 0x3E: return F4;
                case 0x3F: return F5;
                case 0x40: return F6;
                case 0x41: return F7;
                case 0x42: return F8;
                case 0x43: return F9;
                case 0x44: return F10;
                case 0x45: return NumLock;
                case 0x46: return Pause; // Scroll Lock (using Pause as closest match)
                case 0x47: return NumPad7; // Home
                case 0x48: return NumPad8; // Up
                case 0x49: return NumPad9; // PgUp
                case 0x4A: return Subtract; // Keypad -
                case 0x4B: return NumPad4; // Left
                case 0x4C: return NumPad5;
                case 0x4D: return NumPad6; // Right
                case 0x4E: return Add; // Keypad +
                case 0x4F: return NumPad1; // End
                case 0x50: return NumPad2; // Down
                case 0x51: return NumPad3; // PgDn
                case 0x52: return NumPad0; // Ins
                case 0x53: return Decimal; // Del
                case 0x57: return F11;
                case 0x58: return F12;
                default: return ConsoleKey.None;
            }
        }
        
        /// <summary>
        /// Get the character for a scancode with current modifiers
        /// </summary>
        private static char GetChar(byte makeCode, bool extended) {
            bool shift = _leftShift || _rightShift;
            bool caps = _capsLock;
            
            // Extended keys don't produce characters
            if (extended) return '\0';
            
            // Special keys that produce characters
            switch (makeCode) {
                case 0x39: return ' '; // Space
                case 0x1C: return '\n'; // Enter
                case 0x0F: return '\t'; // Tab
                case 0x0E: return '\b'; // Backspace
            }
            
            // Letters (A-Z)
            if (makeCode >= 0x1E && makeCode <= 0x2C) {
                char[] letters = new char[] { 'a', 's', 'd', 'f', 'g', 'h', 'j', 'k', 'l' };
                if (makeCode <= 0x26) {
                    char c = letters[makeCode - 0x1E];
                    if (shift ^ caps) c = (char)(c - 32); // Convert to uppercase
                    return c;
                }
            }
            if (makeCode >= 0x10 && makeCode <= 0x19) {
                char[] letters = new char[] { 'q', 'w', 'e', 'r', 't', 'y', 'u', 'i', 'o', 'p' };
                char c = letters[makeCode - 0x10];
                if (shift ^ caps) c = (char)(c - 32);
                return c;
            }
            if (makeCode >= 0x2C && makeCode <= 0x32) {
                char[] letters = new char[] { 'z', 'x', 'c', 'v', 'b', 'n', 'm' };
                char c = letters[makeCode - 0x2C];
                if (shift ^ caps) c = (char)(c - 32);
                return c;
            }
            
            // Numbers and symbols on number row
            if (!shift) {
                switch (makeCode) {
                    case 0x02: return '1';
                    case 0x03: return '2';
                    case 0x04: return '3';
                    case 0x05: return '4';
                    case 0x06: return '5';
                    case 0x07: return '6';
                    case 0x08: return '7';
                    case 0x09: return '8';
                    case 0x0A: return '9';
                    case 0x0B: return '0';
                    case 0x0C: return '-';
                    case 0x0D: return '=';
                    case 0x1A: return '[';
                    case 0x1B: return ']';
                    case 0x2B: return '\\';
                    case 0x27: return ';';
                    case 0x28: return '\'';
                    case 0x29: return '`';
                    case 0x33: return ',';
                    case 0x34: return '.';
                    case 0x35: return '/';
                }
            } else {
                // Shifted symbols
                switch (makeCode) {
                    case 0x02: return '!';
                    case 0x03: return '@';
                    case 0x04: return '#';
                    case 0x05: return '$';
                    case 0x06: return '%';
                    case 0x07: return '^';
                    case 0x08: return '&';
                    case 0x09: return '*';
                    case 0x0A: return '(';
                    case 0x0B: return ')';
                    case 0x0C: return '_';
                    case 0x0D: return '+';
                    case 0x1A: return '{';
                    case 0x1B: return '}';
                    case 0x2B: return '|';
                    case 0x27: return ':';
                    case 0x28: return '"';
                    case 0x29: return '~';
                    case 0x33: return '<';
                    case 0x34: return '>';
                    case 0x35: return '?';
                }
            }
            
            // Numeric keypad (when NumLock is on)
            if (_numLock && !extended) {
                switch (makeCode) {
                    case 0x47: return '7';
                    case 0x48: return '8';
                    case 0x49: return '9';
                    case 0x4B: return '4';
                    case 0x4C: return '5';
                    case 0x4D: return '6';
                    case 0x4F: return '1';
                    case 0x50: return '2';
                    case 0x51: return '3';
                    case 0x52: return '0';
                    case 0x53: return '.';
                    case 0x37: return '*';
                    case 0x4A: return '-';
                    case 0x4E: return '+';
                }
            }
            
            return '\0';
        }
    }
}