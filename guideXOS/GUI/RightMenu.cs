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
        public RightMenu() : base(Control.MousePosition.X, Control.MousePosition.Y, 220, 180) {
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
            int mx = Control.MousePosition.X;
            int my = Control.MousePosition.Y;

            bool leftClick = Control.MouseButtons.HasFlag(MouseButtons.Left);

            if (leftClick) {
                // Item 0: Display Options
                if (Hit(0, mx, my, itemH)) {
                    WindowManager.EnqueueDisplayOptions(Control.MousePosition.X, Control.MousePosition.Y, 800, 600);
                    this.Visible = false;
                    return;
                }
                // Item 1: Performance Widget toggle
                if (Hit(1, mx, my, itemH)) {
                    if (Program.perfWidget != null) {
                        Program.perfWidget.Visible = !Program.perfWidget.Visible;
                        if (Program.perfWidget.Visible) {
                            WindowManager.MoveToEnd(Program.perfWidget);
                        }
                    }
                    this.Visible = false;
                    return;
                }
                // Item 2: Up One Level (only when not root)
                if (Hit(2, mx, my, itemH) && Desktop.Dir.Length > 0) {
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
                // Item 3..7: Icon Size options
                int[] sizes = new[] { 16, 24, 32, 48, 128 };
                for (int i = 0; i < sizes.Length; i++) {
                    if (Hit(3 + i, mx, my, itemH)) {
                        Desktop.SetIconSize(sizes[i]);
                        this.Visible = false;
                        return;
                    }
                }

                // Click anywhere else -> close
                this.Visible = false;
            }
        }
        private bool Hit(int index, int mx, int my, int itemH) {
            int y = Y + index * itemH;
            return (mx >= X && mx <= X + Width && my >= y && my <= y + itemH);
        }
        /// <summary>
        /// On Draw
        /// </summary>
        public override void OnDraw() {
            int itemH = 28;
            int extra = 2 + (Desktop.Dir.Length > 0 ? 1 : 0); // +1 for Display Options, +1 for Performance Widget
            int iconItems = 5;
            Height = itemH * (extra + iconItems + 1);
            // Background
            Framebuffer.Graphics.AFillRectangle(X, Y, Width, Height, 0xCC222222);

            int y = Y;
            WindowManager.font.DrawString(X + 8, y + (itemH / 2) - (WindowManager.font.FontSize / 2), "Display Options"); y += itemH;
            
            // Performance Widget toggle
            string perfLabel = "Performance Widget";
            if (Program.perfWidget != null && Program.perfWidget.Visible) {
                perfLabel += " ?";
            }
            WindowManager.font.DrawString(X + 8, y + (itemH / 2) - (WindowManager.font.FontSize / 2), perfLabel); 
            perfLabel.Dispose();
            y += itemH;
            
            if (Desktop.Dir.Length > 0) { 
                WindowManager.font.DrawString(X + 8, y + (itemH / 2) - (WindowManager.font.FontSize / 2), "Up one level"); 
                y += itemH; 
            }
            WindowManager.font.DrawString(X + 8, y + (itemH / 2) - (WindowManager.font.FontSize / 2), "Icon Size:"); y += itemH;
            int[] sizes = new[] { 16, 24, 32, 48, 128 };
            for (int i = 0; i < sizes.Length; i++) {
                string label = sizes[i].ToString();
                if (sizes[i] == Desktop.IconSize) label += " (current)";
                WindowManager.font.DrawString(X + 20, y + (itemH / 2) - (WindowManager.font.FontSize / 2), label); y += itemH; label.Dispose();
            }
            DrawBorder(false);
        }
    }
}