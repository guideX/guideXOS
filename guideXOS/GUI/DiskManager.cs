using guideXOS.FS;
using guideXOS.Kernel.Drivers;
using System;
using System.Windows.Forms;

namespace guideXOS.GUI {
    internal class DiskManager : Window {
        private string _status = string.Empty;
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

        public DiskManager(int x, int y, int w = 520, int h = 300) : base(x, y, w, h) {
            Title = "Disk Manager";
            UpdateStatus();
        }

        private void UpdateStatus() {
            string driver;
            if (File.Instance == null) driver = "<none>";
            else if (File.Instance is FAT) driver = "FAT";
            else if (File.Instance is FATFS) driver = "FATFS";
            else if (File.Instance is TarFS) driver = "TarFS";
            else driver = "Unknown";

            string detected = "Unknown";
            try {
                var buf = new byte[FileSystem.SectorSize];
                unsafe { fixed (byte* p = buf) Disk.Instance.Read(0, 1, p); }
                if (buf.Length >= 512 && buf[257] == (byte)'u' && buf[258] == (byte)'s' && buf[259] == (byte)'t' && buf[260] == (byte)'a' && buf[261] == (byte)'r') detected = "TAR (initrd)";
                else if (buf.Length >= 512 && buf[510] == 0x55 && buf[511] == 0xAA) detected = "FAT (boot sector)";
            } catch { }
            _status = $"Driver: {driver}\nDetected media: {detected}";
        }

        public override void OnInput() {
            base.OnInput(); if (!Visible) return;
            int mx = Control.MousePosition.X; int my = Control.MousePosition.Y;
            bool left = Control.MouseButtons.HasFlag(MouseButtons.Left);
            if (left) {
                if (_clickLock) return;
                // Detect
                if (Hit(mx, my, _bxDetectX, _bxDetectY, BtnW, BtnH)) { UpdateStatus(); _clickLock = true; return; }
                // Switch to FAT (RW)
                if (Hit(mx, my, _bxSwitchFatX, _bxSwitchFatY, BtnW, BtnH)) { try { File.Instance = new FAT(); Desktop.InvalidateDirCache(); _status = "Switched driver to FAT (RW)."; } catch { _status = "Switch to FAT failed."; } _clickLock = true; return; }
                // Switch to TAR (RO)
                if (Hit(mx, my, _bxSwitchTarX, _bxSwitchTarY, BtnW, BtnH)) { try { File.Instance = new TarFS(); Desktop.InvalidateDirCache(); _status = "Switched driver to TAR (read-only)."; } catch { _status = "Switch to TAR failed."; } _clickLock = true; return; }
                // Format exFAT
                if (Hit(mx, my, _bxFormatExfatX, _bxFormatExfatY, BtnW, BtnH)) {
                    try {
                        var fs = new FATFS();
                        fs.Format();
                        File.Instance = fs;
                        Desktop.InvalidateDirCache();
                        _status = "Formatted RAM disk as exFAT and switched driver.";
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
            Framebuffer.Graphics.FillRectangle(X + 2, Y + 2, Width - 4, Height - 4, 0xFF2B2B2B);

            // Status
            WindowManager.font.DrawString(X + Pad, Y + Pad, _status, Width - Pad * 2, WindowManager.font.FontSize * 4);

            int btnY = Y + Pad + 80;
            _bxDetectX = X + Pad; _bxDetectY = btnY;
            DrawButton(_bxDetectX, _bxDetectY, BtnW, BtnH, "Detect now"); btnY += BtnH + Gap;

            _bxSwitchFatX = X + Pad; _bxSwitchFatY = btnY;
            DrawButton(_bxSwitchFatX, _bxSwitchFatY, BtnW, BtnH, "Use FAT driver (RW)"); btnY += BtnH + Gap;

            _bxSwitchTarX = X + Pad; _bxSwitchTarY = btnY;
            DrawButton(_bxSwitchTarX, _bxSwitchTarY, BtnW, BtnH, "Use TAR driver (RO)"); btnY += BtnH + Gap;

            _bxFormatExfatX = X + Pad; _bxFormatExfatY = btnY;
            DrawButton(_bxFormatExfatX, _bxFormatExfatY, BtnW, BtnH, "Format RAM as exFAT");
        }

        private void DrawButton(int x, int y, int w, int h, string text) {
            int mx = Control.MousePosition.X; int my = Control.MousePosition.Y;
            bool hover = Hit(mx, my, x, y, w, h);
            uint bg = hover ? 0xFF3A3A3A : 0xFF323232;
            Framebuffer.Graphics.FillRectangle(x, y, w, h, bg);
            WindowManager.font.DrawString(x + 10, y + (h / 2 - WindowManager.font.FontSize / 2), text);
        }
    }
}
