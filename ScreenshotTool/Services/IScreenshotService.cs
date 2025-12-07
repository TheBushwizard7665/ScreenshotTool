using System.Drawing;
using System.Windows.Forms;

namespace ScreenshotTool
{
    public interface IScreenshotService
    {
        string CaptureScreen(Screen screen, string outputFolder);
        string CaptureRegion(Screen screen, Rectangle region, string outputFolder);
    }
}
