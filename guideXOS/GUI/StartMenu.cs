using guideXOS.Kernel.Drivers;
using guideXOS.OS;
namespace guideXOS.GUI {
    /// <summary>
    /// Start Menu
    /// </summary>
    internal class StartMenu : Window {
        /// <summary>
        /// Apps
        /// </summary>
        public static AppCollection Apps;
        /// <summary>
        /// Y2
        /// </summary>
        private int _y2;
        /// <summary>
        /// X2
        /// </summary>
        private int _x2;
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="X"></param>
        /// <param name="Y"></param>
        /// <param name="X2"></param>
        /// <param name="Y2"></param>
        public unsafe StartMenu(int X, int Y, int X2, int Y2) : base(X, Y, X2, Y2) {
            Title = "Start";
            BarHeight = 0;
            Apps = new AppCollection();
            _y2 = Y2;
            _x2 = X2;
        }
        /// <summary>
        /// Draw
        /// </summary>
        public void Draw() {
            Framebuffer.Graphics.DrawImage(_x2, _y2, Apps.Icon(0));
        }
    }
}