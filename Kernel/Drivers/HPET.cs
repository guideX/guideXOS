//https://wiki.osdev.org/HPET#Interrupt_routing
using static guideXOS.Misc.MMIO;
namespace guideXOS.Kernel.Drivers {
    /// <summary>
    /// HPET
    /// </summary>
    public static unsafe class HPET {
        /// <summary>
        /// Clock
        /// </summary>
        public static ulong Clock;
        /// <summary>
        /// Ticks
        /// </summary>
        public static ulong Ticks {
            get {
                return ReadRegister(0xF0);
            }
            set {
                WriteRegister(0x10, 0);
                WriteRegister(0xF0, value);
                WriteRegister(0x10, 1);
            }
        }
        /// <summary>
        /// Initialize
        /// </summary>
        public static void Initialize() {
            if (ACPI.HPET == null) {
                //Console.WriteLine("[HPET] HPET not found!");
                return;
            }
            //1 Femtosecond= 1e-15 sec
            Clock = (ReadRegister(0) >> 32);
            WriteRegister(0x10, 0);
            WriteRegister(0xF0, 0);
            WriteRegister(0x10, 1);
            //Console.WriteLine("[HPET] HPET Initialized");
        }
        /// <summary>
        /// Write Register
        /// </summary>
        /// <param name="reg"></param>
        /// <param name="value"></param>
        public static void WriteRegister(ulong reg, ulong value) {
            Out64((ulong*)(ACPI.HPET->Addresses.Address + reg), value);
        }

        public static ulong ReadRegister(ulong reg) {
            return In64((ulong*)(ACPI.HPET->Addresses.Address + reg));
        }

        public static void Wait(ulong Millionseconds) {
            WaitMicrosecond(Millionseconds * 10000);
        }

        public static void WaitMicrosecond(ulong Microsecond) {
            Ticks = 0;
            ulong Until = Ticks + (Microsecond * 1000000000) / Clock;
            while (Ticks < Until) Native.Nop();
        }
    }
}