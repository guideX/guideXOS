namespace guideXOS.GUI {
    // Global UI settings for animations
    internal static class UISettings {
        // Fading animations (open/close) 
        public static bool EnableFadeAnimations = false; // enable by default
        public static int FadeInDurationMs = 180;
        public static int FadeOutDurationMs = 180;

        // Window slide animations (minimize/restore)
        public static bool EnableWindowSlideAnimations = false; // can be enabled later
        public static int WindowSlideDurationMs = 220;
        
        // Background rotation settings
        public static bool EnableAutoBackgroundRotation = false;
        public static int BackgroundRotationIntervalMinutes = 5; // 5 minutes default
        public static bool EnableBackgroundFadeTransition = true;
        public static int BackgroundFadeDurationMs = 1000; // 1 second fade
    }
}
