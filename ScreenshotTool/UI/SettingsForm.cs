using System;
using System.Drawing;
using System.Windows.Forms;

namespace ScreenshotTool
{
    public sealed class SettingsForm : Form
    {
        private NumericUpDown nudMaxFiles = null!;
        private NumericUpDown nudMaxAge = null!;
        private NumericUpDown nudPreviewInterval = null!;
        private CheckBox chkBringToFront = null!;
        private CheckBox chkRecordAudio = null!;
        private NumericUpDown nudBitrate = null!;
        private NumericUpDown nudFramerate = null!;
        private Button btnOk = null!;
        private Button btnCancel = null!;

        public AppSettings Settings { get; }

        public SettingsForm(AppSettings current)
        {
            Settings = new AppSettings
            {
                MaxFileCount = current.MaxFileCount,
                MaxAgeDays = current.MaxAgeDays,
                LivePreviewIntervalMs = current.LivePreviewIntervalMs,
                LastMediaView = current.LastMediaView,
                BringToFrontAfterHotkey = current.BringToFrontAfterHotkey,
                RecordingIncludeAudio = current.RecordingIncludeAudio,
                VideoBitrate = current.VideoBitrate,
                VideoFramerate = current.VideoFramerate
            };

            Text = "Settings";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            ClientSize = new Size(400, 350);
            MaximizeBox = true;
            MinimizeBox = true;

            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 8,
                Padding = new Padding(10),
                AutoSize = true
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));

            void AddRow(string label, Control control)
            {
                var l = new Label
                {
                    Text = label,
                    AutoSize = true,
                    Anchor = AnchorStyles.Left | AnchorStyles.Right,
                    TextAlign = ContentAlignment.MiddleLeft
                };
                table.Controls.Add(l);
                control.Anchor = AnchorStyles.Left | AnchorStyles.Right;
                table.Controls.Add(control);
            }

            nudMaxFiles = new NumericUpDown { Minimum = 10, Maximum = 100000, Value = Settings.MaxFileCount };
            AddRow("Max files to keep:", nudMaxFiles);

            nudMaxAge = new NumericUpDown { Minimum = 1, Maximum = 3650, Value = Settings.MaxAgeDays };
            AddRow("Max age (days):", nudMaxAge);

            nudPreviewInterval = new NumericUpDown { Minimum = 100, Maximum = 10000, Increment = 100, Value = Settings.LivePreviewIntervalMs };
            AddRow("Live preview interval (ms):", nudPreviewInterval);

            nudBitrate = new NumericUpDown { Minimum = 100000, Maximum = 100000000, Increment = 100000, Value = Settings.VideoBitrate };
            AddRow("Video Bitrate (bps):", nudBitrate);

            nudFramerate = new NumericUpDown { Minimum = 10, Maximum = 144, Value = Settings.VideoFramerate };
            AddRow("Video Framerate (fps):", nudFramerate);

            chkBringToFront = new CheckBox
            {
                Text = "Bring main window to front after hotkey",
                AutoSize = true,
                Checked = Settings.BringToFrontAfterHotkey
            };
            table.Controls.Add(chkBringToFront);
            table.SetColumnSpan(chkBringToFront, 2);

            chkRecordAudio = new CheckBox
            {
                Text = "Include audio in recordings",
                AutoSize = true,
                Checked = Settings.RecordingIncludeAudio
            };
            table.Controls.Add(chkRecordAudio);
            table.SetColumnSpan(chkRecordAudio, 2);

            var buttonPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                Dock = DockStyle.Bottom,
                Height = 40,
                AutoSize = true
            };

            btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel };
            btnOk = new Button { Text = "OK", DialogResult = DialogResult.OK };
            btnOk.Click += BtnOk_Click;

            buttonPanel.Controls.Add(btnCancel);
            buttonPanel.Controls.Add(btnOk);

            Controls.Add(table);
            Controls.Add(buttonPanel);

            AcceptButton = btnOk;
            CancelButton = btnCancel;

            ApplyDarkTheme();
        }

        private void BtnOk_Click(object? sender, EventArgs e)
        {
            Settings.MaxFileCount = (int)nudMaxFiles.Value;
            Settings.MaxAgeDays = (int)nudMaxAge.Value;
            Settings.LivePreviewIntervalMs = (int)nudPreviewInterval.Value;
            Settings.BringToFrontAfterHotkey = chkBringToFront.Checked;
            Settings.RecordingIncludeAudio = chkRecordAudio.Checked;
            Settings.VideoBitrate = (int)nudBitrate.Value;
            Settings.VideoFramerate = (int)nudFramerate.Value;
        }

        private void ApplyDarkTheme()
        {
            Color back = Color.FromArgb(47, 79, 79); // DarkSlateGray
            Color text = Color.White;
            Color controlBack = Color.FromArgb(50, 80, 80);

            BackColor = back;
            ForeColor = text;

            foreach (Control ctl in Controls)
                ApplyTheme(ctl, back, controlBack, text);
        }

        private void ApplyTheme(Control ctl, Color back, Color controlBack, Color text)
        {
            if (ctl is Button)
            {
                ctl.BackColor = controlBack;
                ctl.ForeColor = text;
            }
            else if (ctl is Label || ctl is CheckBox)
            {
                // Labels in TableLayoutPanel might need transparent or matching back
                ctl.BackColor = Color.Transparent;
                ctl.ForeColor = text;
            }
            else if (ctl is NumericUpDown)
            {
                ctl.BackColor = controlBack;
                ctl.ForeColor = text;
            }
            else if (ctl is TableLayoutPanel || ctl is FlowLayoutPanel)
            {
                ctl.BackColor = Color.Transparent;
            }

            foreach (Control child in ctl.Controls)
                ApplyTheme(child, back, controlBack, text);
        }
    }
}
