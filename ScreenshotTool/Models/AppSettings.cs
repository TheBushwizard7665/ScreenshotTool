using System;

namespace ScreenshotTool
{
    public sealed class AppSettings
    {
        public int MaxFileCount { get; set; } = 500;
        public int MaxAgeDays { get; set; } = 90;

        public int LivePreviewIntervalMs { get; set; } = 800;

        public string LastMediaView { get; set; } = "Screenshots";

        public bool BringToFrontAfterHotkey { get; set; } = true;

        public bool RecordingIncludeAudio { get; set; } = true;

        public int VideoBitrate { get; set; } = 8000000; // 8 Mbps
        public int VideoFramerate { get; set; } = 60;
    }
}
