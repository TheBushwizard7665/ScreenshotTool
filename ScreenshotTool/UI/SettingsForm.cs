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
                CompactMode = current.CompactMode,
                LastMediaView = current.LastMediaView,
                BringToFrontAfterHotkey = current.BringToFrontAfterHotkey,
                RecordingIncludeAudio = current.RecordingIncludeAudio
            };

            Text = "Settings";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable; // Resizable
            ClientSize = new Size(380, 220);
            MaximizeBox = true; // Enabled
            MinimizeBox = true; // Enabled

            int labelLeft = 12;
            int controlLeft = 170;
            int top = 15;
            int row = 0;
            int rowHeight = 28;

            Label L(string text, int rowIndex)
            {
                var l = new Label
                {
                    Text = text,
                    AutoSize = true,
                    Left = labelLeft,
                    Top = top + rowIndex * rowHeight + 4
                };
                Controls.Add(l);
                return l;
            }

            L("Max files to keep:", row);
            nudMaxFiles = new NumericUpDown
            {
                Left = controlLeft,
                Top = top + row * rowHeight,
                Width = 80,
                Minimum = 10,
                Maximum = 100000,
                Value = Settings.MaxFileCount
            };
            Controls.Add(nudMaxFiles);

            row++;
            L("Max age (days):", row);
            nudMaxAge = new NumericUpDown
            {
                Left = controlLeft,
                Top = top + row * rowHeight,
                Width = 80,
                Minimum = 1,
                Maximum = 3650,
                Value = Settings.MaxAgeDays
            };
            Controls.Add(nudMaxAge);

            row++;
            L("Live preview interval (ms):", row);
            nudPreviewInterval = new NumericUpDown
            {
                Left = controlLeft,
                Top = top + row * rowHeight,
                Width = 80,
                Minimum = 100,
                Maximum = 10000,
                Increment = 100,
                Value = Settings.LivePreviewIntervalMs
            };
            Controls.Add(nudPreviewInterval);

            row++;
            chkBringToFront = new CheckBox
            {
                Text = "Bring main window to front after hotkey",
                Left = labelLeft,
                Top = top + row * rowHeight,
                AutoSize = true,
                Checked = Settings.BringToFrontAfterHotkey
            };
            Controls.Add(chkBringToFront);

            row++;
            chkRecordAudio = new CheckBox
            {
                Text = "Include system audio + microphone in recordings",
                Left = labelLeft,
                Top = top + row * rowHeight,
                AutoSize = true,
                Checked = Settings.RecordingIncludeAudio
            };
            Controls.Add(chkRecordAudio);

            btnOk = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Left = 200,
                Top = 175,
                Width = 75
            };

            btnCancel = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Left = 285,
                Top = 175,
                Width = 75
            };

            btnOk.Click += BtnOk_Click;

            Controls.Add(btnOk);
            Controls.Add(btnCancel);

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
        }

        private void ApplyDarkTheme()
        {
            Color back = Color.FromArgb(0, 0, 139); // DarkBlue
            Color text = Color.White;
            Color controlBack = Color.FromArgb(0, 0, 128); // Navy

            BackColor = back;
            ForeColor = text;

            foreach (Control ctl in Controls)
            {
                if (ctl is Button)
                {
                    ctl.BackColor = controlBack;
                    ctl.ForeColor = text;
                }
                else if (ctl is Label || ctl is CheckBox)
                {
                    ctl.BackColor = back;
                    ctl.ForeColor = text;
                }
                else if (ctl is NumericUpDown)
                {
                    ctl.BackColor = controlBack;
                    ctl.ForeColor = text;
                }
            }
        }
    }
}
