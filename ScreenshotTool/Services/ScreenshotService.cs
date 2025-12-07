using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace ScreenshotTool.Services
{
    public class ScreenshotService : IScreenshotService
    {
        public string CaptureScreen(Screen screen, string outputFolder)
        {
            return Capture(screen.Bounds, screen, outputFolder, "Screen");
        }

        public string CaptureRegion(Screen screen, Rectangle region, string outputFolder)
        {
            return Capture(region, screen, outputFolder, "Region");
        }

        private string Capture(Rectangle bounds, Screen screen, string outputFolder, string prefix)
        {
            int index = Array.IndexOf(Screen.AllScreens, screen);
            if (index < 0) index = 0;

            string monitorFolder = Path.Combine(outputFolder, $"Monitor{index + 1}");
            Directory.CreateDirectory(monitorFolder);

            // Use MMddyy_HHmmss for shorter filenames
            string timestamp = DateTime.Now.ToString("MMddyy_HHmmss");
            string fileName = $"{prefix}_{timestamp}.png";
            string filePath = Path.Combine(monitorFolder, fileName);

            using var bmp = new Bitmap(bounds.Width, bounds.Height);
            using (var g = Graphics.FromImage(bmp))
            {
                // Source point is bounds.Location (absolute screen coords)
                g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
            }
            bmp.Save(filePath);

            return filePath;
        }
    }
}
