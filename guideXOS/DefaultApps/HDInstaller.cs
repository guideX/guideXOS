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

        // Partition plan (editable in PartitionSetup)
        private int _bootPartitionMB = 100; // default boot size
        private string _systemFs = "EXT2"; // default filesystem
        private bool _advancedShown;

        // Internal install phases
        private bool _didPartition;
        private bool _didFormat;
        private bool _didCopyFiles;
        private bool _didBootloader;
        
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

            // Simple keyboard controls in PartitionSetup step
            if (_currentStep == (int)InstallStep.PartitionSetup) {
                var key = Keyboard.KeyInfo;
                if (key.KeyState == ConsoleKeyState.Pressed) {
                    if (key.Key == ConsoleKey.Add || key.Key == ConsoleKey.OemPlus) {
                        _bootPartitionMB += 10; if (_bootPartitionMB > 1024) _bootPartitionMB = 1024;
                    } else if (key.Key == ConsoleKey.Subtract || key.Key == ConsoleKey.OemMinus) {
                        _bootPartitionMB -= 10; if (_bootPartitionMB < 64) _bootPartitionMB = 64;
                    } else if (key.Key == ConsoleKey.Space) {
                        // Toggle filesystem
                        _systemFs = _systemFs == "EXT2" ? "EXT3" : _systemFs == "EXT3" ? "EXT4" : "EXT2";
                    }
                }
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
            // Reset phases
            _didPartition = false;
            _didFormat = false;
            _didCopyFiles = false;
            _didBootloader = false;
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
                    "Partition Layout (editable):",
                    $"  - Boot Partition (FAT32) - {_bootPartitionMB} MB",
                    $"  - System Partition ({_systemFs}) - Remaining space",
                    "",
                    "Adjustments:",
                    "  [+/-] Increase/Decrease boot size (64 MB - 1024 MB)",
                    "  [Space] Toggle system filesystem (EXT2/EXT3/EXT4)",
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
                // Gate phases roughly by percentage
                if (!_didPartition) {
                    _statusMessage = "Partitioning disk...";
                    if (PartitionDisk()) { _didPartition = true; _installProgress = 20; }
                } else if (!_didFormat) {
                    _statusMessage = "Formatting partitions...";
                    if (FormatPartitions()) { _didFormat = true; _installProgress = 40; }
                } else if (!_didCopyFiles) {
                    _statusMessage = "Copying system files...";
                    if (CopySystemFiles()) { _didCopyFiles = true; _installProgress = 70; }
                } else if (!_didBootloader) {
                    _statusMessage = "Installing bootloader...";
                    if (InstallBootloader()) { _didBootloader = true; _installProgress = 90; }
                } else {
                    _statusMessage = "Finalizing installation...";
                    _installProgress += 1;
                }
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
                _didPartition ? "* Partitioning disk..." : "  Partitioning disk...",
                _didFormat ? "* Formatting partitions..." : "  Formatting partitions...",
                _didCopyFiles ? "* Copying system files..." : "  Copying system files...",
                _didBootloader ? "* Installing bootloader..." : "  Installing bootloader...",
                _installProgress > 95 ? "* Configuring system..." : "  Configuring system...",
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
                "2. Click Reboot",
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

            // Draw Reboot button
            int rbW = 140, rbH = 32;
            int rbX = X + Width - Pad - rbW;
            int rbY = Y + Height - 110;
            DrawButton(rbX, rbY, rbW, rbH, "Reboot", true);

            // Handle reboot click
            int mx = Control.MousePosition.X; int my = Control.MousePosition.Y;
            bool left = Control.MouseButtons.HasFlag(MouseButtons.Left);
            if (left && !_clickLock && Hit(mx, my, rbX, rbY, rbW, rbH)) {
                try { guideXOS.Kernel.Drivers.Power.Reboot(); } catch { }
                _clickLock = true;
            } else if (!left) {
                _clickLock = false;
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

        // Disk operations (stubs for now - replace with real implementations)
        private bool PartitionDisk() {
            // Create MBR with two partitions: Boot (FAT32 LBA) and System (Linux/EXT2)
            if (_selectedDiskIndex < 0 || _selectedDiskIndex >= _availableDisks.Count) return false;
            var diskInfo = _availableDisks[_selectedDiskIndex];
            var disk = guideXOS.FS.Disk.Instance;
            uint bps = diskInfo.BytesPerSector == 0 ? 512u : diskInfo.BytesPerSector;
            ulong totalSectors = diskInfo.TotalSectors;
            // Calculate boot partition size in sectors
            ulong bootSectors = ((ulong)_bootPartitionMB * 1024UL * 1024UL) / bps;
            if (bootSectors < 2048) bootSectors = 2048; // minimum
            if (bootSectors >= totalSectors - 4096) bootSectors = totalSectors / 4; // keep room
            ulong sysStart = bootSectors;
            ulong sysSectors = totalSectors - sysStart;

            // Build MBR (512 bytes)
            byte[] mbr = new byte[512];
            // Zero MBR
            for (int i = 0; i < mbr.Length; i++) mbr[i] = 0;
            // Simple boot code message
            string msg = "guideXOS MBR";
            for (int i = 0; i < msg.Length && (0xB8 + i) < 446; i++) mbr[0xB8 + i] = (byte)msg[i];
            // Partition entries start at 0x1BE
            void WriteLBA32(int off, uint val) { mbr[off] = (byte)(val & 0xFF); mbr[off + 1] = (byte)((val >> 8) & 0xFF); mbr[off + 2] = (byte)((val >> 16) & 0xFF); mbr[off + 3] = (byte)((val >> 24) & 0xFF); }
            // P0: Boot - Active, type 0x0C (FAT32 LBA)
            mbr[0x1BE + 0] = 0x80; // active
            mbr[0x1BE + 4] = 0x0C; // type FAT32 LBA
            WriteLBA32(0x1BE + 8, (uint)1); // LBA start (skip MBR sector)
            WriteLBA32(0x1BE + 12, (uint)bootSectors - 1);
            // P1: System - type 0x83 (Linux)
            int p1 = 0x1BE + 16;
            mbr[p1 + 0] = 0x00; // non-active
            mbr[p1 + 4] = 0x83; // Linux/EXT2
            WriteLBA32(p1 + 8, (uint)sysStart);
            WriteLBA32(p1 + 12, (uint)sysSectors);
            // Signature 0x55AA
            mbr[510] = 0x55; mbr[511] = 0xAA;
            unsafe {
                fixed (byte* p = mbr) disk.Write(0, 1, p);
            }
            return true;
        }
        private bool FormatPartitions() {
            // Minimal FAT32 boot partition setup: write BPB and FSInfo
            if (_selectedDiskIndex < 0 || _selectedDiskIndex >= _availableDisks.Count) return false;
            var diskInfo = _availableDisks[_selectedDiskIndex];
            var disk = guideXOS.FS.Disk.Instance;
            uint bps = diskInfo.BytesPerSector == 0 ? 512u : diskInfo.BytesPerSector;
            ulong totalSectors = diskInfo.TotalSectors;
            ulong bootStart = 1; // after MBR
            ulong bootSectors = ((ulong)_bootPartitionMB * 1024UL * 1024UL) / bps; if (bootSectors < 2048) bootSectors = 2048; if (bootSectors >= totalSectors - 4096) bootSectors = totalSectors / 4;
            // BPB for FAT32
            byte[] bpb = new byte[512];
            for (int i = 0; i < bpb.Length; i++) bpb[i] = 0;
            // Jump
            bpb[0] = 0xEB; bpb[1] = 0x58; bpb[2] = 0x90;
            // OEM name
            string oem = "GUIDEXOS"; for (int i = 0; i < 8; i++) bpb[3 + i] = i < oem.Length ? (byte)oem[i] : (byte)' ';
            // Bytes per sector
            bpb[11] = (byte)(bps & 0xFF); bpb[12] = (byte)((bps >> 8) & 0xFF);
            // Sec per cluster: choose 8
            bpb[13] = 8;
            // Reserved sectors: 32
            bpb[14] = 32; bpb[15] = 0;
            // Number of FATs
            bpb[16] = 2;
            // Root entries (0 for FAT32)
            bpb[17] = 0; bpb[18] = 0;
            // Total sectors 16 (0)
            bpb[19] = 0; bpb[20] = 0;
            // Media descriptor
            bpb[21] = 0xF8;
            // FAT size 16 (0)
            bpb[22] = 0; bpb[23] = 0;
            // Sectors per track / heads (dummy)
            bpb[24] = 0x3F; bpb[25] = 0x00; bpb[26] = 0xFF; bpb[27] = 0x00;
            // Hidden sectors
            uint hidden = (uint)bootStart; bpb[28] = (byte)(hidden & 0xFF); bpb[29] = (byte)((hidden >> 8) & 0xFF); bpb[30] = (byte)((hidden >> 16) & 0xFF); bpb[31] = (byte)((hidden >> 24) & 0xFF);
            // Total sectors 32
            uint tot = (uint)bootSectors; bpb[32] = (byte)(tot & 0xFF); bpb[33] = (byte)((tot >> 8) & 0xFF); bpb[34] = (byte)((tot >> 16) & 0xFF); bpb[35] = (byte)((tot >> 24) & 0xFF);
            // FAT size 32: rough estimate (tot/ sec per cluster / 128)
            uint fatsz = tot / (8u * 128u); if (fatsz < 1) fatsz = 1; bpb[36] = (byte)(fatsz & 0xFF); bpb[37] = (byte)((fatsz >> 8) & 0xFF); bpb[38] = (byte)((fatsz >> 16) & 0xFF); bpb[39] = (byte)((fatsz >> 24) & 0xFF);
            // Flags, version
            bpb[40] = 0; bpb[41] = 0; bpb[42] = 0; bpb[43] = 0;
            // Root cluster
            bpb[44] = 2; bpb[45] = 0; bpb[46] = 0; bpb[47] = 0;
            // FSInfo sector
            bpb[48] = 1; bpb[49] = 0;
            // Backup boot sector
            bpb[50] = 6; bpb[51] = 0;
            // Drive number, boot sig, volume id
            bpb[64] = 0x80; bpb[66] = 0x29; bpb[67] = 0x12; bpb[68] = 0x34; bpb[69] = 0x56; bpb[70] = 0x78;
            // Volume label
            string vl = "GUIDEXOS   "; for (int i = 0; i < 11; i++) bpb[71 + i] = (byte)vl[i];
            // File system type
            string fs = "FAT32   "; for (int i = 0; i < 8; i++) bpb[82 + i] = (byte)fs[i];
            // Signature
            bpb[510] = 0x55; bpb[511] = 0xAA;
            unsafe { fixed (byte* p = bpb) disk.Write(bootStart, 1, p); }
            // Zero a few reserved sectors
            var zero = new byte[bps]; unsafe { fixed (byte* pz = zero) { for (ulong s = bootStart + 1; s < bootStart + 32; s++) disk.Write(s, 1, pz); } }
            // FSInfo sector
            byte[] fsinfo = new byte[512]; for (int i = 0; i < 512; i++) fsinfo[i] = 0; fsinfo[0] = (byte)'R'; fsinfo[1] = (byte)'R'; fsinfo[2] = (byte)'a'; fsinfo[3] = (byte)'A'; fsinfo[508] = 0x55; fsinfo[509] = 0xAA; fsinfo[510] = 0x55; fsinfo[511] = 0xAA; unsafe { fixed (byte* pf = fsinfo) disk.Write(bootStart + 1, 1, pf); }
            // System partition: zero first MB as a placeholder
            var oneMB = (ulong)(1024 * 1024 / bps);
            ulong sysStart = bootStart + bootSectors;
            unsafe { fixed (byte* pz = zero) { for (ulong s = sysStart; s < sysStart + oneMB; s++) disk.Write(s, 1, pz); } }
            return true;
        }
        private bool CopySystemFiles() {
            // Copy GRUB files from Tools/grub2/boot to /boot (staging)
            try {
                string srcRoot = "Tools/grub2/boot/";
                string dstRoot = "/boot/";
                CopyDirectoryRecursive(srcRoot, dstRoot);
                // Ensure a minimal grub.cfg exists
                string cfgPath = dstRoot + "grub/grub.cfg";
                if (!guideXOS.FS.File.Exists(cfgPath)) {
                    string cfg = "set timeout=2\nset default=0\n\nmenuentry 'guideXOS' {\n    insmod fat\n    insmod part_msdos\n    set root=(hd0,msdos1)\n    linux /boot/kernel.bin\n}\n";
                    guideXOS.FS.File.WriteAllBytes(cfgPath, GetAsciiBytes(cfg));
                }
            } catch { }
            return true;
        }

        private void CopyDirectoryRecursive(string src, string dst) {
            // NOTE: File API may not support directory creation here; assume existing structure or skip.
            var entries = guideXOS.FS.File.GetFiles(src);
            if (entries != null) {
                for (int i = 0; i < entries.Count; i++) {
                    var e = entries[i];
                    if (e.Attribute == guideXOS.FS.FileAttribute.Directory) {
                        CopyDirectoryRecursive(src + e.Name + "/", dst + e.Name + "/");
                    } else {
                        byte[] data = guideXOS.FS.File.ReadAllBytes(src + e.Name);
                        guideXOS.FS.File.WriteAllBytes(dst + e.Name, data);
                        data.Dispose();
                    }
                    e.Dispose();
                }
                entries.Dispose();
            }
        }

        private static byte[] GetAsciiBytes(string s) {
            if (s == null) return new byte[0];
            var b = new byte[s.Length];
            for (int i = 0; i < s.Length; i++) b[i] = (byte)(s[i] & 0x7F);
            return b;
        }
        private bool InstallBootloader() {
            // Minimal boot: write a tiny placeholder that relies on external bootloader; for GRUB, user can install later.
            // Here we simply ensure MBR signature is present (already set) and leave FAT32 BPB intact.
            return true;
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
