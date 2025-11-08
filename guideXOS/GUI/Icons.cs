using guideXOS.FS;
using guideXOS.Misc;
using System.Drawing;
namespace guideXOS.GUI {
    /// <summary>
    /// Icons Privateq
    /// </summary>
    public class IconsPrivate {
        /// <summary>
        /// Document Icon
        /// </summary>
        public Image DocumentIcon;
        /// <summary>
        /// Image Icon
        /// </summary>
        public Image ImageIcon;
        /// <summary>
        /// Audio Icon
        /// </summary>
        public Image AudioIcon;
        /// <summary>
        /// Folder Icon
        /// </summary>
        public Image FolderIcon;
        /// <summary>
        /// Taskbar Icon
        /// </summary>
        public Image TaskbarIcon;
        /// <summary>
        /// Start Icon
        /// </summary>
        public Image StartIcon;
        /// <summary>
        /// Icons Private
        /// </summary>
        /// <param name="size"></param>
        public IconsPrivate(int size) {
            DocumentIcon = new PNG(File.ReadAllBytes($"Images/BlueVelvet/{size}/documents.png"));
            AudioIcon = new PNG(File.ReadAllBytes($"Images/BlueVelvet/{size}/music.png"));
            ImageIcon = new PNG(File.ReadAllBytes($"Images/BlueVelvet/{size}/image.png"));
            FolderIcon = new PNG(File.ReadAllBytes($"Images/BlueVelvet/{size}/folder.png"));
            TaskbarIcon = new PNG(File.ReadAllBytes($"Images/BlueVelvet/{size}/up.png"));
            StartIcon = new PNG(File.ReadAllBytes($"Images/BlueVelvet/{size}/play.png"));
        }
    }
    /// <summary>
    /// Icons
    /// </summary>
    public static class Icons {
        /// <summary>
        /// Icons Private
        /// </summary>
        private static IconsPrivate _iconsPrivate16 = new IconsPrivate(16);
        /// <summary>
        /// Icons Private
        /// </summary>
        private static IconsPrivate _iconsPrivate24 = new IconsPrivate(24);
        /// <summary>
        /// Icons Private
        /// </summary>
        private static IconsPrivate _iconsPrivate32 = new IconsPrivate(32);
        /// <summary>
        /// Icons Private 48
        /// </summary>
        private static IconsPrivate _iconsPrivate48 = new IconsPrivate(48);
        /// <summary>
        /// Icons Private 128
        /// </summary>
        private static IconsPrivate _iconsPrivate128 = new IconsPrivate(128);
        /// <summary>
        /// Document Icon
        /// </summary>
        public static Image DocumentIcon(int size) {
            switch(size) {
                case 16:
                    return _iconsPrivate16.DocumentIcon;
                case 24:
                    return _iconsPrivate24.DocumentIcon;
                case 32:
                    return _iconsPrivate32.DocumentIcon;
                case 48:
                    return _iconsPrivate48.DocumentIcon;
                case 128:
                    return _iconsPrivate128.DocumentIcon;
                default:
                    return _iconsPrivate32.DocumentIcon;
            }
        }
        /// <summary>
        /// Image Icon
        /// </summary>
        public static Image ImageIcon(int size) {
            switch (size) {
                case 16:
                    return _iconsPrivate16.ImageIcon;
                case 24:
                    return _iconsPrivate24.ImageIcon;
                case 32:
                    return _iconsPrivate32.ImageIcon;
                case 48:
                    return _iconsPrivate48.ImageIcon;
                case 128:
                    return _iconsPrivate128.ImageIcon;
                default:
                    return _iconsPrivate32.ImageIcon;
            }
        }
        /// <summary>
        /// Audio Icon
        /// </summary>
        public static Image AudioIcon(int size) {
            switch (size) {
                case 16:
                    return _iconsPrivate16.AudioIcon;
                case 24:
                    return _iconsPrivate24.AudioIcon;
                case 32:
                    return _iconsPrivate32.AudioIcon;
                case 48:
                    return _iconsPrivate48.AudioIcon;
                case 128:
                    return _iconsPrivate128.AudioIcon;
                default:
                    return _iconsPrivate32.AudioIcon;
            }
        }
        /// <summary>
        /// Folder Icon
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        public static Image FolderIcon(int size) {
            switch (size) {
                case 16:
                    return _iconsPrivate16.FolderIcon;
                case 24:
                    return _iconsPrivate24.FolderIcon;
                case 32:
                    return _iconsPrivate32.FolderIcon;
                case 48:
                    return _iconsPrivate48.FolderIcon;
                case 128:
                    return _iconsPrivate128.FolderIcon;
                default:
                    return _iconsPrivate32.FolderIcon;
            }
        }
        /// <summary>
        /// Taskbar Icon
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        public static Image TaskbarIcon(int size) {
            switch (size) {
                case 16:
                    return _iconsPrivate16.TaskbarIcon;
                case 24:
                    return _iconsPrivate24.TaskbarIcon;
                case 32:
                    return _iconsPrivate32.TaskbarIcon;
                case 48:
                    return _iconsPrivate48.TaskbarIcon;
                case 128:
                    return _iconsPrivate128.TaskbarIcon;
                default:
                    return _iconsPrivate32.TaskbarIcon;
            }
        }
        /// <summary>
        /// Start Icon
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        public static Image StartIcon(int size) {
            switch (size) {
                case 16:
                    return _iconsPrivate16.StartIcon;
                case 24:
                    return _iconsPrivate24.StartIcon;
                case 32:
                    return _iconsPrivate32.StartIcon;
                case 48:
                    return _iconsPrivate48.StartIcon;
                case 128:
                    return _iconsPrivate128.StartIcon;
                default:
                    return _iconsPrivate32.StartIcon;
            }
        }
    }
}