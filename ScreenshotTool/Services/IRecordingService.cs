using System;
using System.Windows.Forms;
using ScreenRecorderLib;

namespace ScreenshotTool.Services
{
    public interface IRecordingService
    {
        void StartRecording(Screen screen, string outputFolder, bool includeAudio, Action<string> onComplete, Action<string> onError);
        void StopRecording();
        bool IsRecording { get; }
    }
}
