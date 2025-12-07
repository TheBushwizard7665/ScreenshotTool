using System.Windows.Forms;

namespace ScreenshotTool
{
    public sealed class MonitorItem
    {
        public Screen Screen { get; }
        public int Index { get; }

        public MonitorItem(Screen screen, int index)
        {
            Screen = screen;
            Index = index;
        }

        public override string ToString()
        {
            // e.g. "Monitor 1 - 1920x1080"
            var b = Screen.Bounds;
            return $"Monitor {Index + 1} - {b.Width}x{b.Height}";
        }
    }
}
