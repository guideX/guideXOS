using guideXOS.FS;
using guideXOS.Misc;
using System.Drawing;
namespace guideXOS.GUI {
    /// <summary>
    /// Icons
    /// </summary>
    public static class Icons {
        /// <summary>
        /// File Icon
        /// </summary>
        public static Image FileIcon {
            get {
                return new PNG(File.ReadAllBytes("Images/file.png"));
            }
        }
        /// <summary>
        /// Iamge Icon
        /// </summary>
        public static Image IamgeIcon {
            get {
                return new PNG(File.ReadAllBytes("Images/Image.png"));
            }
        }
        /// <summary>
        /// Audio Icon
        /// </summary>
        public static Image AudioIcon {
            get {
                return new PNG(File.ReadAllBytes("Images/Audio.png"));
            }
        }
        /// <summary>
        /// Folder Icon
        /// </summary>
        public static Image FolderIcon {
            get {
                return new PNG(File.ReadAllBytes("Images/Folder.png"));
            }
        }
        /// <summary>
        /// Folder Icon
        /// </summary>
        public static Image TaskbarIcon {
            get {
                return new PNG(File.ReadAllBytes("Images/taskbar.png"));
            }
        }
        /// <summary>
        /// Folder Icon
        /// </summary>
        public static Image StartIcon {
            get {
                return new PNG(File.ReadAllBytes("Images/Start.png"));
            }
        }
    }
}