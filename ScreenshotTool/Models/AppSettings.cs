using System;

namespace ScreenshotTool
{
    public sealed class AppSettings
    {
        public int MaxFileCount { get; set; } = 500;
        public int MaxAgeDays { get; set; } = 90;

        public int LivePreviewIntervalMs { get; set; } = 800;

        public bool CompactMode { get; set; } = false;
        public string LastMediaView { get; set; } = "Screenshots";

        public bool BringToFrontAfterHotkey { get; set; } = true;

        public bool RecordingIncludeAudio { get; set; } = true;
    }
}
