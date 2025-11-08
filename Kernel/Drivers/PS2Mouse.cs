/*
using guideXOS.Misc;
using System.Windows.Forms;
using static Native;

namespace guideXOS.Kernel.Drivers {
    /// <summary>
    /// PS/2 Mouse driver (median + EMA + edge clamping)
    /// </summary>
    public static unsafe class PS2Mouse {
        private const byte Data = 0x60;
        private const byte Command = 0x64;
        private const byte SetDefaults = 0xF6;
        private const byte EnableDataReporting = 0xF4;
        private const byte SetSampleRate = 0xF3;
        private const byte Identify = 0xF2;

        private static int Phase = 1;
        public static byte[] MData;

        private static double _fx, _fy;
        private static float _vx, _vy; // smoothed velocity (EMA of deltas)

        // Small median filter to reject spikes
        private static int[] _dxBuf = new int[3];
        private static int[] _dyBuf = new int[3];
        private static int _bufIdx = 0;

        // Tunables
        private const float Sensitivity = 1.10f;   // base speed multiplier (lower = less jumpy)
        private const int MaxDelta = 24;           // clamp per-packet delta (lower = reject big spikes)
        private const float Alpha = 0.25f;         // EMA factor (0..1); lower = smoother
        private const float VelDeadzone = 0.35f;   // small velocity below this is considered zero

        public static int ScreenWidth = 0;
        public static int ScreenHeight = 0;

        // True when PS/2 mouse actively provides movement; lets USB path back off
        public static volatile bool Active = false;

        public static void Initialise() {
            MData = new byte[3];
            Phase = 1;
            _fx = Control.MousePosition.X;
            _fy = Control.MousePosition.Y;
            _vx = _vy = 0;
            _dxBuf[0] = _dxBuf[1] = _dxBuf[2] = 0;
            _dyBuf[0] = _dyBuf[1] = _dyBuf[2] = 0;
            _bufIdx = 0;
            Active = false;

            Interrupts.EnableInterrupt(0x2C, &OnInterrupt);

            // Enable PS/2 auxiliary device
            Hlt(); Out8(Command, 0xA8);

            // Enable IRQ12 and IRQ1 in controller status
            Hlt(); Out8(Command, 0x20);
            Hlt(); byte status = In8(0x60);
            status = (byte)(status | 0x03);
            Hlt(); Out8(Command, 0x60);
            Hlt(); Out8(Data, status);

            FlushOutput();

            // Initialize mouse
            SendToMouse(SetDefaults);
            // Sample rate tuning (100Hz is smoother/stabler in VirtualBox)
            SendToMouse(SetSampleRate); SendToMouse(100);
            SendToMouse(Identify); _ = TryReadData(out _);
            // Enable streaming last
            SendToMouse(EnableDataReporting);
            FlushOutput();

            Control.MouseButtons = 0;
        }

        private static void FlushOutput(int maxReads = 32) {
            for (int i = 0; i < maxReads; i++) {
                byte s = In8(0x64);
                if ((s & 1) == 0) break;
                _ = In8(0x60);
                Hlt();
            }
        }

        private static void SendToMouse(byte value) {
            Hlt(); Out8(Command, 0xD4);
            Hlt(); Out8(Data, value);
            _ = ReadData(); // consume ACK if any
        }

        private static byte ReadData() { Hlt(); return In8(Data); }
        private static bool TryReadData(out byte b) { byte s = In8(0x64); if ((s & 1) != 0) { b = In8(0x60); return true; } b = 0; return false; }

        public static void OnInterrupt() {
            byte D = In8(Data);
            if (VMwareTools.Available) return;
            if (D == 0xFA) return; // ACK

            if (Phase == 1) {
                if ((D & 0x08) == 0x08) { MData[0] = D; Phase = 2; }
                return;
            }
            if (Phase == 2) { MData[1] = D; Phase = 3; return; }

            if (Phase == 3) {
                MData[2] = D; Phase = 1;

                // Valid packet received; PS/2 active
                Active = true;

                // Drop on overflow
                if ((MData[0] & 0x40) != 0 || (MData[0] & 0x80) != 0) return;

                // Buttons (use enum flags)
                MouseButtons buttons = MouseButtons.None;
                if ((MData[0] & 0x01) != 0) buttons |= MouseButtons.Left;
                if ((MData[0] & 0x02) != 0) buttons |= MouseButtons.Right;
                if ((MData[0] & 0x04) != 0) buttons |= MouseButtons.Middle;
                Control.MouseButtons = buttons;

                // Signed deltas
                int dx = unchecked((sbyte)MData[1]);
                int dy = unchecked((sbyte)MData[2]);

                // Clamp deltas (avoid spikes)
                if (dx > MaxDelta) dx = MaxDelta; else if (dx < -MaxDelta) dx = -MaxDelta;
                if (dy > MaxDelta) dy = MaxDelta; else if (dy < -MaxDelta) dy = -MaxDelta;

                // Invert Y for screen coords (PS/2 Y is up)
                dy = -dy;

                // Push into 3-sample ring buffer
                int i = _bufIdx;
                _dxBuf[i] = dx; _dyBuf[i] = dy;
                _bufIdx = (i + 1) % 3;
                // Median-of-3 filter
                dx = Median3(_dxBuf[0], _dxBuf[1], _dxBuf[2]);
                dy = Median3(_dyBuf[0], _dyBuf[1], _dyBuf[2]);

                // EMA smoothing of deltas into velocity
                _vx = Alpha * _vx + (1f - Alpha) * dx;
                _vy = Alpha * _vy + (1f - Alpha) * dy;

                // Small velocity deadzone
                if (AbsF(_vx) < VelDeadzone) _vx = 0;
                if (AbsF(_vy) < VelDeadzone) _vy = 0;

                // Accumulate with sensitivity
                _fx += _vx * Sensitivity;
                _fy += _vy * Sensitivity;

                int maxX = Framebuffer.Width > 0 ? Framebuffer.Width - 1 : 0;
                int maxY = Framebuffer.Height > 0 ? Framebuffer.Height - 1 : 0;

                // Hard clamp and reset velocity to avoid edge oscillation/flicker
                if (_fx <= 0) { _fx = 0; if (_vx < 0) _vx = 0; }
                else if (_fx >= maxX) { _fx = maxX; if (_vx > 0) _vx = 0; }
                if (_fy <= 0) { _fy = 0; if (_vy < 0) _vy = 0; }
                else if (_fy >= maxY) { _fy = maxY; if (_vy > 0) _vy = 0; }

                // Publish floored integer positions
                Control.MousePosition.X = (int)_fx;
                Control.MousePosition.Y = (int)_fy;
            }
        }

        private static int Median3(int a, int b, int c) {
            // branchless-ish median of 3
            if (a > b) { int t = a; a = b; b = t; }
            if (b > c) { int t = b; b = c; c = t; }
            if (a > b) { int t = a; a = b; b = t; }
            return b;
        }

        private static int AbsF(float v) { return v < 0 ? (int)(-v) : (int)v; }
    }
}
*/