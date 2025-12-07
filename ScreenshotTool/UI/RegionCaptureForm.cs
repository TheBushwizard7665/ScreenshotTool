using System;
using System.Drawing;
using System.Windows.Forms;

namespace ScreenshotTool
{
    public sealed class RegionCaptureForm : Form
    {
        private readonly Screen targetScreen;
        private bool isDragging;
        private Point dragStart;
        private Rectangle selectedRegion;

        public Rectangle SelectedRegion => selectedRegion;

        public RegionCaptureForm(Screen screen)
        {
            targetScreen = screen;

            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            Bounds = screen.Bounds;
            DoubleBuffered = true;
            TopMost = true;
            Opacity = 0.35;
            BackColor = Color.Black;
            Cursor = Cursors.Cross;

            KeyPreview = true;
            KeyDown += RegionCaptureForm_KeyDown;

            MouseDown += RegionCaptureForm_MouseDown;
            MouseMove += RegionCaptureForm_MouseMove;
            MouseUp += RegionCaptureForm_MouseUp;
        }

        private void RegionCaptureForm_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                DialogResult = DialogResult.Cancel;
                Close();
            }
        }

        private void RegionCaptureForm_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
                return;

            isDragging = true;
            dragStart = e.Location;
            selectedRegion = Rectangle.Empty;
            Invalidate();
        }

        private void RegionCaptureForm_MouseMove(object? sender, MouseEventArgs e)
        {
            if (!isDragging)
                return;

            int x1 = Math.Min(dragStart.X, e.X);
            int y1 = Math.Min(dragStart.Y, e.Y);
            int x2 = Math.Max(dragStart.X, e.X);
            int y2 = Math.Max(dragStart.Y, e.Y);

            selectedRegion = new Rectangle(x1, y1, x2 - x1, y2 - y1);
            Invalidate();
        }

        private void RegionCaptureForm_MouseUp(object? sender, MouseEventArgs e)
        {
            if (!isDragging)
                return;

            isDragging = false;

            if (selectedRegion.Width > 0 && selectedRegion.Height > 0)
            {
                // translate from local coordinates to screen coordinates
                var screenRect = new Rectangle(
                    selectedRegion.Left + targetScreen.Bounds.Left,
                    selectedRegion.Top + targetScreen.Bounds.Top,
                    selectedRegion.Width,
                    selectedRegion.Height);

                selectedRegion = screenRect;
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                DialogResult = DialogResult.Cancel;
                Close();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (selectedRegion.Width <= 0 || selectedRegion.Height <= 0)
                return;

            // draw selection rectangle in form coordinates
            Rectangle local = new Rectangle(
                selectedRegion.Left - targetScreen.Bounds.Left,
                selectedRegion.Top - targetScreen.Bounds.Top,
                selectedRegion.Width,
                selectedRegion.Height);

            using var pen = new Pen(Color.Lime, 2);
            using var brush = new SolidBrush(Color.FromArgb(50, Color.Lime));

            e.Graphics.DrawRectangle(pen, local);
            e.Graphics.FillRectangle(brush, local);
        }
    }
}
