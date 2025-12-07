using System;
using System.Drawing;
using System.Windows.Forms;

namespace ScreenshotTool
{
    /// <summary>
    /// Full-screen overlay on a specific monitor that lets the user drag
    /// to select a rectangular region. Press Esc to cancel.
    /// </summary>
    public class RegionCaptureForm : Form
    {
        private readonly Rectangle monitorBounds;
        private bool drawing;
        private Point startPoint;
        private Point currentPoint;

        public bool HasRegion { get; private set; }
        /// <summary>
        /// Selected rectangle in coordinates relative to the monitor's top-left.
        /// </summary>
        public Rectangle SelectedRegionLocal { get; private set; }

        public RegionCaptureForm(Screen monitor)
        {
            monitorBounds = monitor.Bounds;

            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            Bounds = monitorBounds;
            TopMost = true;
            DoubleBuffered = true;
            BackColor = Color.Black;
            Opacity = 0.15;
            Cursor = Cursors.Cross;
            KeyPreview = true;

            // Capture mouse events
            MouseDown += RegionCaptureForm_MouseDown;
            MouseMove += RegionCaptureForm_MouseMove;
            MouseUp += RegionCaptureForm_MouseUp;
            KeyDown += RegionCaptureForm_KeyDown;
        }

        private void RegionCaptureForm_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                HasRegion = false;
                DialogResult = DialogResult.Cancel;
                Close();
            }
        }

        private void RegionCaptureForm_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
                return;

            drawing = true;
            startPoint = e.Location;
            currentPoint = e.Location;
            Invalidate();
        }

        private void RegionCaptureForm_MouseMove(object? sender, MouseEventArgs e)
        {
            if (!drawing)
                return;

            currentPoint = e.Location;
            Invalidate();
        }

        private void RegionCaptureForm_MouseUp(object? sender, MouseEventArgs e)
        {
            if (!drawing || e.Button != MouseButtons.Left)
                return;

            drawing = false;
            currentPoint = e.Location;

            Rectangle rect = GetCurrentRect();
            if (rect.Width > 0 && rect.Height > 0)
            {
                SelectedRegionLocal = rect;
                HasRegion = true;
                DialogResult = DialogResult.OK;
            }
            else
            {
                HasRegion = false;
                DialogResult = DialogResult.Cancel;
            }

            Close();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (!drawing)
                return;

            Rectangle rect = GetCurrentRect();
            if (rect.Width <= 0 || rect.Height <= 0)
                return;

            using var borderPen = new Pen(Color.Lime, 2);
            using var fillBrush = new SolidBrush(Color.FromArgb(60, Color.Lime));

            e.Graphics.DrawRectangle(borderPen, rect);
            e.Graphics.FillRectangle(fillBrush, rect);
        }

        private Rectangle GetCurrentRect()
        {
            int x1 = startPoint.X;
            int y1 = startPoint.Y;
            int x2 = currentPoint.X;
            int y2 = currentPoint.Y;

            int left = Math.Min(x1, x2);
            int top = Math.Min(y1, y2);
            int right = Math.Max(x1, x2);
            int bottom = Math.Max(y1, y2);

            return new Rectangle(left, top, right - left, bottom - top);
        }
    }
}
