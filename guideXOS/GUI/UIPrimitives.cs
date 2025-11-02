using guideXOS.Graph;
using guideXOS.Kernel.Drivers;

namespace guideXOS.GUI {
    // Lightweight UI drawing helpers for rounded rectangles using only basic primitives
    internal static class UIPrimitives {
        private static Graphics G => Framebuffer.Graphics;

        public static void AFillRoundedRect(int x, int y, int w, int h, uint color, int radius = 4) {
            if (w <= 0 || h <= 0) return;
            if (radius < 1) { G.AFillRectangle(x, y, w, h, color); return; }
            if (radius * 2 > w) radius = w / 2;
            if (radius * 2 > h) radius = h / 2;

            // Top rounded rows
            for (int i = 0; i < radius; i++) {
                int inset = CornerInset(radius, i);
                int yy = y + i;
                int ww = w - inset * 2;
                if (ww > 0) G.AFillRectangle(x + inset, yy, ww, 1, color);
            }
            // Middle full rows
            int middleH = h - radius * 2;
            if (middleH > 0) G.AFillRectangle(x, y + radius, w, middleH, color);
            // Bottom rounded rows
            for (int i = 0; i < radius; i++) {
                int inset = CornerInset(radius, radius - 1 - i);
                int yy = y + h - radius + i;
                int ww = w - inset * 2;
                if (ww > 0) G.AFillRectangle(x + inset, yy, ww, 1, color);
            }
        }

        // Only top corners rounded (for title bars)
        public static void AFillRoundedRectTop(int x, int y, int w, int h, uint color, int radius = 4) {
            if (w <= 0 || h <= 0) return;
            if (radius < 1) { G.AFillRectangle(x, y, w, h, color); return; }
            if (radius * 2 > w) radius = w / 2;
            if (radius > h) radius = h;

            for (int i = 0; i < radius; i++) {
                int inset = CornerInset(radius, i);
                int yy = y + i;
                int ww = w - inset * 2;
                if (ww > 0) G.AFillRectangle(x + inset, yy, ww, 1, color);
            }
            int remaining = h - radius;
            if (remaining > 0) G.AFillRectangle(x, y + radius, w, remaining, color);
        }

        // Only bottom corners rounded (for body bottoms)
        public static void AFillRoundedRectBottom(int x, int y, int w, int h, uint color, int radius = 4) {
            if (w <= 0 || h <= 0) return;
            if (radius < 1) { G.AFillRectangle(x, y, w, h, color); return; }
            if (radius * 2 > w) radius = w / 2;
            if (radius > h) radius = h;

            int middle = h - radius;
            if (middle > 0) G.AFillRectangle(x, y, w, middle, color);
            for (int i = 0; i < radius; i++) {
                int inset = CornerInset(radius, radius - 1 - i);
                int yy = y + middle + i;
                int ww = w - inset * 2;
                if (ww > 0) G.AFillRectangle(x + inset, yy, ww, 1, color);
            }
        }

        public static void DrawRoundedRect(int x, int y, int w, int h, uint color, int thickness = 1, int radius = 4) {
            if (w <= 0 || h <= 0 || thickness <= 0) return;
            if (radius < 1) { G.DrawRectangle(x, y, w, h, color, thickness); return; }
            if (radius * 2 > w) radius = w / 2;
            if (radius * 2 > h) radius = h / 2;

            // Draw horizontal edges
            for (int t = 0; t < thickness; t++) {
                // top
                int yTop = y + t;
                int yBot = y + h - 1 - t;
                G.FillRectangle(x + radius, yTop, w - radius * 2, 1, color);
                // bottom
                G.FillRectangle(x + radius, yBot, w - radius * 2, 1, color);
                // left
                int xLeft = x + t;
                int xRight = x + w - 1 - t;
                G.FillRectangle(xLeft, y + radius, 1, h - radius * 2, color);
                // right
                G.FillRectangle(xRight, y + radius, 1, h - radius * 2, color);
            }

            // Approximate corner arcs with short 45-degree steps
            for (int i = 0; i < radius; i++) {
                int inset = CornerInset(radius, i);
                // top-left and top-right pixels
                G.FillRectangle(x + inset, y + i, 1, 1, color);
                G.FillRectangle(x + w - 1 - inset, y + i, 1, 1, color);
                // bottom-left and bottom-right
                G.FillRectangle(x + inset, y + h - 1 - i, 1, 1, color);
                G.FillRectangle(x + w - 1 - inset, y + h - 1 - i, 1, 1, color);
            }
        }

        // Simple inset lookup approximating a quarter-circle for small radii
        private static int CornerInset(int radius, int step) {
            // Precomputed maps for common small radii
            if (radius <= 2) { int[] map = { 1, 0 }; return map[step]; }
            if (radius == 3) { int[] map = { 2, 1, 0 }; return map[step]; }
            if (radius == 4) { int[] map = { 3, 2, 1, 0 }; return map[step]; }
            if (radius == 5) { int[] map = { 4, 3, 2, 1, 0 }; return map[step]; }
            // Fallback linear inset
            return (radius - 1 - step);
        }
    }
}
