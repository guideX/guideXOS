using guideXOS.FS;
using guideXOS.Misc;
using System.Drawing;
namespace guideXOS.GUI {
    /// <summary>
    /// Icons
    /// </summary>
    public static class Icons {
        /// <summary>
        /// Document Icon
        /// </summary>
        private static Image _documentIcon;
        /// <summary>
        /// Image Icon
        /// </summary>
        private static Image _imageIcon;
        /// <summary>
        /// Audio Icon
        /// </summary>
        private static Image _audioIcon;
        /// <summary>
        /// Folder Icon
        /// </summary>
        private static Image _folderIcon;
        /// <summary>
        /// Taskbar Icon
        /// </summary>
        private static Image _taskbarIcon;
        /// <summary>
        /// Start Icon
        /// </summary>
        private static Image _startIcon;
        /// <summary>
        /// Document Icon
        /// </summary>
        public static Image DocumentIcon {
            get {
                if (_documentIcon == null) _documentIcon = new PNG(File.ReadAllBytes("Images/BlueVelvet/32/documents.png"));
                return _documentIcon;
            }
        }
        /// <summary>
        /// Image Icon
        /// </summary>
        public static Image ImageIcon {
            get {
                if (_imageIcon == null) _imageIcon = new PNG(File.ReadAllBytes("Images/BlueVelvet/32/Image.png"));
                return _imageIcon;
            }
        }
        /// <summary>
        /// Audio Icon
        /// </summary>
        public static Image AudioIcon {
            get {
                if (_audioIcon == null) _audioIcon = new PNG(File.ReadAllBytes("Images/BlueVelvet/32/Audio.png"));
                return _audioIcon;
            }
        }
        /// <summary>
        /// Folder Icon
        /// </summary>
        public static Image FolderIcon {
            get {
                if (_folderIcon == null) _folderIcon = new PNG(File.ReadAllBytes("Images/BlueVelvet/32/folder.png"));
                return _folderIcon;
            }
        }
        /// <summary>
        /// Folder Icon
        /// </summary>
        public static Image TaskbarIcon {
            get {
                if (_taskbarIcon == null) _taskbarIcon = new PNG(File.ReadAllBytes("Images/BlueVelvet/32/up.png"));
                return _taskbarIcon;
            }
        }
        /// <summary>
        /// Folder Icon
        /// </summary>
        public static Image StartIcon {
            get {
                if (_startIcon == null) _startIcon = new PNG(File.ReadAllBytes("Images/BlueVelvet/32/play.png"));
                return _startIcon;
            }
        }
    }
}