using System;

/// <summary>
/// Project: 6502 CPU Emulator
/// Author: Mustafa Al-Ghayeb
/// Licence: MIT license
/// </summary>
namespace _6502CpuEmulator {

    class Program {
        static void Main(string[] args) {

            var cpu = new Cpu();
            // This will make the cpu output it's state to the console each step
            cpu.debug = true; 

            // Create bus with memory of size: 0xFFFF = 65,535 bytes
            var bus = new Bus(cpu, 0xFFFF);
            
            // Start reading code at address 0
            bus.Write(0xFFFC, 0); // The cpu will start by jumping to the address at 0xFFFC
            bus.Write(0xFFFD, 0);


            // Example Code of adding to X
            // Increment register X by 1
            bus.Write(0x0000, 0xE8); // INX
            // Jump to address 0
            bus.Write(0x0001, 0x4C); // JMP
            bus.Write(0x0002, 0x00); // 0   (abs address)

            // Run with each Instruction step taking 1000 milliseconds
            bus.Run(1000);
        }
    }
}
