using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace ScreenshotTool
{
    public partial class MainForm : Form
    {
        // ========= MONITORS + CAPTURE =========

        private void PopulateMonitorList()
        {
            comboMonitors.Items.Clear();
            var screens = Screen.AllScreens;
            for (int i = 0; i < screens.Length; i++)
            {
                comboMonitors.Items.Add(new MonitorItem(screens[i], i));
            }

            if (comboMonitors.Items.Count > 0)
                comboMonitors.SelectedIndex = 0;
        }

        private void ComboMonitors_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (chkLivePreview.Checked)
                RefreshLivePreview();
        }

        private void BtnCapture_Click(object? sender, EventArgs e)
        {
            if (comboMonitors.SelectedItem is not MonitorItem mi)
                return;

            CaptureMonitor(mi.Screen);
        }

        /// <summary>
        /// NEW: Capture button for drag-select region.
        /// </summary>
        private void BtnCaptureRegion_Click(object? sender, EventArgs e)
        {
            if (comboMonitors.SelectedItem is not MonitorItem mi)
                return;

            // Show an overlay on this monitor that lets the user drag out a rectangle
            using var overlay = new RegionCaptureForm(mi.Screen);
            if (overlay.ShowDialog(this) == DialogResult.OK && overlay.HasRegion)
            {
                // overlay.SelectedRegionLocal is relative to the monitor's top-left corner
                Rectangle local = overlay.SelectedRegionLocal;
                if (local.Width <= 0 || local.Height <= 0)
                    return;

                // Convert to absolute screen coordinates
                Rectangle absolute = new Rectangle(
                    mi.Screen.Bounds.X + local.X,
                    mi.Screen.Bounds.Y + local.Y,
                    local.Width,
                    local.Height);

                CaptureRegion(mi.Screen, absolute);
            }
        }

        private void CaptureMonitor(Screen screen)
        {
            try
            {
                int index = Array.IndexOf(Screen.AllScreens, screen);
                if (index < 0) index = 0;

                string monitorFolder = Path.Combine(screenshotsFolder, $"Monitor{index + 1}");
                Directory.CreateDirectory(monitorFolder);

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string filePath = Path.Combine(monitorFolder, $"Screen_{timestamp}.png");

                using var bmp = new Bitmap(screen.Bounds.Width, screen.Bounds.Height);
                using (var g = Graphics.FromImage(bmp))
                {
                    g.CopyFromScreen(screen.Bounds.Location, Point.Empty, screen.Bounds.Size);
                }
                bmp.Save(filePath);

                RunAutoCleanup();
                LoadScreenshotList();

                if (currentView == MediaViewMode.Screenshots)
                    SelectLatestInList();

                SetFileStatus($"Saved {Path.GetFileName(filePath)}");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error capturing screen: " + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Capture just a rectangular region on the given screen.
        /// regionOnScreen is in absolute screen coordinates.
        /// </summary>
        private void CaptureRegion(Screen screen, Rectangle regionOnScreen)
        {
            try
            {
                if (regionOnScreen.Width <= 0 || regionOnScreen.Height <= 0)
                    return;

                int index = Array.IndexOf(Screen.AllScreens, screen);
                if (index < 0) index = 0;

                string monitorFolder = Path.Combine(screenshotsFolder, $"Monitor{index + 1}");
                Directory.CreateDirectory(monitorFolder);

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string filePath = Path.Combine(monitorFolder, $"Region_{timestamp}.png");

                using var bmp = new Bitmap(regionOnScreen.Width, regionOnScreen.Height);
                using (var g = Graphics.FromImage(bmp))
                {
                    g.CopyFromScreen(regionOnScreen.Location, Point.Empty, regionOnScreen.Size);
                }
                bmp.Save(filePath);

                RunAutoCleanup();
                LoadScreenshotList();

                if (currentView == MediaViewMode.Screenshots)
                    SelectLatestInList();

                SetFileStatus($"Saved region {Path.GetFileName(filePath)}");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error capturing region: " + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CaptureMonitorUnderCursor()
        {
            var screen = Screen.FromPoint(Cursor.Position);
            CaptureMonitor(screen);

            if (settings.BringToFrontAfterHotkey)
            {
                ShowFromTray();
            }
        }

        // ========= SCREENSHOT LIST =========

        private void LoadScreenshotList()
        {
            allScreenshots.Clear();
            if (!Directory.Exists(screenshotsFolder))
                return;

            var files = Directory.GetFiles(screenshotsFolder, "*.png", SearchOption.AllDirectories);
            Array.Sort(files, (a, b) => File.GetCreationTime(b).CompareTo(File.GetCreationTime(a)));
            foreach (var f in files)
                allScreenshots.Add(new ScreenshotItem(f));

            if (currentView == MediaViewMode.Screenshots)
                ApplySearchFilter();
        }

        // ========= SCREENSHOT PREVIEW =========

        private void ShowPreviewForFile(string path)
        {
            try
            {
                if (!File.Exists(path))
                    return;

                string ext = Path.GetExtension(path).ToLowerInvariant();

                // Only show image preview if we're not in live preview mode
                if (chkLivePreview.Checked)
                    return;

                if (ext is ".png" or ".jpg" or ".jpeg" or ".bmp" or ".gif")
                {
                    var player = mediaPlayer?.Player;
                    if (player != null)
                    {
                        try { player.controls.stop(); } catch { }
                    }

                    mediaPlayer.Visible = false;
                    picturePreview.Visible = true;

                    picturePreview.Image?.Dispose();
                    try
                    {
                        using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        using (var img = Image.FromStream(fs))
                        {
                            picturePreview.Image = new Bitmap(img);
                        }
                    }
                    catch
                    {
                        picturePreview.Image = null;
                    }
                }
                else
                {
                    picturePreview.Image?.Dispose();
                    picturePreview.Image = null;
                    mediaPlayer.Visible = false;
                }
            }
            catch
            {
                // ignore
            }
        }

        // ========= LIVE PREVIEW =========

        private void ChkLivePreview_CheckedChanged(object? sender, EventArgs e)
        {
            if (chkLivePreview.Checked && !settings.CompactMode)
            {
                livePreviewTimer.Interval = settings.LivePreviewIntervalMs;
                livePreviewTimer.Start();
            }
            else
            {
                livePreviewTimer.Stop();
            }
        }

        private void RefreshLivePreview()
        {
            if (!chkLivePreview.Checked || settings.CompactMode)
                return;

            if (comboMonitors.SelectedItem is not MonitorItem mi)
                return;

            try
            {
                var screen = mi.Screen;
                using var bmp = new Bitmap(screen.Bounds.Width, screen.Bounds.Height);
                using (var g = Graphics.FromImage(bmp))
                {
                    g.CopyFromScreen(screen.Bounds.Location, Point.Empty, screen.Bounds.Size);
                }

                var player = mediaPlayer?.Player;
                if (player != null)
                {
                    try { player.controls.stop(); } catch { }
                }
                mediaPlayer.Visible = false;
                picturePreview.Visible = true;

                picturePreview.Image?.Dispose();
                picturePreview.Image = new Bitmap(bmp);
            }
            catch
            {
                // ignore
            }
        }

        // ========= OPEN FOLDERS / CLEANUP =========

        private void RunAutoCleanup()
        {
            try
            {
                if (!Directory.Exists(screenshotsFolder))
                    return;

                var files = Directory.GetFiles(screenshotsFolder, "*.png", SearchOption.AllDirectories)
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(fi => fi.CreationTimeUtc)
                    .ToList();

                if (files.Count > settings.MaxFileCount)
                {
                    foreach (var fi in files.Skip(settings.MaxFileCount))
                    {
                        try { fi.Delete(); } catch { }
                    }
                }

                DateTime cutoff = DateTime.Now.AddDays(-settings.MaxAgeDays);
                foreach (var fi in files)
                {
                    if (fi.CreationTime < cutoff)
                    {
                        try { fi.Delete(); } catch { }
                    }
                }
            }
            catch
            {
            }
        }

        private void BtnOpenFolder_Click(object? sender, EventArgs e)
        {
            if (comboMonitors.SelectedItem is not MonitorItem mi)
                return;

            string monitorFolder = Path.Combine(screenshotsFolder, $"Monitor{mi.Index + 1}");
            Directory.CreateDirectory(monitorFolder);
            Process.Start("explorer.exe", $"\"{monitorFolder}\"");
        }

        private void BtnOpenAllFolders_Click(object? sender, EventArgs e)
        {
            if (!Directory.Exists(screenshotsFolder))
                return;

            Process.Start("explorer.exe", $"\"{screenshotsFolder}\"");
        }
    }

    /// <summary>
    /// Full-screen overlay on a specific monitor that lets the user drag
    /// to select a rectangular region. Press Esc to cancel.
    /// </summary>
    internal class RegionCaptureForm : Form
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
