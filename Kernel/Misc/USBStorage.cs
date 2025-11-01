using guideXOS.Misc;
using guideXOS.GUI;

namespace guideXOS.Kernel.Drivers {
    // Registry of connected USB mass-storage devices to surface icons in UI.
    public static class USBStorage {
        private static int _count;
        public static int Count => _count;

        // Called by driver when a mass storage device is enumerated
        public static void Register(USBDevice dev) { _count++; }
        // Called when device removed (not implemented yet)
        public static void Unregister(USBDevice dev) { if (_count > 0) _count--; }
    }
}
