using System;
using System.Windows.Forms;
using ScreenRecorderLib;

namespace ScreenshotTool
{
    public interface IRecordingService
    {
        void StartRecording(Screen screen, string outputFolder, bool includeAudio, int bitrate, int framerate, Action<string> onComplete, Action<string> onError);
        void StopRecording();
        bool IsRecording { get; }
    }
}
