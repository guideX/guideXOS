using guideXOS.FS;
using guideXOS.Kernel.Drivers;
using Internal.Runtime.CompilerHelpers;
using System;
using System.Runtime;
using System.Runtime.InteropServices;
namespace guideXOS.Misc {
    internal static unsafe class EntryPoint {
        [RuntimeExport("Entry")]
        public static void Entry(MultibootInfo* Info, IntPtr Modules, IntPtr Trampoline) {
            Allocator.Initialize((IntPtr)0x20000000);

            StartupCodeHelpers.InitializeModules(Modules);

            PageTable.Initialise();

            ASC16.Initialise();

            VBEInfo* info = (VBEInfo*)Info->VBEInfo;
            if (info->PhysBase != 0) {
                Framebuffer.Initialize(info->ScreenWidth, info->ScreenHeight, (uint*)info->PhysBase);
                Framebuffer.Graphics.Clear(0x0);
            } else {
                for (; ; ) Native.Hlt();
            }

            // Boot splash init
            BootSplash.Initialize("Team Nexgen", "guideXOS", "Version: 0.2");

            Console.Setup();
            IDT.Disable();
            GDT.Initialise();

            // Set initial kernel stack for privilege transitions
            {
                const ulong kStackSize = 64 * 1024;
                ulong rsp0 = (ulong)Allocator.Allocate(kStackSize) + kStackSize;
                GDT.SetKernelStack(rsp0);
            }

            IDT.Initialize();

            // Make syscall vector user-accessible via int 0x80
            IDT.AllowUserSoftwareInterrupt(0x80);

            Interrupts.Initialize();
            IDT.Enable();

            SSE.enable_sse();
			// Initialize Serial early for debugging output
            Serial.Initialise();
            Serial.Write("[BOOT] Serial console initialized\r\n");

            // CPU Feature Detection - Using safe pointer-based implementation
            Serial.Write("[BOOT] Detecting CPU features...\r\n");
            Console.WriteLine("[CPU] Detecting features...");

            if (CPUIDHelperSafe.IsCPUIDSupported()) {
                CPUIDHelperSafe.PrintCPUInfo();
                
                // Check for SSE support before enabling
                if (CPUIDHelperSafe.HasSSE()) {
                    Serial.Write("[BOOT] Enabling SSE...\r\n");
                    SSE.enable_sse();
                    Console.WriteLine("[CPU] SSE enabled");
                    Serial.Write("[BOOT] SSE enabled successfully\r\n");
                } else {
                    Console.WriteLine("[CPU] WARNING: SSE not supported on this CPU!");
                    Serial.Write("[BOOT] ERROR: SSE not supported!\r\n");
                    // SSE is required for floating point operations in modern C#
                    // If SSE is not available, we need to halt or provide fallback
                    Console.WriteLine("[CPU] ERROR: guideXOS requires SSE support");
                    Console.WriteLine("[CPU] This CPU is too old to run guideXOS");
                    Console.WriteLine("[CPU] System halted");
                    for (; ; ) Native.Hlt();
                }
                
                // AVX is optional - only enable if supported
                if (CPUIDHelperSafe.HasAVX()) {
                    Serial.Write("[BOOT] AVX support detected but not enabled\r\n");
                    Console.WriteLine("[CPU] AVX support detected (not enabled)");
                    //AVX.init_avx();  // Uncomment when AVX support is stable
                }
                
                // Check other features
                if (!CPUIDHelperSafe.HasPAE()) {
                    Console.WriteLine("[CPU] WARNING: PAE not supported");
                }
                
                if (!CPUIDHelperSafe.HasAPIC()) {
                    Console.WriteLine("[CPU] WARNING: APIC not supported, using PIC fallback");
                }
            } else {
                Console.WriteLine("[CPU] ERROR: CPUID instruction not supported!");
                Serial.Write("[BOOT] ERROR: CPUID not supported!\r\n");
                Console.WriteLine("[CPU] This CPU is too old to run guideXOS");
                Console.WriteLine("[CPU] System halted");
                for (; ; ) Native.Hlt();
            }

            Serial.Write("[BOOT] Initializing ACPI...\r\n");
            ACPI.Initialize();
            
#if UseAPIC
            Serial.Write("[BOOT] Using APIC interrupt controller\r\n");
            PIC.Disable();
            LocalAPIC.Initialize();
            IOAPIC.Initialize();
#else
            Serial.Write("[BOOT] Using PIC interrupt controller\r\n");
            PIC.Enable();
#endif

            Serial.Write("[BOOT] Initializing Timer...\r\n");
            Timer.Initialize();

            Serial.Write("[BOOT] Initializing Keyboard...\r\n");
            Keyboard.Initialize();

            Serial.Write("[BOOT] Initializing PS/2 Controller...\r\n");
			//Serial.Initialise();

            PS2Controller.Initialize();
            
            Serial.Write("[BOOT] Initializing VMware Tools...\r\n");
            VMwareTools.Initialize();

            Serial.Write("[BOOT] Initializing SMBIOS...\r\n");
            SMBIOS.Initialise();

            Serial.Write("[BOOT] Initializing PCI...\r\n");
            PCI.Initialise();

            Serial.Write("[BOOT] Initializing IDE...\r\n");
            IDE.Initialize();
            
            Serial.Write("[BOOT] Initializing SATA...\r\n");
            SATA.Initialize();

            Serial.Write("[BOOT] Initializing ThreadPool...\r\n");
            ThreadPool.Initialize();

            Console.WriteLine($"[SMP] Trampoline: 0x{((ulong)Trampoline).ToString("x2")}");
            //Serial.Write($"[BOOT] SMP Trampoline: 0x{((ulong)Trampoline).ToString("x2")}\r\n");
            Native.Movsb((byte*)SMP.Trampoline, (byte*)Trampoline, 512);

            Serial.Write("[BOOT] Initializing SMP...\r\n");
            SMP.Initialize((uint)SMP.Trampoline);
            Serial.Write("[BOOT] SMP initialized\r\n");

            //Only fixed size vhds are supported!
            Console.Write("[Initrd] Initrd: 0x");
            Console.WriteLine((Info->Mods[0]).ToString("x2"));
            Serial.Write($"[BOOT] Initrd address: 0x{(Info->Mods[0]).ToString("x2")}\r\n");
            
            Console.WriteLine("[Initrd] Initializing Ramdisk");
            Serial.Write("[BOOT] Initializing Ramdisk...\r\n");
            new Ramdisk((IntPtr)(Info->Mods[0]));
            
            // Initialize filesystem: Auto-detect FAT (12/16/32) or TAR
            Serial.Write("[BOOT] Initializing Filesystem...\r\n");
            new AutoFS();
            Serial.Write("[BOOT] Filesystem initialized\r\n");

            // While we are still here (single core boot), animate splash a bit
            Serial.Write("[BOOT] Animating boot splash...\r\n");
            for (int i = 0; i < 120; i++) { // ~2 seconds at 60Hz
                BootSplash.Tick();
            }

            // Cleanup boot splash resources before transitioning to desktop
            Serial.Write("[BOOT] Cleaning up boot splash...\r\n");
            BootSplash.Cleanup();

            Serial.Write("[BOOT] Starting kernel main...\r\n");
            KMain();
        }

        [DllImport("*")]
        public static extern void KMain();
    }
}