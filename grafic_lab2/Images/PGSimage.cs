using grafic_lab2.Images.ColorExtracters;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace grafic_lab2.Images;

public class PGSimage : IBitmatable
{
    public const string FileExtention = "pgs";

    // 4 байта
    public int Width { get; private set; }
    public int Height { get; private set; }

    // чтоб прочитать
    private byte pixelSize = 4;
    private ushort colorsCount = 16;

    private Color[][] _palate;
    private byte[] _data;

    private PGSimage() { }

    public Bitmap ToBitmap()
    {
        var res = new Bitmap(Width, Height);


        BitmapData image_data = res.LockBits(
            new Rectangle(0, 0, Width, Height),
            ImageLockMode.ReadOnly,
            PixelFormat.Format24bppRgb);

        int bytes = image_data.Stride * image_data.Height;
        res.UnlockBits(image_data);

        byte[] result = new byte[bytes];

        int image_index = 0;

        // проблема в декодировании

        for (int i = 0; i < _data.Length; ++i)
        {
            byte pixel = (byte)((_data[i] >> 4) & 15);

            Color color = _palate[pixel >> 2][pixel & 3];
            result[image_index++] = color.B;
            result[image_index++] = color.G;
            result[image_index++] = color.R;

            pixel = (byte)(_data[i] & 15);

            color = _palate[pixel >> 2][pixel & 3];
            result[image_index++] = color.B;
            result[image_index++] = color.G;
            result[image_index++] = color.R;
        }


        // формирование компрессионного изображения
        BitmapData res_data = res.LockBits(
            new Rectangle(0, 0, Width, Height),
            ImageLockMode.WriteOnly,
            PixelFormat.Format24bppRgb);

        Marshal.Copy(result, 0, res_data.Scan0, bytes);
        res.UnlockBits(res_data);

        return res;
    }

    public static PGSimage? Create(string filePath)
    {
        using (BinaryReader reader = new BinaryReader(File.Open(filePath, FileMode.Open)))
        {
            PGSimage res = new PGSimage();

            res.Width = reader.ReadInt32();
            res.Height = reader.ReadInt32();

            res.pixelSize = reader.ReadByte();

            if (res.pixelSize != 4)
            {
                return null;
            }

            res.colorsCount = reader.ReadUInt16();

            if (res.colorsCount != 16)
            {
                return null;
            }

            var palate = new Color[4][];

            for (int i = 0; i < 4; i++)
            {
                palate[i] = new Color[4];
            }

            for (byte i = 0; i < palate.Length; i++)
            {
                for (byte j = 0; j < palate[i].Length; j++)
                {
                    palate[i][j] = Color.FromArgb(
                        reader.ReadByte(),
                        reader.ReadByte(),
                        reader.ReadByte(),
                        reader.ReadByte());
                }
            }

            res._palate = palate;

            //вычисление количества байтов в строке
            int bytes = res.Width * res.Height / (8 / res.pixelSize);
            res._data = reader.ReadBytes(bytes);

            return res;
        }
    }

    public static PGSimage Create(BMP24image image)
    {
        PGSimage res = new PGSimage();

        var bitmap = image.ToBitmap();

        var compressor = new BitmapColorCompressor(bitmap, 16);
        compressor.Run();
        
        res._palate = MakePalette(compressor.Palette);
        res._data = ToIndexesPixels(bitmap, res._palate, compressor.CompressedColors);

        res.Width = bitmap.Width;
        res.Height = bitmap.Height;

        return res;
    }

    public void Save(string pathFile)
    {
        if (!pathFile.EndsWith(pathFile))
            throw new ArgumentException("not siported file extantion");

        using (BinaryWriter writer = new BinaryWriter(File.Open(pathFile, FileMode.OpenOrCreate)))
        {
            writer.Write(Width);
            writer.Write(Height);
            writer.Write(pixelSize);
            writer.Write(colorsCount);

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

    private static byte[] ToIndexesPixels(Bitmap compressedBitmap, Color[][] palette, Dictionary<Color, Color> compressedColors)
    {
        var res = new byte[compressedBitmap.Width * compressedBitmap.Height / 2];

        // копирование массива битов
        BitmapData image_data = compressedBitmap.LockBits(
            new Rectangle(0, 0, compressedBitmap.Width, compressedBitmap.Height),
            ImageLockMode.ReadOnly,
            PixelFormat.Format24bppRgb);

        int bytes = image_data.Stride * image_data.Height;
        byte[] buffer = new byte[bytes];

        Marshal.Copy(image_data.Scan0, buffer, 0, bytes);
        compressedBitmap.UnlockBits(image_data);

        var colorIndexes = CalcIndexes(palette, 2);

        for (int pack_index = 0, image_index = 0; pack_index < res.Length; ++pack_index, image_index += 3)
        {
            byte doublePixel = colorIndexes[compressedColors[Color.FromArgb(buffer[image_index + 2], buffer[image_index + 1], buffer[image_index])]];
            image_index += 3;

            doublePixel <<= 4;
            doublePixel |= colorIndexes[compressedColors[Color.FromArgb(buffer[image_index + 2], buffer[image_index + 1], buffer[image_index])]]; ;

            res[pack_index] = doublePixel;
        }

        return res;
    }

    private static Dictionary<Color, byte> CalcIndexes(Color[][] palate, byte offset)
    {
        var res = new Dictionary<Color, byte>();

        for (byte i = 0; i < palate.Length; i++)
        {
            for (byte j = 0; j < palate[i].Length; j++)
            {
                // 2 потому что
                res.TryAdd(palate[i][j], (byte)((i << offset) | j));
            }
        }

        return res;
    }


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

        var redSorter = new Comparison<Color>((Color first, Color second) => second.R.CompareTo(first.R));
        var greenSorter = new Comparison<Color>((Color first, Color second) => second.G.CompareTo(first.G));
        var blueSorter = new Comparison<Color>((Color first, Color second) => second.B.CompareTo(first.B));
        var ortherSorter = new Comparison<Color>((Color first, Color second) => (second.R + second.G + second.B).CompareTo(first.R + first.G + first.B));


        red.Sort(redSorter);
        green.Sort(greenSorter);
        blue.Sort(blueSorter);
        orther.Sort(ortherSorter);

        List<Color> toMove = new List<Color>();

        RemoveLessSuitable(red, toMove);
        RemoveLessSuitable(green, toMove);
        RemoveLessSuitable(blue, toMove);
        RemoveLessSuitable(orther, toMove);

        AddSuitable(red, redSorter, toMove);
        AddSuitable(green, redSorter, toMove);
        AddSuitable(blue, redSorter, toMove);
        AddSuitable(orther, redSorter, toMove);

        return new Color[4][]
        {
                red.ToArray(),
                green.ToArray(),
                blue.ToArray(),
                orther.ToArray()
        };
    }

    private static void AddSuitable(List<Color> colorFamaly, Comparison<Color> colorFamalySorter, List<Color> toMove)
    {
        if (colorFamaly.Count < 4)
        {
            toMove.Sort(colorFamalySorter);

            while (colorFamaly.Count < 4)
            {
                colorFamaly.Add(toMove.First());
                toMove.RemoveAt(0);
            }
        }
    }

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
