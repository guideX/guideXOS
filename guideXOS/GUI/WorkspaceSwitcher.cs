using guideXOS.Kernel.Drivers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace guideXOS.GUI {
    /// <summary>
    /// Workspace Switcher - GUI component for switching between workspaces
    /// Shows a grid of workspace thumbnails with actual screenshots, allows switching and moving windows
    /// </summary>
    internal class WorkspaceSwitcher {
        /// <summary>
        /// Visibility state
        /// </summary>
        public bool Visible { get; set; }

        /// <summary>
        /// Window snapshot for rendering thumbnails
        /// </summary>
        private class WindowSnapshot {
            public string Title;
            public int X, Y, Width, Height;
            public Image Icon;
            public int WorkspaceIndex;
        }

        /// <summary>
        /// Workspace thumbnail cache
        /// </summary>
        private class WorkspaceThumbnail {
            public Image Screenshot;
            public bool NeedsUpdate;
            public int WindowCount;
        }

        private List<WindowSnapshot> _windowCache;
        private WorkspaceThumbnail[] _workspaceThumbnails;
        private int _selectedWorkspace;
        private bool _mouseDownLatch;
        private ConsoleKey _lastKey;
        private const int WorkspaceGridCols = 4; // 2x4 grid for 8 workspaces
        private const int WorkspaceGridRows = 2;
        private const int ThumbnailWidth = 280;
        private const int ThumbnailHeight = 180;
        private const int GridPadding = 20;
        private const int HeaderHeight = 60;
        private const int FooterHeight = 40;

        // Overlay dimensions
        private int _overlayX, _overlayY, _overlayW, _overlayH;

        public WorkspaceSwitcher() {
            _windowCache = new List<WindowSnapshot>();
            _workspaceThumbnails = new WorkspaceThumbnail[WorkspaceManager.MaxWorkspaces];
            for (int i = 0; i < _workspaceThumbnails.Length; i++) {
                _workspaceThumbnails[i] = new WorkspaceThumbnail {
                    Screenshot = null,
                    NeedsUpdate = true,
                    WindowCount = 0
                };
            }
            _selectedWorkspace = WorkspaceManager.Current;
            _mouseDownLatch = false;
            _lastKey = ConsoleKey.NoName;
            Visible = false;
        }

        /// <summary>
        /// Capture screenshot of current workspace before switching
        /// </summary>
        public void CaptureCurrentWorkspace() {
            int currentWs = WorkspaceManager.Current;
            if (currentWs < 0 || currentWs >= _workspaceThumbnails.Length) return;

            try {
                // Create a scaled-down screenshot
                int fbWidth = Framebuffer.Width;
                int fbHeight = Framebuffer.Height;
                
                // Calculate scale to fit thumbnail - use power-of-2 downscaling for speed
                // Instead of arbitrary scale, use fast integer division
                int scaleDiv = 4; // 1920x1080 -> 480x270, 1280x720 -> 320x180
                
                int scaledWidth = fbWidth / scaleDiv;
                int scaledHeight = fbHeight / scaleDiv;
                
                // Clamp to thumbnail size
                if (scaledWidth > ThumbnailWidth) {
                    int newDiv = fbWidth / ThumbnailWidth;
                    if (newDiv > scaleDiv) scaleDiv = newDiv;
                    scaledWidth = fbWidth / scaleDiv;
                    scaledHeight = fbHeight / scaleDiv;
                }
                
                // Dispose old screenshot if it exists
                if (_workspaceThumbnails[currentWs].Screenshot != null) {
                    _workspaceThumbnails[currentWs].Screenshot.Dispose();
                }
                
                // Create new thumbnail image
                var thumbnail = new Image(scaledWidth, scaledHeight);
                
                // Fast downscaling using integer stride - MUCH faster than per-pixel scaling
                unsafe {
                    uint* src = Framebuffer.Graphics.VideoMemory;
                    
                    fixed (int* dstFixed = thumbnail.RawData) {
                        uint* dst = (uint*)dstFixed;
                        
                        // Simple box downsampling with stride (very fast)
                        for (int y = 0; y < scaledHeight; y++) {
                            int srcY = y * scaleDiv;
                            uint* srcRow = src + (srcY * fbWidth);
                            uint* dstRow = dst + (y * scaledWidth);
                            
                            for (int x = 0; x < scaledWidth; x++) {
                                dstRow[x] = srcRow[x * scaleDiv];
                            }
                        }
                    }
                }
                
                _workspaceThumbnails[currentWs].Screenshot = thumbnail;
                _workspaceThumbnails[currentWs].NeedsUpdate = false;
                
                // Debug output
                Console.WriteLine($"[WorkspaceSwitcher] Captured screenshot for workspace {currentWs} ({scaledWidth}x{scaledHeight}, div={scaleDiv})");
            } catch {
                // If screenshot fails, mark for update
                _workspaceThumbnails[currentWs].NeedsUpdate = true;
                Console.WriteLine($"[WorkspaceSwitcher] Failed to capture workspace {currentWs}");
            }
        }

        /// <summary>
        /// Mark workspace thumbnail as needing update
        /// </summary>
        public void InvalidateWorkspace(int workspaceIndex) {
            if (workspaceIndex >= 0 && workspaceIndex < _workspaceThumbnails.Length) {
                _workspaceThumbnails[workspaceIndex].NeedsUpdate = true;
            }
        }

        /// <summary>
        /// Mark all workspaces as needing update
        /// </summary>
        public void InvalidateAll() {
            for (int i = 0; i < _workspaceThumbnails.Length; i++) {
                _workspaceThumbnails[i].NeedsUpdate = true;
            }
        }

        /// <summary>
        /// Refresh window cache for rendering
        /// </summary>
        public void RefreshWindowCache() {
            // Clear old cache
            if (_windowCache != null) {
                for (int i = 0; i < _windowCache.Count; i++) {
                    if (_windowCache[i] != null) {
                        _windowCache[i].Title?.Dispose();
                    }
                }
                _windowCache.Clear();
            } else {
                _windowCache = new List<WindowSnapshot>();
            }

            // Ensure all windows are tracked
            WorkspaceManager.EnsureAllWindowsTracked();

            // Snapshot visible windows
            var windows = WindowManager.Windows;
            if (windows != null) {
                for (int i = 0; i < windows.Count; i++) {
                    var w = windows[i];
                    if (w == null || !w.ShowInTaskbar) continue;

                    var snap = new WindowSnapshot {
                        Title = w.Title ?? "Untitled",
                        X = w.X,
                        Y = w.Y,
                        Width = w.Width,
                        Height = w.Height,
                        Icon = w.TaskbarIcon,
                        WorkspaceIndex = WorkspaceManager.GetWorkspace(w)
                    };
                    _windowCache.Add(snap);
                }
            }

            // Update window counts
            int[] counts = WorkspaceManager.WorkspaceWindowCounts();
            for (int i = 0; i < Math.Min(counts.Length, _workspaceThumbnails.Length); i++) {
                _workspaceThumbnails[i].WindowCount = counts[i];
            }

            _selectedWorkspace = WorkspaceManager.Current;
        }

        /// <summary>
        /// Handle input events
        /// </summary>
        public void OnInput() {
            if (!Visible) return;

            int mx = Control.MousePosition.X;
            int my = Control.MousePosition.Y;
            bool leftDown = Control.MouseButtons.HasFlag(MouseButtons.Left);

            // Keyboard navigation using KeyInfo
            var keyInfo = Keyboard.KeyInfo;
            
            // Only process key press events (not release)
            if (keyInfo.KeyState == ConsoleKeyState.Pressed && keyInfo.Key != _lastKey) {
                _lastKey = keyInfo.Key;
                
                // Arrow key navigation
                if (keyInfo.Key == ConsoleKey.Right) {
                    _selectedWorkspace++;
                    if (_selectedWorkspace >= WorkspaceManager.Count) _selectedWorkspace = WorkspaceManager.Count - 1;
                } else if (keyInfo.Key == ConsoleKey.Left) {
                    _selectedWorkspace--;
                    if (_selectedWorkspace < 0) _selectedWorkspace = 0;
                } else if (keyInfo.Key == ConsoleKey.Up) {
                    _selectedWorkspace -= WorkspaceGridCols;
                    if (_selectedWorkspace < 0) _selectedWorkspace = 0;
                } else if (keyInfo.Key == ConsoleKey.Down) {
                    _selectedWorkspace += WorkspaceGridCols;
                    if (_selectedWorkspace >= WorkspaceManager.Count) _selectedWorkspace = WorkspaceManager.Count - 1;
                } else if (keyInfo.Key == ConsoleKey.Enter) {
                    // Switch to selected workspace and close
                    WorkspaceManager.SwitchTo(_selectedWorkspace);
                    Visible = false;
                } else if (keyInfo.Key == ConsoleKey.Escape) {
                    // Close without switching
                    Visible = false;
                } else if (keyInfo.Key == ConsoleKey.Add || keyInfo.Key == ConsoleKey.OemPlus || 
                          keyInfo.Key == ConsoleKey.A || keyInfo.Key == ConsoleKey.N) {
                    // Add new workspace (+ or = or A or N key)
                    if (WorkspaceManager.AddWorkspace()) {
                        _selectedWorkspace = WorkspaceManager.Count - 1;
                    }
                }
            }
            
            // Reset key tracking when key is released
            if (keyInfo.KeyState != ConsoleKeyState.Pressed) {
                _lastKey = ConsoleKey.NoName;
            }

            // Calculate overlay position
            _overlayW = (ThumbnailWidth + GridPadding) * WorkspaceGridCols + GridPadding;
            _overlayH = HeaderHeight + (ThumbnailHeight + GridPadding) * WorkspaceGridRows + GridPadding + FooterHeight;
            _overlayX = (Framebuffer.Width - _overlayW) / 2;
            _overlayY = (Framebuffer.Height - _overlayH) / 2;

            // Mouse click handling
            if (leftDown) {
                if (!_mouseDownLatch) {
                    // Check if clicking on a workspace thumbnail
                    int gridX = _overlayX + GridPadding;
                    int gridY = _overlayY + HeaderHeight + GridPadding;

                    for (int ws = 0; ws < WorkspaceManager.Count; ws++) {
                        int col = ws % WorkspaceGridCols;
                        int row = ws / WorkspaceGridCols;
                        
                        int thumbX = gridX + col * (ThumbnailWidth + GridPadding);
                        int thumbY = gridY + row * (ThumbnailHeight + GridPadding);

                        if (mx >= thumbX && mx <= thumbX + ThumbnailWidth &&
                            my >= thumbY && my <= thumbY + ThumbnailHeight) {
                            // Click on workspace - switch to it
                            WorkspaceManager.SwitchTo(ws);
                            Visible = false;
                            _mouseDownLatch = true;
                            return;
                        }
                    }

                    // Check if clicking outside overlay - close it
                    if (mx < _overlayX || mx > _overlayX + _overlayW ||
                        my < _overlayY || my > _overlayY + _overlayH) {
                        Visible = false;
                        _mouseDownLatch = true;
                    }
                }
            } else {
                _mouseDownLatch = false;
            }

            // Update hover state
            int hoverWs = -1;
            int gridX2 = _overlayX + GridPadding;
            int gridY2 = _overlayY + HeaderHeight + GridPadding;

            for (int ws = 0; ws < WorkspaceManager.Count; ws++) {
                int col = ws % WorkspaceGridCols;
                int row = ws / WorkspaceGridCols;
                
                int thumbX = gridX2 + col * (ThumbnailWidth + GridPadding);
                int thumbY = gridY2 + row * (ThumbnailHeight + GridPadding);

                if (mx >= thumbX && mx <= thumbX + ThumbnailWidth &&
                    my >= thumbY && my <= thumbY + ThumbnailHeight) {
                    hoverWs = ws;
                    break;
                }
            }

            if (hoverWs >= 0) {
                _selectedWorkspace = hoverWs;
            }
        }

        /// <summary>
        /// Render the workspace switcher overlay
        /// </summary>
        public void OnDraw() {
            if (!Visible) return;

            var g = Framebuffer.Graphics;
            if (g == null) return;

            // Draw semi-transparent backdrop
            g.AFillRectangle(0, 0, Framebuffer.Width, Framebuffer.Height, 0xCC000000);

            // Draw main overlay panel
            UIPrimitives.AFillRoundedRect(_overlayX, _overlayY, _overlayW, _overlayH, 0xEE1A1A1A, 8);
            UIPrimitives.DrawRoundedRect(_overlayX, _overlayY, _overlayW, _overlayH, 0xFF3A3A3A, 2, 8);

            // Draw header
            if (WindowManager.font != null) {
                string title = "Workspaces";
                int titleW = WindowManager.font.MeasureString(title);
                int titleX = _overlayX + (_overlayW - titleW) / 2;
                int titleY = _overlayY + (HeaderHeight - WindowManager.font.FontSize) / 2;
                WindowManager.font.DrawString(titleX, titleY, title);

                // Draw current workspace indicator
                string current = "Current: " + (WorkspaceManager.Current + 1).ToString();
                int currentW = WindowManager.font.MeasureString(current);
                WindowManager.font.DrawString(_overlayX + 20, titleY, current, currentW, WindowManager.font.FontSize);
                
                title.Dispose();
                current.Dispose();
            }

            // Draw workspace grid
            int gridX = _overlayX + GridPadding;
            int gridY = _overlayY + HeaderHeight + GridPadding;

            int[] windowCounts = WorkspaceManager.WorkspaceWindowCounts();

            for (int ws = 0; ws < WorkspaceManager.Count; ws++) {
                int col = ws % WorkspaceGridCols;
                int row = ws / WorkspaceGridCols;
                
                int thumbX = gridX + col * (ThumbnailWidth + GridPadding);
                int thumbY = gridY + row * (ThumbnailHeight + GridPadding);

                DrawWorkspaceThumbnail(thumbX, thumbY, ws, ws == _selectedWorkspace, ws == WorkspaceManager.Current, windowCounts[ws]);
            }

            // Draw footer with instructions
            if (WindowManager.font != null) {
                int footerY = _overlayY + _overlayH - FooterHeight + 10;
                string help = "Click or Enter to switch • ESC to close • Arrows to navigate • A/N/+ to add workspace";
                int helpW = WindowManager.font.MeasureString(help);
                int helpX = _overlayX + (_overlayW - helpW) / 2;
                WindowManager.font.DrawString(helpX, footerY, help);
                help.Dispose();
            }
        }

        /// <summary>
        /// Draw a single workspace thumbnail
        /// </summary>
        private void DrawWorkspaceThumbnail(int x, int y, int workspaceIndex, bool selected, bool current, int windowCount) {
            var g = Framebuffer.Graphics;
            
            // Background color based on state
            uint bgColor;
            if (current) {
                bgColor = 0xFF2A4A6A; // Blue tint for current workspace
            } else if (selected) {
                bgColor = 0xFF3A3A3A; // Lighter for selected
            } else {
                bgColor = 0xFF252525; // Dark for inactive
            }

            // Draw thumbnail background
            UIPrimitives.AFillRoundedRect(x, y, ThumbnailWidth, ThumbnailHeight, bgColor, 6);
            
            // Draw border (thicker for selected/current)
            uint borderColor;
            int borderThickness;
            if (current) {
                borderColor = 0xFF4A8FD8;
                borderThickness = 3;
            } else if (selected) {
                borderColor = 0xFF6A6A6A;
                borderThickness = 2;
            } else {
                borderColor = 0xFF3A3A3A;
                borderThickness = 1;
            }
            UIPrimitives.DrawRoundedRect(x, y, ThumbnailWidth, ThumbnailHeight, borderColor, borderThickness, 6);

            // Draw workspace label
            if (WindowManager.font != null) {
                string label = "Workspace " + (workspaceIndex + 1).ToString();
                int labelW = WindowManager.font.MeasureString(label);
                WindowManager.font.DrawString(x + 10, y + 10, label);
                label.Dispose();

                // Draw window count
                if (windowCount > 0) {
                    string count = windowCount.ToString() + " window" + (windowCount == 1 ? "" : "s");
                    int countW = WindowManager.font.MeasureString(count);
                    int countX = x + ThumbnailWidth - countW - 10;
                    WindowManager.font.DrawString(countX, y + 10, count, countW, WindowManager.font.FontSize);
                    count.Dispose();
                }
            }

            // Draw the actual screenshot or fallback to window representations
            int contentAreaX = x + 10;
            int contentAreaY = y + 35;
            int contentAreaW = ThumbnailWidth - 20;
            int contentAreaH = ThumbnailHeight - 45;

            // Try to use screenshot first
            bool drewScreenshot = false;
            if (workspaceIndex < _workspaceThumbnails.Length) {
                var thumbnail = _workspaceThumbnails[workspaceIndex];
                if (thumbnail != null && thumbnail.Screenshot != null && !thumbnail.NeedsUpdate) {
                    // Center the screenshot in the content area
                    int screenshotX = contentAreaX + (contentAreaW - thumbnail.Screenshot.Width) / 2;
                    int screenshotY = contentAreaY + (contentAreaH - thumbnail.Screenshot.Height) / 2;
                    
                    // Draw the screenshot
                    g.DrawImage(screenshotX, screenshotY, thumbnail.Screenshot);
                    drewScreenshot = true;
                } else {
                    // Debug: why didn't we draw screenshot?
                    if (thumbnail == null) {
                        Console.WriteLine($"[WorkspaceSwitcher] WS{workspaceIndex}: thumbnail is null");
                    } else if (thumbnail.Screenshot == null) {
                        Console.WriteLine($"[WorkspaceSwitcher] WS{workspaceIndex}: Screenshot is null");
                    } else if (thumbnail.NeedsUpdate) {
                        Console.WriteLine($"[WorkspaceSwitcher] WS{workspaceIndex}: NeedsUpdate=true");
                    }
                }
            }

            // Fallback to window representations if no screenshot
            if (!drewScreenshot) {
                // Draw a grid pattern to represent the desktop
                uint gridColor = 0x22FFFFFF;
                for (int gx = 0; gx < contentAreaW; gx += 20) {
                    g.FillRectangle(contentAreaX + gx, contentAreaY, 1, contentAreaH, gridColor);
                }
                for (int gy = 0; gy < contentAreaH; gy += 20) {
                    g.FillRectangle(contentAreaX, contentAreaY + gy, contentAreaW, 1, gridColor);
                }

                // Draw small rectangles representing windows in this workspace
                int drawnWindows = 0;
                for (int i = 0; i < _windowCache.Count && drawnWindows < 6; i++) {
                    var win = _windowCache[i];
                    if (win.WorkspaceIndex != workspaceIndex) continue;

                    // Scale down window position/size to fit in thumbnail
                    float scaleX = (float)contentAreaW / Framebuffer.Width;
                    float scaleY = (float)contentAreaH / Framebuffer.Height;
                    
                    int miniX = contentAreaX + (int)(win.X * scaleX);
                    int miniY = contentAreaY + (int)(win.Y * scaleY);
                    int miniW = (int)(win.Width * scaleX);
                    int miniH = (int)(win.Height * scaleY);

                    // Ensure minimum size
                    if (miniW < 20) miniW = 20;
                    if (miniH < 15) miniH = 15;

                    // Clamp to mini area
                    if (miniX < contentAreaX) miniX = contentAreaX;
                    if (miniY < contentAreaY) miniY = contentAreaY;
                    if (miniX + miniW > contentAreaX + contentAreaW) miniW = contentAreaX + contentAreaW - miniX;
                    if (miniY + miniH > contentAreaY + contentAreaH) miniH = contentAreaY + contentAreaH - miniY;

                    // Draw mini window
                    UIPrimitives.AFillRoundedRect(miniX, miniY, miniW, miniH, 0xAA4A4A4A, 2);
                    UIPrimitives.DrawRoundedRect(miniX, miniY, miniW, miniH, 0xFF6A6A6A, 1, 2);

                    // Draw mini title bar
                    int miniBarH = miniH / 4;
                    if (miniBarH < 3) miniBarH = 3;
                    if (miniBarH > 8) miniBarH = 8;
                    UIPrimitives.AFillRoundedRect(miniX, miniY, miniW, miniBarH, 0xAA2E86C1, 2);

                    drawnWindows++;
                }

                // If no windows, show empty state
                if (windowCount == 0 && WindowManager.font != null) {
                    string emptyText = "Empty";
                    int emptyW = WindowManager.font.MeasureString(emptyText);
                    int emptyX = x + (ThumbnailWidth - emptyW) / 2;
                    int emptyY = y + (ThumbnailHeight - WindowManager.font.FontSize) / 2;
                    WindowManager.font.DrawString(emptyX, emptyY, emptyText);
                    emptyText.Dispose();
                }
            }

            // Draw "Current" badge if this is the active workspace
            if (current && WindowManager.font != null) {
                string badge = "● CURRENT";
                int badgeW = WindowManager.font.MeasureString(badge);
                int badgeX = x + (ThumbnailWidth - badgeW) / 2;
                int badgeY = y + ThumbnailHeight - 25;
                
                // Badge background
                UIPrimitives.AFillRoundedRect(badgeX - 5, badgeY - 3, badgeW + 10, WindowManager.font.FontSize + 6, 0xDD4A8FD8, 3);
                WindowManager.font.DrawString(badgeX, badgeY, badge);
                badge.Dispose();
            }
        }

        /// <summary>
        /// Dispose resources
        /// </summary>
        public void Dispose() {
            // Clean up screenshots
            if (_workspaceThumbnails != null) {
                for (int i = 0; i < _workspaceThumbnails.Length; i++) {
                    if (_workspaceThumbnails[i] != null && _workspaceThumbnails[i].Screenshot != null) {
                        _workspaceThumbnails[i].Screenshot.Dispose();
                        _workspaceThumbnails[i].Screenshot = null;
                    }
                }
            }

            // Clean up window cache
            if (_windowCache != null) {
                for (int i = 0; i < _windowCache.Count; i++) {
                    if (_windowCache[i] != null) {
                        _windowCache[i].Title?.Dispose();
                    }
                }
                _windowCache.Clear();
            }
        }
    }
}