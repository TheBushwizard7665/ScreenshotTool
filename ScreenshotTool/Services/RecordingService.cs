using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using ScreenRecorderLib;

namespace ScreenshotTool.Services
{
    public class RecordingService : IRecordingService
    {
        private Recorder? _recorder;
        private bool _isRecording;

        public bool IsRecording => _isRecording;

        public void StartRecording(Screen screen, string outputFolder, bool includeAudio, Action<string> onComplete, Action<string> onError)
        {
            if (_isRecording) return;

            try
            {
                int index = Array.IndexOf(Screen.AllScreens, screen);
                if (index < 0) index = 0;

                string monitorFolder = Path.Combine(outputFolder, $"Monitor{index + 1}");
                Directory.CreateDirectory(monitorFolder);

                // Use MMddyy_HHmmss for shorter filenames
                string timestamp = DateTime.Now.ToString("MMddyy_HHmmss");
                string filePath = Path.Combine(monitorFolder, $"Rec_M{index + 1}_{timestamp}.mp4");

                var displays = Recorder.GetDisplays();
                DisplayRecordingSource displaySource =
                    displays.FirstOrDefault(d => string.Equals(d.DeviceName,
                                                               screen.DeviceName,
                                                               StringComparison.OrdinalIgnoreCase))
                    ?? displays.ElementAtOrDefault(index)
                    ?? displays.First();

                var options = new RecorderOptions
                {
                    SourceOptions = new SourceOptions
                    {
                        RecordingSources = new List<RecordingSourceBase> { displaySource }
                    },
                    AudioOptions = new AudioOptions
                    {
                        IsAudioEnabled = includeAudio,
                        IsInputDeviceEnabled = includeAudio,
                        IsOutputDeviceEnabled = includeAudio
                    }
                };

                _recorder = Recorder.CreateRecorder(options);

                _recorder.OnRecordingComplete += (s, e) =>
                {
                    _isRecording = false;
                    onComplete?.Invoke(e.FilePath);
                };

                _recorder.OnRecordingFailed += (s, e) =>
                {
                    _isRecording = false;
                    onError?.Invoke(e.Error);
                };

                _recorder.Record(filePath);
                _isRecording = true;
            }
            catch (Exception ex)
            {
                _isRecording = false;
                onError?.Invoke(ex.Message);
            }
        }

        public void StopRecording()
        {
            if (!_isRecording || _recorder == null) return;

            try
            {
                _recorder.Stop();
            }
            catch
            {
                // handle or log
            }
        }
    }
}
