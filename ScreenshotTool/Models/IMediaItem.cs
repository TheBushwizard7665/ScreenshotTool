using System;

namespace ScreenshotTool
{
    public interface IMediaItem
    {
        string FullPath { get; }
        string FileName { get; }
        string DisplayName { get; }

        string MonitorLabel { get; }
        string MonitorShortLabel { get; }

        string FolderLabel { get; }

        DateTime Created { get; }
    }
}
