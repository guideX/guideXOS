using guideXOS.Kernel.Drivers;
using System.Drawing;
using System.Windows.Forms;
namespace guideXOS.GUI {
    /// <summary>
    /// TaskBar
    /// </summary>
    internal class Taskbar {
        /// <summary>
        /// Start Menu
        /// </summary>
        public StartMenu StartMenu;
        /// <summary>
        /// Bar Height
        /// </summary>
        private int _barHeight;
        /// <summary>
        /// Start Icon
        /// </summary>
        private Image _startIcon;
        /// <summary>
        /// Clock toggle state (12h/24h) and click debounce
        /// </summary>
        private bool _clockUse12Hour = false;
        private bool _clockClickLatch = false;
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="barHeight"></param>
        public Taskbar(int barHeight, Image startIcon) {
            _barHeight = barHeight;
            _startIcon = startIcon;
        }
        /// <summary>
        /// Draw Task Bar
        /// </summary>
        public void Draw() {
            // Semi-transparent taskbar
            Framebuffer.Graphics.AFillRectangle(0, Framebuffer.Height - _barHeight, Framebuffer.Width, _barHeight, 0xCC222222);
            Framebuffer.Graphics.DrawImage(12, Framebuffer.Height - _barHeight + 4, _startIcon);

            int textW, textX, textY;
            int hitX0, hitY0, hitX1, hitY1;

            if (_clockUse12Hour) {
                // 12-hour format: h:MM AM/PM (no leading zero on hour, no seconds)
                string colon = ":";
                string space = " ";
                bool isPM = RTC.Hour >= 12;
                string suffix = isPM ? "PM" : "AM";

                int hour12 = (RTC.Hour % 12 == 0) ? 12 : (RTC.Hour % 12);
                string shour = hour12.ToString(); // no padding

                string sminute = RTC.Minute.ToString();
                if (RTC.Minute < 10) { string tmp = "0" + sminute; sminute.Dispose(); sminute = tmp; }

                // Build h:MM AM/PM
                string time = shour + colon;            // h:
                string t2 = time + sminute; time.Dispose(); time = t2; // h:MM
                string t3 = time + space;  time.Dispose(); time = t3;   // h:MM 
                string t4 = time + suffix; time.Dispose(); time = t4;   // h:MM AM/PM

                textW = WindowManager.font.MeasureString(time);
                textX = Framebuffer.Width - 12 - textW;
                textY = Framebuffer.Height - _barHeight + ((_barHeight - WindowManager.font.FontSize) / 2);
                WindowManager.font.DrawString(textX, textY, time);

                // Hit test area for clock
                hitX0 = textX;
                hitY0 = Framebuffer.Height - _barHeight;
                hitX1 = textX + textW;
                hitY1 = Framebuffer.Height;

                // Dispose temps
                colon.Dispose();
                space.Dispose();
                shour.Dispose();
                sminute.Dispose();
                suffix.Dispose();
                time.Dispose();
            } else {
                // 24-hour format: HH:MM:SS
                string colon = ":";

                string shour = RTC.Hour.ToString();
                if (RTC.Hour < 10) { string tmp = "0" + shour; shour.Dispose(); shour = tmp; }

                string sminute = RTC.Minute.ToString();
                if (RTC.Minute < 10) { string tmp = "0" + sminute; sminute.Dispose(); sminute = tmp; }

                string ssecond = RTC.Second.ToString();
                if (RTC.Second < 10) { string tmp = "0" + ssecond; ssecond.Dispose(); ssecond = tmp; }

                // Build HH:MM:SS step-by-step to control temporaries
                string time = shour + colon; // HH:
                string tmp2 = time + sminute; time.Dispose(); time = tmp2; // HH:MM
                string tmp3 = time + colon; time.Dispose(); time = tmp3;   // HH:MM:
                string tmp4 = time + ssecond; time.Dispose(); time = tmp4; // HH:MM:SS

                textW = WindowManager.font.MeasureString(time);
                textX = Framebuffer.Width - 12 - textW;
                textY = Framebuffer.Height - _barHeight + ((_barHeight - WindowManager.font.FontSize) / 2);
                WindowManager.font.DrawString(textX, textY, time);

                // Hit test area for clock
                hitX0 = textX;
                hitY0 = Framebuffer.Height - _barHeight;
                hitX1 = textX + textW;
                hitY1 = Framebuffer.Height;

                // Dispose temps
                colon.Dispose();
                shour.Dispose();
                sminute.Dispose();
                ssecond.Dispose();
                time.Dispose();
            }

            // Input handling
            if (Control.MouseButtons.HasFlag(MouseButtons.Left)) {
                int mx = Control.MousePosition.X;
                int my = Control.MousePosition.Y;

                // Toggle 12h/24h when clicking clock (debounced)
                if (mx >= hitX0 && mx <= hitX1 && my >= hitY0 && my <= hitY1) {
                    if (!_clockClickLatch) {
                        _clockUse12Hour = !_clockUse12Hour;
                        _clockClickLatch = true;
                    }
                }

                if (Control.MousePosition.X > 15 && Control.MousePosition.X < 35 && Control.MousePosition.Y > 700 && Control.MousePosition.Y < 800) {
                    if (StartMenu == null) {
                        StartMenu = new StartMenu();
                    } else {
                        if (StartMenu != null && StartMenu.Visible) {
                            StartMenu.Visible = false;
                        } else {
                            StartMenu.Visible = true;
                        }
                    }
                } else {
                    if (StartMenu != null) {
                        if (!StartMenu.IsUnderMouse()) {
                            StartMenu.Visible = false;
                            StartMenu = null;
                        }
                    }
                }
            } else {
                // Release latch when mouse button is released
                _clockClickLatch = false;
            }
        }
    }
}