using ScreenRecorderLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ScreenshotTool
{
    public enum MediaViewMode
    {
        Screenshots,
        Recordings
    }

    public sealed class MainForm : Form
    {
        // Paths
        private readonly string appRootFolder;
        private readonly string screenshotsFolder;
        private readonly string recordingsFolder;
        private readonly string settingsPath;

        // Settings
        private AppSettings settings = new AppSettings();

        // UI
        private ComboBox comboMonitors = null!;
        private Button btnCapture = null!;
        private Button btnCaptureRegion = null!;
        private Button btnRecord = null!;
        private Button btnOpenFolder = null!;
        private Button btnOpenAllFolders = null!;
        private Button btnSettings = null!;
        private CheckBox chkCompact = null!;
        private Label lblList = null!;
        private ComboBox comboView = null!;
        private TextBox txtSearch = null!;
        private ListView listMedia = null!;
        private Panel leftPanel = null!;
        private Splitter mainSplitter = null!;
        private Panel rightPanel = null!;
        private Panel leftBottomPanel = null!;
        private Button btnDelete = null!;
        private Button btnMove = null!;
        private CheckBox chkLivePreview = null!;
        private Label lblStatus = null!;
        private Label lblFileStatus = null!;
        private ToolTip tooltips = null!;

        // Right-side preview
        private Panel previewHost = null!;
        private PictureBox picturePreview = null!;
        private Button btnPopout = null!;

        // Column widths for compact mode
        private int colMonitorWidth = 70;
        private int colTagWidth = 70;
        private int colFolderWidth = 80;
        private int colDateWidth = 120;

        // Data
        private readonly List<ScreenshotItem> allScreenshots = new();
        private readonly List<RecordingItem> allRecordings = new();
        private MediaViewMode currentView = MediaViewMode.Screenshots;

        // Sorting
        private int currentSortColumn = 4;
        private bool sortAscending = false;

        // Live preview
        private System.Windows.Forms.Timer livePreviewTimer = null!;

        // Tray + hotkey
        private NotifyIcon trayIcon = null!;
        private ContextMenuStrip trayMenu = null!;
        private const int HOTKEY_ID = 0x1234;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;
        private const uint MOD_NOREPEAT = 0x4000;
        private const uint VK_SPACE = 0x20;
        private bool hotkeyRegistered;
        private string HOTKEY_TEXT => "Ctrl+Shift+Space";
        private bool isExiting;

        // Recording
        private Recorder? recorder;
        private bool isRecording;

        // Context menu
        private ContextMenuStrip mediaContextMenu = null!;
        private ToolStripMenuItem ctxCopyImage = null!;
        private ToolStripMenuItem ctxUploadChatGPT = null!;
        private ToolStripMenuItem ctxEditPaint = null!;

        public MainForm()
        {
            Text = "Screenshot Tool";
            Font = new Font("Segoe UI", 9F);
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(900, 500);

            appRootFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SimpleScreenshotTool");

            screenshotsFolder = Path.Combine(appRootFolder, "Screenshots");
            recordingsFolder = Path.Combine(appRootFolder, "Recordings");
            settingsPath = Path.Combine(appRootFolder, "settings.json");

            Directory.CreateDirectory(appRootFolder);
            Directory.CreateDirectory(screenshotsFolder);
            Directory.CreateDirectory(recordingsFolder);

            LoadSettings();
            InitializeUI();

            // Live preview timer
            livePreviewTimer = new System.Windows.Forms.Timer();
            livePreviewTimer.Interval = settings.LivePreviewIntervalMs;
            livePreviewTimer.Tick += (s, e) => RefreshLivePreview();

            InitializeTrayIcon();
            ApplyDarkTheme();
            PopulateMonitorList();

            LoadScreenshotList();
            LoadRecordingList();

            // Initial view & compact mode
            if (string.Equals(settings.LastMediaView, "Recordings", StringComparison.OrdinalIgnoreCase))
                currentView = MediaViewMode.Recordings;
            else
                currentView = MediaViewMode.Screenshots;

            comboView.SelectedIndex = currentView == MediaViewMode.Screenshots ? 0 : 1;
            chkCompact.Checked = settings.CompactMode;
            ApplyCompactMode();

            InitializeHotkey();
        }

        #region Settings

        private void LoadSettings()
        {
            try
            {
                if (File.Exists(settingsPath))
                {
                    var json = File.ReadAllText(settingsPath);
                    var loaded = System.Text.Json.JsonSerializer.Deserialize<AppSettings>(json);
                    if (loaded != null)
                        settings = loaded;
                }
            }
            catch
            {
                // ignore
            }
        }

        private void SaveSettings()
        {
            try
            {
                Directory.CreateDirectory(appRootFolder);
                var json = System.Text.Json.JsonSerializer.Serialize(settings,
                    new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(settingsPath, json);
            }
            catch
            {
                // ignore
            }
        }

        #endregion

        #region UI setup

        private void InitializeUI()
        {
            // Tooltips
            tooltips = new ToolTip
            {
                AutoPopDelay = 8000,
                InitialDelay = 600,
                ReshowDelay = 200,
                ShowAlways = true
            };

            // Top bar
            var topBar = new Panel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };

            var topFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(8, 9, 8, 9)
            };

            var lblMonitors = new Label
            {
                Text = "Monitor:",
                AutoSize = true,
                Margin = new Padding(0, 5, 4, 0)
            };

            comboMonitors = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 220
            };
            comboMonitors.SelectedIndexChanged += ComboMonitors_SelectedIndexChanged;
            tooltips.SetToolTip(comboMonitors, "Select which monitor to capture or record.");

            btnCapture = new Button
            {
                Text = "Capture",
                Width = 90,
                Margin = new Padding(8, 0, 0, 0)
            };
            btnCapture.Click += BtnCapture_Click;
            tooltips.SetToolTip(btnCapture,
                "Capture a full screenshot of the selected monitor.\r\n" +
                $"{HOTKEY_TEXT} captures the monitor under your cursor.");

            btnCaptureRegion = new Button
            {
                Text = "Region",
                Width = 90,
                Margin = new Padding(6, 0, 0, 0)
            };
            btnCaptureRegion.Click += BtnCaptureRegion_Click;
            tooltips.SetToolTip(btnCaptureRegion,
                "Capture a custom rectangle by dragging on the selected monitor.");

            btnRecord = new Button
            {
                Text = "Record",
                Width = 90,
                Margin = new Padding(6, 0, 0, 0)
            };
            btnRecord.Click += BtnRecord_Click;
            tooltips.SetToolTip(btnRecord,
                "Start/stop screen recording of the selected monitor.");

            btnOpenFolder = new Button
            {
                Text = "Open folder",
                Width = 100,
                Margin = new Padding(6, 0, 0, 0)
            };
            btnOpenFolder.Click += BtnOpenFolder_Click;
            tooltips.SetToolTip(btnOpenFolder,
                "Open the screenshot folder for the selected monitor.");

            btnOpenAllFolders = new Button
            {
                Text = "Open all",
                Width = 90,
                Margin = new Padding(6, 0, 0, 0)
            };
            btnOpenAllFolders.Click += BtnOpenAllFolders_Click;
            tooltips.SetToolTip(btnOpenAllFolders,
                "Open the root folder that contains all screenshots.");

            btnSettings = new Button
            {
                Text = "Settings",
                Width = 90,
                Margin = new Padding(6, 0, 0, 0)
            };
            btnSettings.Click += BtnSettings_Click;
            tooltips.SetToolTip(btnSettings,
                "Adjust cleanup, preview, recording, and hotkey behavior.");

            chkCompact = new CheckBox
            {
                Text = "Compact",
                AutoSize = true,
                Margin = new Padding(10, 6, 0, 0)
            };
            chkCompact.CheckedChanged += ChkCompact_CheckedChanged;
            tooltips.SetToolTip(chkCompact,
                "Compact mode hides extra columns and the preview area.");

            topFlow.Controls.Add(lblMonitors);
            topFlow.Controls.Add(comboMonitors);
            topFlow.Controls.Add(btnCapture);
            topFlow.Controls.Add(btnCaptureRegion);
            topFlow.Controls.Add(btnRecord);
            topFlow.Controls.Add(btnOpenFolder);
            topFlow.Controls.Add(btnOpenAllFolders);
            topFlow.Controls.Add(btnSettings);
            topFlow.Controls.Add(chkCompact);

            topBar.Controls.Add(topFlow);

            // Left panel - list
            leftPanel = new Panel
            {
                Dock = DockStyle.Left,
                Width = 360
            };

            lblList = new Label
            {
                Text = "Screenshots",
                Dock = DockStyle.Top,
                Height = 20,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 2, 0, 0)
            };

            comboView = new ComboBox
            {
                Dock = DockStyle.Top,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Height = 22
            };
            comboView.Items.Add("Screenshots");
            comboView.Items.Add("Recordings");
            comboView.SelectedIndexChanged += ComboView_SelectedIndexChanged;
            tooltips.SetToolTip(comboView, "Switch between screenshot list and recording list.");

            txtSearch = new TextBox
            {
                Dock = DockStyle.Top,
                PlaceholderText = "Search by name..."
            };
            txtSearch.TextChanged += (s, e) => ApplySearchFilter();
            tooltips.SetToolTip(txtSearch, "Filter files by name. Type to search.");

            listMedia = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                HideSelection = false,
                MultiSelect = true
            };
            listMedia.Columns.Add("Name", 180);
            listMedia.Columns.Add("Monitor", 70);
            listMedia.Columns.Add("Tag", 70);
            listMedia.Columns.Add("Folder", 80);
            listMedia.Columns.Add("Date", 120);

            colMonitorWidth = listMedia.Columns[1].Width;
            colTagWidth = listMedia.Columns[2].Width;
            colFolderWidth = listMedia.Columns[3].Width;
            colDateWidth = listMedia.Columns[4].Width;

            listMedia.ColumnClick += ListMedia_ColumnClick;
            listMedia.SelectedIndexChanged += ListMedia_SelectedIndexChanged;
            listMedia.KeyDown += ListMedia_KeyDown;
            listMedia.MouseDoubleClick += (s, e) =>
            {
                if (listMedia.SelectedItems.Count > 0)
                    OpenSelected();
            };
            tooltips.SetToolTip(listMedia,
                "Click to select. Delete = remove, Enter = open.");

            InitializeMediaContextMenu();
            listMedia.ContextMenuStrip = mediaContextMenu;

            leftBottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 36
            };

            btnDelete = new Button
            {
                Text = "Delete",
                Width = 90,
                Left = 6,
                Top = 6
            };
            btnDelete.Click += (s, e) => DeleteSelectedFiles();
            tooltips.SetToolTip(btnDelete, "Delete the selected file(s).");

            btnMove = new Button
            {
                Text = "Move...",
                Width = 90,
                Left = 102,
                Top = 6
            };
            btnMove.Click += (s, e) => MoveSelectedFiles();
            tooltips.SetToolTip(btnMove, "Move selected file(s) to another folder.");

            leftBottomPanel.Controls.Add(btnDelete);
            leftBottomPanel.Controls.Add(btnMove);

            leftPanel.Controls.Add(listMedia);
            leftPanel.Controls.Add(leftBottomPanel);
            leftPanel.Controls.Add(txtSearch);
            leftPanel.Controls.Add(comboView);
            leftPanel.Controls.Add(lblList);

            // Right panel - preview + status
            rightPanel = new Panel
            {
                Dock = DockStyle.Fill
            };

            previewHost = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(20, 20, 20)
            };

            picturePreview = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.FromArgb(20, 20, 20),
                Margin = new Padding(4)
            };

            previewHost.Controls.Add(picturePreview);

            btnPopout = new Button
            {
                Text = "⇱",
                Width = 28,
                Height = 24,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.Gainsboro,
                TabStop = false,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnPopout.FlatAppearance.BorderSize = 1;
            btnPopout.FlatAppearance.BorderColor = Color.FromArgb(70, 70, 70);
            btnPopout.Location = new Point(previewHost.Width - btnPopout.Width - 6, 6);
            btnPopout.Click += BtnPopout_Click;
            btnPopout.MouseEnter += (s, e) =>
            {
                btnPopout.BackColor = Color.FromArgb(60, 60, 60);
                btnPopout.FlatAppearance.BorderColor = Color.FromArgb(120, 120, 120);
            };
            btnPopout.MouseLeave += (s, e) =>
            {
                btnPopout.BackColor = Color.FromArgb(30, 30, 30);
                btnPopout.FlatAppearance.BorderColor = Color.FromArgb(70, 70, 70);
            };
            tooltips.SetToolTip(btnPopout,
                "Pop out preview window (Shift+Click anywhere on the preview).");

            previewHost.Controls.Add(btnPopout);
            btnPopout.BringToFront();

            previewHost.Resize += (s, e) =>
            {
                btnPopout.Location = new Point(
                    previewHost.Width - btnPopout.Width - 6,
                    6);
            };

            // Shift+click area to pop out
            previewHost.MouseDown += PreviewArea_MouseDown;
            picturePreview.MouseDown += PreviewArea_MouseDown;

            var rightBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60
            };

            chkLivePreview = new CheckBox
            {
                Text = "Live preview",
                Left = 8,
                Top = 6,
                AutoSize = true
            };
            chkLivePreview.CheckedChanged += ChkLivePreview_CheckedChanged;
            tooltips.SetToolTip(chkLivePreview,
                "Continuously mirror the selected monitor in the preview area.");

            lblStatus = new Label
            {
                Text = "Ready",
                Left = 8,
                Top = 28,
                AutoSize = true
            };

            lblFileStatus = new Label
            {
                Text = "",
                Left = 220,
                Top = 28,
                AutoSize = true
            };

            rightBottom.Controls.Add(chkLivePreview);
            rightBottom.Controls.Add(lblStatus);
            rightBottom.Controls.Add(lblFileStatus);

            rightPanel.Controls.Add(previewHost);
            rightPanel.Controls.Add(rightBottom);

            // Splitter
            mainSplitter = new Splitter
            {
                Dock = DockStyle.Left,
                Width = 4,
                BackColor = Color.FromArgb(40, 40, 40)
            };

            Controls.Add(rightPanel);
            Controls.Add(mainSplitter);
            Controls.Add(leftPanel);
            Controls.Add(topBar);
        }

        private void ApplyDarkTheme()
        {
            Color back = Color.FromArgb(32, 32, 32);
            Color panelBack = Color.FromArgb(24, 24, 24);
            Color text = Color.Gainsboro;

            BackColor = back;
            ForeColor = text;

            foreach (Control ctl in Controls)
                ApplyDarkThemeToControl(ctl, back, panelBack, text);

            listMedia.GridLines = false;
        }

        private void ApplyDarkThemeToControl(Control ctl, Color back, Color panelBack, Color text)
        {
            if (ctl is Panel or FlowLayoutPanel or GroupBox)
                ctl.BackColor = panelBack;
            else if (ctl is Splitter)
                ctl.BackColor = Color.FromArgb(40, 40, 40);
            else if (ctl is ListView or TextBox or ComboBox)
                ctl.BackColor = Color.FromArgb(28, 28, 28);
            else
                ctl.BackColor = back;

            ctl.ForeColor = text;

            foreach (Control child in ctl.Controls)
                ApplyDarkThemeToControl(child, back, panelBack, text);
        }

        private void InitializeMediaContextMenu()
        {
            mediaContextMenu = new ContextMenuStrip();

            var ctxOpen = new ToolStripMenuItem("Open");
            ctxOpen.Click += (s, e) => OpenSelected();

            var ctxOpenFolder = new ToolStripMenuItem("Open folder");
            ctxOpenFolder.Click += (s, e) => OpenSelectedFolder();

            var ctxCopyPath = new ToolStripMenuItem("Copy path");
            ctxCopyPath.Click += (s, e) => CopyPathOfSelected();

            ctxCopyImage = new ToolStripMenuItem("Copy image to clipboard");
            ctxCopyImage.Click += (s, e) => CopySelectedImageToClipboard();

            ctxEditPaint = new ToolStripMenuItem("Edit in Paint");
            ctxEditPaint.Click += (s, e) => EditSelectedInPaint();

            ctxUploadChatGPT = new ToolStripMenuItem("Upload to ChatGPT (open file)");
            ctxUploadChatGPT.Click += (s, e) => OpenSelected();

            var ctxRename = new ToolStripMenuItem("Rename...");
            ctxRename.Click += (s, e) => RenameSelected();

            var ctxNewFolder = new ToolStripMenuItem("New folder (move here)...");
            ctxNewFolder.Click += (s, e) => NewFolderAndMoveSelected();

            var ctxDelete = new ToolStripMenuItem("Delete");
            ctxDelete.Click += (s, e) => DeleteSelectedFiles();

            mediaContextMenu.Items.AddRange(new ToolStripItem[]
            {
                ctxOpen,
                ctxOpenFolder,
                ctxCopyPath,
                new ToolStripSeparator(),
                ctxCopyImage,
                ctxEditPaint,
                ctxUploadChatGPT,
                new ToolStripSeparator(),
                ctxRename,
                ctxNewFolder,
                new ToolStripSeparator(),
                ctxDelete
            });
        }

        #endregion

        #region Tray + hotkey

        private void InitializeTrayIcon()
        {
            trayMenu = new ContextMenuStrip();
            var mShow = new ToolStripMenuItem("Show");
            mShow.Click += (s, e) => ShowFromTray();

            var mCaptureCursor = new ToolStripMenuItem("Capture monitor under cursor");
            mCaptureCursor.Click += (s, e) => CaptureMonitorUnderCursor();

            var mExit = new ToolStripMenuItem("Exit");
            mExit.Click += (s, e) =>
            {
                isExiting = true;
                Close();
            };

            trayMenu.Items.AddRange(new ToolStripItem[]
            {
                mShow,
                mCaptureCursor,
                new ToolStripSeparator(),
                mExit
            });

            trayIcon = new NotifyIcon
            {
                Icon = SystemIcons.Information,
                Text = "Simple Screenshot Tool",
                Visible = true,
                ContextMenuStrip = trayMenu
            };
            trayIcon.DoubleClick += (s, e) => ShowFromTray();

            Resize += (s, e) =>
            {
                if (WindowState == FormWindowState.Minimized)
                    Hide();
            };
        }

        private void ShowFromTray()
        {
            Show();
            WindowState = FormWindowState.Normal;
            Activate();
        }

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private void InitializeHotkey()
        {
            try
            {
                hotkeyRegistered = RegisterHotKey(Handle, HOTKEY_ID,
                    MOD_CONTROL | MOD_SHIFT | MOD_NOREPEAT, VK_SPACE);
                SetStatus($"{HOTKEY_TEXT}: registered");
            }
            catch
            {
                hotkeyRegistered = false;
                SetStatus($"{HOTKEY_TEXT}: failed to register");
            }
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_HOTKEY = 0x0312;
            if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_ID)
            {
                CaptureMonitorUnderCursor();
                return;
            }
            base.WndProc(ref m);
        }

        #endregion

        #region Monitors + capture

        private void PopulateMonitorList()
        {
            comboMonitors.Items.Clear();
            var screens = Screen.AllScreens;
            for (int i = 0; i < screens.Length; i++)
            {
                comboMonitors.Items.Add(new MonitorItem(screens[i], i));
            }

            if (comboMonitors.Items.Count > 0)
                comboMonitors.SelectedIndex = 0;
        }

        private void ComboMonitors_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (chkLivePreview.Checked)
                RefreshLivePreview();
        }

        private void BtnCapture_Click(object? sender, EventArgs e)
        {
            if (comboMonitors.SelectedItem is not MonitorItem mi)
                return;

            CaptureMonitor(mi.Screen);
        }

        private void BtnCaptureRegion_Click(object? sender, EventArgs e)
        {
            Screen screen;
            int index;

            if (comboMonitors.SelectedItem is MonitorItem mi)
            {
                screen = mi.Screen;
                index = mi.Index;
            }
            else
            {
                screen = Screen.PrimaryScreen ?? Screen.AllScreens[0];
                index = Array.IndexOf(Screen.AllScreens, screen);
                if (index < 0) index = 0;
            }

            using (var regionForm = new RegionCaptureForm(screen))
            {
                if (regionForm.ShowDialog(this) != DialogResult.OK)
                    return;

                var rect = regionForm.SelectedRegion;
                if (rect.Width <= 0 || rect.Height <= 0)
                    return;

                try
                {
                    string monitorFolder = Path.Combine(screenshotsFolder, $"Monitor{index + 1}");
                    Directory.CreateDirectory(monitorFolder);

                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    string filePath = Path.Combine(monitorFolder, $"Region_{timestamp}.png");

                    using var bmp = new Bitmap(rect.Width, rect.Height);
                    using (var g = Graphics.FromImage(bmp))
                    {
                        g.CopyFromScreen(rect.Location, Point.Empty, rect.Size);
                    }
                    bmp.Save(filePath);

                    RunAutoCleanup();
                    LoadScreenshotList();

                    if (currentView == MediaViewMode.Screenshots)
                        SelectLatestInList();

                    SetFileStatus($"Saved {Path.GetFileName(filePath)}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error capturing region: " + ex.Message,
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void CaptureMonitor(Screen screen)
        {
            try
            {
                int index = Array.IndexOf(Screen.AllScreens, screen);
                if (index < 0) index = 0;

                string monitorFolder = Path.Combine(screenshotsFolder, $"Monitor{index + 1}");
                Directory.CreateDirectory(monitorFolder);

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string filePath = Path.Combine(monitorFolder, $"Screen_{timestamp}.png");

                using var bmp = new Bitmap(screen.Bounds.Width, screen.Bounds.Height);
                using (var g = Graphics.FromImage(bmp))
                {
                    g.CopyFromScreen(screen.Bounds.Location, Point.Empty, screen.Bounds.Size);
                }
                bmp.Save(filePath);

                RunAutoCleanup();
                LoadScreenshotList();

                if (currentView == MediaViewMode.Screenshots)
                    SelectLatestInList();

                SetFileStatus($"Saved {Path.GetFileName(filePath)}");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error capturing screen: " + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CaptureMonitorUnderCursor()
        {
            var screen = Screen.FromPoint(Cursor.Position);
            CaptureMonitor(screen);

            if (settings.BringToFrontAfterHotkey)
            {
                ShowFromTray();
            }
        }

        #endregion

        #region Recording

        private void BtnRecord_Click(object? sender, EventArgs e)
        {
            if (!isRecording)
                StartRecordingSelectedMonitor();
            else
                StopRecording();
        }

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
                        RecordingSources = new List<RecordingSourceBase> { displaySource }
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

        #endregion

        #region Media list

        private void LoadScreenshotList()
        {
            allScreenshots.Clear();
            if (!Directory.Exists(screenshotsFolder))
                return;

            var files = Directory.GetFiles(screenshotsFolder, "*.png", SearchOption.AllDirectories);
            Array.Sort(files, (a, b) => File.GetCreationTime(b).CompareTo(File.GetCreationTime(a)));
            foreach (var f in files)
                allScreenshots.Add(new ScreenshotItem(f));

            if (currentView == MediaViewMode.Screenshots)
                ApplySearchFilter();
        }

        private void AddMediaItemToList(IMediaItem item)
        {
            var lvi = new ListViewItem(item.DisplayName) { Tag = item };
            lvi.SubItems.Add(item.MonitorShortLabel);
            lvi.SubItems.Add(item.TagLabel);
            lvi.SubItems.Add(item.FolderLabel);
            lvi.SubItems.Add(item.Created.ToString("yyyy-MM-dd HH:mm"));
            listMedia.Items.Add(lvi);
        }

        private void ApplySearchFilter()
        {
            string filter = txtSearch.Text?.Trim().ToLowerInvariant() ?? "";

            listMedia.BeginUpdate();
            listMedia.Items.Clear();

            if (currentView == MediaViewMode.Screenshots)
            {
                foreach (var item in allScreenshots)
                {
                    if (!string.IsNullOrEmpty(filter) &&
                        !item.FileName.ToLowerInvariant().Contains(filter))
                        continue;
                    AddMediaItemToList(item);
                }
            }
            else
            {
                foreach (var item in allRecordings)
                {
                    if (!string.IsNullOrEmpty(filter) &&
                        !item.FileName.ToLowerInvariant().Contains(filter))
                        continue;
                    AddMediaItemToList(item);
                }
            }

            listMedia.EndUpdate();

            listMedia.ListViewItemSorter = new MediaListViewComparer(currentSortColumn, sortAscending);
            listMedia.Sort();
        }

        private void ListMedia_ColumnClick(object? sender, ColumnClickEventArgs e)
        {
            if (e.Column == currentSortColumn)
                sortAscending = !sortAscending;
            else
            {
                currentSortColumn = e.Column;
                sortAscending = true;
            }

            listMedia.ListViewItemSorter = new MediaListViewComparer(currentSortColumn, sortAscending);
            listMedia.Sort();
        }

        private void SelectLatestInList()
        {
            if (listMedia.Items.Count == 0)
                return;
            listMedia.SelectedIndices.Clear();
            listMedia.Items[0].Selected = true;
            listMedia.Items[0].Focused = true;
        }

        private IMediaItem? GetFirstSelectedItem()
        {
            if (listMedia.SelectedItems.Count == 0)
                return null;
            return listMedia.SelectedItems[0].Tag as IMediaItem;
        }

        private IMediaItem[] GetSelectedItems()
        {
            var list = new List<IMediaItem>();
            foreach (ListViewItem lvi in listMedia.SelectedItems)
            {
                if (lvi.Tag is IMediaItem m)
                    list.Add(m);
            }
            return list.ToArray();
        }

        private void ListMedia_SelectedIndexChanged(object? sender, EventArgs e)
        {
            var item = GetFirstSelectedItem();
            if (item == null)
            {
                picturePreview.Image?.Dispose();
                picturePreview.Image = null;

                SetFileStatus("");
                return;
            }

            if (chkLivePreview.Checked)
            {
                chkLivePreview.Checked = false;
            }

            UpdateFileStatusDetails(item.FullPath);

            if (currentView == MediaViewMode.Screenshots)
            {
                ShowPreviewForFile(item.FullPath);
            }
            else
            {
                // For recordings we don't inline preview; just show info
                picturePreview.Image?.Dispose();
                picturePreview.Image = null;
            }
        }

        private void ListMedia_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                DeleteSelectedFiles();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Enter)
            {
                OpenSelected();
                e.Handled = true;
            }
        }

        #endregion

        #region Status / preview

        private void SetStatus(string text) => lblStatus.Text = text;

        private void SetFileStatus(string text) => lblFileStatus.Text = text;

        private void UpdateFileStatusDetails(string path)
        {
            try
            {
                var fi = new FileInfo(path);
                string ext = fi.Extension.ToLowerInvariant();
                string size = $"{fi.Length / 1024.0:0} KB";

                if (ext is ".png" or ".jpg" or ".jpeg" or ".bmp" or ".gif")
                {
                    using var img = Image.FromFile(path);
                    SetFileStatus($"{fi.Name}  {img.Width}x{img.Height}  {size}");
                }
                else
                {
                    SetFileStatus($"{fi.Name}  {size}");
                }
            }
            catch
            {
                SetFileStatus(Path.GetFileName(path));
            }
        }

        private void ShowPreviewForFile(string path)
        {
            try
            {
                if (!File.Exists(path))
                    return;

                string ext = Path.GetExtension(path).ToLowerInvariant();

                if (chkLivePreview.Checked)
                    return;

                if (ext is ".png" or ".jpg" or ".jpeg" or ".bmp" or ".gif")
                {
                    picturePreview.Image?.Dispose();
                    picturePreview.Image = Image.FromFile(path);
                }
                else
                {
                    picturePreview.Image?.Dispose();
                    picturePreview.Image = null;
                }
            }
            catch
            {
                // ignore
            }
        }

        private void ChkLivePreview_CheckedChanged(object? sender, EventArgs e)
        {
            if (chkLivePreview.Checked && !settings.CompactMode)
            {
                livePreviewTimer.Interval = settings.LivePreviewIntervalMs;
                livePreviewTimer.Start();
            }
            else
            {
                livePreviewTimer.Stop();
            }
        }

        private void RefreshLivePreview()
        {
            if (!chkLivePreview.Checked || settings.CompactMode)
                return;

            if (comboMonitors.SelectedItem is not MonitorItem mi)
                return;

            try
            {
                var screen = mi.Screen;
                using var bmp = new Bitmap(screen.Bounds.Width, screen.Bounds.Height);
                using (var g = Graphics.FromImage(bmp))
                {
                    g.CopyFromScreen(screen.Bounds.Location, Point.Empty, screen.Bounds.Size);
                }

                picturePreview.Image?.Dispose();
                picturePreview.Image = new Bitmap(bmp);
            }
            catch
            {
                // ignore
            }
        }

        #endregion

        #region Compact mode / view mode

        private void ChkCompact_CheckedChanged(object? sender, EventArgs e)
        {
            settings.CompactMode = chkCompact.Checked;
            SaveSettings();
            ApplyCompactMode();
        }

        private void ApplyCompactMode()
        {
            if (listMedia.Columns.Count < 5) return;

            if (settings.CompactMode)
            {
                listMedia.Columns[1].Width = 0;
                listMedia.Columns[2].Width = 0;
                listMedia.Columns[3].Width = 0;
                listMedia.Columns[4].Width = 0;

                chkLivePreview.Enabled = false;
                livePreviewTimer.Stop();

                rightPanel.Visible = false;
                mainSplitter.Visible = false;
                leftPanel.Dock = DockStyle.Fill;
            }
            else
            {
                listMedia.Columns[1].Width = colMonitorWidth;
                listMedia.Columns[2].Width = colTagWidth;
                listMedia.Columns[3].Width = colFolderWidth;
                listMedia.Columns[4].Width = colDateWidth;

                chkLivePreview.Enabled = true;
                rightPanel.Visible = true;
                mainSplitter.Visible = true;
                leftPanel.Dock = DockStyle.Left;
                leftPanel.Width = 360;
            }
        }

        private void ComboView_SelectedIndexChanged(object? sender, EventArgs e)
        {
            currentView = comboView.SelectedIndex == 1
                ? MediaViewMode.Recordings
                : MediaViewMode.Screenshots;

            settings.LastMediaView = currentView == MediaViewMode.Screenshots
                ? "Screenshots"
                : "Recordings";
            SaveSettings();

            lblList.Text = currentView == MediaViewMode.Screenshots ? "Screenshots" : "Recordings";

            chkLivePreview.Enabled = !settings.CompactMode;
            if (!chkLivePreview.Enabled)
            {
                livePreviewTimer.Stop();
            }
            else if (chkLivePreview.Checked)
            {
                livePreviewTimer.Interval = settings.LivePreviewIntervalMs;
                livePreviewTimer.Start();
            }

            bool screenshots = (currentView == MediaViewMode.Screenshots);
            ctxCopyImage.Enabled = screenshots;
            ctxEditPaint.Enabled = screenshots;
            ctxUploadChatGPT.Enabled = screenshots;

            ApplySearchFilter();
        }

        #endregion

        #region File operations

        private void OpenSelected()
        {
            var item = GetFirstSelectedItem();
            if (item == null) return;

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = item.FullPath,
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
            catch (Exception ex)
            {
                try
                {
                    string args = "/select,\"" + item.FullPath + "\"";
                    Process.Start("explorer.exe", args);
                    MessageBox.Show(
                        "Windows doesn't have a default app set for this file type.\n\n" +
                        "I opened the folder instead. Right-click the file, choose 'Open with…' " +
                        "and set a default app to make this work directly.",
                        "No associated app",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                catch
                {
                    MessageBox.Show("Unable to open file:\n" + ex.Message,
                        "Open", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void OpenSelectedFolder()
        {
            var item = GetFirstSelectedItem();
            if (item == null) return;

            try
            {
                string args = "/select,\"" + item.FullPath + "\"";
                Process.Start("explorer.exe", args);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to open folder:\n" + ex.Message,
                    "Open folder", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CopyPathOfSelected()
        {
            var items = GetSelectedItems();
            if (items.Length == 0) return;
            string text = string.Join(Environment.NewLine, items.Select(i => i.FullPath));
            Clipboard.SetText(text);
            SetStatus("Copied path(s) to clipboard.");
        }

        private void DeleteSelectedFiles()
        {
            var items = GetSelectedItems();
            if (items.Length == 0) return;

            if (MessageBox.Show($"Delete {items.Length} file(s)?",
                    "Delete", MessageBoxButtons.OKCancel,
                    MessageBoxIcon.Warning) != DialogResult.OK)
                return;

            foreach (var item in items)
            {
                try
                {
                    if (File.Exists(item.FullPath))
                        File.Delete(item.FullPath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error deleting file:\n" + ex.Message,
                        "Delete", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            LoadScreenshotList();
            LoadRecordingList();
            ApplySearchFilter();
        }

        private void MoveSelectedFiles()
        {
            var items = GetSelectedItems();
            if (items.Length == 0) return;

            using var fbd = new FolderBrowserDialog
            {
                Description = "Select destination folder"
            };
            if (fbd.ShowDialog(this) != DialogResult.OK)
                return;

            string destRoot = fbd.SelectedPath;

            foreach (var item in items)
            {
                try
                {
                    if (!File.Exists(item.FullPath))
                        continue;

                    string destPath = Path.Combine(destRoot, item.FileName);
                    if (File.Exists(destPath))
                    {
                        string name = Path.GetFileNameWithoutExtension(item.FileName);
                        string ext = Path.GetExtension(item.FileName);
                        string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                        destPath = Path.Combine(destRoot, $"{name}_{stamp}{ext}");
                    }

                    File.Move(item.FullPath, destPath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error moving file:\n" + ex.Message,
                        "Move", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            LoadScreenshotList();
            LoadRecordingList();
            ApplySearchFilter();
        }

        private string GetBaseMonitorPathForFile(string filePath)
        {
            string dir = Path.GetDirectoryName(filePath) ?? "";
            string monitorFolderName = "";

            var parts = dir.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            foreach (var p in parts)
            {
                if (p.StartsWith("Monitor", StringComparison.OrdinalIgnoreCase))
                {
                    monitorFolderName = p;
                    break;
                }
            }

            string root = dir.StartsWith(recordingsFolder, StringComparison.OrdinalIgnoreCase)
                ? recordingsFolder
                : screenshotsFolder;

            if (!string.IsNullOrEmpty(monitorFolderName))
                return Path.Combine(root, monitorFolderName);

            return dir;
        }

        private void NewFolderAndMoveSelected()
        {
            var items = GetSelectedItems();
            if (items.Length == 0) return;

            var baseItem = items[0];
            using var dlg = new RenameForm("NewFolder");
            dlg.Text = "New folder";
            if (dlg.ShowDialog(this) != DialogResult.OK)
                return;

            string folderName = dlg.NewName.Trim();
            if (string.IsNullOrEmpty(folderName))
                return;

            foreach (char c in Path.GetInvalidFileNameChars())
                folderName = folderName.Replace(c, '_');

            try
            {
                string baseMonitorPath = GetBaseMonitorPathForFile(baseItem.FullPath);
                string newDir = Path.Combine(baseMonitorPath, folderName);
                Directory.CreateDirectory(newDir);

                foreach (var item in items)
                {
                    if (!File.Exists(item.FullPath))
                        continue;

                    string destPath = Path.Combine(newDir, item.FileName);
                    if (File.Exists(destPath))
                    {
                        string name = Path.GetFileNameWithoutExtension(item.FileName);
                        string ext = Path.GetExtension(item.FileName);
                        string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                        destPath = Path.Combine(newDir, $"{name}_{stamp}{ext}");
                    }

                    File.Move(item.FullPath, destPath);
                }

                LoadScreenshotList();
                LoadRecordingList();
                ApplySearchFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error creating folder or moving files:\n" + ex.Message,
                    "New folder", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RenameSelected()
        {
            var items = GetSelectedItems();
            if (items.Length != 1)
            {
                MessageBox.Show("Please select a single file to rename.",
                    "Rename", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var item = items[0];
            string currentName = Path.GetFileNameWithoutExtension(item.FileName);
            string ext = Path.GetExtension(item.FileName);

            using var dlg = new RenameForm(currentName);
            if (dlg.ShowDialog(this) != DialogResult.OK)
                return;

            string newName = dlg.NewName.Trim();
            foreach (char c in Path.GetInvalidFileNameChars())
                newName = newName.Replace(c, '_');
            if (string.IsNullOrEmpty(newName))
                return;

            string dir = Path.GetDirectoryName(item.FullPath) ?? "";
            string destPath = Path.Combine(dir, newName + ext);

            try
            {
                if (File.Exists(destPath))
                {
                    MessageBox.Show("A file with that name already exists.",
                        "Rename", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                File.Move(item.FullPath, destPath);
                LoadScreenshotList();
                LoadRecordingList();
                ApplySearchFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error renaming file:\n" + ex.Message,
                    "Rename", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region Clipboard / Paint

        private void CopySelectedImageToClipboard()
        {
            if (currentView != MediaViewMode.Screenshots)
            {
                MessageBox.Show("Copy image is only available for screenshots.",
                    "Copy image", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var item = GetFirstSelectedItem();
            if (item == null) return;

            try
            {
                using var img = Image.FromFile(item.FullPath);
                Clipboard.SetImage(new Bitmap(img));
                SetStatus("Image copied to clipboard.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error copying image:\n" + ex.Message,
                    "Copy image", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void EditSelectedInPaint()
        {
            if (currentView != MediaViewMode.Screenshots)
            {
                MessageBox.Show("Edit in Paint is only available for screenshots.",
                    "Edit in Paint", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var item = GetFirstSelectedItem();
            if (item == null) return;

            try
            {
                Process.Start("mspaint.exe", $"\"{item.FullPath}\"");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error opening Paint:\n" + ex.Message,
                    "Edit in Paint", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region Cleanup + settings + lifecycle

        private void RunAutoCleanup()
        {
            try
            {
                if (!Directory.Exists(screenshotsFolder))
                    return;

                var files = Directory.GetFiles(screenshotsFolder, "*.png", SearchOption.AllDirectories)
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(fi => fi.CreationTimeUtc)
                    .ToList();

                if (files.Count > settings.MaxFileCount)
                {
                    foreach (var fi in files.Skip(settings.MaxFileCount))
                    {
                        try { fi.Delete(); } catch { }
                    }
                }

                DateTime cutoff = DateTime.Now.AddDays(-settings.MaxAgeDays);
                foreach (var fi in files)
                {
                    if (fi.CreationTime < cutoff)
                    {
                        try { fi.Delete(); } catch { }
                    }
                }
            }
            catch
            {
                // ignore
            }
        }

        private void BtnOpenFolder_Click(object? sender, EventArgs e)
        {
            if (comboMonitors.SelectedItem is not MonitorItem mi)
                return;

            string monitorFolder = Path.Combine(screenshotsFolder, $"Monitor{mi.Index + 1}");
            Directory.CreateDirectory(monitorFolder);
            Process.Start("explorer.exe", $"\"{monitorFolder}\"");
        }

        private void BtnOpenAllFolders_Click(object? sender, EventArgs e)
        {
            if (!Directory.Exists(screenshotsFolder))
                return;

            Process.Start("explorer.exe", $"\"{screenshotsFolder}\"");
        }

        private void BtnSettings_Click(object? sender, EventArgs e)
        {
            using var dlg = new SettingsForm(settings);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                settings = dlg.Settings;
                SaveSettings();
                livePreviewTimer.Interval = settings.LivePreviewIntervalMs;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!isExiting && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
                return;
            }

            base.OnFormClosing(e);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            livePreviewTimer?.Stop();

            if (hotkeyRegistered)
                UnregisterHotKey(Handle, HOTKEY_ID);

            if (trayIcon != null)
            {
                trayIcon.Visible = false;
                trayIcon.Dispose();
            }

            if (isRecording && recorder != null)
            {
                try { recorder.Stop(); } catch { }
            }

            base.OnFormClosed(e);
        }

        #endregion

        #region Pop-out preview

        private void PreviewArea_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
                return;
            if ((ModifierKeys & Keys.Shift) == 0)
                return;

            var item = GetFirstSelectedItem();
            if (item == null || !File.Exists(item.FullPath))
                return;

            if (currentView == MediaViewMode.Recordings)
            {
                // For recordings just open system player
                OpenSelected();
                return;
            }

            OpenFloatingPreview(item.FullPath);
        }

        private void BtnPopout_Click(object? sender, EventArgs e)
        {
            var item = GetFirstSelectedItem();
            if (item == null || !File.Exists(item.FullPath))
                return;

            if (currentView == MediaViewMode.Recordings)
            {
                OpenSelected();
                return;
            }

            OpenFloatingPreview(item.FullPath);
        }

        private void OpenFloatingPreview(string path)
        {
            try
            {
                var preview = new FloatingPreviewForm(path);

                var startRect = new Rectangle(
                    Location.X + Width / 2,
                    Location.Y + Height / 2,
                    10,
                    10);

                preview.StartPosition = FormStartPosition.Manual;
                preview.Bounds = startRect;
                preview.Opacity = 0.0;
                preview.Show(this);

                var screenBounds = Screen.FromControl(this).WorkingArea;

                Rectangle finalRect;

                if (FloatingPreviewForm.LastBounds.HasValue)
                {
                    var last = FloatingPreviewForm.LastBounds.Value;

                    int w = Math.Min(last.Width, screenBounds.Width - 40);
                    int h = Math.Min(last.Height, screenBounds.Height - 80);

                    int x = Math.Max(screenBounds.Left,
                                     Math.Min(last.X, screenBounds.Right - w));
                    int y = Math.Max(screenBounds.Top,
                                     Math.Min(last.Y, screenBounds.Bottom - h));

                    finalRect = new Rectangle(x, y, w, h);
                }
                else
                {
                    int targetW = Math.Min(900, screenBounds.Width - 80);
                    int targetH = Math.Min(600, screenBounds.Height - 120);

                    finalRect = new Rectangle(
                        screenBounds.Left + (screenBounds.Width - targetW) / 2,
                        screenBounds.Top + (screenBounds.Height - targetH) / 2,
                        targetW,
                        targetH);
                }

                int step = 0;
                var animTimer = new System.Windows.Forms.Timer
                {
                    Interval = 15
                };

                animTimer.Tick += (s, e) =>
                {
                    step++;
                    double t = Math.Min(1.0, step / 15.0);

                    int x = (int)(startRect.X + (finalRect.X - startRect.X) * t);
                    int y = (int)(startRect.Y + (finalRect.Y - startRect.Y) * t);
                    int w = (int)(startRect.Width + (finalRect.Width - startRect.Width) * t);
                    int h = (int)(startRect.Height + (finalRect.Height - startRect.Height) * t);

                    preview.Bounds = new Rectangle(x, y, w, h);
                    preview.Opacity = t;

                    if (t >= 1.0)
                    {
                        animTimer.Stop();
                        animTimer.Dispose();
                    }
                };

                animTimer.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error opening floating preview:\n" + ex.Message,
                    "Preview", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion
    }
}
