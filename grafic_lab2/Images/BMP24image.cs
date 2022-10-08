using System.IO;

namespace grafic_lab2.Images;

public class BMP24image : IBitmatable
{
    private Header _header;
    private InforHeader _inforHeader;

    public byte[] Bitmap { get; private set; }
    public int Width => (int)_inforHeader.ImgWidth;
    public int Height => (int)_inforHeader.ImgHeight;

    private BMP24image() { }

    /// <summary>
    /// создать из файла
    /// </summary>
    /// <param name="filepath">путь</param>
    /// <returns>объект BMP24image хранящий мета-информацию и битовую карту</returns>
    public static BMP24image Create(string filepath)
    {
        BMP24image res = new BMP24image();

        using (BinaryReader br = new BinaryReader(File.OpenRead(filepath)))
        {
            res._header.ReadFrom(br);

            // проверка типа файле
            if (res._header.FileType == 0x4d42)
            {
                return null;
            }

            res._inforHeader.ReadFrom(br);

            if (res._inforHeader.BitPerPixel != 24)
            {
                return null;
            }

            //вычисление количества байтов в строке
            int bytes = (int)(res._inforHeader.ImgWidth * 3);

            //вычисление количества "дополнительных" байтов
            // выравнивание до 32 бит (4 байт)
            int align = (4 * ((bytes + 3) / 4)) - bytes;

            //определение размеров пиксельных данных и строки
            res.Bitmap = new byte[bytes * (int)res._inforHeader.ImgHeight];

            //цикл: последовательное считывание строк изображения
            for (int line = 0; line < res._inforHeader.ImgHeight; line++)
            {
                var scanline = br.ReadBytes(bytes);

                //если существует выравнивание
                if (align > 0)
                {
                    br.ReadBytes(align);
                }

                scanline.CopyTo(res.Bitmap, line * bytes);
            }

            return res;
        } 
    }

    public Bitmap ToBitmap()
    {
        Bitmap res = new Bitmap(Width, Height);

        for (int y = 0; y < res.Height; y++)
        {
            for (int x = 0; x < res.Width; x++)
            {
                int index = (x + (res.Height - 1 - y) * res.Width) * 3;

                res.SetPixel(x, y, Color.FromArgb(Bitmap[index + 2], Bitmap[index + 1], Bitmap[index]));
            }
        }

        return res;
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