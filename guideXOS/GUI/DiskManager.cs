using guideXOS.FS;
using guideXOS.Kernel.Drivers;
using System;
using System.Windows.Forms;

namespace guideXOS.GUI {
    internal class DiskManager : Window {
        private string _status = string.Empty;
        private string _detected = "Unknown";
        private bool _clickLock;

        // UI layout
        private const int Pad = 14;
        private const int BtnW = 220;
        private const int BtnH = 28;
        private const int Gap = 10;

        // Button rects (computed in draw)
        private int _bxDetectX, _bxDetectY;
        private int _bxSwitchFatX, _bxSwitchFatY;
        private int _bxSwitchTarX, _bxSwitchTarY;
        private int _bxFormatExfatX, _bxFormatExfatY;

        public DiskManager(int x, int y, int w = 520, int h = 240) : base(x, y, w, h) {
            Title = "Disk Manager";
            _status = BuildStatus();
        }

        private string BuildStatus() {
            string driver;
            if (File.Instance == null) driver = "<none>";
            else if (File.Instance is FAT) driver = "FAT";
            else if (File.Instance is FATFS) driver = "FATFS";
            else if (File.Instance is TarFS) driver = "TarFS";
            else driver = "Unknown";
            return $"Driver: {driver}\nDetected media: {_detected}";
        }

        private void ProbeOnce() {
            try {
                var buf = new byte[FileSystem.SectorSize];
                unsafe { fixed (byte* p = buf) Disk.Instance.Read(0, 1, p); }
                if (buf.Length >= 512 && buf[257] == (byte)'u' && buf[258] == (byte)'s' && buf[259] == (byte)'t' && buf[260] == (byte)'a' && buf[261] == (byte)'r') _detected = "TAR (initrd)";
                else if (buf.Length >= 512 && buf[510] == 0x55 && buf[511] == 0xAA) _detected = "FAT (boot sector)";
                else _detected = "Unknown";
            } catch { _detected = "Unknown"; }
            _status = BuildStatus();
        }

        public override void OnInput() {
            base.OnInput(); if (!Visible) return;
            int mx = Control.MousePosition.X; int my = Control.MousePosition.Y;
            bool left = Control.MouseButtons.HasFlag(MouseButtons.Left);
            if (left) {
                if (_clickLock) return;
                if (Hit(mx, my, _bxDetectX, _bxDetectY, BtnW, BtnH)) { ProbeOnce(); _clickLock = true; return; }
                if (Hit(mx, my, _bxSwitchFatX, _bxSwitchFatY, BtnW, BtnH)) { try { File.Instance = new FAT(); Desktop.InvalidateDirCache(); _status = BuildStatus(); } catch { _status = "Switch to FAT failed."; } _clickLock = true; return; }
                if (Hit(mx, my, _bxSwitchTarX, _bxSwitchTarY, BtnW, BtnH)) { try { File.Instance = new TarFS(); Desktop.InvalidateDirCache(); _status = BuildStatus(); } catch { _status = "Switch to TAR failed."; } _clickLock = true; return; }
                if (Hit(mx, my, _bxFormatExfatX, _bxFormatExfatY, BtnW, BtnH)) {
                    try {
                        var fs = new FATFS();
                        fs.Format();
                        File.Instance = fs;
                        Desktop.InvalidateDirCache();
                        _detected = "FAT (boot sector)";
                        _status = BuildStatus();
                    } catch { _status = "Format failed."; }
                    _clickLock = true; return;
                }
            } else {
                _clickLock = false;
            }
        }

        private static bool Hit(int mx, int my, int x, int y, int w, int h) { return mx >= x && mx <= x + w && my >= y && my <= y + h; }

        public override void OnDraw() {
            base.OnDraw();
            // Background panel
            Framebuffer.Graphics.FillRectangle(X + 1, Y + 1, Width - 2, Height - 2, 0xFF2B2B2B);

            // Cache mouse for this frame
            int mx = Control.MousePosition.X; int my = Control.MousePosition.Y;

            // Status (short, single call)
            WindowManager.font.DrawString(X + Pad, Y + Pad, _status);

            int btnY = Y + Pad + WindowManager.font.FontSize * 2 + 12;
            _bxDetectX = X + Pad; _bxDetectY = btnY;
            DrawButton(mx, my, _bxDetectX, _bxDetectY, BtnW, BtnH, "Detect now"); btnY += BtnH + Gap;

            _bxSwitchFatX = X + Pad; _bxSwitchFatY = btnY;
            DrawButton(mx, my, _bxSwitchFatX, _bxSwitchFatY, BtnW, BtnH, "Use FAT driver (RW)"); btnY += BtnH + Gap;

            _bxSwitchTarX = X + Pad; _bxSwitchTarY = btnY;
            DrawButton(mx, my, _bxSwitchTarX, _bxSwitchTarY, BtnW, BtnH, "Use TAR driver (RO)"); btnY += BtnH + Gap;

            _bxFormatExfatX = X + Pad; _bxFormatExfatY = btnY;
            DrawButton(mx, my, _bxFormatExfatX, _bxFormatExfatY, BtnW, BtnH, "Format RAM as exFAT");
        }

        private void DrawButton(int mx, int my, int x, int y, int w, int h, string text) {
            bool hover = Hit(mx, my, x, y, w, h);
            uint bg = hover ? 0xFF3A3A3A : 0xFF323232;
            Framebuffer.Graphics.FillRectangle(x, y, w, h, bg);
            WindowManager.font.DrawString(x + 10, y + (h / 2 - WindowManager.font.FontSize / 2), text);
        }
    }
}
