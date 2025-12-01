using System.Drawing;
using System.Collections.Generic;

namespace guideXOS.Misc {
    /// <summary>
    /// Simple SVG rasterizer for guideXOS icons
    /// Supports basic rect, circle elements with fill colors
    /// This is a minimal implementation for icon rendering
    /// </summary>
    public unsafe class SVG : Image {
        private struct SVGElement {
            public int Type; // 0=rect, 1=circle, 2=ellipse
            public uint FillColor;
            public float X, Y, Width, Height, Rx, Ry, R;
            public float CX, CY;
        }

        private static void BuildFallback(out int[] data, out int w, out int h) {
            w = 8; h = 8; data = new int[w * h];
            // simple checker pattern (cyan/blue) to indicate SVG
            for (int y = 0; y < h; y++) {
                for (int x = 0; x < w; x++) {
                    bool c = ((x ^ y) & 1) == 0;
                    data[y * w + x] = c ? unchecked((int)0xFF00FFFF) : unchecked((int)0xFF0000FF);
                }
            }
        }

        /// <summary>
        /// Parse SVG from byte array
        /// </summary>
        /// <param name="data">SVG file data</param>
        /// <param name="targetWidth">Desired width</param>
        /// <param name="targetHeight">Desired height</param>
        public SVG(byte[] data, int targetWidth = 32, int targetHeight = 32) {
            try {
                // Convert bytes to string using simple ASCII conversion
                char[] chars = new char[data.Length];
                for (int i = 0; i < data.Length; i++) {
                    chars[i] = (char)data[i];
                }
                string svg = new string(chars);
                chars.Dispose();
                
                // Set dimensions
                Width = targetWidth > 0 ? targetWidth : 32;
                Height = targetHeight > 0 ? targetHeight : 32;
                Bpp = 4;
                RawData = new int[Width * Height];
                
                // Clear background (transparent)
                for (int i = 0; i < RawData.Length; i++) {
                    RawData[i] = 0x00000000;
                }
                
                // Parse and render simple elements
                RenderSimpleSVG(svg);
                
                svg.Dispose();
            } catch {
                BuildFallback(out RawData, out int fw, out int fh);
                Width = fw;
                Height = fh;
                Bpp = 4;
            }
        }

        private void RenderSimpleSVG(string svg) {
            // Very simple parser - just look for rect and circle elements with fill colors
            // This is enough for basic icon rendering
            
            int pos = 0;
            while (pos < svg.Length) {
                // Find next element
                int rectIdx = FindString(svg, "<rect", pos);
                int circleIdx = FindString(svg, "<circle", pos);
                
                int nextIdx = -1;
                int elementType = -1; // 0=rect, 1=circle
                
                if (rectIdx >= 0 && (circleIdx < 0 || rectIdx < circleIdx)) {
                    nextIdx = rectIdx;
                    elementType = 0;
                } else if (circleIdx >= 0) {
                    nextIdx = circleIdx;
                    elementType = 1;
                } else {
                    break; // No more elements
                }
                
                // Find end of element
                int endIdx = FindChar(svg, '>', nextIdx);
                if (endIdx < 0) break;
                
                // Extract element string
                string element = svg.Substring(nextIdx, endIdx - nextIdx + 1);
                
                // Parse and render based on type
                if (elementType == 0) {
                    RenderRectElement(element);
                } else if (elementType == 1) {
                    RenderCircleElement(element);
                }
                
                element.Dispose();
                pos = endIdx + 1;
            }
        }

        private int FindString(string str, string search, int startPos) {
            if (startPos >= str.Length) return -1;
            for (int i = startPos; i <= str.Length - search.Length; i++) {
                bool match = true;
                for (int j = 0; j < search.Length; j++) {
                    if (str[i + j] != search[j]) {
                        match = false;
                        break;
                    }
                }
                if (match) return i;
            }
            return -1;
        }

        private int FindChar(string str, char c, int startPos) {
            for (int i = startPos; i < str.Length; i++) {
                if (str[i] == c) return i;
            }
            return -1;
        }

        private void RenderRectElement(string element) {
            float x = ParseFloatAttr(element, "x=");
            float y = ParseFloatAttr(element, "y=");
            float w = ParseFloatAttr(element, "width=");
            float h = ParseFloatAttr(element, "height=");
            uint color = ParseColorAttr(element);
            
            if (w <= 0 || h <= 0) return;
            
            // Scale to target size (assume 24x24 viewbox)
            float scaleX = Width / 24.0f;
            float scaleY = Height / 24.0f;
            
            int x1 = (int)(x * scaleX);
            int y1 = (int)(y * scaleY);
            int x2 = (int)((x + w) * scaleX);
            int y2 = (int)((y + h) * scaleY);
            
            for (int py = y1; py < y2 && py < Height; py++) {
                if (py < 0) continue;
                for (int px = x1; px < x2 && px < Width; px++) {
                    if (px < 0) continue;
                    SetPixel(px, py, color);
                }
            }
        }

        private void RenderCircleElement(string element) {
            float cx = ParseFloatAttr(element, "cx=");
            float cy = ParseFloatAttr(element, "cy=");
            float r = ParseFloatAttr(element, "r=");
            uint color = ParseColorAttr(element);
            
            if (r <= 0) return;
            
            // Scale to target size
            float scaleX = Width / 24.0f;
            float scaleY = Height / 24.0f;
            float scale = (scaleX + scaleY) / 2.0f;
            
            float centerX = cx * scaleX;
            float centerY = cy * scaleY;
            float radius = r * scale;
            
            int x1 = (int)(centerX - radius);
            int y1 = (int)(centerY - radius);
            int x2 = (int)(centerX + radius);
            int y2 = (int)(centerY + radius);
            
            for (int py = y1; py <= y2 && py < Height; py++) {
                if (py < 0) continue;
                for (int px = x1; px <= x2 && px < Width; px++) {
                    if (px < 0) continue;
                    float dx = px - centerX;
                    float dy = py - centerY;
                    if (dx * dx + dy * dy <= radius * radius) {
                        SetPixel(px, py, color);
                    }
                }
            }
        }

        private float ParseFloatAttr(string element, string attrName) {
            int idx = FindString(element, attrName, 0);
            if (idx < 0) return 0;
            
            idx += attrName.Length;
            // Skip quote if present
            if (idx < element.Length && element[idx] == '"') idx++;
            
            // Extract number
            string numStr = "";
            while (idx < element.Length) {
                char c = element[idx];
                if ((c >= '0' && c <= '9') || c == '.' || c == '-') {
                    numStr += c;
                } else {
                    break;
                }
                idx++;
            }
            
            if (numStr.Length == 0) return 0;
            
            // Simple float parser
            float result = 0;
            bool negative = false;
            bool afterDecimal = false;
            float decimalPlace = 0.1f;
            
            for (int i = 0; i < numStr.Length; i++) {
                char c = numStr[i];
                if (c == '-') {
                    negative = true;
                } else if (c == '.') {
                    afterDecimal = true;
                } else if (c >= '0' && c <= '9') {
                    int digit = c - '0';
                    if (afterDecimal) {
                        result += digit * decimalPlace;
                        decimalPlace /= 10.0f;
                    } else {
                        result = result * 10 + digit;
                    }
                }
            }
            
            numStr.Dispose();
            return negative ? -result : result;
        }

        private uint ParseColorAttr(string element) {
            // Look for fill="#RRGGBB"
            int fillIdx = FindString(element, "fill", 0);
            if (fillIdx < 0) return 0xFF808080;
            
            int hashIdx = FindChar(element, '#', fillIdx);
            if (hashIdx < 0) return 0xFF808080;
            
            hashIdx++; // Skip #
            
            // Parse 6 hex digits
            uint color = 0;
            for (int i = 0; i < 6 && hashIdx + i < element.Length; i++) {
                char c = element[hashIdx + i];
                uint digit = 0;
                if (c >= '0' && c <= '9') digit = (uint)(c - '0');
                else if (c >= 'a' && c <= 'f') digit = (uint)(c - 'a' + 10);
                else if (c >= 'A' && c <= 'F') digit = (uint)(c - 'A' + 10);
                else break;
                color = (color << 4) | digit;
            }
            
            return 0xFF000000 | color; // Add alpha
        }

        private void SetPixel(int x, int y, uint color) {
            if (x < 0 || x >= Width || y < 0 || y >= Height) return;
            
            int idx = y * Width + x;
            uint alpha = (color >> 24) & 0xFF;
            
            if (alpha == 0xFF) {
                RawData[idx] = (int)color;
            } else if (alpha > 0) {
                // Simple alpha blending
                uint srcColor = color & 0x00FFFFFF;
                uint dstColor = (uint)RawData[idx];
                uint invAlpha = 255 - alpha;
                uint r = (((srcColor >> 16) & 0xFF) * alpha + ((dstColor >> 16) & 0xFF) * invAlpha) / 255;
                uint g = (((srcColor >> 8) & 0xFF) * alpha + ((dstColor >> 8) & 0xFF) * invAlpha) / 255;
                uint b = ((srcColor & 0xFF) * alpha + (dstColor & 0xFF) * invAlpha) / 255;
                RawData[idx] = (int)(0xFF000000 | (r << 16) | (g << 8) | b);
            }
        }
    }
}
