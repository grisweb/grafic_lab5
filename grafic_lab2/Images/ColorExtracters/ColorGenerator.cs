using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace grafic_lab2.Images.ColorExtracters;

public static class RGBColorCreator
{
    private static Random _random = new Random();

    public static Color GetRandomColor()
    {
        byte red = (byte)_random.Next(1, 256);
        byte green = (byte)_random.Next(1, 256);
        byte blue = (byte)_random.Next(1, 256);

        return Color.FromArgb(255, red, green, blue);
    }
}
