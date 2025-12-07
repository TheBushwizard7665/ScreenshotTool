using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using ScreenRecorderLib;

namespace ScreenshotTool
{
    public partial class MainForm : Form
    {
        // ========= RECORD BUTTON =========

        private void BtnRecord_Click(object? sender, EventArgs e)
        {
            if (!isRecording)
                StartRecordingSelectedMonitor();
            else
                StopRecording();
        }

        // ========= START / STOP RECORDING =========

        private void StartRecordingSelectedMonitor()
        {
            if (isRecording)
                return;

            if (comboMonitors.SelectedItem is not MonitorItem mi)
            {
                MessageBox.Show("Please select a monitor first.", "Recording",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                int monitorIndex = mi.Index;

                string monitorFolder = Path.Combine(recordingsFolder, $"Monitor{monitorIndex + 1}");
                Directory.CreateDirectory(monitorFolder);

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string filePath = Path.Combine(monitorFolder, $"Rec_M{monitorIndex + 1}_{timestamp}.mp4");

                var displays = Recorder.GetDisplays();
                DisplayRecordingSource displaySource =
                    displays.FirstOrDefault(d => string.Equals(d.DeviceName,
                                                               mi.Screen.DeviceName,
                                                               StringComparison.OrdinalIgnoreCase))
                    ?? displays.ElementAtOrDefault(monitorIndex)
                    ?? displays.First();

                var options = new RecorderOptions
                {
                    SourceOptions = new SourceOptions
                    {
                        RecordingSources = new System.Collections.Generic.List<RecordingSourceBase> { displaySource }
                    },
                    AudioOptions = new AudioOptions
                    {
                        IsAudioEnabled = settings.RecordingIncludeAudio,
                        IsInputDeviceEnabled = settings.RecordingIncludeAudio,
                        IsOutputDeviceEnabled = settings.RecordingIncludeAudio
                    }
                };

                recorder = Recorder.CreateRecorder(options);

                recorder.OnRecordingComplete += (s, e) =>
                {
                    if (!IsHandleCreated) return;
                    BeginInvoke(new Action(() =>
                    {
                        isRecording = false;
                        btnRecord.Text = "Record";
                        LoadRecordingList();
                        if (currentView == MediaViewMode.Recordings)
                            SelectLatestInList();
                        SetFileStatus($"Recording saved: {Path.GetFileName(e.FilePath)}");
                    }));
                };

                recorder.OnRecordingFailed += (s, e) =>
                {
                    if (!IsHandleCreated) return;
                    BeginInvoke(new Action(() =>
                    {
                        isRecording = false;
                        btnRecord.Text = "Record";
                        MessageBox.Show("Recording failed:\n" + e.Error,
                            "Recording Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }));
                };

                recorder.Record(filePath);
                isRecording = true;
                btnRecord.Text = "Stop";
                SetFileStatus($"Recording Monitor {monitorIndex + 1}…");
            }
            catch (Exception ex)
            {
                isRecording = false;
                btnRecord.Text = "Record";
                MessageBox.Show("Error starting recording:\n" + ex.Message,
                    "Recording Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void StopRecording()
        {
            if (!isRecording || recorder == null)
                return;

            try
            {
                recorder.Stop();
                SetFileStatus("Stopping recording…");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error stopping recording:\n" + ex.Message,
                    "Recording Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ========= RECORDING LIST =========

        private void LoadRecordingList()
        {
            allRecordings.Clear();
            if (!Directory.Exists(recordingsFolder))
                return;

            var files = Directory.GetFiles(recordingsFolder, "*.mp4", SearchOption.AllDirectories);
            Array.Sort(files, (a, b) => File.GetCreationTime(b).CompareTo(File.GetCreationTime(a)));
            foreach (var f in files)
                allRecordings.Add(new RecordingItem(f));

            if (currentView == MediaViewMode.Recordings)
                ApplySearchFilter();
        }

        // ========= RECORDING PREVIEW =========

        private void ShowRecordingPreview(string path)
        {
            if (!File.Exists(path))
                return;

            if (chkLivePreview.Checked)
                return;

            try
            {
                var player = mediaPlayer?.Player;
                if (player == null)
                    return;

                mediaPlayer.Visible = true;
                picturePreview.Visible = false;

                player.URL = path;
                player.settings.volume = 100;
                player.controls.play();
            }
            catch
            {
                mediaPlayer.Visible = false;
            }
        }
    }
}
