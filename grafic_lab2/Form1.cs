using grafic_lab2.Images;
using System.IO;

namespace grafic_lab2;

public partial class Form1 : Form
{
    private bool _isOpen = false;
    PGSimage? _PGSimage = null;

    public Form1()
    {
        InitializeComponent();

        openFileDialog1.Filter = $"Bitmap files (*.bmp)|*.bmp|MyImage files(*.{PGSimage.FileExtention})|*.{PGSimage.FileExtention}";
        saveFileDialog1.Filter = $"MyImage files(*.{PGSimage.FileExtention})|*.{PGSimage.FileExtention}";
    }

    private void Open_Click(object sender, EventArgs e)
    {
        if (openFileDialog1.ShowDialog() == DialogResult.Cancel)
            return;

        // получаем выбранный файл
        string filename = openFileDialog1.FileName;

        _PGSimage = null;

        if (filename.EndsWith(".bmp"))
        {
            var bit = BMP24image.Create(filename);

            _PGSimage = PGSimage.Create(bit);

            _isOpen = true;
        }
        else if (filename.EndsWith(PGSimage.FileExtention))
        {
            _PGSimage = PGSimage.Create(filename);

            _isOpen = true;
        }

        pictureBox1.Image = _PGSimage?.ToBitmap();
    }

    private void Save_Click(object sender, EventArgs e)
    {
        if (_isOpen)
        {
            if (saveFileDialog1.ShowDialog() == DialogResult.Cancel)
                return;
            // получаем выбранный файл
            string filename = saveFileDialog1.FileName;

            _PGSimage?.Save(filename);
        }
    }
}