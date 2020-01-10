
/// <summary>
/// Project: 6502 CPU Emulator
/// Author: Mustafa Al-Ghayeb
/// Licence: MIT license
/// </summary>
namespace _6502CpuEmulator {
    
    public static class BitMath {
        /// <summary>Get bit at index 0 to 7. Where least significant (0th) to the most significant (7th).</summary>
        public static bool GetBit(this byte b, int bitIndex) {
            return (b & (1 << bitIndex)) != 0;
        }
        /// <summary>Set bit at index 0 to 7. Where least significant (0th) to the most significant (7th).</summary>
        public static byte SetBit(this byte b, int bitIndex, bool value) {
            if(value) {
                return (byte)(b | (1 << bitIndex));
            } else  {
                return (byte)(b & (~(1 << bitIndex)));
            }
        } 
            
        /// <summary>Convert byte to binary string of 1s and 0s</summary>
        public static string ToBinaryString(this byte b) {
            var str = "";
            for(var i=7; i>=0; i--) {
                str += b.GetBit(i)?"1":"0";
            }
            return str;
        }
        /// <summary>Convert boolean to 1 or 0</summary>
        public static byte BooleanToByte(this bool b) {
            if(b) {
                return 1;
            } else {
                return 0;
            }
        }


        public static byte RotateLeft(this byte value, int count) {
            return (byte)((value << count) | (value >> (8 - count)));
        }
        public static byte RotateRight(this byte value, int count) {
            return (byte)((value >> count) | (value << (8 - count)));
        }
    }

}