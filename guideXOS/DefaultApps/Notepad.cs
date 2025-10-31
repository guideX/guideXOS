using guideXOS.FS;
using guideXOS.GUI;
using guideXOS.Kernel.Drivers;
using guideXOS.Misc;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace guideXOS.DefaultApps {
    /// <summary>
    /// Simple Notepad app: type text and save to a file to test filesystem writes.
    /// </summary>
    internal class Notepad : Window {
        private string _text;
        private bool _clickLock;
        private int _padding = 8;
        private int _btnW = 64;
        private int _btnH = 24;
        private string _fileName = "notes.txt";

        public Notepad(int x, int y) : base(x, y, 560, 360) {
            Title = "Notepad";
            _text = string.Empty;
            _clickLock = false;
            // subscribe keyboard handler
            Keyboard.OnKeyChanged += Keyboard_OnKeyChanged;
        }

        private void Keyboard_OnKeyChanged(object sender, ConsoleKeyInfo key) {
            if (!Visible) return;
            if (key.KeyState != ConsoleKeyState.Pressed) return;
            // Basic editing
            if (key.Key == ConsoleKey.Backspace) {
                if (_text.Length > 0) _text = _text.Substring(0, _text.Length - 1);
                return;
            }
            if (key.Key == ConsoleKey.Enter) {
                _text += "\n";
                return;
            }
            if (key.Key == ConsoleKey.Tab) { _text += "    "; return; }

            // Space: map when scan code 57 (standard space) is set
            if (Keyboard.KeyInfo.ScanCode == 57) { _text += " "; return; }

            // Letters A-Z
            if (key.Key >= ConsoleKey.A && key.Key <= ConsoleKey.Z) {
                char c = (char)('a' + (key.Key - ConsoleKey.A));
                _text += c;
                return;
            }
            // Digits 0-9
            if (key.Key >= ConsoleKey.D0 && key.Key <= ConsoleKey.D9) {
                char c = (char)('0' + (key.Key - ConsoleKey.D0));
                _text += c;
                return;
            }
            // Punctuation basics (best-effort)
            switch (key.Key) {
                case ConsoleKey.OemPeriod: _text += "."; break;
                case ConsoleKey.OemComma: _text += ","; break;
                case ConsoleKey.OemMinus: _text += "-"; break;
                case ConsoleKey.OemPlus: _text += "+"; break;
                case ConsoleKey.Oem1: _text += ";"; break;
                case ConsoleKey.Oem2: _text += "/"; break;
                case ConsoleKey.Oem3: _text += "`"; break;
                case ConsoleKey.Oem4: _text += "["; break;
                case ConsoleKey.Oem5: _text += "\\"; break;
                case ConsoleKey.Oem6: _text += "]"; break;
                case ConsoleKey.Oem7: _text += "'"; break;
            }
        }

        private string CurrentPath => Desktop.Dir + _fileName;

        public override void OnInput() {
            base.OnInput();
            bool left = Control.MouseButtons.HasFlag(MouseButtons.Left);
            int mx = Control.MousePosition.X;
            int my = Control.MousePosition.Y;
            int bx = X + _padding;
            int by = Y + _padding;
            if (left) {
                if (!_clickLock && mx >= bx && mx <= bx + _btnW && my >= by && my <= by + _btnH) {
                    SaveFile();
                    _clickLock = true; return;
                }
            } else {
                _clickLock = false;
            }
        }

        private void SaveFile() {
            // Save to Desktop.Dir + notes.txt
            string path = CurrentPath;
            // Convert to bytes (ASCII subset)
            byte[] data = new byte[_text.Length];
            for (int i = 0; i < _text.Length; i++) data[i] = (byte)_text[i];
            File.WriteAllBytes(path, data);
            data.Dispose();
            // Feedback
            Desktop.msgbox.X = X + 40;
            Desktop.msgbox.Y = Y + 80;
            Desktop.msgbox.SetText($"Saved: {path}");
            WindowManager.MoveToEnd(Desktop.msgbox);
            Desktop.msgbox.Visible = true;
            RecentManager.AddDocument(path, Icons.FileIcon);
            path.Dispose();
        }

        public override void OnDraw() {
            base.OnDraw();
            // Content area
            int cx = X + _padding;
            int cy = Y + _padding;
            int cw = Width - _padding * 2;
            int ch = Height - _padding * 2;
            // Draw Save button
            Framebuffer.Graphics.FillRectangle(cx, cy, _btnW, _btnH, 0xFF3A3A3A);
            WindowManager.font.DrawString(cx + 10, cy + (_btnH / 2 - WindowManager.font.FontSize / 2), "Save");
            // Text area background
            int tx = cx;
            int ty = cy + _btnH + 8;
            int tw = cw;
            int th = ch - (_btnH + 8);
            Framebuffer.Graphics.AFillRectangle(tx, ty, tw, th, 0x80282828);
            // Draw text (wrapped)
            WindowManager.font.DrawString(tx + 6, ty + 6, _text, tw - 12, WindowManager.font.FontSize * 3);
        }
    }
}