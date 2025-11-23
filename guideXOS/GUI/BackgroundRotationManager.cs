using guideXOS.FS;
using guideXOS.Kernel.Drivers;
using guideXOS.Misc;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace guideXOS.GUI {
    /// <summary>
    /// Manages automatic background rotation with fade transitions
    /// </summary>
    internal static class BackgroundRotationManager {
        private static List<string> _backgroundPaths;
        private static int _currentIndex = 0;
        private static ulong _lastRotationTick = 0;
        private static bool _initialized = false;
        
        // Fade transition state
        private static bool _isFading = false;
        private static Image _oldBackground = null;
        private static Image _newBackground = null;
        private static ulong _fadeStartTick = 0;
        private static byte _fadeAlpha = 0;
        
        /// <summary>
        /// Initialize the background rotation manager
        /// </summary>
        public static void Initialize() {
            if (_initialized) return;
            
            _backgroundPaths = new List<string>();
            LoadBackgroundPaths();
            _lastRotationTick = Timer.Ticks;
            _initialized = true;
        }
        
        /// <summary>
        /// Load all available background image paths
        /// </summary>
        private static void LoadBackgroundPaths() {
            _backgroundPaths.Clear();
            
            var files = File.GetFiles(@"Backgrounds/");
            if (files != null && files.Count > 0) {
                for (int i = 0; i < files.Count; i++) {
                    var fi = files[i];
                    if (fi.Attribute != FileAttribute.Directory) {
                        string name = fi.Name;
                        // Check for image files - case insensitive
                        bool isPng = name.EndsWith(".png") || name.EndsWith(".PNG");
                        bool isJpg = name.EndsWith(".jpg") || name.EndsWith(".JPG") || 
                                    name.EndsWith(".jpeg") || name.EndsWith(".JPEG");
                        bool isBmp = name.EndsWith(".bmp") || name.EndsWith(".BMP");
                        
                        // Skip thumbnail files
                        if (name.EndsWith("_thumb.png") || name.EndsWith("_thumb.PNG")) {
                            continue;
                        }
                        
                        if (isPng || isJpg || isBmp) {
                            string path = "Backgrounds/" + name;
                            _backgroundPaths.Add(path);
                        }
                    }
                    fi.Dispose();
                }
                files.Dispose();
            }
        }
        
        /// <summary>
        /// Update rotation logic - call this from main loop
        /// </summary>
        public static void Update() {
            if (!_initialized) Initialize();
            
            // Handle fade transition
            if (_isFading) {
                UpdateFadeTransition();
                return;
            }
            
            // Check if auto-rotation is enabled
            if (!UISettings.EnableAutoBackgroundRotation) return;
            
            // Check if we have backgrounds to rotate
            if (_backgroundPaths.Count <= 1) return;
            
            // Check if enough time has passed
            ulong elapsed = Timer.Ticks >= _lastRotationTick ? 
                           Timer.Ticks - _lastRotationTick : 0;
            ulong intervalMs = (ulong)UISettings.BackgroundRotationIntervalMinutes * 60000;
            
            if (elapsed >= intervalMs) {
                RotateToNext();
                _lastRotationTick = Timer.Ticks;
            }
        }
        
        /// <summary>
        /// Rotate to next background
        /// </summary>
        private static void RotateToNext() {
            if (_backgroundPaths.Count == 0) return;
            
            // Pick next background (random or sequential)
            _currentIndex = (_currentIndex + 1) % _backgroundPaths.Count;
            
            try {
                byte[] data = File.ReadAllBytes(_backgroundPaths[_currentIndex]);
                if (data != null) {
                    var img = new PNG(data);
                    data.Dispose();
                    var resized = img.ResizeImage(Framebuffer.Width, Framebuffer.Height);
                    img.Dispose();
                    
                    // Check if fade transition is enabled
                    if (UISettings.EnableBackgroundFadeTransition && Program.Wallpaper != null) {
                        StartFadeTransition(resized);
                    } else {
                        // Instant change
                        if (Program.Wallpaper != null) Program.Wallpaper.Dispose();
                        Program.Wallpaper = resized;
                    }
                }
            } catch {
                // Failed to load background, try next one on next rotation
            }
        }
        
        /// <summary>
        /// Start fade transition between backgrounds
        /// </summary>
        private static void StartFadeTransition(Image newBackground) {
            _isFading = true;
            _oldBackground = Program.Wallpaper;
            _newBackground = newBackground;
            _fadeStartTick = Timer.Ticks;
            _fadeAlpha = 0;
        }
        
        /// <summary>
        /// Update fade transition animation
        /// </summary>
        private static void UpdateFadeTransition() {
            if (!_isFading) return;
            
            ulong elapsed = Timer.Ticks >= _fadeStartTick ? 
                           Timer.Ticks - _fadeStartTick : 0;
            int duration = UISettings.BackgroundFadeDurationMs;
            
            // Calculate alpha (0-255)
            float t = duration > 0 ? (float)elapsed / duration : 1.0f;
            if (t > 1.0f) t = 1.0f;
            
            _fadeAlpha = (byte)(t * 255);
            
            // Complete transition
            if (t >= 1.0f) {
                CompleteFadeTransition();
            }
        }
        
        /// <summary>
        /// Complete the fade transition
        /// </summary>
        private static void CompleteFadeTransition() {
            _isFading = false;
            
            if (_oldBackground != null) {
                _oldBackground.Dispose();
                _oldBackground = null;
            }
            
            Program.Wallpaper = _newBackground;
            _newBackground = null;
            _fadeAlpha = 0;
        }
        
        /// <summary>
        /// Draw the current background with fade effect if active
        /// </summary>
        public static void DrawBackground() {
            if (_isFading && _oldBackground != null && _newBackground != null) {
                // Draw old background
                Framebuffer.Graphics.DrawImage(0, 0, _oldBackground, false);
                
                // Draw new background with alpha blending
                DrawImageWithAlpha(0, 0, _newBackground, _fadeAlpha);
            } else if (Program.Wallpaper != null) {
                // Draw regular wallpaper
                Framebuffer.Graphics.DrawImage(0, 0, Program.Wallpaper, false);
            } else {
                // Fill with default color
                Framebuffer.Graphics.FillRectangle(0, 0, Framebuffer.Width, Framebuffer.Height, 0xFF1E1E1E);
            }
        }
        
        /// <summary>
        /// Draw image with alpha blending (simple implementation)
        /// </summary>
        private static void DrawImageWithAlpha(int x, int y, Image img, byte alpha) {
            if (img == null) return;
            
            // Simple alpha blending - blend each pixel
            for (int py = 0; py < img.Height; py++) {
                for (int px = 0; px < img.Width; px++) {
                    int sx = x + px;
                    int sy = y + py;
                    
                    if (sx >= 0 && sx < Framebuffer.Width && sy >= 0 && sy < Framebuffer.Height) {
                        uint newPixel = (uint)img.RawData[py * img.Width + px];
                        uint oldPixel = Framebuffer.Graphics.GetPoint(sx, sy);
                        
                        // Extract ARGB components
                        int newA = (int)((newPixel >> 24) & 0xFF);
                        int newR = (int)((newPixel >> 16) & 0xFF);
                        int newG = (int)((newPixel >> 8) & 0xFF);
                        int newB = (int)(newPixel & 0xFF);
                        
                        int oldA = (int)((oldPixel >> 24) & 0xFF);
                        int oldR = (int)((oldPixel >> 16) & 0xFF);
                        int oldG = (int)((oldPixel >> 8) & 0xFF);
                        int oldB = (int)(oldPixel & 0xFF);
                        
                        // Blend using alpha
                        float t = alpha / 255.0f;
                        int outA = (int)(oldA + (newA - oldA) * t);
                        int outR = (int)(oldR + (newR - oldR) * t);
                        int outG = (int)(oldG + (newG - oldG) * t);
                        int outB = (int)(oldB + (newB - oldB) * t);
                        
                        // Clamp values
                        if (outA > 255) outA = 255;
                        if (outR > 255) outR = 255;
                        if (outG > 255) outG = 255;
                        if (outB > 255) outB = 255;
                        
                        uint blendedPixel = (uint)((outA << 24) | (outR << 16) | (outG << 8) | outB);
                        Framebuffer.Graphics.DrawPoint(sx, sy, blendedPixel);
                    }
                }
            }
        }
        
        /// <summary>
        /// Reload background paths (call when new backgrounds are added)
        /// </summary>
        public static void ReloadBackgrounds() {
            LoadBackgroundPaths();
            _currentIndex = 0;
        }
        
        /// <summary>
        /// Force immediate rotation to next background
        /// </summary>
        public static void ForceRotateNext() {
            if (_isFading) return; // Don't rotate while fading
            RotateToNext();
            _lastRotationTick = Timer.Ticks;
        }
        
        /// <summary>
        /// Get count of available backgrounds
        /// </summary>
        public static int GetBackgroundCount() {
            return _backgroundPaths.Count;
        }
    }
}
