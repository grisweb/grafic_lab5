namespace grafic_lab2.Images.ColorExtracters
{
    public class BMP24imageColorCompressor
    {
        public Color[]? Palette { get; private set; }
        public Dictionary<Color, Color>? CompressedColors { get; private set; }

        private BMP24image _image;
        private int _clusters;

        public BMP24imageColorCompressor(BMP24image image, int clusters)
        {
            _image = image;
            _clusters = clusters;
        }

        public void Run()
        {
            byte[] buffer = _image.Bitmap;

            // среднее значение цветов в кластерах
            Color[] means = new Color[_clusters];
            // вычисленное на данной итерации значение цветов в кластерах
            Color[] new_means = new Color[_clusters];


            // инициализация алгоритма
            // выделение случайных средних значений для кластеров
            for (int i = 0; i < _clusters; i++)
            {
                Color init_mean = RGBColorCreator.GetRandomColor();

                // значения не должны повторяться на начале алгоритма
                while (means.Contains(init_mean))
                {
                    init_mean = RGBColorCreator.GetRandomColor();
                }

                means[i] = init_mean;
            }

            // цвета в кластере
            List<Color>[] samples = new List<Color>[_clusters];

            // сумма расстояний между новым и старым значениями цветов
            double error = 0;
            bool isEnd = false;

            while (!isEnd)
            {
                // создание пустых кластеров
                for (int i = 0; i < _clusters; i++)
                {
                    samples[i] = new List<Color>();
                }

                // распределение пикселей по кластерам
                for (int i = 0; i < buffer.Length; i += 3)
                {
                    // текущий пиксель
                    Color cur = Color.FromArgb(buffer[i + 2], buffer[i + 1], buffer[i]);

                    double norm = double.MaxValue;
                    int cluster = 0;

                    // цвет принадлежит к тому кластеру, у которого среднее значение цвета в кластере ближе к данному значению
                    for (int j = 0; j < _clusters; j++)
                    {
                        double temp = Distance(means[j], cur);

                        if (norm > temp)
                        {
                            norm = temp;
                            cluster = j;
                        }
                    }

                    samples[cluster].Add(cur);
                }

                // подсчёт новых значений цветов кластеров
                for (int i = 0; i < _clusters; i++)
                {
                    new_means[i] = Average(samples[i]);
                }

                double new_error = 0;

                // вычисление суммы расстояния между новыми и старыми средними значениями кластеров
                for (int i = 0; i < _clusters; i++)
                {
                    new_error += Distance(means[i], new_means[i]);
                    means[i] = new_means[i];
                }

                // если особых изменений не произошло, то алгоритм заканчивает работу
                isEnd = Math.Abs(error - new_error) < 1;

                error = new_error;
            }

            // результаты

            // палитра состоит из средних значений кластеров
            Palette = means;

            // словарь цвет - цвет из палитры
            CompressedColors = new Dictionary<Color, Color>();

            for (int i = 0; i < _clusters; i++)
            {
                for (int j = 0; j < samples[i].Count; j++)
                {
                    CompressedColors.TryAdd(samples[i][j], means[i]);
                }
            }
        }

        // вычисление среднего статистического цвета
        private static Color Average(IList<Color> colors)
        {
            long red = 0, green = 0, blue = 0;

            for (int i = 0; i < colors.Count; i++)
            {
                red += colors[i].R;
                green += colors[i].G;
                blue += colors[i].B;
            }

            int count = colors.Count > 0 ? colors.Count : 1;

            return Color.FromArgb((int)(red / count), (int)(green / count), (int)(blue / count));
        }

        // Определение расстояние между цветами
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
