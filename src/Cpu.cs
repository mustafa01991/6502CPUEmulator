using System;
using System.Collections.Generic;

using static _6502CpuEmulator.CpuInstruction;
using static _6502CpuEmulator.CpuAddressingMode;
using static _6502CpuEmulator.CpuInstructionType;

/// <summary>
/// Project: 6502 CPU Emulator
/// Author: Mustafa Al-Ghayeb
/// Licence: MIT license
/// </summary>
namespace _6502CpuEmulator {
    
    // https://en.wikipedia.org/wiki/MOS_Technology_6502
    // http://archive.6502.org/datasheets/rockwell_r650x_r651x.pdf
    
    enum CpuInstruction {
        ADC, // Add with Carry
        AND, // Logical AND
        ASL, // Arithmetic Shift Left

        BCC, // Branch if Carry Clear
        BCS, // Branch if Carry Set
        BEQ, // Branch if Equal
        BIT, // Bit Test
        BMI, // Branch if Minus
        BNE, // Branch if Not Equal
        BPL, // Branch if Positive
        BRK, // Force Interrupt
        BVC, // Branch if Overflow Clear 
        BVS, // Branch if Overflow Set

        CLC, // Clear Carry Flag
        CLD, // Clear Decimal Mode
        CLI, // Clear Interrupt Disable
        CLV, // Clear Overflow Flag
        CMP, // Compare
        CPX, // Compare X Register
        CPY, // Compare Y Register
        
        DEC, // Decrement Memory
        DEX, // Decrement X Register
        DEY, // Decrement Y Register

        EOR, // Exclusive OR

        INC, // Increment Memory
        INX, // Increment X Register
        INY, // Increment Y Register

        JMP, // Jump
        JSR, // Jump to Subroutine
        
        LDA, // Load Accumulator
        LDX, // Load X Register
        LDY, // Load Y Register
        LSR, // Logical Shift Right

        NOP, // No Operation
        
        ORA, // Logical Inclusive OR

        PHA, // Push Accumulator
        PHP, // Push Processor Status
        PLA, // Pull Accumulator
        PLP, // Pull Processor Status

        ROL, // Rotate Left
        ROR, // Rotate Right
        RTI, // Return from Interrupt
        RTS, // Return from Subroutine

        SBC, // Subtract with Carry
        SEC, // Set Carry Flag
        SED, // Set Decimal Flag
        SEI, // Set Interrupt Disable
        STA, // Store Accumulator
        STX, // Store X Register
        STY, // Store Y Register

        TAX, // Transfer Accumulator to X
        TAY, // Transfer Accumulator to Y
        TSX, // Transfer Stack Pointer to X
        TXA, // Transfer X to Accumulator
        TXS, // Transfer X to Stack Pointer
        TYA  // Transfer Y to Accumulator
    }
    enum CpuAddressingMode { 
        Implied,                        // All Commands
        Accumulator,       // A
        Immediate,         // #
        ZeroPage,          // zpg
        ZeroPageX,         // zpg,X
        ZeroPageY,         // zpg,Y
        Relative,          // rel       // All Branches
        Absolute,          // abs
        AbsoluteX,         // abs,X
        AbsoluteY,         // abs,Y
        Indirect,          // ind
        IndirectX,         // ind,X
        IndirectY          // ind,Y
    }
    enum CpuInstructionType {
        Command,    // Implied only
        Branch,     // Relative only

        Argument,               // Possible Immediate ; No Accumulator
        MemoryWrite,            // ; No Immediate, Accumulator
        AccumulatorWrite,       // Possible Accumulator ; No Immediate
    }

    
    // MOS Technology 6502
    class Cpu {

        public bool debug = false;

        /// Instruction length of "1 byte mean 0 arg" and "3 byte mean 2 args"
        ///  (CpuInstruction, CpuAddressingMode, cycles)
        static readonly Dictionary<byte, (CpuInstruction, CpuAddressingMode, byte)> decodeInstruction = new Dictionary<byte, (CpuInstruction, CpuAddressingMode, byte)> {
            { 0x00, (BRK, Implied, 7)},
            { 0x01, (ORA, IndirectX, 6)},
            { 0x05, (ORA, ZeroPage, 3)},
            { 0x06, (ASL, ZeroPage, 5)},
            { 0x08, (PHP, Implied, 3)},
            { 0x09, (ORA, Immediate, 2)},
            { 0x0A, (ASL, Accumulator, 2)},
            { 0x0D, (ORA, Absolute, 4)},
            { 0x0E, (ASL, Absolute, 6)},

            { 0x10, (BPL, Relative, 2)},
            { 0x11, (ORA, IndirectY, 5)},
            { 0x15, (ORA, ZeroPageX, 4)},
            { 0x16, (ASL, ZeroPageX, 6)},
            { 0x18, (CLC, Implied, 2)},
            { 0x19, (ORA, AbsoluteY, 4)},
            { 0x1D, (ORA, AbsoluteX, 4)},
            { 0x1E, (ASL, AbsoluteX, 7)},

            { 0x20, (JSR, Absolute, 6)},
            { 0x21, (AND, IndirectX, 6)},
            { 0x24, (BIT, ZeroPage, 3)},
            { 0x25, (AND, ZeroPage, 3)},
            { 0x26, (ROL, ZeroPage, 5)},
            { 0x28, (PLP, Implied, 4)},
            { 0x29, (AND, Immediate, 2)},
            { 0x2A, (ROL, Accumulator, 2)},
            { 0x2C, (BIT, Absolute, 4)},
            { 0x2D, (AND, Absolute, 4)},
            { 0x2E, (ROL, Absolute, 6)},

            { 0x30, (BMI, Relative, 2)},
            { 0x31, (AND, IndirectY, 5)},
            { 0x35, (AND, ZeroPageX, 4)},
            { 0x36, (ROL, ZeroPageX, 6)},
            { 0x38, (SEC, Implied, 2)},
            { 0x39, (AND, AbsoluteY, 4)},
            { 0x3D, (AND, AbsoluteX, 4)},
            { 0x3E, (ROL, AbsoluteX, 7)},

            { 0x40, (RTI, Implied, 6)},
            { 0x41, (EOR, IndirectX, 6)},
            { 0x45, (EOR, ZeroPage, 3)},
            { 0x46, (LSR, ZeroPage, 5)},
            { 0x48, (PHA, Implied, 3)},
            { 0x49, (EOR, Immediate, 2)},
            { 0x4A, (LSR, Accumulator, 2)},
            { 0x4C, (JMP, Absolute, 3)},
            { 0x4D, (EOR, Absolute, 4)},
            { 0x4E, (LSR, Absolute, 6)},

            { 0x50, (BVC, Relative, 2)},
            { 0x51, (EOR, IndirectY, 5)},
            { 0x55, (EOR, ZeroPageX, 4)},
            { 0x56, (LSR, ZeroPageX, 6)},
            { 0x58, (CLI, Implied, 2)},
            { 0x59, (EOR, AbsoluteY, 4)},
            { 0x5D, (EOR, AbsoluteX, 4)},
            { 0x5E, (LSR, AbsoluteX, 7)},

            { 0x60, (RTS, Implied, 6)},
            { 0x61, (ADC, IndirectX, 6)},
            { 0x65, (ADC, ZeroPage, 3)},
            { 0x66, (ROR, ZeroPage, 5)},
            { 0x68, (PLA, Implied, 4)},
            { 0x69, (ADC, Immediate, 2)},
            { 0x6A, (ROR, Accumulator, 2)},
            { 0x6C, (JMP, Indirect, 5)},
            { 0x6D, (ADC, Absolute, 4)},
            { 0x6E, (ROR, Absolute, 6)},

            { 0x70, (BVS, Relative, 2)},
            { 0x71, (ADC, IndirectY, 5)},
            { 0x75, (ADC, ZeroPageX, 4)},
            { 0x76, (ROR, ZeroPageX, 6)},
            { 0x78, (SEI, Implied, 2)},
            { 0x79, (ADC, AbsoluteY, 4)},
            { 0x7D, (ADC, AbsoluteX, 4)},
            { 0x7E, (ROR, AbsoluteX, 7)},

            { 0x81, (STA, IndirectX, 6)},
            { 0x84, (STY, ZeroPage, 3)},
            { 0x85, (STA, ZeroPage, 3)},
            { 0x86, (STX, ZeroPage, 3)},
            { 0x88, (DEY, Implied, 2)},
            { 0x8A, (TXA, Implied, 2)},
            { 0x8C, (STY, Absolute, 4)},
            { 0x8D, (STA, Absolute, 4)},
            { 0x8E, (STX, Absolute, 4)},

            { 0x90, (BCC, Relative, 2)},
            { 0x91, (STA, IndirectY, 6)},
            { 0x94, (STY, ZeroPageX, 4)},
            { 0x95, (STA, ZeroPageX, 4)},
            { 0x96, (STX, ZeroPageY, 4)},
            { 0x98, (TYA, Implied, 2)},
            { 0x99, (STA, AbsoluteY, 5)},
            { 0x9A, (TXS, Implied, 2)},
            { 0x9D, (STA, AbsoluteX, 5)},

            { 0xA0, (LDY, Immediate, 2)},
            { 0xA1, (LDA, IndirectX, 6)},
            { 0xA2, (LDX, Immediate, 2)},
            { 0xA4, (LDY, ZeroPage, 3)},
            { 0xA5, (LDA, ZeroPage, 3)},
            { 0xA6, (LDX, ZeroPage, 3)},
            { 0xA8, (TAY, Implied, 2)},
            { 0xA9, (LDA, Immediate, 2)},
            { 0xAA, (TAX, Implied, 2)},
            { 0xAC, (LDY, Absolute, 4)},
            { 0xAD, (LDA, Absolute, 4)},
            { 0xAE, (LDX, Absolute, 4)},

            { 0xB0, (BCS, Relative, 2)},
            { 0xB1, (LDA, IndirectY, 5)},
            { 0xB4, (LDY, ZeroPageX, 4)},
            { 0xB5, (LDA, ZeroPageX, 4)},
            { 0xB6, (LDX, ZeroPageY, 4)},
            { 0xB8, (CLV, Implied, 2)},
            { 0xB9, (LDA, AbsoluteY, 4)},
            { 0xBA, (TSX, Implied, 2)},
            { 0xBC, (LDY, AbsoluteX, 4)},
            { 0xBD, (LDA, AbsoluteX, 4)},
            { 0xBE, (LDX, AbsoluteX, 4)},

            { 0xC0, (CPY, Immediate, 2)},
            { 0xC1, (CMP, IndirectX, 6)},
            { 0xC4, (CPY, ZeroPage, 3)},
            { 0xC5, (CMP, ZeroPage, 3)},
            { 0xC6, (DEC, ZeroPage, 5)},
            { 0xC8, (INY, Implied, 2)},
            { 0xC9, (CMP, Immediate, 2)},
            { 0xCA, (DEX, Implied, 2)},
            { 0xCC, (CPY, Absolute, 4)},
            { 0xCD, (CMP, Absolute, 4)},
            { 0xCE, (DEC, Absolute, 6)},

            { 0xD0, (BNE, Relative, 2)},
            { 0xD1, (CMP, IndirectY, 5)},
            { 0xD5, (CMP, ZeroPageX, 4)},
            { 0xD6, (DEC, ZeroPageX, 6)},
            { 0xD8, (CLD, Implied, 2)},
            { 0xD9, (CMP, AbsoluteY, 4)},
            { 0xDD, (CMP, AbsoluteX, 4)},
            { 0xDE, (DEC, AbsoluteX, 7)},

            { 0xE0, (CPX, Immediate, 2)},
            { 0xE1, (SBC, IndirectX, 6)},
            { 0xE4, (CPX, ZeroPage, 3)},
            { 0xE5, (SBC, ZeroPage, 3)},
            { 0xE6, (INC, ZeroPage, 5)},
            { 0xE8, (INX, Implied, 2)},
            { 0xE9, (SBC, Immediate, 2)},
            { 0xEA, (NOP, Implied, 2)},
            { 0xEC, (CPX, Absolute, 4)},
            { 0xED, (SBC, Absolute, 4)},
            { 0xEE, (INC, Absolute, 6)},

            { 0xF0, (BEQ, Relative, 2)},
            { 0xF1, (SBC, IndirectY, 5)},
            { 0xF5, (SBC, ZeroPageX, 4)},
            { 0xF6, (INC, ZeroPageX, 6)},
            { 0xF8, (SED, Implied, 2)},
            { 0xF9, (SBC, AbsoluteY, 4)},
            { 0xFD, (SBC, AbsoluteX, 4)},
            { 0xFE, (INC, AbsoluteX, 7)}, 

        };

        static readonly Dictionary<CpuAddressingMode, byte> addressingModeSize = new Dictionary<CpuAddressingMode, byte> {
            { Implied,        1 },
            { Accumulator,    1 },
            { Immediate,      2 },
            { ZeroPage,       2 },
            { ZeroPageX,      2 },
            { ZeroPageY,      2 },
            { Relative,       2 },
            { Absolute,       3 },
            { AbsoluteX,      3 },
            { AbsoluteY,      3 },
            { Indirect,       3 },
            { IndirectX,      2 },
            { IndirectY,      2 },
        };

        static readonly Dictionary<CpuInstruction, CpuInstructionType> instructionType = new Dictionary<CpuInstruction, CpuInstructionType> {
            {BRK, Command},
            {CLC, Command},
            {CLD, Command},
            {CLI, Command},
            {CLV, Command},
            {DEX, Command},
            {DEY, Command},
            {INX, Command},
            {INY, Command},
            {NOP, Command},
            {PHA, Command},
            {PHP, Command},
            {PLA, Command},
            {PLP, Command},
            {RTI, Command},
            {RTS, Command},
            {SEC, Command},
            {SED, Command},
            {SEI, Command},
            {TAX, Command},
            {TAY, Command},
            {TSX, Command},
            {TXA, Command},
            {TXS, Command},
            {TYA, Command},

            {BCC, Branch},
            {BCS, Branch},
            {BEQ, Branch},
            {BMI, Branch},
            {BNE, Branch},
            {BPL, Branch},
            {BVC, Branch},
            {BVS, Branch},

            {ADC, Argument},
            {AND, Argument},
            {BIT, Argument},
            {CMP, Argument},
            {CPX, Argument},
            {CPY, Argument},
            {EOR, Argument},
            {LDA, Argument},
            {LDX, Argument},
            {LDY, Argument},
            {ORA, Argument},
            {SBC, Argument},
            
            {JMP, MemoryWrite},
            {JSR, MemoryWrite},
            {DEC, MemoryWrite},
            {INC, MemoryWrite},
            {STA, MemoryWrite},
            {STX, MemoryWrite},
            {STY, MemoryWrite},

            {ASL, AccumulatorWrite},
            {LSR, AccumulatorWrite},
            {ROL, AccumulatorWrite},
            {ROR, AccumulatorWrite}
        };


        // Registers
        byte A; // Accumulator
        byte X; // X index
        byte Y; // Y index
        byte S; // Stack Pointer // hardward to $01SS ie $0100 to $01FF
        ushort PC; // Program Counter 
        // Flags   NV-BDIZC
        byte P;
        bool N { 
            get => P.GetBit(7);
            set => P = P.SetBit(7, value);
        }
        bool V { 
            get => P.GetBit(6);
            set => P = P.SetBit(6, value);
        }
        bool B { 
            get => P.GetBit(4);
            set => P = P.SetBit(4, value);
        }
        bool D { 
            get => P.GetBit(3);
            set => P = P.SetBit(3, value);
        }
        bool I { 
            get => P.GetBit(2);
            set => P = P.SetBit(2, value);
        }
        bool Z { 
            get => P.GetBit(1);
            set => P = P.SetBit(1, value);
        }
        bool C { 
            get => P.GetBit(0);
            set => P = P.SetBit(0, value);
        }

        /// <summary>The CPU needs to be connected to a bus to work</summary>
        public Bus bus;

        public override string ToString() {
            var str = "";
            str += "A=$"+A.ToString("X2")+" ";
            str += "X=$"+X.ToString("X2")+" ";
            str += "Y=$"+Y.ToString("X2")+" ";
            str += "SP=$"+S.ToString("X2")+" ";
            str += "PC=$"+PC.ToString("X2")+" ";
            str += "\n";
            str += "NV-BDIZC\n";
            str += P.ToBinaryString();
            return str;
        }


        byte Read(ushort address) => bus.Read(address);
        void Write(ushort address, byte value) => bus.Write(address, value);



        byte StepPC() {
            var value = Read(PC);
            PC++;
            return value;
        }
        



        void SetFlagsForNumber(byte number) {
            Z = (number == 0);
            N = number.GetBit(7);
        }
        byte SetFlagsForNumberWithCarry(int number) {
            byte value = (byte)number;
            C = (number > 255);
            SetFlagsForNumber(value);
            return value;
        }
        
        void StackPush(byte value) {
            Write((ushort)(0x0100+S), value);
            S--;
        }
        void StackPush2(ushort value) {
            byte byte1 = (byte)((value >> 8) & 0x00FF);
            byte byte2 = (byte)((value & 0x00FF));
            StackPush(byte1);
            StackPush(byte2);
        }
        byte StackPop() {
            S++;
            return Read((ushort)(0x0100+S));
        }
        ushort StackPop2() {
            var byte2 = StackPop();
            var byte1 = StackPop() << 8 ;
            return (ushort)(byte1+byte2);
        }

        void SetPCByAddress(ushort pcAddress) {
            var lo = Read(pcAddress);
            var hi = Read((ushort)(pcAddress+1));
            PC = (ushort)((hi << 8) | lo);
        }


        // Signals
        public void Reset() {
            A = 0;
            X = 0;
            Y = 0;
            S = 0xFF;
            //    NV-BDIZC
            P = 0b00100000;
            SetPCByAddress(0xFFFC);
        }
        void Irq() {
            if(!I) {
                StackPush2(PC);
                B = false;
                I = true;
                StackPush(P);
                SetPCByAddress(0xFFFE);
            }
        }
        void Nmi() {
            StackPush2(PC);
            B = false;
            I = true;
            StackPush(P);
            SetPCByAddress(0xFFFA);
        }


        void DoCommandInstructions(CpuInstruction instruction) {
            switch(instruction) {
                case BRK:
                    StackPush2(PC);
                    B = true;
                    StackPush(P);
                    SetPCByAddress(0xFFFE);
                    break;
                case CLC: C = false; break;
                case CLD: D = false; break;
                case CLI: I = false; break;
                case CLV: V = false; break;
                case DEX: 
                    X = (byte)(X-1);
                    SetFlagsForNumber(X);
                    break;
                case DEY: 
                    Y = (byte)(Y-1);
                    SetFlagsForNumber(Y);
                    break;
                case INX: 
                    X = (byte)(X+1);
                    SetFlagsForNumber(X);
                    break;
                case INY: 
                    Y = (byte)(Y+1);
                    SetFlagsForNumber(Y);
                    break;
                case NOP: break;
                case PHA: StackPush(A); break;
                case PHP: StackPush(P); break;
                case PLA: 
                    A = StackPop();
                    SetFlagsForNumber(A);
                    break;
                case PLP: P = StackPop(); break;
                case RTI: 
                    P = StackPop();
                    B = !B;
                    PC = StackPop2();
                    break;
                case RTS: 
                    PC = (ushort)(StackPop2()-1);
                    break;
                case SEC: C = true; break;
                case SED: D = true; break;
                case SEI: I = true; break;
                case TAX: 
                    X = A;
                    SetFlagsForNumber(X);
                    break;
                case TAY: 
                    Y = A;
                    SetFlagsForNumber(Y);
                    break;
                case TSX: 
                    X = S;
                    SetFlagsForNumber(X);
                    break;
                case TXA: 
                    A = X;
                    SetFlagsForNumber(A);
                    break;
                case TXS: 
                    S = X;
                    break;
                case TYA: 
                    A = Y;
                    SetFlagsForNumber(A);
                    break;
                default: throw new Exception("Instruction is not command "+instruction);
            }
        }
        // Relative Instructions
        void DoBranchInstructions(CpuInstruction instruction, byte a) {
            ushort relativeAddress = a;
            if(a.GetBit(7)) {
                relativeAddress = (ushort)(relativeAddress | 0xFF00); // Sign extend negative values to 32bit
            }
            switch(instruction) {
                case BCC:
                    if(!C) PC += relativeAddress;
                    break;
                case BCS:
                    if(C) PC += relativeAddress;
                    break;
                case BEQ:
                    if(Z) PC += relativeAddress;
                    break;
                case BMI:
                    if(N) PC += relativeAddress;
                    break;
                case BNE:
                    if(!Z) PC += relativeAddress;
                    break;
                case BPL:
                    if(!N) PC += relativeAddress;
                    break;
                case BVC:
                    if(!V) PC += relativeAddress;
                    break;
                case BVS:
                    if(V) PC += relativeAddress;
                    break;
                default: throw new Exception("Instruction is not unary "+instruction);
            }
        }
        
        // This will Do ArgumentInstructions and AccumulatorInstructions
        void DoMemoryWriteInstructions(CpuInstruction instruction, ushort address) {
            var m = Read(address);
            switch(instruction) {
                case JMP:
                    PC = address;
                    break;
                case JSR:
                    StackPush2((ushort)(PC-1));
                    PC = address; 
                    break;
                case DEC: // M,Z,N = M-1
                    var dec = (byte)(m-1);
                    SetFlagsForNumber(dec);
                    Write(address, dec);
                    break;
                case INC: // M,Z,N = M+1
                    var inc = (byte)(m+1);
                    SetFlagsForNumber(inc);
                    Write(address, inc);
                    break;
                case STA: // M = A
                    Write(address, A);
                    break;
                case STX: // M = X
                    Write(address, X);
                    break;
                case STY: // M = Y
                    Write(address, Y);
                    break;
                default: // Try the Accumulator instructions
                    switch(instructionType[instruction]) {
                        case Argument:
                            DoArgumentInstructions(instruction, m); // no need to write now
                            break;
                        case AccumulatorWrite:
                            Write(address, DoAccumulatorInstructions(instruction, m));
                            break;
                        default: throw new Exception("Instruction is not binary "+instruction);
                    }
                    break;
            }
        }

        void DoArgumentInstructions(CpuInstruction instruction, byte m) {
            switch(instruction) {
                case ADC: { // A,Z,C,N = A+M+C
                    var result = SetFlagsForNumberWithCarry(A+m+C.BooleanToByte());
                    V = (A.GetBit(7) ^ result.GetBit(7)) && (!(A.GetBit(7) ^ m.GetBit(7)));
                    A = result;
                    break; }
                case SBC: { // A,Z,C,N = A-M-(1-C)
                    var result = SetFlagsForNumberWithCarry(A-m-(1-C.BooleanToByte()));
                    V = (A.GetBit(7) ^ result.GetBit(7)) && (!(A.GetBit(7) ^ m.GetBit(7)));
                    A = result;
                    break; }
                case AND: // A,Z,N = A&M
                    A = (byte)(A&m);
                    SetFlagsForNumber(A);
                    break;
                case BIT: // A & M, N = M7, V = M6
                    var mBit = (byte)(A & m);
                    V = mBit.GetBit(6);
                    SetFlagsForNumber(mBit);
                    break;
                case CMP: // Z,C,N = A-M
                    C = A >= m;
                    SetFlagsForNumber((byte)(A-m));
                    break;
                case CPX: // Z,C,N = X-M
                    C = X >= m;
                    SetFlagsForNumber((byte)(X-m));
                    break;
                case CPY: // Z,C,N = Y-M
                    C = Y >= m;
                    SetFlagsForNumber((byte)(Y-m));
                    break;
                case EOR: // A,Z,N = A^M
                    A = (byte)(A^m);
                    SetFlagsForNumber(A);
                    break;
                case LDA: // A,Z,N = M
                    A = m;
                    SetFlagsForNumber(A);
                    break;
                case LDX: // X,Z,N = M
                    X = m;
                    SetFlagsForNumber(X);
                    break;
                case LDY: // Y,Z,N = M
                    Y = m;
                    SetFlagsForNumber(Y); 
                    break;
                case ORA: // A,Z,N = A|M
                    A = (byte)(A|m);
                    SetFlagsForNumber(A);
                    break;
                default: throw new Exception("Instruction is not binary "+instruction);
            }
        }


        byte DoAccumulatorInstructions(CpuInstruction instruction, byte m) {
            switch(instruction) {
                case ASL: // A,Z,C,N = M*2 or M,Z,C,N = M*2
                    return SetFlagsForNumberWithCarry(m*2);
                case LSR: // A,C,Z,N = A/2 or M,C,Z,N = M/2
                    return SetFlagsForNumberWithCarry(m/2);
                case ROL: 
                    return SetFlagsForNumberWithCarry(m.RotateLeft(1));
                case ROR:
                    return SetFlagsForNumberWithCarry(m.RotateRight(1));
                default: throw new Exception("Instruction is not binary "+instruction);
            }
        }


        ushort IndirectAddress(ushort address) {
            if((address & 0xFF) == 0x00FF) {
                // Simulate page boundary hardware bug
                return (ushort)((Read((ushort)(address & 0xFF00)) << 8) | Read(address));
            } else {
                // Behave normally
                return (ushort)((Read((ushort)(address + 1)) << 8) | Read(address));
            }
        }
        

        // Execute returns number of cycles
        public byte Step() {
            
            byte instruction = StepPC();

            (CpuInstruction, CpuAddressingMode, byte) decodedInstruction;
            if(!decodeInstruction.TryGetValue(instruction, out decodedInstruction)) {
                Console.WriteLine("Using unofficial opcode "+instruction);
                return 1;
            }
            var (inst, addressing, cycles) = decodedInstruction;
            var argsCount = addressingModeSize[addressing]-1;

            if(debug) {
                Console.WriteLine("=== CPU State");
                Console.WriteLine(this);
                Console.WriteLine("=== Next instruction");
                Console.Write(inst+"\t "+addressing);
            }
            
            switch(argsCount) {
                case 0:
                    switch(addressing) {
                        case Implied:
                            DoCommandInstructions(inst);
                            break;
                        case Accumulator:
                            A = DoAccumulatorInstructions(inst, A);
                            break;
                        default: throw new Exception("CPU addressing Error: "+addressing+" argsCount: "+argsCount);
                    }
                    break;
                case 1:
                    var a = StepPC();
                    if(debug) Console.Write("\t "+a.ToString("X2"));
                    switch(addressing) {
                        case Immediate:
                            DoArgumentInstructions(inst, a);
                            break;
                        case ZeroPage:
                            DoMemoryWriteInstructions(inst, a);
                            break;
                        case ZeroPageX:
                            DoMemoryWriteInstructions(inst, (ushort)(a+X));
                            break;
                        case ZeroPageY:
                            DoMemoryWriteInstructions(inst, (ushort)(a+Y));
                            break;
                        case Relative:
                            DoBranchInstructions(inst, a);
                            break;
                        case IndirectX:
                            DoMemoryWriteInstructions(inst, IndirectAddress((ushort)(a+X)));
                            break;
                        case IndirectY:
                            DoMemoryWriteInstructions(inst, (ushort)(IndirectAddress(a)+Y));
                            break;
                        default: throw new Exception("CPU addressing Error: "+addressing+" argsCount: "+argsCount);
                    }
                    break;
                case 2:
                    byte lo = StepPC();
                    byte hi = StepPC();
                    ushort abs = (ushort)((hi << 8) | lo);
                    if(debug) Console.Write("\t "+abs.ToString("X4"));
                    switch(addressing) {
                        case Absolute:
                            DoMemoryWriteInstructions(inst, abs);
                            break;
                        case AbsoluteX:
                            DoMemoryWriteInstructions(inst, (ushort)(abs+X));
                            break;
                        case AbsoluteY:
                            DoMemoryWriteInstructions(inst, (ushort)(abs+Y));
                            break;
                        case Indirect:
                            DoMemoryWriteInstructions(inst, IndirectAddress(abs));
                            break;
                        default: throw new Exception("CPU addressing Error: "+addressing+" argsCount: "+argsCount);
                    }
                    break;
                default: throw new Exception("CPU Error wrong number of arguments: "+argsCount);
            }
            if(debug) Console.WriteLine();
            return cycles;
        }



        
        
    }

}