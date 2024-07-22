using guideXOS.FS;
using guideXOS.Kernel.Drivers;
using guideXOS.Misc;
using guideXOS.OS;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
namespace guideXOS.GUI {
    /// <summary>
    /// Desktop
    /// </summary>
    internal class Desktop {
        /// <summary>
        /// File Icon
        /// </summary>
        private static Image FileIcon;
        /// <summary>
        /// Iamge Icon
        /// </summary>
        private static Image IamgeIcon;
        /// <summary>
        /// Audio Icon
        /// </summary>
        private static Image AudioIcon;
        /// <summary>
        /// Folder Icon
        /// </summary>
        private static Image FolderIcon;
        /// <summary>
        /// Taskbar Icon
        /// </summary>
        private static Image TaskbarIcon;
        /// <summary>
        /// Start Icon
        /// </summary>
        private static Image StartIcon;
        /// <summary>
        /// Prefix
        /// </summary>
        //public static string Prefix;
        /// <summary>
        /// Dir
        /// </summary>
        public static string Dir;
        /// <summary>
        /// Taskbar
        /// </summary>
        public static Taskbar Taskbar;
        /// <summary>
        /// Image Viewer
        /// </summary>
        public static ImageViewer imageViewer;
        /// <summary>
        /// Message Box
        /// </summary>
        public static MessageBox msgbox;
        /// <summary>
        /// Wav Player
        /// </summary>
        public static WAVPlayer wavplayer;
        /// <summary>
        /// Apps
        /// </summary>
        public static AppCollection Apps;
        /// <summary>
        /// Is At Root
        /// </summary>
        public static bool IsAtRoot {
            get => Desktop.Dir.Length < 1;
        }
        /// <summary>
        /// Initialize
        /// </summary>
        public static void Initialize() {
            Apps = new AppCollection();
            IndexClicked = -1;
            try {
                TaskbarIcon = new PNG(File.ReadAllBytes("Images/taskbar.png"));
                FileIcon = new PNG(File.ReadAllBytes("Images/file.png"));
                IamgeIcon = new PNG(File.ReadAllBytes("Images/Image.png"));
                AudioIcon = new PNG(File.ReadAllBytes("Images/Audio.png"));
                FolderIcon = new PNG(File.ReadAllBytes("Images/Folder.png"));
                StartIcon = new PNG(File.ReadAllBytes("Images/Start.png"));
            } catch {
            }
            Taskbar = new Taskbar(40, TaskbarIcon);
            //Prefix = " root@guidexos: ";
            Dir = "";
            imageViewer = new ImageViewer(400, 400);
            msgbox = new MessageBox(100, 300);
            wavplayer = new WAVPlayer(450, 200);
            imageViewer.Visible = false;
            msgbox.Visible = false;
            wavplayer.Visible = false;
            LastPoint.X = -1;
            LastPoint.Y = -1;
        }
        /// <summary>
        /// Bar Height
        /// </summary>
        const int BarHeight = 40;
        /// <summary>
        /// Update
        /// </summary>
        public static void Update() {
            List<FileInfo> names = File.GetFiles(Dir);
            int Devide = 60;
            int X = Devide;
            int Y = Devide;
            if (IsAtRoot) {
                for (int i = 0; i < Apps.Length; i++) {
                    if (Y + FileIcon.Height + Devide > Framebuffer.Graphics.Height - Devide) {
                        Y = Devide;
                        X += FileIcon.Width + Devide;
                    }
                    ClickEvent(Apps.Name(i), false, X, Y, i);
                    Framebuffer.Graphics.DrawImage(X, Y, Apps.Icon(i));
                    WindowManager.font.DrawString(X, Y + FileIcon.Height, Apps.Name(i), FileIcon.Width + 8, WindowManager.font.FontSize * 3);
                    Y += FileIcon.Height + Devide;
                }
            }

            for (int i = 0; i < names.Count; i++) {
                if (Y + FileIcon.Height + Devide > Framebuffer.Graphics.Height - Devide) {
                    Y = Devide;
                    X += FileIcon.Width + Devide;
                }
                ClickEvent(names[i].Name, names[i].Attribute == FileAttribute.Directory, X, Y, i + (IsAtRoot ? Apps.Length : 0));
                if (names[i].Name.EndsWith(".png") || names[i].Name.EndsWith(".bmp")) {
                    Framebuffer.Graphics.DrawImage(X, Y, IamgeIcon);
                } else if (names[i].Name.EndsWith(".wav")) {
                    Framebuffer.Graphics.DrawImage(X, Y, AudioIcon);
                } else if (names[i].Attribute == FileAttribute.Directory) {
                    Framebuffer.Graphics.DrawImage(X, Y, FolderIcon);
                } else {
                    Framebuffer.Graphics.DrawImage(X, Y, FileIcon);
                }
                WindowManager.font.DrawString(X, Y + FileIcon.Height, names[i].Name, FileIcon.Width + 8, WindowManager.font.FontSize * 3);
                Y += FileIcon.Height + Devide;
                names[i].Dispose();
            }
            names.Dispose();
            if (Control.MouseButtons.HasFlag(MouseButtons.Left) && !WindowManager.HasWindowMoving && !WindowManager.MouseHandled) {
                if (LastPoint.X == -1 && LastPoint.Y == -1) {
                    LastPoint.X = Control.MousePosition.X;
                    LastPoint.Y = Control.MousePosition.Y;
                } else {
                    if (Control.MousePosition.X > LastPoint.X && Control.MousePosition.Y > LastPoint.Y) {
                        Framebuffer.Graphics.AFillRectangle(
                            LastPoint.X,
                            LastPoint.Y,
                            Control.MousePosition.X - LastPoint.X,
                            Control.MousePosition.Y - LastPoint.Y,
                            0x7F2E86C1);
                    }

                    if (Control.MousePosition.X < LastPoint.X && Control.MousePosition.Y < LastPoint.Y) {
                        Framebuffer.Graphics.AFillRectangle(
                            Control.MousePosition.X,
                            Control.MousePosition.Y,
                            LastPoint.X - Control.MousePosition.X,
                            LastPoint.Y - Control.MousePosition.Y,
                            0x7F2E86C1);
                    }

                    if (Control.MousePosition.X < LastPoint.X && Control.MousePosition.Y > LastPoint.Y) {
                        Framebuffer.Graphics.AFillRectangle(
                            Control.MousePosition.X,
                            LastPoint.Y,
                            LastPoint.X - Control.MousePosition.X,
                            Control.MousePosition.Y - LastPoint.Y,
                            0x7F2E86C1);
                    }

                    if (Control.MousePosition.X > LastPoint.X && Control.MousePosition.Y < LastPoint.Y) {
                        Framebuffer.Graphics.AFillRectangle(
                            LastPoint.X,
                            Control.MousePosition.Y,
                            Control.MousePosition.X - LastPoint.X,
                            LastPoint.Y - Control.MousePosition.Y,
                            0x7F2E86C1);
                    }
                }
            } else {
                LastPoint.X = -1;
                LastPoint.Y = -1;
            }

            Taskbar.Draw();
        }
        /// <summary>
        /// Last Point
        /// </summary>
        public static Point LastPoint;
        /// <summary>
        /// Click Event
        /// </summary>
        /// <param name="name"></param>
        /// <param name="isDirectory"></param>
        /// <param name="X"></param>
        /// <param name="Y"></param>
        /// <param name="i"></param>
        private static void ClickEvent(string name, bool isDirectory, int X, int Y, int i) {
            if (Control.MouseButtons == MouseButtons.Left) {
                bool clickable = true;
                for (int d = 0; d < WindowManager.Windows.Count; d++) {
                    if (WindowManager.Windows[d].Visible)
                        if (WindowManager.Windows[d].IsUnderMouse()) {
                            clickable = false;
                        }
                }

                if (!WindowManager.HasWindowMoving && clickable && !ClickLock && Control.MousePosition.X > X && Control.MousePosition.X < X + FileIcon.Width && Control.MousePosition.Y > Y && Control.MousePosition.Y < Y + FileIcon.Height) {
                    IndexClicked = i;
                    OnClick(name, isDirectory, X, Y);
                }
            } else {
                ClickLock = false;
            }

            if (IndexClicked == i) {
                int w = (int)(FileIcon.Width * 1.5f);
                Framebuffer.Graphics.AFillRectangle(X + ((FileIcon.Width / 2) - (w / 2)), Y, w, FileIcon.Height * 2, 0x7F2E86C1);
            }
        }

        static bool ClickLock = false;
        static int IndexClicked;
        /// <summary>
        /// On Click
        /// </summary>
        /// <param name="name"></param>
        /// <param name="isDirectory"></param>
        /// <param name="itemX"></param>
        /// <param name="itemY"></param>
        public static void OnClick(string name, bool isDirectory, int itemX, int itemY) {
            //if (!string.IsNullOrWhiteSpace(name)) { guideXOS.GUI.NotificationManager.Add(new Nofity("Clicked: " + name)); }
            ClickLock = true;
            string devider = "/";
            string path = Dir + name;
            if (isDirectory) {
                string newd = Dir + name + devider;
                Dir.Dispose();
                Dir = newd;
                //guideXOS.GUI.NotificationManager.Add(new Nofity("New Dir: " + Dir));
            } else if (name.EndsWith(".png")) {
                byte[] buffer = File.ReadAllBytes(path);
                PNG png = new PNG(buffer);
                buffer.Dispose();
                imageViewer.SetImage(png);
                png.Dispose();
                WindowManager.MoveToEnd(imageViewer);
                imageViewer.Visible = true;
            } else if (name.EndsWith(".bmp")) {
                byte[] buffer = File.ReadAllBytes(path);
                Bitmap png = new Bitmap(buffer);
                buffer.Dispose();
                imageViewer.SetImage(png);
                png.Dispose();
                WindowManager.MoveToEnd(imageViewer);
                imageViewer.Visible = true;
            } else if (name.EndsWith(".mue")) {
                byte[] buffer = File.ReadAllBytes(path);
                Process.Start(buffer);
            } else if (name.EndsWith(".wav")) {
                if (Audio.HasAudioDevice) {
                    wavplayer.Visible = true;
                    byte[] buffer = File.ReadAllBytes(path);
                    unsafe {
                        //name will be disposed after this loop so create a new one
                        fixed (char* ptr = name)
                            wavplayer.Play(buffer, new string(ptr));
                    }
                } else {
                    msgbox.X = itemX + 75;
                    msgbox.Y = itemY + 75;
                    msgbox.SetText("Audio controller is unavailable!");
                    WindowManager.MoveToEnd(msgbox);
                    msgbox.Visible = true;
                }
            } else if (!Apps.Load(name)) {
                msgbox.X = itemX + 75;
                msgbox.Y = itemY + 75;
                msgbox.SetText("No application can open this file!");
                WindowManager.MoveToEnd(msgbox);
                msgbox.Visible = true;
            }
            path.Dispose();
            devider.Dispose();
        }
    }
}