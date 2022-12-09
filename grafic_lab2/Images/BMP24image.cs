using System.IO;

namespace grafic_lab2.Images;

public class BMP24image : IBitmatable
{
    private Header _header;
    private InforHeader _inforHeader;

    public Pixel[][]? Bitmap { get; private set; }
    public int Width => (int)_inforHeader.ImgWidth;
    public int Height => (int)_inforHeader.ImgHeight;

    private BMP24image() { }

    /// <summary>
    /// создать из файла
    /// </summary>
    /// <param name="filepath">путь</param>
    /// <returns>объект BMP24image хранящий мета-информацию и битовую карту</returns>
    public static BMP24image? Create(string filepath)
    {
        BMP24image res = new BMP24image();

        using (BinaryReader br = new BinaryReader(File.OpenRead(filepath)))
        {
            res._header.ReadFrom(br);

            // проверка типа файле
            if (res._header.FileType != 0x4d42)
            {
                return null;
            }

            res._inforHeader.ReadFrom(br);

            if (res._inforHeader.BitPerPixel != 24)
            {
                return null;
            }

            //вычисление количества байт в строке
            int bytes = (int)(res._inforHeader.ImgWidth * 3);

            //вычисление количества "дополнительных" байт
            // выравнивание до 32 бит (4 байт)
            int align = (4 * ((bytes + 3) / 4)) - bytes;

            //определение размеров пиксельных данных и строки
            res.Bitmap = new Pixel[res._inforHeader.ImgHeight][];

            //цикл: последовательное считывание строк изображения
            for (int line = 0; line < res._inforHeader.ImgHeight; line++)
            {
                res.Bitmap[line] = new Pixel[res._inforHeader.ImgWidth];

                var scanline = br.ReadBytes(bytes);

                //если существует выравнивание
                if (align > 0)
                {
                    br.ReadBytes(align);
                }

                for (int i = 0; i < scanline.Length; i += 3)
                {
                    res.Bitmap[line][i / 3] = new Pixel(scanline[i], scanline[i + 1], scanline[i + 2]);
                }
            }

            return res;
        }
    }

    public Bitmap ToBitmap()
    {
        Bitmap res = new Bitmap(Width, Height);

        if (Bitmap != null)
        {
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    var pixel = Bitmap[y][x];
                    res.SetPixel(x, res.Height - y - 1, Color.FromArgb(pixel.Red, pixel.Green, pixel.Blue));
                }
            }
        }

        return res;
    }

    public void ConvertToGray()
    {
        if (Bitmap != null)
        {
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    Bitmap[y][x].ConvertToGray();
                }
            }
        }
    }

    private double _coefficient = 1.0 / 9.0;
    int[,] _matrix = { { 1, 1, 1 }, { 1, 1, 1 }, { 1, 1, 1 } };

    private double H(int p, int q)
    {
        return _matrix[p, q];
    }

    private double Q(int x, int y)
    {
        const int m = 3;

        double result = 0;

        Pixel dummyPixel = new Pixel();

        if (Bitmap != null)
        {
            for (int p = 0; p < m; p++)
            {
                for (int q = 0; q < m; q++)
                {
                    var i = y - 1 + p;
                    var j = x - 1 + q;

                    if (i < 0 || j < 0 || i >= Height || j >= Width)
                    {
                        //result += dummyPixel.Red * H(p, q);
                    }
                    else
                    {
                        result += Bitmap[i][j].Red * H(p, q);
                    }
                }
            }
        }

        return result * _coefficient;
    }

    public void LinearFilter()
    {
        if (Bitmap != null)
        {
            Pixel[][] filteredBitmap = new Pixel[Height][];

            for (int y = 0; y < Height; y++)
            {
                filteredBitmap[y] = new Pixel[Width];

                for (var x = 0; x < Width; x++)
                {
                    //Bitmap[y][x].SetUInt((UInt32)Q(x, y));
                    var filterColor = Q(x, y);

                    byte color = (byte)filterColor;

                    //if (filterColor > 255.0)
                    //{
                    //    color = 255;
                    //}
                    //else if (filterColor < 0.0)
                    //{
                    //    color = (byte)filterColor;
                    //}


                    filteredBitmap[y][x] = new Pixel(color, color, color);
                }
            }

            Bitmap = filteredBitmap;
        }
    }

    public void Filter()
    {
        int[,] matrix = { { 1, 1, 1 }, { 1, 1, 1 }, { 1, 1, 1 } };
        _matrix = matrix;
        _coefficient = 1.0 / 9.0;
        LinearFilter();

        //int[,] matrix1 = { { 1, 1, 1 }, { 1, 2, 1 }, { 1, 1, 1 } };
        //_matrix = matrix1;
        //_coefficient = 1.0 / 10.0;
        //LinearFilter();

        int[,] matrix2 = { { 1, 2, 1 }, { 2, 4, 2 }, { 1, 2, 1 } };
        _matrix = matrix2;
        _coefficient = 1.0 / 10.0;
        LinearFilter();

        int[,] matrix3 = { { 2, 1, 2 }, { 1, 4, 1 }, { 2, 1, 2 } };
        _matrix = matrix3;
        _coefficient = 1.0 / 10.0;
        LinearFilter();

        //int[,] matrix4 = { { 0, -1, 0 }, { -1, 5, -1 }, { 0, -1, 0 } };
        //_matrix = matrix4;
        //_coefficient = 1.0 / 10.0;
        //LinearFilter();

        int[,] matrix5 = { { -1, -1, -1 }, { -1, 9, -1 }, { -1, -1, -1 } };
        _matrix = matrix5;
        _coefficient = 1.0 / 10.0;
        LinearFilter();

        //int[,] matrix6 = { { 1, -2, 1 }, { -2, 5, -2 }, { 1, -2, 1 } };
        //_matrix = matrix6;
        //_coefficient = 1.0 / 10.0;
        //LinearFilter();

        int[,] matrix7 = { { 1, -1, -1 }, { 1, -2, -1 }, { 1, 1, 1 } };
        _matrix = matrix7;
        _coefficient = 1.0 / 10.0;
        LinearFilter();

        //int[,] matrix8 = { { -1, -1, -1 }, { -1, 8, -1 }, { -1, -1, -1 } };
        //_matrix = matrix8;
        //_coefficient = 1.0 / 10.0;
        //LinearFilter();
    }

    private struct Header
    {
        public ushort FileType; //определяет сигнатуру файла
        public uint FileSize; //размер всего файла
        public ushort Reserved1; //зарезервировано
        public ushort Reserved2; //зарезервировано
        public uint DataOffset; //смещение пиксельных данных

        public void ReadFrom(BinaryReader binaryReader)
        {
            FileType = binaryReader.ReadUInt16();
            FileSize = binaryReader.ReadUInt32();
            Reserved1 = binaryReader.ReadUInt16();
            Reserved2 = binaryReader.ReadUInt16();
            DataOffset = binaryReader.ReadUInt32();
        }
    }

    private struct InforHeader
    {
        public uint StructSize; //размер этой структуры
        public uint ImgWidth; //ширина изображения в пикселях
        public uint ImgHeight; //высота изображения в пикселях
        public ushort ColorPlanes; //количество цветовых плоскостей
        public ushort BitPerPixel; //количество битов на пиксель
        public uint Compression;//определяет тип сжатия
        public uint ImageSize; //размер непосредственного изображения
        public uint DPIX; //количество пикселей на единицу расстояния
        public uint DPIY; //количество пикселей на единицу расстояния
        public uint Colors; //размер таблицы цветов
        public uint ColorsUsed; //количество задействованных цветов

        public void ReadFrom(BinaryReader binaryReader)
        {
            StructSize = binaryReader.ReadUInt32();
            ImgWidth = binaryReader.ReadUInt32();
            ImgHeight = binaryReader.ReadUInt32();
            ColorPlanes = binaryReader.ReadUInt16();
            BitPerPixel = binaryReader.ReadUInt16();
            Compression = binaryReader.ReadUInt32();
            ImageSize = binaryReader.ReadUInt32();
            DPIX = binaryReader.ReadUInt32();
            DPIY = binaryReader.ReadUInt32();
            Colors = binaryReader.ReadUInt32();
            ColorsUsed = binaryReader.ReadUInt32();
        }
    }
}