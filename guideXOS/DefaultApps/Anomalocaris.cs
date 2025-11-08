using guideXOS.GUI;
using guideXOS.Misc;
using guideXOS.Kernel.Drivers;
using System.Windows.Forms;

namespace guideXOS.DefaultApps {
    // Minimal browser stub window
    internal class Anomalocaris : Window {
        private string _url = "http://example";
        private bool _clickLock;
        public Anomalocaris(int x, int y) : base(x, y, 720, 480) {
            Title = "Anomalocaris";
        }
        public override void OnInput() {
            base.OnInput();
            // just a close button click-guard placeholder
            if (Control.MouseButtons.HasFlag(MouseButtons.Left)) {
                _clickLock = true;
            } else _clickLock = false;
        }
        public override void OnDraw() {
            base.OnDraw();
            // toolbar
            int pad = 10; int h = 28; int tx = X + pad; int ty = Y + pad; int w = Width - pad * 2;
            Framebuffer.Graphics.FillRectangle(tx, ty, w, h, 0xFF2E2E2E);
            WindowManager.font.DrawString(tx + 8, ty + (h/2 - WindowManager.font.FontSize/2), _url, w - 16, WindowManager.font.FontSize);
            // page area
            int py = ty + h + 8; int ph = Height - (py - Y) - pad;
            Framebuffer.Graphics.AFillRectangle(tx, py, w, ph, 0x80282828);
            WindowManager.font.DrawString(tx + 8, py + 8, "Web rendering not implemented.");
        }
    }
}
