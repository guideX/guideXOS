using guideXOS.Misc;

namespace guideXOS.Kernel.Drivers {
    // Minimal USB Mass Storage recognizer (Bulk-Only) that registers the device for UI purposes.
    public static class USBMSC {
        public static void Initialize(USBDevice device) {
            // Class 0x08 = Mass Storage; SubClass 0x06 = SCSI transparent; Protocol 0x50 = Bulk-Only
            // We only register the device for now. A full BOT/SCSI implementation can be added later.
            USBStorage.Register(device);
        }
    }
}
