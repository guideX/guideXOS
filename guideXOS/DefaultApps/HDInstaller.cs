using guideXOS.FS;
using guideXOS.GUI;
using guideXOS.Kernel.Drivers;
using guideXOS.Misc;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace guideXOS.DefaultApps {
    /// <summary>
    /// guideXOS Hard Drive Installer - Guides users through installing guideXOS to their hard drive
    /// when booting from a USB flash drive
    /// </summary>
    internal class HDInstaller : Window {
        private int _currentStep = 0;
        private bool _clickLock;
        
        // Installation steps
        private enum InstallStep {
            Welcome = 0,
            DiskSelection = 1,
            PartitionSetup = 2,
            FormatWarning = 3,
            Installing = 4,
            Complete = 5
        }
        
        // Disk information
        private class DiskInfo {
            public string Name;
            public bool IsUSB;
            public ulong TotalSectors;
            public uint BytesPerSector;
            public USBMSCBot.USBDisk UsbDisk;
        }
        
        private List<DiskInfo> _availableDisks = new List<DiskInfo>();
        private int _selectedDiskIndex = -1;
        private bool _installInProgress = false;
        private int _installProgress = 0;
        private string _statusMessage = "";
        
        // UI Layout constants
        private const int Pad = 20;
        private const int BtnW = 120;
        private const int BtnH = 32;
        
        // Button coordinates
        private int _btnNextX, _btnNextY;
        private int _btnBackX, _btnBackY;
        private int _btnCancelX, _btnCancelY;

        public HDInstaller(int x, int y) : base(x, y, 700, 500) {
            Title = "Install guideXOS to Hard Drive";
            IsResizable = false;
            ShowMaximize = false;
            ShowMinimize = true;
            ShowInTaskbar = true;
            ShowInStartMenu = false;
            
            ScanDisks();
        }

        private void ScanDisks() {
            _availableDisks.Clear();
            
            // System disk (IDE)
            if (Disk.Instance is IDEDevice ide) {
                var sysInfo = new DiskInfo {
                    Name = "System Disk 0 (IDE)",
                    IsUSB = false,
                    BytesPerSector = IDEDevice.SectorSize,
                    TotalSectors = ide.Size / IDEDevice.SectorSize
                };
                _availableDisks.Add(sysInfo);
            }
            
            // USB disks
            var devices = USBStorage.GetAll();
            if (devices != null) {
                int usbIndex = 1;
                for (int i = 0; i < devices.Length; i++) {
                    var d = devices[i];
                    if (d == null) continue;
                    if (!(d.Class == 0x08 && d.SubClass == 0x06 && d.Protocol == 0x50)) continue;
                    
                    var usbDisk = USBMSC.TryOpenDisk(d);
                    if (usbDisk == null || !usbDisk.IsReady) continue;
                    
                    var info = new DiskInfo {
                        Name = $"USB Disk {usbIndex} (Removable)",
                        IsUSB = true,
                        BytesPerSector = usbDisk.LogicalBlockSize,
                        TotalSectors = usbDisk.TotalBlocks,
                        UsbDisk = usbDisk
                    };
                    _availableDisks.Add(info);
                    usbIndex++;
                }
            }
        }

        public override void OnInput() {
            base.OnInput();
            if (!Visible) return;
            
            int mx = Control.MousePosition.X;
            int my = Control.MousePosition.Y;
            bool left = Control.MouseButtons.HasFlag(MouseButtons.Left);
            
            if (left && !_clickLock) {
                // Navigation buttons
                if (_currentStep > 0 && _currentStep < (int)InstallStep.Installing && 
                    Hit(mx, my, _btnBackX, _btnBackY, BtnW, BtnH)) {
                    _currentStep--;
                    _clickLock = true;
                    return;
                }
                
                if (_currentStep < (int)InstallStep.Installing &&
                    Hit(mx, my, _btnNextX, _btnNextY, BtnW, BtnH)) {
                    if (HandleNextButton()) {
                        _currentStep++;
                    }
                    _clickLock = true;
                    return;
                }
                
                if (Hit(mx, my, _btnCancelX, _btnCancelY, BtnW, BtnH)) {
                    if (_currentStep != (int)InstallStep.Installing) {
                        Visible = false;
                    }
                    _clickLock = true;
                    return;
                }
                
                // Disk selection in step 1
                if (_currentStep == (int)InstallStep.DiskSelection) {
                    int listY = Y + 120;
                    for (int i = 0; i < _availableDisks.Count; i++) {
                        int diskY = listY + i * 60;
                        if (Hit(mx, my, X + Pad, diskY, Width - Pad * 2, 50)) {
                            _selectedDiskIndex = i;
                            _clickLock = true;
                            return;
                        }
                    }
                }
            } else {
                _clickLock = false;
            }
        }

        private bool HandleNextButton() {
            switch ((InstallStep)_currentStep) {
                case InstallStep.Welcome:
                    return true;
                    
                case InstallStep.DiskSelection:
                    if (_selectedDiskIndex < 0) {
                        _statusMessage = "Please select a disk to install to.";
                        return false;
                    }
                    return true;
                    
                case InstallStep.PartitionSetup:
                    return true;
                    
                case InstallStep.FormatWarning:
                    // Start installation
                    _installInProgress = true;
                    _installProgress = 0;
                    StartInstallation();
                    return true;
                    
                case InstallStep.Complete:
                    Visible = false;
                    return false;
            }
            return true;
        }

        private void StartInstallation() {
            _statusMessage = "Installing guideXOS...";
            // Simulate installation progress
            // In a real implementation, this would copy files, install bootloader, etc.
        }

        public override void OnDraw() {
            base.OnDraw();
            
            // Background
            Framebuffer.Graphics.FillRectangle(X + 1, Y + 1, Width - 2, Height - 2, 0xFF2B2B2B);
            
            // Draw step indicator at top
            DrawStepIndicator();
            
            // Draw content based on current step
            switch ((InstallStep)_currentStep) {
                case InstallStep.Welcome:
                    DrawWelcomeStep();
                    break;
                case InstallStep.DiskSelection:
                    DrawDiskSelectionStep();
                    break;
                case InstallStep.PartitionSetup:
                    DrawPartitionSetupStep();
                    break;
                case InstallStep.FormatWarning:
                    DrawFormatWarningStep();
                    break;
                case InstallStep.Installing:
                    DrawInstallingStep();
                    break;
                case InstallStep.Complete:
                    DrawCompleteStep();
                    break;
            }
            
            // Draw navigation buttons at bottom
            DrawNavigationButtons();
            
            // Draw status message if any
            if (!string.IsNullOrEmpty(_statusMessage)) {
                WindowManager.font.DrawString(X + Pad, Y + Height - 80, _statusMessage, Width - Pad * 2, WindowManager.font.FontSize);
            }
        }

        private void DrawStepIndicator() {
            int indicatorY = Y + 40;
            int stepCount = 5;
            int stepWidth = (Width - Pad * 2) / stepCount;
            
            for (int i = 0; i < stepCount; i++) {
                int stepX = X + Pad + i * stepWidth;
                uint color = i <= _currentStep ? 0xFF4C8BF5 : 0xFF555555;
                
                // Draw circle
                Framebuffer.Graphics.FillRectangle(stepX + stepWidth / 2 - 15, indicatorY - 15, 30, 30, color);
                
                // Draw line to next step
                if (i < stepCount - 1) {
                    uint lineColor = i < _currentStep ? 0xFF4C8BF5 : 0xFF555555;
                    Framebuffer.Graphics.FillRectangle(stepX + stepWidth / 2 + 15, indicatorY - 2, stepWidth - 30, 4, lineColor);
                }
                
                // Draw step number
                string stepNum = (i + 1).ToString();
                WindowManager.font.DrawString(stepX + stepWidth / 2 - 8, indicatorY - 8, stepNum);
            }
        }

        private void DrawWelcomeStep() {
            int contentY = Y + 100;
            
            WindowManager.font.DrawString(X + Pad, contentY, "Welcome to guideXOS Installer");
            contentY += 40;
            
            string[] lines = new string[] {
                "This wizard will guide you through installing guideXOS",
                "to your computer's hard drive.",
                "",
                "You are currently running guideXOS from a USB flash drive.",
                "Installing to your hard drive will provide:",
                "",
                "  - Faster boot times",
                "  - Persistent storage for files and settings",
                "  - Better performance",
                "  - No need for the USB drive after installation",
                "",
                "WARNING: This will erase all data on the selected disk!",
                "Make sure you have backed up any important data.",
                "",
                "Click Next to continue."
            };
            
            for (int i = 0; i < lines.Length; i++) {
                WindowManager.font.DrawString(X + Pad, contentY, lines[i], Width - Pad * 2, WindowManager.font.FontSize);
                contentY += WindowManager.font.FontSize + 6;
            }
        }

        private void DrawDiskSelectionStep() {
            int contentY = Y + 100;
            
            WindowManager.font.DrawString(X + Pad, contentY, "Select Installation Disk");
            contentY += 30;
            
            WindowManager.font.DrawString(X + Pad, contentY, "Choose the disk where guideXOS will be installed:", Width - Pad * 2, WindowManager.font.FontSize);
            contentY += 30;
            
            if (_availableDisks.Count == 0) {
                WindowManager.font.DrawString(X + Pad, contentY, "No suitable disks found.", Width - Pad * 2, WindowManager.font.FontSize);
                return;
            }
            
            // Draw disk list
            for (int i = 0; i < _availableDisks.Count; i++) {
                var disk = _availableDisks[i];
                bool selected = i == _selectedDiskIndex;
                
                uint bgColor = selected ? 0xFF3A3A3A : 0xFF2A2A2A;
                uint borderColor = selected ? 0xFF4C8BF5 : 0xFF555555;
                
                Framebuffer.Graphics.FillRectangle(X + Pad, contentY, Width - Pad * 2, 50, bgColor);
                Framebuffer.Graphics.DrawRectangle(X + Pad, contentY, Width - Pad * 2, 50, borderColor, 2);
                
                string sizeStr = FormatSize(disk.TotalSectors * (disk.BytesPerSector == 0 ? 512UL : disk.BytesPerSector));
                WindowManager.font.DrawString(X + Pad + 10, contentY + 8, disk.Name, Width - Pad * 2 - 20, WindowManager.font.FontSize);
                WindowManager.font.DrawString(X + Pad + 10, contentY + 28, $"Size: {sizeStr}", Width - Pad * 2 - 20, WindowManager.font.FontSize);
                
                contentY += 60;
            }
        }

        private void DrawPartitionSetupStep() {
            int contentY = Y + 100;
            
            WindowManager.font.DrawString(X + Pad, contentY, "Partition Configuration");
            contentY += 40;
            
            if (_selectedDiskIndex >= 0 && _selectedDiskIndex < _availableDisks.Count) {
                var disk = _availableDisks[_selectedDiskIndex];
                
                string[] lines = new string[] {
                    "The installer will create the following partitions:",
                    "",
                    $"Disk: {disk.Name}",
                    $"Total Size: {FormatSize(disk.TotalSectors * (disk.BytesPerSector == 0 ? 512UL : disk.BytesPerSector))}",
                    "",
                    "Partition Layout:",
                    "  - Boot Partition (FAT32) - 100 MB",
                    "  - System Partition (EXT2) - Remaining space",
                    "",
                    "The boot partition will contain:",
                    "  - Bootloader (GRUB)",
                    "  - Kernel image",
                    "",
                    "The system partition will contain:",
                    "  - System files",
                    "  - Applications",
                    "  - User data",
                    "",
                    "Click Next to continue."
                };
                
                for (int i = 0; i < lines.Length; i++) {
                    WindowManager.font.DrawString(X + Pad, contentY, lines[i], Width - Pad * 2, WindowManager.font.FontSize);
                    contentY += WindowManager.font.FontSize + 6;
                }
            }
        }

        private void DrawFormatWarningStep() {
            int contentY = Y + 100;
            
            WindowManager.font.DrawString(X + Pad, contentY, "WARNING: Data Will Be Erased");
            contentY += 50;
            
            if (_selectedDiskIndex >= 0 && _selectedDiskIndex < _availableDisks.Count) {
                var disk = _availableDisks[_selectedDiskIndex];
                
                string[] lines = new string[] {
                    "The following disk will be formatted:",
                    "",
                    $"  {disk.Name}",
                    $"  Size: {FormatSize(disk.TotalSectors * (disk.BytesPerSector == 0 ? 512UL : disk.BytesPerSector))}",
                    "",
                    "ALL DATA ON THIS DISK WILL BE PERMANENTLY DELETED!",
                    "",
                    "This includes:",
                    "  - All files and folders",
                    "  - Any existing operating systems",
                    "  - All personal data",
                    "  - All applications and programs",
                    "",
                    "This action CANNOT be undone!",
                    "",
                    "Make sure you have:",
                    "  * Backed up all important data",
                    "  * Selected the correct disk",
                    "  * Saved any work in progress",
                    "",
                    "Click Next to begin installation, or Back to change settings."
                };
                
                for (int i = 0; i < lines.Length; i++) {
                    WindowManager.font.DrawString(X + Pad, contentY, lines[i], Width - Pad * 2, WindowManager.font.FontSize);
                    contentY += WindowManager.font.FontSize + 6;
                }
            }
        }

        private void DrawInstallingStep() {
            int contentY = Y + 100;
            
            WindowManager.font.DrawString(X + Pad, contentY, "Installing guideXOS");
            contentY += 50;
            
            // Simulate installation progress
            if (_installInProgress) {
                _installProgress += 2;
                if (_installProgress >= 100) {
                    _installProgress = 100;
                    _installInProgress = false;
                    _currentStep = (int)InstallStep.Complete;
                }
            }
            
            // Progress bar
            int progressBarW = Width - Pad * 4;
            int progressBarH = 30;
            Framebuffer.Graphics.FillRectangle(X + Pad * 2, contentY, progressBarW, progressBarH, 0xFF1E1E1E);
            
            int fillWidth = (progressBarW * _installProgress) / 100;
            Framebuffer.Graphics.FillRectangle(X + Pad * 2, contentY, fillWidth, progressBarH, 0xFF4C8BF5);
            
            string progressText = $"{_installProgress}%";
            int textX = X + Width / 2 - 20;
            WindowManager.font.DrawString(textX, contentY + 8, progressText);
            
            contentY += progressBarH + 30;
            
            // Installation steps
            string[] steps = new string[] {
                "* Partitioning disk...",
                _installProgress > 20 ? "* Formatting partitions..." : "  Formatting partitions...",
                _installProgress > 40 ? "* Copying system files..." : "  Copying system files...",
                _installProgress > 60 ? "* Installing bootloader..." : "  Installing bootloader...",
                _installProgress > 80 ? "* Configuring system..." : "  Configuring system...",
                _installProgress >= 100 ? "* Installation complete!" : "  Finalizing installation..."
            };
            
            for (int i = 0; i < steps.Length; i++) {
                WindowManager.font.DrawString(X + Pad, contentY, steps[i], Width - Pad * 2, WindowManager.font.FontSize);
                contentY += WindowManager.font.FontSize + 10;
            }
            
            contentY += 20;
            WindowManager.font.DrawString(X + Pad, contentY, "Please wait, do not power off your computer...", Width - Pad * 2, WindowManager.font.FontSize);
        }

        private void DrawCompleteStep() {
            int contentY = Y + 120;
            
            WindowManager.font.DrawString(X + Pad, contentY, "Installation Complete!");
            contentY += 50;
            
            string[] lines = new string[] {
                "guideXOS has been successfully installed to your hard drive!",
                "",
                "To complete the setup:",
                "",
                "1. Remove the USB flash drive",
                "2. Restart your computer",
                "3. Your computer will boot from the hard drive",
                "",
                "You can now enjoy the full guideXOS experience with:",
                "  * Faster boot times",
                "  * Persistent file storage",
                "  * Better performance",
                "  * Full system access",
                "",
                "Thank you for choosing guideXOS!",
                "",
                "Click Finish to close this installer."
            };
            
            for (int i = 0; i < lines.Length; i++) {
                WindowManager.font.DrawString(X + Pad, contentY, lines[i], Width - Pad * 2, WindowManager.font.FontSize);
                contentY += WindowManager.font.FontSize + 6;
            }
        }

        private void DrawNavigationButtons() {
            int btnY = Y + Height - 60;
            
            // Next/Finish button
            _btnNextX = X + Width - Pad - BtnW;
            _btnNextY = btnY;
            string nextLabel = _currentStep == (int)InstallStep.Complete ? "Finish" : 
                              _currentStep == (int)InstallStep.FormatWarning ? "Install" : "Next";
            bool nextEnabled = _currentStep != (int)InstallStep.Installing;
            DrawButton(_btnNextX, _btnNextY, BtnW, BtnH, nextLabel, nextEnabled);
            
            // Back button
            _btnBackX = X + Width - Pad - BtnW * 2 - 10;
            _btnBackY = btnY;
            bool backEnabled = _currentStep > 0 && _currentStep < (int)InstallStep.Installing;
            if (backEnabled) {
                DrawButton(_btnBackX, _btnBackY, BtnW, BtnH, "Back", true);
            }
            
            // Cancel button
            _btnCancelX = X + Pad;
            _btnCancelY = btnY;
            bool cancelEnabled = _currentStep != (int)InstallStep.Installing;
            DrawButton(_btnCancelX, _btnCancelY, BtnW, BtnH, "Cancel", cancelEnabled);
        }

        private void DrawButton(int x, int y, int w, int h, string text, bool enabled = true) {
            if (!enabled) {
                Framebuffer.Graphics.FillRectangle(x, y, w, h, 0xFF1A1A1A);
                WindowManager.font.DrawString(x + w / 2 - text.Length * 4, y + h / 2 - WindowManager.font.FontSize / 2, text, w, WindowManager.font.FontSize);
                return;
            }
            
            int mx = Control.MousePosition.X;
            int my = Control.MousePosition.Y;
            bool hover = Hit(mx, my, x, y, w, h);
            
            uint bgColor = hover ? 0xFF4C8BF5 : 0xFF3A3A3A;
            Framebuffer.Graphics.FillRectangle(x, y, w, h, bgColor);
            WindowManager.font.DrawString(x + w / 2 - text.Length * 4, y + h / 2 - WindowManager.font.FontSize / 2, text);
        }

        private static bool Hit(int mx, int my, int x, int y, int w, int h) {
            return mx >= x && mx <= x + w && my >= y && my <= y + h;
        }

        private static string FormatSize(ulong bytes) {
            const ulong KB = 1024;
            const ulong MB = 1024 * 1024;
            const ulong GB = 1024 * 1024 * 1024;
            
            if (bytes >= GB) return ((bytes + GB / 10) / GB).ToString() + " GB";
            if (bytes >= MB) return ((bytes + MB / 10) / MB).ToString() + " MB";
            if (bytes >= KB) return ((bytes + KB / 10) / KB).ToString() + " KB";
            return bytes.ToString() + " B";
        }
    }
}
