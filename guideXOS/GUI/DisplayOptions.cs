using guideXOS.Kernel.Drivers;
using System;
using System.Windows.Forms;

namespace guideXOS.GUI {
    internal class DisplayOptions : Window {
        private int _itemHeight = 28;
        private int _padding = 10;
        private int _selectedIndex = -1; // current selection in list
        private int _hoverIndex = -1;
        private bool _confirmVisible = false;
        private int _countdown = 15;
        private Resolution _previous;
        private Resolution _pending;
        private ulong _lastCountdownTick;
        private string[] _labels; // precomputed labels to avoid per-frame allocations

        public DisplayOptions(int X, int Y, int W = 360, int H = 320) : base(X, Y, W, H) {
            Title = "Display Options";
            var list = DisplayManager.AvailableResolutions;
            if (list == null || list.Length == 0) {
                _labels = new string[0];
                _selectedIndex = -1;
                return;
            }
            // Select current
            var cur = DisplayManager.Current;
            for (int i = 0; i < list.Length; i++) {
                if (list[i].Width == cur.Width && list[i].Height == cur.Height) { _selectedIndex = i; break; }
            }
            if (_selectedIndex < 0 && list.Length > 0) _selectedIndex = 0;

            // Precompute display labels once
            _labels = new string[list.Length];
            for (int i = 0; i < list.Length; i++) {
                _labels[i] = list[i].Width.ToString() + " x " + list[i].Height.ToString();
            }
        }

        public override void OnInput() {
            base.OnInput();
            if (!Visible) return;

            var list = DisplayManager.AvailableResolutions;
            if (list == null || list.Length == 0) return;

            int listX = X + _padding;
            int listY = Y + _padding + WindowManager.font.FontSize + 6;
            int listW = Width - _padding * 2;
            int count = list.Length;

            int mx = Control.MousePosition.X;
            int my = Control.MousePosition.Y;
            _hoverIndex = -1;

            // countdown tick (~60Hz main loop) when visible
            if (_confirmVisible) {
                if (_lastCountdownTick == 0) _lastCountdownTick = RTC.Second;
                if (_lastCountdownTick != RTC.Second) {
                    _lastCountdownTick = RTC.Second;
                    if (_countdown > 0) _countdown--;
                    if (_countdown == 0) {
                        RevertResolution();
                    }
                }
            }

            if (Control.MouseButtons == MouseButtons.Left) {
                // If confirm area visible, handle Yes/No first
                if (_confirmVisible) {
                    int btnW = 80, btnH = 26, gap = 10;
                    int btnY = Y + Height - _padding - btnH;
                    int noX = X + Width - _padding - btnW - gap - btnW;
                    int yesX = X + Width - _padding - btnW;

                    if (mx >= yesX && mx <= yesX + btnW && my >= btnY && my <= btnY + btnH) {
                        KeepResolution();
                        return;
                    }
                    if (mx >= noX && mx <= noX + btnW && my >= btnY && my <= btnY + btnH) {
                        RevertResolution();
                        return;
                    }
                }

                // list click
                if (mx >= listX && mx <= listX + listW && my >= listY && my <= listY + (count * _itemHeight)) {
                    int index = (my - listY) / _itemHeight;
                    if (index >= 0 && index < count && index != _selectedIndex) {
                        // Start apply
                        _previous = DisplayManager.Current;
                        var chosen = list[index];
                        if (DisplayManager.TrySetResolution(chosen.Width, chosen.Height)) {
                            _pending = chosen;
                            _selectedIndex = index;
                            _confirmVisible = true;
                            _countdown = 15;
                            _lastCountdownTick = 0;
                        } else {
                            NotificationManager.Add(new Nofity("Resolution change not supported on this hardware", NotificationLevel.Error));
                        }
                    }
                }
            }
        }

        private void KeepResolution() {
            _confirmVisible = false;
            DisplayManager.SaveResolution(_pending);
        }

        private void RevertResolution() {
            _confirmVisible = false;
            if (!DisplayManager.TrySetResolution(_previous.Width, _previous.Height)) {
                NotificationManager.Add(new Nofity("Failed to revert resolution", NotificationLevel.Error));
            } else {
                var list = DisplayManager.AvailableResolutions;
                if (list != null) {
                    for (int i = 0; i < list.Length; i++) if (list[i].Width == _previous.Width && list[i].Height == _previous.Height) { _selectedIndex = i; break; }
                }
            }
        }

        public override void OnDraw() {
            base.OnDraw();

            if (WindowManager.font == null) return;

            int cx = X + _padding;
            int cy = Y + _padding;
            int cw = Width - _padding * 2;

            WindowManager.font.DrawString(cx, cy, "Available resolutions:");

            int listX = cx;
            int listY = cy + WindowManager.font.FontSize + 6;

            var list = DisplayManager.AvailableResolutions;
            if (list == null) return;

            int count = list.Length;
            for (int i = 0; i < count; i++) {
                int rowY = listY + i * _itemHeight;
                bool selected = (i == _selectedIndex);
                uint rowBg = selected ? 0xFF2A2A2A : 0xFF222222;
                Framebuffer.Graphics.FillRectangle(listX, rowY, cw, _itemHeight - 2, rowBg);

                string label;
                if (_labels != null && i < _labels.Length && _labels[i] != null) {
                    label = _labels[i];
                } else {
                    label = list[i].Width.ToString() + " x " + list[i].Height.ToString();
                }
                WindowManager.font.DrawString(listX + 8, rowY + (_itemHeight / 2) - (WindowManager.font.FontSize / 2), label);
            }

            // confirm area
            if (_confirmVisible) {
                int btnW = 80, btnH = 26, gap = 10;
                int btnY = Y + Height - _padding - btnH;
                int yesX = X + Width - _padding - btnW;
                int noX = yesX - gap - btnW;

                string msg = "Your previous resolution will be re-applied in " + _countdown.ToString() + " seconds";
                WindowManager.font.DrawString(cx, btnY - WindowManager.font.FontSize - 6, msg);

                Framebuffer.Graphics.FillRectangle(noX, btnY, btnW, btnH, 0xFF2A2A2A);
                Framebuffer.Graphics.DrawRectangle(noX, btnY, btnW, btnH, 0xFF3F3F3F, 1);
                WindowManager.font.DrawString(noX + 26, btnY + (btnH / 2) - (WindowManager.font.FontSize / 2), "No");

                Framebuffer.Graphics.FillRectangle(yesX, btnY, btnW, btnH, 0xFF2A2A2A);
                Framebuffer.Graphics.DrawRectangle(yesX, btnY, btnW, btnH, 0xFF3F3F3F, 1);
                WindowManager.font.DrawString(yesX + 22, btnY + (btnH / 2) - (WindowManager.font.FontSize / 2), "Yes");
            }
        }
    }
}
