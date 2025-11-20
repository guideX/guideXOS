using guideXOS.Kernel.Drivers;
using guideXOS.Misc;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace guideXOS.GUI {
    /// <summary>
    /// Visual Effects Settings Window - Exposes all animation settings and GUI-related parameters
    /// </summary>
    internal class VisualEffectsSettings : Window {
        private int _padding = 20;
        private int _lineHeight = 48;
        private int _labelWidth = 280;
        private int _sliderWidth = 240;
        private int _checkboxSize = 24;
        
        // Scroll state
        private int _scrollY = 0;
        private bool _scrollDragging = false;
        private int _scrollStartY = 0;
        private int _scrollStartScroll = 0;
        
        // Slider state
        private int _draggingSlider = -1;
        
        // Settings (local copies that get applied when clicking Apply)
        private bool _enableFadeAnimations;
        private int _fadeInDuration;
        private int _fadeOutDuration;
        private bool _enableWindowSlideAnimations;
        private int _windowSlideDuration;
        
        // Window frame rate related
        private int _windowFrameDelay = 2; // Thread.Sleep value in main loop
        
        // Animation easing (if Animator exists)
        private int _animationSpeed = 150; // Generic animation speed
        
        // UI responsiveness
        private int _mouseResponsiveMs = 100; // ActiveMoveMs from Program.cs
        
        // Button state
        private bool _btnClickLatch = false;
        
        public VisualEffectsSettings(int X, int Y) : base(X, Y, 700, 550) {
            IsResizable = false;
            ShowInTaskbar = true;
            ShowMaximize = false;
            ShowMinimize = true;
            ShowTombstone = true;
            Title = "Visual Effects";
            
            // Load current settings
            _enableFadeAnimations = UISettings.EnableFadeAnimations;
            _fadeInDuration = UISettings.FadeInDurationMs;
            _fadeOutDuration = UISettings.FadeOutDurationMs;
            _enableWindowSlideAnimations = UISettings.EnableWindowSlideAnimations;
            _windowSlideDuration = UISettings.WindowSlideDurationMs;
        }
        
        public override void OnInput() {
            base.OnInput();
            if (!Visible) return;
            
            // Only process input if mouse is within the window bounds
            if (!IsUnderMouse()) return;
            
            int mx = Control.MousePosition.X;
            int my = Control.MousePosition.Y;
            bool leftDown = Control.MouseButtons.HasFlag(MouseButtons.Left);
            
            int cx = X + _padding;
            int cy = Y + _padding;
            int cw = Width - _padding * 2;
            int contentH = Height - _padding * 2 - 60; // Leave space for Apply/Reset buttons
            
            // Scrollbar handling
            int sbW = 12;
            int sbX = X + Width - _padding - sbW;
            int sbY = cy;
            int sbH = contentH;
            
            int totalHeight = _lineHeight * 12; // Approximate total content height
            int maxScroll = totalHeight > contentH ? totalHeight - contentH : 0;
            
            if (leftDown) {
                // Check scrollbar drag
                if (mx >= sbX && mx <= sbX + sbW && my >= sbY && my <= sbY + sbH) {
                    if (!_scrollDragging) {
                        _scrollDragging = true;
                        _scrollStartY = my;
                        _scrollStartScroll = _scrollY;
                    }
                }
                
                if (!_btnClickLatch) {
                    // Check checkboxes
                    int currentY = cy - _scrollY;
                    
                    // Fade Animations checkbox
                    currentY += _lineHeight;
                    if (mx >= cx && mx <= cx + _checkboxSize && my >= currentY && my <= currentY + _checkboxSize) {
                        _enableFadeAnimations = !_enableFadeAnimations;
                        _btnClickLatch = true;
                    }
                    
                    // Slide Animations checkbox
                    currentY += _lineHeight * 3; // Skip fade duration sliders
                    if (mx >= cx && mx <= cx + _checkboxSize && my >= currentY && my <= currentY + _checkboxSize) {
                        _enableWindowSlideAnimations = !_enableWindowSlideAnimations;
                        _btnClickLatch = true;
                    }
                    
                    // Apply button
                    int btnW = 120;
                    int btnH = 38;
                    int btnY = Y + Height - _padding - btnH - 10;
                    int applyX = X + Width - _padding - btnW;
                    int resetX = applyX - btnW - 16;
                    
                    if (mx >= applyX && mx <= applyX + btnW && my >= btnY && my <= btnY + btnH) {
                        ApplySettings();
                        _btnClickLatch = true;
                    }
                    
                    if (mx >= resetX && mx <= resetX + btnW && my >= btnY && my <= btnY + btnH) {
                        ResetToDefaults();
                        _btnClickLatch = true;
                    }
                    
                    // Check sliders
                    int sliderY = cy + _lineHeight - _scrollY;
                    
                    // Fade In Duration slider
                    sliderY += _lineHeight;
                    if (my >= sliderY + 8 && my <= sliderY + 24 && mx >= cx + _labelWidth && mx <= cx + _labelWidth + _sliderWidth) {
                        _draggingSlider = 0;
                    }
                    
                    // Fade Out Duration slider
                    sliderY += _lineHeight;
                    if (my >= sliderY + 8 && my <= sliderY + 24 && mx >= cx + _labelWidth && mx <= cx + _labelWidth + _sliderWidth) {
                        _draggingSlider = 1;
                    }
                    
                    // Window Slide Duration slider
                    sliderY += _lineHeight * 2;
                    if (my >= sliderY + 8 && my <= sliderY + 24 && mx >= cx + _labelWidth && mx <= cx + _labelWidth + _sliderWidth) {
                        _draggingSlider = 2;
                    }
                    
                    // Window Frame Delay slider
                    sliderY += _lineHeight;
                    if (my >= sliderY + 8 && my <= sliderY + 24 && mx >= cx + _labelWidth && mx <= cx + _labelWidth + _sliderWidth) {
                        _draggingSlider = 3;
                    }
                    
                    // Animation Speed slider
                    sliderY += _lineHeight;
                    if (my >= sliderY + 8 && my <= sliderY + 24 && mx >= cx + _labelWidth && mx <= cx + _labelWidth + _sliderWidth) {
                        _draggingSlider = 4;
                    }
                    
                    // Mouse Responsive Time slider
                    sliderY += _lineHeight;
                    if (my >= sliderY + 8 && my <= sliderY + 24 && mx >= cx + _labelWidth && mx <= cx + _labelWidth + _sliderWidth) {
                        _draggingSlider = 5;
                    }
                }
                
                // Handle slider dragging
                if (_draggingSlider >= 0) {
                    int sliderX = cx + _labelWidth;
                    float t = (float)(mx - sliderX) / _sliderWidth;
                    if (t < 0) t = 0;
                    if (t > 1) t = 1;
                    
                    switch (_draggingSlider) {
                        case 0: // Fade In Duration (50-500ms)
                            _fadeInDuration = (int)(50 + t * 450);
                            break;
                        case 1: // Fade Out Duration (50-500ms)
                            _fadeOutDuration = (int)(50 + t * 450);
                            break;
                        case 2: // Window Slide Duration (50-500ms)
                            _windowSlideDuration = (int)(50 + t * 450);
                            break;
                        case 3: // Window Frame Delay (0-10ms)
                            _windowFrameDelay = (int)(t * 10);
                            break;
                        case 4: // Animation Speed (50-500ms)
                            _animationSpeed = (int)(50 + t * 450);
                            break;
                        case 5: // Mouse Responsive Time (0-200ms)
                            _mouseResponsiveMs = (int)(t * 200);
                            break;
                    }
                }
                
            } else {
                _scrollDragging = false;
                _draggingSlider = -1;
                _btnClickLatch = false;
            }
            
            // Update scroll position during drag
            if (_scrollDragging) {
                int dy = my - _scrollStartY;
                _scrollY = _scrollStartScroll + dy;
                if (_scrollY < 0) _scrollY = 0;
                if (_scrollY > maxScroll) _scrollY = maxScroll;
            }
        }
        
        public override void OnDraw() {
            base.OnDraw();
            if (WindowManager.font == null) return;
            
            var g = Framebuffer.Graphics;
            int cx = X + _padding;
            int cy = Y + _padding;
            int cw = Width - _padding * 2 - 16; // Account for scrollbar
            int contentH = Height - _padding * 2 - 60;
            
            // Draw content area background
            g.FillRectangle(cx - 4, cy - 4, cw + 8, contentH + 8, 0xFF1A1A1A);
            
            // Set up clipping for scrollable content (manual clipping in draw calls)
            int currentY = cy - _scrollY;
            
            // Title section
            WindowManager.font.DrawString(cx, currentY, "Window Animation Settings");
            currentY += _lineHeight;
            
            // Fade Animations checkbox
            DrawCheckbox(cx, currentY, _enableFadeAnimations);
            WindowManager.font.DrawString(cx + _checkboxSize + 12, currentY + (_checkboxSize - WindowManager.font.FontSize) / 2, "Enable Fade Animations");
            currentY += _lineHeight;
            
            // Fade In Duration slider
            if (currentY >= cy - _lineHeight && currentY <= cy + contentH) {
                WindowManager.font.DrawString(cx + 32, currentY, "Fade In Duration:");
                DrawSlider(cx + _labelWidth, currentY, _sliderWidth, (_fadeInDuration - 50) / 450.0f);
                string val = _fadeInDuration.ToString() + " ms";
                WindowManager.font.DrawString(cx + _labelWidth + _sliderWidth + 16, currentY, val);
                val.Dispose();
            }
            currentY += _lineHeight;
            
            // Fade Out Duration slider
            if (currentY >= cy - _lineHeight && currentY <= cy + contentH) {
                WindowManager.font.DrawString(cx + 32, currentY, "Fade Out Duration:");
                DrawSlider(cx + _labelWidth, currentY, _sliderWidth, (_fadeOutDuration - 50) / 450.0f);
                string val = _fadeOutDuration.ToString() + " ms";
                WindowManager.font.DrawString(cx + _labelWidth + _sliderWidth + 16, currentY, val);
                val.Dispose();
            }
            currentY += _lineHeight;
            
            // Window Slide Animations checkbox
            if (currentY >= cy - _lineHeight && currentY <= cy + contentH) {
                DrawCheckbox(cx, currentY, _enableWindowSlideAnimations);
                WindowManager.font.DrawString(cx + _checkboxSize + 12, currentY + (_checkboxSize - WindowManager.font.FontSize) / 2, "Enable Window Slide Animations");
            }
            currentY += _lineHeight;
            
            // Window Slide Duration slider
            if (currentY >= cy - _lineHeight && currentY <= cy + contentH) {
                WindowManager.font.DrawString(cx + 32, currentY, "Slide Duration:");
                DrawSlider(cx + _labelWidth, currentY, _sliderWidth, (_windowSlideDuration - 50) / 450.0f);
                string val = _windowSlideDuration.ToString() + " ms";
                WindowManager.font.DrawString(cx + _labelWidth + _sliderWidth + 16, currentY, val);
                val.Dispose();
            }
            currentY += _lineHeight;
            
            // Performance Settings section
            if (currentY >= cy - _lineHeight && currentY <= cy + contentH) {
                WindowManager.font.DrawString(cx, currentY, "Performance Settings");
            }
            currentY += _lineHeight;
            
            // Window Frame Delay slider
            if (currentY >= cy - _lineHeight && currentY <= cy + contentH) {
                WindowManager.font.DrawString(cx + 32, currentY, "Frame Delay:");
                DrawSlider(cx + _labelWidth, currentY, _sliderWidth, _windowFrameDelay / 10.0f);
                string val = _windowFrameDelay.ToString() + " ms";
                WindowManager.font.DrawString(cx + _labelWidth + _sliderWidth + 16, currentY, val);
                val.Dispose();
            }
            currentY += _lineHeight;
            
            // Animation Speed slider
            if (currentY >= cy - _lineHeight && currentY <= cy + contentH) {
                WindowManager.font.DrawString(cx + 32, currentY, "Animation Speed:");
                DrawSlider(cx + _labelWidth, currentY, _sliderWidth, (_animationSpeed - 50) / 450.0f);
                string val = _animationSpeed.ToString() + " ms";
                WindowManager.font.DrawString(cx + _labelWidth + _sliderWidth + 16, currentY, val);
                val.Dispose();
            }
            currentY += _lineHeight;
            
            // Mouse Responsive Time slider
            if (currentY >= cy - _lineHeight && currentY <= cy + contentH) {
                WindowManager.font.DrawString(cx + 32, currentY, "Mouse Response Time:");
                DrawSlider(cx + _labelWidth, currentY, _sliderWidth, _mouseResponsiveMs / 200.0f);
                string val = _mouseResponsiveMs.ToString() + " ms";
                WindowManager.font.DrawString(cx + _labelWidth + _sliderWidth + 16, currentY, val);
                val.Dispose();
            }
            currentY += _lineHeight;
            
            // Info section
            if (currentY >= cy - _lineHeight && currentY <= cy + contentH) {
                WindowManager.font.DrawString(cx, currentY, "Note: Changes take effect after clicking Apply");
            }
            
            // Scrollbar
            int sbW = 12;
            int sbX = X + Width - _padding - sbW;
            int sbY = cy;
            int sbH = contentH;
            int totalHeight = _lineHeight * 12;
            int maxScroll = totalHeight > contentH ? totalHeight - contentH : 0;
            
            g.FillRectangle(sbX, sbY, sbW, sbH, 0xFF0F0F0F);
            if (maxScroll > 0 && totalHeight > 0) {
                int thumbH = sbH * sbH / totalHeight;
                if (thumbH < 24) thumbH = 24;
                if (thumbH > sbH) thumbH = sbH;
                int thumbY = sbH * _scrollY / totalHeight;
                if (thumbY + thumbH > sbH) thumbY = sbH - thumbH;
                g.FillRectangle(sbX + 1, sbY + thumbY, sbW - 2, thumbH, 0xFF3F3F3F);
            }
            
            // Apply and Reset buttons
            int btnW = 120;
            int btnH = 38;
            int btnY = Y + Height - _padding - btnH - 10;
            int applyX = X + Width - _padding - btnW;
            int resetX = applyX - btnW - 16;
            
            // Reset button
            g.FillRectangle(resetX, btnY, btnW, btnH, 0xFF2A2A2A);
            g.DrawRectangle(resetX, btnY, btnW, btnH, 0xFF3F3F3F, 1);
            WindowManager.font.DrawString(resetX + 30, btnY + (btnH - WindowManager.font.FontSize) / 2, "Reset");
            
            // Apply button
            g.FillRectangle(applyX, btnY, btnW, btnH, 0xFF2E7F3F);
            g.DrawRectangle(applyX, btnY, btnW, btnH, 0xFF3F3F3F, 1);
            WindowManager.font.DrawString(applyX + 34, btnY + (btnH - WindowManager.font.FontSize) / 2, "Apply");
        }
        
        private void DrawCheckbox(int x, int y, bool checked_) {
            var g = Framebuffer.Graphics;
            
            // Draw checkbox background
            g.FillRectangle(x, y, _checkboxSize, _checkboxSize, 0xFF2A2A2A);
            g.DrawRectangle(x, y, _checkboxSize, _checkboxSize, 0xFF4F4F4F, 2);
            
            // Draw checkmark if checked
            if (checked_) {
                // Simple checkmark using lines
                int cx = x + _checkboxSize / 2;
                int cy = y + _checkboxSize / 2;
                
                // Draw a filled checkmark
                for (int i = 0; i < 3; i++) {
                    // Short line (down-left)
                    g.FillRectangle(x + 6 + i, y + 10 + i, 2, 2, 0xFF4A8FD8);
                    // Long line (up-right)
                    g.FillRectangle(x + 8 + i, y + 12 - i, 2, 2, 0xFF4A8FD8);
                    g.FillRectangle(x + 10 + i, y + 10 - i, 2, 2, 0xFF4A8FD8);
                }
            }
        }
        
        private void DrawSlider(int x, int y, int width, float value) {
            var g = Framebuffer.Graphics;
            
            // Clamp value
            if (value < 0) value = 0;
            if (value > 1) value = 1;
            
            int trackY = y + 12;
            int trackH = 6;
            
            // Draw track
            g.FillRectangle(x, trackY, width, trackH, 0xFF1A1A1A);
            g.DrawRectangle(x, trackY, width, trackH, 0xFF3F3F3F, 1);
            
            // Draw filled portion
            int fillW = (int)(width * value);
            if (fillW > 0) {
                g.FillRectangle(x, trackY, fillW, trackH, 0xFF4A8FD8);
            }
            
            // Draw thumb
            int thumbX = x + (int)(width * value) - 8;
            int thumbY = y + 8;
            int thumbSize = 16;
            
            g.FillRectangle(thumbX, thumbY, thumbSize, thumbSize, 0xFF4A8FD8);
            g.DrawRectangle(thumbX, thumbY, thumbSize, thumbSize, 0xFF6AAFF8, 2);
        }
        
        private void ApplySettings() {
            // Apply all settings
            UISettings.EnableFadeAnimations = _enableFadeAnimations;
            UISettings.FadeInDurationMs = _fadeInDuration;
            UISettings.FadeOutDurationMs = _fadeOutDuration;
            UISettings.EnableWindowSlideAnimations = _enableWindowSlideAnimations;
            UISettings.WindowSlideDurationMs = _windowSlideDuration;
            
            // Show notification
            NotificationManager.Add(new Notify("Visual effects settings applied", NotificationLevel.None));
            
            // Note: Frame delay, animation speed, and mouse responsive time would need
            // to be integrated into Program.cs and other parts of the codebase
            // For now, they're just stored in this window
        }
        
        private void ResetToDefaults() {
            _enableFadeAnimations = false;
            _fadeInDuration = 180;
            _fadeOutDuration = 180;
            _enableWindowSlideAnimations = false;
            _windowSlideDuration = 220;
            _windowFrameDelay = 2;
            _animationSpeed = 150;
            _mouseResponsiveMs = 100;
            
            NotificationManager.Add(new Notify("Settings reset to defaults", NotificationLevel.None));
        }
    }
}
