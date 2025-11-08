//http://cc.etsii.ull.es/ftp/antiguo/EC/AOA/APPND/Apndxc.pdf
using guideXOS.Misc;
using System;
using static System.ConsoleKey;
namespace guideXOS.Kernel.Drivers {
    /// <summary>
    /// PS2 Keyboard
    /// </summary>
    public static unsafe class PS2Keyboard {
        /// <summary>
        /// Key Chars
        /// </summary>
        private static char[] _keyChars;
        /// <summary>
        /// Key Chars Shift
        /// </summary>
        private static char[] _keyCharsShift;
        /// <summary>
        /// Keys
        /// </summary>
        private static ConsoleKey[] _keys;

        // Keep track of modifier state separately
        private static bool _shiftPressed = false;
        private static bool _ctrlPressed = false;
        private static bool _altPressed = false;
        private static bool _capsLockOn = false;

        /// <summary>
        /// Initialize
        /// </summary>
        /// <returns></returns>
        public static bool Initialize() {
            _keyChars = new char[] {
                '\0','\0','1','2','3','4','5','6','7','8','9','0','-','=','\b','\t',
                'q','w','e','r','t','y','u','i','o','p','[',']','\n','\0',
                'a','s','d','f','g','h','j','k','l',';','\'','`','\0','\\',
                'z','x','c','v','b','n','m',',','.','/','\0','*','\0',' ','\0',
                '\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','7','8','9','-',
                '4','5','6','+','1','2','3','0','.','\0','\0','\0','\0','\0'
            };
            _keyCharsShift = new char[] {
                '\0','\0','!','@','#','$','%','^','&','*','(',')','_','+','\b','\t',
                'Q','W','E','R','T','Y','U','I','O','P','{','}','\n','\0',
                'A','S','D','F','G','H','J','K','L',':','\"','~','\0','|',
                'Z','X','C','V','B','N','M','<','>','?','\0','*','\0',' ','\0',
                '\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','7','8','9','-',
                '4','5','6','+','1','2','3','0','.','\0','\0','\0','\0','\0'
            };
            _keys = new[] {
                None, Escape, D1, D2, D3, D4, D5, D6, D7, D8, D9, D0, OemMinus, OemPlus, Backspace, Tab,
                Q, W, E, R, T, Y, U, I, O, P, Oem4, Oem6, Return, LControlKey,
                A, S, D, F, G, H, J, K, L, Oem1, Oem7, Oem3, LShiftKey, Oem8,
                Z, X, C, V, B, N, M, OemComma, OemPeriod, Oem2, RShiftKey, Multiply, LMenu, Space, Capital, F1, F2, F3, F4, F5,
                F6, F7, F8, F9, F10, NumLock, Scroll, Home, Up, Prior, Subtract, Left, Clear, Right, Add, End,
                Down, Next, Insert, Delete, Snapshot, None, Oem5, F11, F12
            };
            Keyboard.CleanKeyInfo();
            Interrupts.EnableInterrupt(0x21, &OnInterrupt);
            return true;
        }
        /// <summary>
        /// On Interrupt
        /// </summary>
        public static void OnInterrupt() {
            byte b = Native.In8(0x60);
            PS2Keyboard.ProcessKey(b);
        }
        /// <summary>
        /// Process Key
        /// </summary>
        /// <param name="b"></param>
        public static void ProcessKey(byte b) {
            bool isRelease = (b & 0x80) != 0;
            byte scanCode = (byte)(b & 0x7F);

            if (scanCode >= _keys.Length) return;

            Keyboard.KeyInfo.ScanCode = b;
            Keyboard.KeyInfo.KeyState = isRelease ? ConsoleKeyState.Released : ConsoleKeyState.Pressed;

            // Handle modifier keys and update our tracked state
            if (scanCode == 0x1D) { // Ctrl
                _ctrlPressed = !isRelease;
            }
            if (scanCode == 0x2A || scanCode == 0x36) { // Left or Right Shift
                _shiftPressed = !isRelease;
            }
            if (scanCode == 0x38) { // Alt
                _altPressed = !isRelease;
            }
            if (scanCode == 0x3A && !isRelease) { // CapsLock toggle on press
                _capsLockOn = !_capsLockOn;
            }

            // Update Keyboard.KeyInfo.Modifiers based on our tracked state
            Keyboard.KeyInfo.Modifiers = ConsoleModifiers.None;
            if (_shiftPressed) Keyboard.KeyInfo.Modifiers |= ConsoleModifiers.Shift;
            if (_ctrlPressed) Keyboard.KeyInfo.Modifiers |= ConsoleModifiers.Control;
            if (_altPressed) Keyboard.KeyInfo.Modifiers |= ConsoleModifiers.Alt;
            if (_capsLockOn) Keyboard.KeyInfo.Modifiers |= ConsoleModifiers.CapsLock;

            // Determine the character based on the current modifier state
            Keyboard.KeyInfo.KeyChar = '\0';

            if (scanCode < _keyChars.Length) {
                char baseChar = _keyChars[scanCode];

                if (_shiftPressed && scanCode < _keyCharsShift.Length) {
                    // Shift is pressed - use shift map
                    Keyboard.KeyInfo.KeyChar = _keyCharsShift[scanCode];
                } else if (_capsLockOn && char.IsLetter(baseChar)) {
                    // CapsLock only affects letters
                    Keyboard.KeyInfo.KeyChar = baseChar.ToUpper();
                } else {
                    // Normal character
                    Keyboard.KeyInfo.KeyChar = baseChar;
                }
            }

            // Set the ConsoleKey
            Keyboard.KeyInfo.Key = _keys[scanCode];

            Keyboard.InvokeOnKeyChanged(Keyboard.KeyInfo);

            //This is for some kind of PC that have PS2 emulation but doesn't have PS2 mouse emulation
            Kbd2Mouse.OnKeyChanged(Keyboard.KeyInfo);
        }
    }
}