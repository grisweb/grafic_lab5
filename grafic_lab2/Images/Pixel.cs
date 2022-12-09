using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace grafic_lab2.Images
{
    public class Pixel
    {
        public byte Red { get; set; }
        public byte Green { get; set; }
        public byte Blue { get; set; }

        public Pixel(byte red = 255, byte green = 255, byte blue = 255)
        {
            Red = red;
            Green = green;
            Blue = blue;
        }

        public UInt32 GetToUInt()
        {
            return 0xFF000000 | ((UInt32)Red << 16) | ((UInt32)Green << 8) | ((UInt32)Blue);
        }

        public void SetUInt(UInt32 pixel)
        {
            Red = (byte)((pixel & 0x00FF0000) >> 16);
            Green = (byte)((pixel & 0x0000FF00) >> 8);
            Red = (byte)(pixel & 0x000000FF);
        }

        public void ConvertToGray()
        {
            Red = Green = Blue = (byte)((Red + Green + Blue) / 3.0f);
        }
    }
}
