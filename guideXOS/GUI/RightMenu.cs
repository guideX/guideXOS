using guideXOS.Kernel.Drivers;
using System.Windows.Forms;
namespace guideXOS.GUI {
    /// <summary>
    /// Right Menu
    /// </summary>
    internal class RightMenu : Window {
        /// <summary>
        /// Right Menu
        /// </summary>
        public RightMenu() : base(Control.MousePosition.X, Control.MousePosition.Y, 180, 80) {
            Visible = false;
        }
        /// <summary>
        /// On Set Visible
        /// </summary>
        /// <param name="value"></param>
        public override void OnSetVisible(bool value) {
            base.OnSetVisible(value);
            if (value) {
                X = Control.MousePosition.X - 8;
                Y = Control.MousePosition.Y - 8;
            }
        }
        /// <summary>
        /// On Input
        /// </summary>
        public override void OnInput() {
            if (!Visible) return;

            int itemH = 28;
            int pad = 6;
            int mx = Control.MousePosition.X;
            int my = Control.MousePosition.Y;

            if (Control.MouseButtons.HasFlag(MouseButtons.Left)) {
                // Item 0: Display Options
                int item0X = X;
                int item0Y = Y;
                int item0W = Width;
                int item0H = itemH;

                // Item 1: Up One Level (only when not root)
                int item1X = X;
                int item1Y = Y + itemH;
                int item1W = Width;
                int item1H = itemH;

                bool in0 = (mx >= item0X && mx <= item0X + item0W && my >= item0Y && my <= item0Y + item0H);
                bool in1 = (mx >= item1X && mx <= item1X + item1W && my >= item1Y && my <= item1Y + item1H);

                if (in0) {
                    WindowManager.EnqueueDisplayOptions(Control.MousePosition.X, Control.MousePosition.Y, 360, 320);
                    this.Visible = false;
                    return;
                }
                if (in1 && Desktop.Dir.Length > 0) {
                    Desktop.Dir.Length--;

                    if (Desktop.Dir.IndexOf('/') != -1) {
                        string ndir = $"{Desktop.Dir.Substring(0, Desktop.Dir.LastIndexOf('/'))}/";
                        Desktop.Dir.Dispose();
                        Desktop.Dir = ndir;
                    } else {
                        Desktop.Dir = "";
                    }
                    this.Visible = false;
                    return;
                }

                // Click anywhere else -> close
                this.Visible = false;
            }
        }
        /// <summary>
        /// On Draw
        /// </summary>
        public override void OnDraw() {
            int itemH = 28;
            int totalItems = 1 + (Desktop.Dir.Length > 0 ? 1 : 0);
            Height = itemH * totalItems;
            // Background
            Framebuffer.Graphics.AFillRectangle(X, Y, Width, Height, 0xCC222222);

            // Item 0: Display Options
            WindowManager.font.DrawString(X + 8, Y + (itemH / 2) - (WindowManager.font.FontSize / 2), "Display Options");

            // Item 1: Up One Level
            if (Desktop.Dir.Length > 0) {
                WindowManager.font.DrawString(X + 8, Y + itemH + (itemH / 2) - (WindowManager.font.FontSize / 2), "Up one level");
            }

            DrawBorder(false);
        }
    }
}