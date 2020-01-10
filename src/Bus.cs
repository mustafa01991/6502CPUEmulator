using System;
using System.Threading;

/// <summary>
/// Project: 6502 CPU Emulator
/// Author: Mustafa Al-Ghayeb
/// Licence: MIT license
/// </summary>
namespace _6502CpuEmulator {
    class Bus {
        
        readonly byte[] ram;
        public readonly Cpu cpu;

        public Bus(Cpu cpu, int ramSize= ushort.MaxValue+1) {
            this.cpu = cpu;
            cpu.bus = this; // Connect the cpu to the bus
            this.ram = new byte[ramSize];
        }

        // Change the Write and Read methods to give the cpu access to IO devices by address memory
        public virtual void Write(ushort address, byte value) {
            if(address < this.ram.Length) {
                this.ram[address] = value;
            } else {
                throw new Exception("Unknown address access: 0x"+address.ToString("X1"));
            }
        }
        public virtual byte Read(ushort address) {
            if(address < this.ram.Length) {
                return this.ram[address];
            } else {
                throw new Exception("Unknown address access: 0x"+address.ToString("X1"));
            }
        }


        public void Run(int delay) {
            this.cpu.Reset();
            while(true) {
                Thread.Sleep(delay);
                this.cpu.Step();
            }
        }

    }
}