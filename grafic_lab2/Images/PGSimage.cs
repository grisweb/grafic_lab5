using grafic_lab2.Images.ColorExtracters;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace grafic_lab2.Images;

public class PGSimage : IBitmatable
{
    public const string FILE_EXTENSION = "pgs";

    // 4 байта
    public int Width { get; private set; }
    public int Height { get; private set; }

    public const byte PIXEL_SIZE = 4;
    public const byte COLOR_COUNT = 16;

    // чтоб прочитать
    private byte _pixelSize;
    private ushort _colorsCount;

    private Color[][] _palate;

    // упакованные пиксели
    private byte[] _data;

    private PGSimage() { }

    public Bitmap ToBitmap()
    {
        Bitmap res = new Bitmap(Width, Height, PixelFormat.Format32bppArgb);

        int i = 0;
        bool isFirstPart = true;
        int pixel;

        for (int y = 0; y < res.Height; ++y)
        {
            for (int x = 0; x < res.Width; ++x)
            {
                if (isFirstPart)
                {
                    pixel = (_data[i] >> 4) & 0b1111;
                }
                else
                {
                    pixel = _data[i] & 0b1111;

                    ++i;
                }

                isFirstPart = !isFirstPart;

                res.SetPixel(x, y, _palate[pixel >> 2][pixel & 0b0011]);
            }
        }

        return res;
    }

    public static PGSimage? Create(string filePath)
    {
        using (BinaryReader reader = new BinaryReader(File.Open(filePath, FileMode.Open)))
        {
            PGSimage res = new PGSimage();

            res.Width = reader.ReadInt32();
            res.Height = reader.ReadInt32();

            res._pixelSize = reader.ReadByte();

            if (res._pixelSize != PIXEL_SIZE)
            {
                return null;
            }

            res._colorsCount = reader.ReadUInt16();

            if (res._colorsCount != COLOR_COUNT)
            {
                return null;
            }

            var pallete = new Color[4][];
            for (int i = 0; i < 4; i++)
            {
                pallete[i] = new Color[4];
            }

            for (byte i = 0; i < pallete.Length; i++)
            {
                for (byte j = 0; j < pallete[i].Length; j++)
                {
                    pallete[i][j] = Color.FromArgb(
                        reader.ReadByte(),
                        reader.ReadByte(),
                        reader.ReadByte(),
                        reader.ReadByte());
                }
            }

            res._palate = pallete;

            //вычисление количества байтов в строке
            int bytes = (res.Width * res.Height + 1) / 2;
            res._data = reader.ReadBytes(bytes);

            return res;
        }
    }


    // починить количество бит
    public static PGSimage Create(BMP24image image)
    {
        PGSimage res = new PGSimage();

        var compressor = new BMP24imageColorCompressor(image, COLOR_COUNT);
        compressor.Run();
        
        res._palate = MakePalette(compressor.Palette);
        res._data = ToIndexesPixels(image, res._palate, compressor.CompressedColors);

        res.Width = image.Width;
        res.Height = image.Height;

        res._pixelSize = PIXEL_SIZE;
        res._colorsCount = COLOR_COUNT;

        return res;
    }

    public void Save(string pathFile)
    {
        if (!pathFile.EndsWith(FILE_EXTENSION))
            throw new ArgumentException("not supported file extension");

        using (BinaryWriter writer = new BinaryWriter(File.Open(pathFile, FileMode.OpenOrCreate)))
        {
            writer.Write(Width);
            writer.Write(Height);
            writer.Write(_pixelSize);
            writer.Write(_colorsCount);

            for (byte i = 0; i < _palate.Length; i++)
            {
                for (byte j = 0; j < _palate[i].Length; j++)
                {
                    writer.Write(_palate[i][j].A);
                    writer.Write(_palate[i][j].R);
                    writer.Write(_palate[i][j].G);
                    writer.Write(_palate[i][j].B);
                }
            }

            writer.Write(_data);
        }
    }

    private static byte[] ToIndexesPixels(BMP24image toConvert, Color[][] palette, Dictionary<Color, Color> compressedColors)
    {
        var res = new byte[(toConvert.Width * toConvert.Height + 1) / 2];
        byte[] buffer = toConvert.Bitmap;

        var colorIndexes = CalcIndexes(palette, 2);

        bool isFirstPart = true;

        byte doublePixel = 0;
        int res_index = 0;

        for (int y = 0; y < toConvert.Height; ++y)
        {
            for (int x = 0; x < toConvert.Width; ++x)
            {
                int image_index = (x + (toConvert.Height - 1 - y) * toConvert.Width) * 3;

                Color colorToCompres = Color.FromArgb(buffer[image_index + 2], buffer[image_index + 1], buffer[image_index]);

                if (isFirstPart)
                {
                    doublePixel = colorIndexes[compressedColors[colorToCompres]];
                    doublePixel <<= 4;
                }
                else
                {
                    doublePixel |= colorIndexes[compressedColors[colorToCompres]];

                    res[res_index] = doublePixel;
                    
                    ++res_index;
                }

                isFirstPart = !isFirstPart;
            }
        }

        return res;
    }

    // словарь цвет - упакованное положение в матрице
    private static Dictionary<Color, byte> CalcIndexes(Color[][] palate, byte offset)
    {
        var res = new Dictionary<Color, byte>();

        for (byte i = 0; i < palate.Length; i++)
        {
            for (byte j = 0; j < palate[i].Length; j++)
            {
                res.TryAdd(palate[i][j], (byte)((i << offset) | j));
            }
        }

        return res;
    }

    // создание двумерного массива из обычного
    private static Color[][] MakePalette(Color[] colors)
    {
        if (colors.Length < 16)
        {
            Array.Resize(ref colors, 16);
        }

        List<Color> red = new List<Color>();
        List<Color> green = new List<Color>();
        List<Color> blue = new List<Color>();
        List<Color> orther = new List<Color>();

        for (int i = 0; i < colors.Length; i++)
        {
            var temp = colors[i];

            int max = Math.Max(temp.R, Math.Max(temp.G, temp.B));

            if (max == temp.R && max == temp.G && max == temp.B
                || max == temp.R && max == temp.G
                || max == temp.R && max == temp.B
                || max == temp.G && max == temp.B)
            {
                orther.Add(temp);
            }
            else if (max == temp.R)
            {
                red.Add(temp);
            }
            else if (max == temp.G)
            {
                green.Add(temp);
            }
            else if (max == temp.B)
            {
                blue.Add(temp);
            }
            else
            {
                orther.Add(temp);
            }
        }

        var redComparator = new Comparison<Color>((Color first, Color second) => second.R.CompareTo(first.R));
        var greenComparator = new Comparison<Color>((Color first, Color second) => second.G.CompareTo(first.G));
        var blueComparator = new Comparison<Color>((Color first, Color second) => second.B.CompareTo(first.B));
        var ortherComparator = new Comparison<Color>((Color first, Color second) => (second.R + second.G + second.B).CompareTo(first.R + first.G + first.B));


        red.Sort(redComparator);
        green.Sort(greenComparator);
        blue.Sort(blueComparator);
        orther.Sort(ortherComparator);

        List<Color> toMove = new List<Color>();

        RemoveLessSuitable(red, toMove);
        RemoveLessSuitable(green, toMove);
        RemoveLessSuitable(blue, toMove);
        RemoveLessSuitable(orther, toMove);

        AddSuitable(red, redComparator, toMove);
        AddSuitable(green, redComparator, toMove);
        AddSuitable(blue, redComparator, toMove);
        AddSuitable(orther, redComparator, toMove);

        return new Color[4][]
        {
                red.ToArray(),
                green.ToArray(),
                blue.ToArray(),
                orther.ToArray()
        };
    }

    // добавить при нехватке наиболее подходящие цвета
    private static void AddSuitable(List<Color> colorFamaly, Comparison<Color> colorFamalyComparator, List<Color> toMove)
    {
        if (colorFamaly.Count < 4)
        {
            toMove.Sort(colorFamalyComparator);

            while (colorFamaly.Count < 4)
            {
                colorFamaly.Add(toMove.First());
                toMove.RemoveAt(0);
            }
        }
    }

    // удалить при избыточности наименее подходящие цвета
    private static void RemoveLessSuitable(List<Color> colorFamaly, List<Color> toMove)
    {
        if (colorFamaly.Count > 4)
        {
            for (int i = 4; i < colorFamaly.Count; i++)
            {
                toMove.Add(colorFamaly[i]);
            }

            colorFamaly.RemoveRange(4, colorFamaly.Count - 4);
        }
    }
}
