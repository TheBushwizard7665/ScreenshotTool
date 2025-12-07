using System;
using System.Collections;
using System.Windows.Forms;

namespace ScreenshotTool
{
    public sealed class MediaListViewComparer : IComparer
    {
        private readonly int column;
        private readonly bool ascending;

        public MediaListViewComparer(int column, bool ascending)
        {
            this.column = column;
            this.ascending = ascending;
        }

        public int Compare(object? x, object? y)
        {
            if (x is not ListViewItem a || y is not ListViewItem b)
                return 0;

            int sign = ascending ? 1 : -1;

            string ax = a.SubItems[column].Text;
            string bx = b.SubItems[column].Text;

            // Date column (index 4) is "yyyy-MM-dd HH:mm"
            if (column == 4 &&
                DateTime.TryParse(ax, out var ad) &&
                DateTime.TryParse(bx, out var bd))
            {
                return sign * DateTime.Compare(ad, bd);
            }

            return sign * string.Compare(ax, bx, StringComparison.OrdinalIgnoreCase);
        }
    }
}
