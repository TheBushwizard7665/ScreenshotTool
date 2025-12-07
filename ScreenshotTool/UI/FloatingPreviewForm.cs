using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace ScreenshotTool
{
    public sealed class FloatingPreviewForm : Form
    {
        private readonly string path;
        private PictureBox picture = null!;

        public static Rectangle? LastBounds { get; set; }

        public FloatingPreviewForm(string path)
        {
            this.path = path;

            Text = Path.GetFileName(path);
            StartPosition = FormStartPosition.Manual;

            picture = new PictureBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Black,
                SizeMode = PictureBoxSizeMode.Zoom
            };

            Controls.Add(picture);

            BackColor = Color.Black;

            Load += FloatingPreviewForm_Load;
            FormClosing += FloatingPreviewForm_FormClosing;
        }

        private void FloatingPreviewForm_Load(object? sender, EventArgs e)
        {
            if (!File.Exists(path))
                return;

            try
            {
                picture.Image = Image.FromFile(path);
            }
            catch
            {
                // ignore
            }
        }

        private void FloatingPreviewForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            LastBounds = Bounds;
            if (picture.Image != null)
            {
                picture.Image.Dispose();
                picture.Image = null;
            }
        }
    }
}
