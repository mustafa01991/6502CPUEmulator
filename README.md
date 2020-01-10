# 6502 CPU Emulator
Fully working [6502 CPU](https://en.wikipedia.org/wiki/MOS_Technology_6502) Emulator written in C# with DotNet Core 3.0.
## Getting Started
Example of setting up the CPU with 65,535 bytes of RAM and a program that increase register X indefinitely.
```csharp
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
```
This will output:
```
=== CPU State
A=$00 X=$00 Y=$00 SP=$FF PC=$01 
NV-BDIZC
00100000
=== Next instruction
INX      Implied
=== CPU State
A=$00 X=$01 Y=$00 SP=$FF PC=$02 
NV-BDIZC
00100000
=== Next instruction
JMP      Absolute        004C
=== CPU State
A=$00 X=$01 Y=$00 SP=$FF PC=$4D 
NV-BDIZC
00100000
=== Next instruction
BRK      Implied
=== CPU State
NV-BDIZC
00110000
=== Next instruction
INX      Implied
=== CPU State
A=$00 X=$02 Y=$00 SP=$FC PC=$02
NV-BDIZC
00110000
```

The CUP state are the cpu registers:
* A // accumulator
* X // X index
* Y // Y index
* SP // Stack pointer
* PC // Program counter 
* NV-BDIZC // The CPU Flags

To add IO devices to the system override or change the `Write()` and `Read()` methods in the `Bus` class. 


## Authors
* **Mustafa Al-Ghayeb** - [mustafa01991](https://github.com/mustafa01991)
## License
This project is licensed under the MIT License