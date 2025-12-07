using System;
using System.IO;

namespace ScreenshotTool
{
    public sealed class RecordingItem : IMediaItem
    {
        public string FullPath { get; }
        public string FileName => Path.GetFileName(FullPath);
        public string DisplayName => FileName;

        public string MonitorLabel { get; }
        public string MonitorShortLabel { get; }

        public string TagLabel { get; }
        public string FolderLabel { get; }

        public DateTime Created { get; }

        public RecordingItem(string path)
        {
            FullPath = path;
            var fi = new FileInfo(path);
            Created = fi.CreationTime;

            string dir = fi.Directory?.FullName ?? "";
            string monitor = "";
            string folder = "";

            var parts = dir.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            for (int i = parts.Length - 1; i >= 0; i--)
            {
                if (parts[i].StartsWith("Monitor", StringComparison.OrdinalIgnoreCase))
                {
                    monitor = parts[i];
                    if (i + 1 < parts.Length)
                        folder = parts[i + 1];
                    break;
                }
            }

            if (string.IsNullOrEmpty(monitor))
            {
                MonitorLabel = "Unknown monitor";
                MonitorShortLabel = "-";
            }
            else
            {
                MonitorLabel = monitor;
                MonitorShortLabel = monitor.Replace("Monitor", "M");
            }

            FolderLabel = string.IsNullOrEmpty(folder) ? "-" : folder;
            TagLabel = "Recording";
        }
    }
}
