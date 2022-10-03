using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace grafic_lab2.Images.ColorExtracters
{
    public class BitmapColorCompressor
    {
        public Color[]? Palette { get; private set; }
        public Dictionary<Color, Color>? CompressedColors { get; private set; }

        Bitmap image;
        int clusters;

        public BitmapColorCompressor(Bitmap image, int clusters)
        {
            this.image = image;
            this.clusters = clusters;
        }

        public void Run()
        {
            int w = image.Width;
            int h = image.Height;


            // копирование массива битов
            BitmapData image_data = image.LockBits(
                new Rectangle(0, 0, w, h),
                ImageLockMode.ReadOnly,
                PixelFormat.Format24bppRgb);

            int bytes = image_data.Stride * image_data.Height;
            byte[] buffer = new byte[bytes];

            Marshal.Copy(image_data.Scan0, buffer, 0, bytes);
            image.UnlockBits(image_data);

            // биты для формирования итогого изображения
//            byte[] result = new byte[bytes];
            // палитра
            Color[] means = new Color[clusters];

            // выделение случайных кластеров
            for (int i = 0; i < clusters; i++)
            {
                Color init_mean = RGBColorCreator.GetRandomColor();

                while (means.Contains(init_mean))
                {
                    init_mean = RGBColorCreator.GetRandomColor();
                }

                means[i] = init_mean;
            }

            double error = 0;
            // цвета в кластере
            List<Color>[] samples = new List<Color>[clusters];

            while (true)
            {
                for (int i = 0; i < clusters; i++)
                {
                    samples[i] = new List<Color>();
                }

                for (int i = 0; i < bytes; i += 3)
                {
                    double norm = double.MaxValue;
                    int cluster = 0;

                    Color cur = Color.FromArgb(buffer[i + 2], buffer[i + 1], buffer[i]);

                    for (int j = 0; j < clusters; j++)
                    {
                        double temp = Distance(means[j], cur);

                        //double temp = Math.Sqrt(
                        //    (buffer[i + 2] - means[j].R) * (buffer[i + 2] - means[j].R)
                        //     + (buffer[i + 1] - means[j].G) * (buffer[i + 1] - means[j].G)
                        //     + (buffer[i] - means[j].B) * (buffer[i] - means[j].B)
                        //);


                        if (norm > temp)
                        {
                            norm = temp;
                            cluster = j;
                        }
                    }

                    samples[cluster].Add(cur);

                    //result[i + 2] = means[cluster].R;
                    //result[i + 1] = means[cluster].G;
                    //result[i] = means[cluster].B;
                }

                Color[] new_means = new Color[clusters];

                // подсчёт новых значений
                for (int i = 0; i < clusters; i++)
                {
                    new_means[i] = Average(samples[i]);
                }

                double new_error = 0;

                for (int i = 0; i < clusters; i++)
                {
                    new_error += Distance(means[i], new_means[i]);
                    means[i] = new_means[i];
                }

                if (Math.Abs(error - new_error) < 1)
                {
                    break;
                }
                else
                {
                    error = new_error;
                }
            }
            
            // результаты
            Palette = means;

            CompressedColors = new Dictionary<Color, Color>();

            for (int i = 0; i < clusters; i++)
            {
                for (int j = 0; j < samples[i].Count; j++)
                {
                    CompressedColors.TryAdd(samples[i][j], means[i]);
                }
            }
        }

        private static Color Average(IList<Color> colors)
        {
            long red = 0, green = 0, blue = 0;

            for (int i = 0; i < colors.Count; i++)
            {
                red += colors[i].R;
                green += colors[i].G;
                blue += colors[i].B;
            }

            return Color.FromArgb((int)(red / (colors.Count + 1)), (int)(green / (colors.Count + 1)), (int)(blue / (colors.Count + 1)));
        }

        private static double Distance(Color first, Color second)
        {
            return Math.Sqrt(
                (first.R - second.R) * (first.R - second.R)
                    + (first.G - second.G) * (first.G - second.G)
                    + (first.B - second.B) * (first.B - second.B)
            );
        }
    }
}
