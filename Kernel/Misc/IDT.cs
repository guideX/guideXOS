using guideXOS;
using guideXOS.Kernel.Drivers;
using guideXOS.Kernel.Helpers;
using guideXOS.Misc;
using Internal.Runtime.CompilerServices;
using System.Runtime;
using System.Runtime.InteropServices;
using static Internal.Runtime.CompilerHelpers.InteropHelpers;

public static class IDT {
    [DllImport("*")]
    private static extern unsafe void set_idt_entries(void* idt);

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct IDTEntry {
        public ushort BaseLow;
        public ushort Selector;
        public byte Reserved0;
        public byte Type_Attributes;
        public ushort BaseMid;
        public uint BaseHigh;
        public uint Reserved1;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct IDTDescriptor {
        public ushort Limit;
        public ulong Base;
    }

    private static IDTEntry[] idt;
    public static IDTDescriptor idtr;


    public static bool Initialized { get; private set; }


    public static unsafe bool Initialize() {
        idt = new IDTEntry[256];

        set_idt_entries(Unsafe.AsPointer(ref idt[0]));

        fixed (IDTEntry* _idt = idt) {
            idtr.Limit = (ushort)((sizeof(IDTEntry) * 256) - 1);
            idtr.Base = (ulong)_idt;
        }

        Native.Load_IDT(ref idtr);

        Initialized = true;
        return true;
    }

    public static void Enable() {
        Native.Sti();
    }

    public static void Disable() {
        Native.Cli();
    }

    public static unsafe void AllowUserSoftwareInterrupt(byte vector) {
        if (!Initialized) return;
        // Set DPL=3 for the given vector gate to allow int from ring3
        fixed (IDTEntry* p = idt) {
            IDTEntry* e = &p[vector];
            // Type 0xEE? preserve type, set DPL bits (bits 5-6) to 3 and Present bit
            e->Type_Attributes = (byte)((e->Type_Attributes & 0x9F) | (3 << 5) | 0x80);
        }
        Native.Load_IDT(ref idtr);
    }

    public struct RegistersStack {
        public ulong rax;
        public ulong rcx;
        public ulong rdx;
        public ulong rbx;
        public ulong rsi;
        public ulong rdi;
        public ulong r8;
        public ulong r9;
        public ulong r10;
        public ulong r11;
        public ulong r12;
        public ulong r13;
        public ulong r14;
        public ulong r15;
    }

    //https://os.phil-opp.com/returning-from-exceptions/
    public struct InterruptReturnStack {
        public ulong rip;
        public ulong cs;
        public ulong rflags;
        public ulong rsp;
        public ulong ss;
    }

    public struct IDTStackGeneric {
        public RegistersStack rs;
        public ulong errorCode;
        public InterruptReturnStack irs;
    }

    [RuntimeExport("intr_handler")]
    public static unsafe void intr_handler(int irq, IDTStackGeneric* stack) {
        if (irq < 0x20) {
            Panic.Error($"CPU{SMP.ThisCPU} KERNEL PANIC!!!", true);

            // Compute correct location of InterruptReturnStack depending on whether the CPU pushed an error code
            InterruptReturnStack* irs;
            bool hasErrorCode = false;
            switch (irq) {
                case 8:
                case 10:
                case 11:
                case 12:
                case 13:
                case 14:
                case 17:
                case 21:
                case 29:
                case 30:
                    // Exceptions that push an error code: irs follows RegistersStack + errorCode
                    irs = (InterruptReturnStack*)(((byte*)stack) + sizeof(RegistersStack) + sizeof(ulong));
                    hasErrorCode = true;
                    break;
                default:
                    // No error code pushed: irs follows only RegistersStack
                    irs = (InterruptReturnStack*)(((byte*)stack) + sizeof(RegistersStack));
                    hasErrorCode = false;
                    break;
            }

            Console.WriteLine("================== EXCEPTION ==================");
            Console.WriteLine($"Vector: 0x{((uint)irq).ToString("x2")}  CPU: {SMP.ThisCPU}  Ticks: {Timer.Ticks}");

            // Decode exception type FIRST (more prominent)
            switch (irq) {
                case 0: Console.WriteLine("TYPE: DIVIDE BY ZERO"); break;
                case 1: Console.WriteLine("TYPE: SINGLE STEP"); break;
                case 2: Console.WriteLine("TYPE: NMI"); break;
                case 3: Console.WriteLine("TYPE: BREAKPOINT"); break;
                case 4: Console.WriteLine("TYPE: OVERFLOW"); break;
                case 5: Console.WriteLine("TYPE: BOUNDS CHECK"); break;
                case 6: Console.WriteLine("TYPE: INVALID OPCODE"); break;
                case 7: Console.WriteLine("TYPE: COPROCESSOR UNAVAILABLE"); break;
                case 8: Console.WriteLine("TYPE: DOUBLE FAULT"); break;
                case 9: Console.WriteLine("TYPE: COPROCESSOR SEGMENT OVERRUN"); break;
                case 10: Console.WriteLine("TYPE: INVALID TSS"); break;
                case 11: Console.WriteLine("TYPE: SEGMENT NOT FOUND"); break;
                case 12: Console.WriteLine("TYPE: STACK EXCEPTION"); break;
                case 13:
                    Console.WriteLine("TYPE: GENERAL PROTECTION FAULT");
                    if (hasErrorCode) Console.WriteLine($"  GP ERROR CODE: 0x{stack->errorCode.ToString("x16")}");
                    break;
                case 14: {
                        ulong pageFaultAddr = Native.ReadCR2();
                        if (pageFaultAddr < 0x1000) {
                            Console.WriteLine("TYPE: NULL POINTER DEREFERENCE");
                        } else {
                            Console.WriteLine("TYPE: PAGE FAULT");
                        }
                        if (hasErrorCode) {
                            ulong ec = stack->errorCode;
                            // PF EC bits: P(0) W/R(1) U/S(2) RSVD(3) I/D(4) PK(5)
                            Console.WriteLine($"  PF Flags: P={(ec & 1UL)!=0} WR={((ec>>1)&1UL)!=0} US={((ec>>2)&1UL)!=0} RSVD={((ec>>3)&1UL)!=0} ID={((ec>>4)&1UL)!=0} PK={((ec>>5)&1UL)!=0}");
                        }
                        Console.WriteLine($"  Fault Address: 0x{pageFaultAddr.ToString("x16")}");
                        break;
                    }
                case 16: Console.WriteLine("TYPE: COPROCESSOR ERROR"); break;
                case 17: Console.WriteLine("TYPE: ALIGNMENT CHECK"); break;
                case 18: Console.WriteLine("TYPE: MACHINE CHECK"); break;
                case 19: Console.WriteLine("TYPE: SIMD FLOATING POINT"); break;
                case 20: Console.WriteLine("TYPE: VIRTUALIZATION"); break;
                default: Console.WriteLine($"TYPE: UNKNOWN EXCEPTION (Vector 0x{((uint)irq).ToString("x2")})"); break;
            }
            
            Console.WriteLine("");
            Console.WriteLine("--- INSTRUCTION POINTER ---");
            Console.WriteLine($"RIP: 0x{irs->rip.ToString("x16")}  CS: 0x{irs->cs.ToString("x4")}  CPL: {((int)(irs->cs & 3))}");
            Console.WriteLine($"RFLAGS: 0x{irs->rflags.ToString("x16")}");
            Console.WriteLine($"RSP: 0x{irs->rsp.ToString("x16")}  SS: 0x{irs->ss.ToString("x4")}");
            if (hasErrorCode) Console.WriteLine($"ERROR CODE: 0x{stack->errorCode.ToString("x16")}");

            Console.WriteLine("");
            Console.WriteLine("--- GENERAL PURPOSE REGISTERS ---");
            Console.WriteLine($"RAX: 0x{stack->rs.rax.ToString("x16")}  RBX: 0x{stack->rs.rbx.ToString("x16")}");
            Console.WriteLine($"RCX: 0x{stack->rs.rcx.ToString("x16")}  RDX: 0x{stack->rs.rdx.ToString("x16")}");
            Console.WriteLine($"RSI: 0x{stack->rs.rsi.ToString("x16")}  RDI: 0x{stack->rs.rdi.ToString("x16")}");
            Console.WriteLine($"R8 : 0x{stack->rs.r8.ToString("x16")}  R9 : 0x{stack->rs.r9.ToString("x16")}");
            Console.WriteLine($"R10: 0x{stack->rs.r10.ToString("x16")}  R11: 0x{stack->rs.r11.ToString("x16")}");
            Console.WriteLine($"R12: 0x{stack->rs.r12.ToString("x16")}  R13: 0x{stack->rs.r13.ToString("x16")}");
            Console.WriteLine($"R14: 0x{stack->rs.r14.ToString("x16")}  R15: 0x{stack->rs.r15.ToString("x16")}");

            Console.WriteLine("");
            Console.WriteLine("--- CONTROL REGISTERS ---");
            ulong cr2 = Native.ReadCR2();
            Console.WriteLine($"CR2 (Page Fault): 0x{cr2.ToString("x16")}");
            
            Console.WriteLine("");
            Console.WriteLine("--- DESCRIPTOR TABLES ---");
            Console.WriteLine($"IDTR: Base=0x{((ulong)idtr.Base).ToString("x16")} Limit=0x{((uint)idtr.Limit).ToString("x4")}");
            Console.WriteLine($"GDTR: Base=0x{GDT.gdtr.Base.ToString("x16")} Limit=0x{GDT.gdtr.Limit.ToString("x4")}");

            Console.WriteLine("");
            Console.WriteLine("--- RFLAGS BREAKDOWN ---");
            ulong flags = irs->rflags;
            Console.WriteLine($"CF={((flags>>0)&1)} PF={((flags>>2)&1)} AF={((flags>>4)&1)} ZF={((flags>>6)&1)} SF={((flags>>7)&1)} TF={((flags>>8)&1)} IF={((flags>>9)&1)} DF={((flags>>10)&1)} OF={((flags>>11)&1)} IOPL={((flags>>12)&3)} NT={((flags>>14)&1)} RF={((flags>>16)&1)} VM={((flags>>17)&1)} AC={((flags>>18)&1)} VIF={((flags>>19)&1)} VIP={((flags>>20)&1)} ID={((flags>>21)&1)}");
            
            Console.WriteLine("");
            Console.WriteLine("--- STACK TRACE (Approximate) ---");
            // Try to read a few values from the stack
            ulong* stackPtr = (ulong*)irs->rsp;
            for (int i = 0; i < 8; i++) {
                try {
                    ulong val = stackPtr[i];
                    uint offset = (uint)(i * 8);
                    Console.Write($"[RSP+{offset.ToString("x2")}]: 0x{val.ToString("x16")}");
                    Console.WriteLine("");
                } catch {
                    uint offset = (uint)(i * 8);
                    Console.Write($"[RSP+{offset.ToString("x2")}]: <invalid>");
                    Console.WriteLine("");
                    break;
                }
            }
            
            Console.WriteLine("===============================================");
            Console.WriteLine("System halted. Please reset.");
            Framebuffer.Update();
            for (; ; ) ;
        }

        //DEAD
        if (irq == 0xFD) {
            Native.Cli();
            Native.Hlt();
            for (; ; ) Native.Hlt();
        }

        //For main processor
        if (SMP.ThisCPU == 0) {
            //System calls
            if (irq == 0x80) {
                var pCell = (MethodFixupCell*)stack->rs.rcx;
                string name = string.FromASCII(pCell->Module->ModuleName, StringHelper.StringLength((byte*)pCell->Module->ModuleName));
                stack->rs.rax = (ulong)API.HandleSystemCall(name);
                name.Dispose();
            }
            switch (irq) {
                case 0x20:
                    //misc.asm Schedule_Next
                    if (stack->rs.rdx != 0x61666E6166696E)
                        Timer.OnInterrupt();
                    break;
            }
            Interrupts.HandleInterrupt(irq);
        }

        if (irq == 0x20) {
            ThreadPool.Schedule(stack);
        }

        Interrupts.EndOfInterrupt((byte)irq);
    }
}