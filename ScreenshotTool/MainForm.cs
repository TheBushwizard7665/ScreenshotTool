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

    public partial class MainForm : Form
    {
        // Services
        private readonly ISettingsService _settingsService;
        private readonly IFileService _fileService;
        private readonly IScreenshotService _screenshotService;
        private readonly IRecordingService _recordingService;

        // Paths
        private readonly string appRootFolder;
        private readonly string screenshotsFolder;
        private readonly string recordingsFolder;

        // Settings
        private AppSettings settings = new AppSettings();

        // UI Controls (defined in Designer, referenced here)
        private ComboBox comboMonitors = null!;
        private Button btnCapture = null!;
        private Button btnCaptureRegion = null!;
        private Button btnRecord = null!;
        private Button btnOpenFolder = null!;
        private Button btnOpenAllFolders = null!;
        private Button btnSettings = null!;
        private Label lblList = null!;
        private ComboBox comboView = null!;
        private TextBox txtSearch = null!;
        private ListView listMedia = null!;
        private Panel leftPanel = null!;
        private Splitter mainSplitter = null!;
        private Panel rightPanel = null!;
        private Panel leftBottomPanel = null!;
        private Button btnDelete = null!;
        private Button btnDeleteAll = null!;
        private Button btnMove = null!;
        private bool isLivePreviewEnabled;
        private Label lblStatus = null!;
        private Label lblFileStatus = null!;
        private ToolTip tooltips = null!;

        // Right-side preview
        private Panel previewHost = null!;
        private PictureBox picturePreview = null!;
        private WmpHost mediaPlayer = null!;
        private Button btnPopout = null!;

        // Data
        private readonly List<IMediaItem> currentMediaList = new();
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

        // Context menu
        private ContextMenuStrip mediaContextMenu = null!;
        private ToolStripMenuItem ctxCopyImage = null!;
        private ToolStripMenuItem ctxCreateFolder = null!;
        private ToolStripMenuItem ctxEditPaint = null!;

        public MainForm()
        {
            // Initialize basic paths first so services can use them
            appRootFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SimpleScreenshotTool");

            // Instantiate Services (Poor man's DI)
            _settingsService = new SettingsService(appRootFolder);
            _fileService = new FileService();
            _screenshotService = new ScreenshotService();
            _recordingService = new RecordingService();

            screenshotsFolder = Path.Combine(appRootFolder, "Screenshots");
            recordingsFolder = Path.Combine(appRootFolder, "Recordings");

            Directory.CreateDirectory(appRootFolder);
            Directory.CreateDirectory(screenshotsFolder);
            Directory.CreateDirectory(recordingsFolder);

            // Load settings
            settings = _settingsService.Load();

            // Initialize UI from Designer
            InitializeComponent();

            // Add Media Player manually
            mediaPlayer = new WmpHost
            {
                Dock = DockStyle.Fill,
                Visible = false
            };
            previewHost.Controls.Add(mediaPlayer);

            WireUpEvents();

            // Additional UI Setup
            livePreviewTimer = new System.Windows.Forms.Timer();
            livePreviewTimer.Interval = settings.LivePreviewIntervalMs;
            livePreviewTimer.Tick += (s, e) => RefreshLivePreview();

            InitializeTrayIcon();
            ApplyDarkTheme();
            PopulateMonitorList();
            InitializeMediaContextMenu();

            // Initial view & compact mode
            if (string.Equals(settings.LastMediaView, "Recordings", StringComparison.OrdinalIgnoreCase))
                currentView = MediaViewMode.Recordings;
            else
                currentView = MediaViewMode.Screenshots;

            comboView.SelectedIndex = currentView == MediaViewMode.Screenshots ? 0 : 1;

            InitializeHotkey();
            RefreshMediaList();
        }

        private void WireUpEvents()
        {
            comboMonitors.SelectedIndexChanged += ComboMonitors_SelectedIndexChanged;
            comboMonitors.DropDown += ComboMonitors_DropDown;
            btnCapture.Click += BtnCapture_Click;
            btnCaptureRegion.Click += BtnCaptureRegion_Click;
            btnRecord.Click += BtnRecord_Click;
            btnOpenFolder.Click += BtnOpenFolder_Click;
            btnOpenAllFolders.Click += BtnOpenAllFolders_Click;
            btnSettings.Click += BtnSettings_Click;

            comboView.SelectedIndexChanged += ComboView_SelectedIndexChanged;
            txtSearch.TextChanged += (s, e) => ApplySearchFilter();

            listMedia.ColumnClick += ListMedia_ColumnClick;
            listMedia.SelectedIndexChanged += ListMedia_SelectedIndexChanged;
            listMedia.KeyDown += ListMedia_KeyDown;
            listMedia.MouseDoubleClick += (s, e) =>
            {
                if (listMedia.SelectedItems.Count > 0)
                    OpenSelected();
            };

            btnDelete.Click += (s, e) => DeleteSelectedFiles();
            btnDeleteAll.Click += (s, e) => DeleteAllVisibleFiles();
            btnMove.Click += (s, e) => MoveSelectedFiles();

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

            previewHost.Resize += (s, e) =>
            {
                btnPopout.Location = new Point(
                    previewHost.Width - btnPopout.Width - 6,
                    6);
            };

            previewHost.MouseDown += PreviewArea_MouseDown;
            picturePreview.MouseDown += PreviewArea_MouseDown;

            listMedia.Resize += (s, e) =>
            {
                if (listMedia.Columns.Count > 0)
                    listMedia.Columns[0].Width = -2;
            };
        }

        #region UI Theming & Setup

        private void ApplyDarkTheme()
        {
            // Dark Blue-Grey theme
            Color back = Color.FromArgb(47, 79, 79); // DarkSlateGray
            Color panelBack = Color.FromArgb(40, 70, 70); // Slightly darker
            Color text = Color.White;
            Color listBack = Color.FromArgb(60, 90, 90); // Lighter for contrast

            BackColor = back;
            ForeColor = text;

            foreach (Control ctl in Controls)
                ApplyDarkThemeToControl(ctl, back, panelBack, text, listBack);

            listMedia.GridLines = false;
        }

        private void ApplyDarkThemeToControl(Control ctl, Color back, Color panelBack, Color text, Color listBack)
        {
            if (ctl is Panel or FlowLayoutPanel or GroupBox)
            {
                if (ctl == leftPanel)
                    ctl.BackColor = listBack;
                else
                    ctl.BackColor = panelBack;
            }
            else if (ctl is Splitter)
                ctl.BackColor = Color.FromArgb(100, 149, 237); // CornflowerBlue
            else if (ctl is ListView)
                ctl.BackColor = listBack;
            else if (ctl is TextBox or ComboBox)
                ctl.BackColor = Color.FromArgb(50, 80, 80);
            else
                ctl.BackColor = back;

            ctl.ForeColor = text;

            foreach (Control child in ctl.Controls)
                ApplyDarkThemeToControl(child, back, panelBack, text, listBack);
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

            var ctxRename = new ToolStripMenuItem("Rename...");
            ctxRename.Click += (s, e) => RenameSelected();

            ctxCreateFolder = new ToolStripMenuItem("Create New Folder...");
            ctxCreateFolder.Click += (s, e) => CreateNewFolder();

            var ctxNewFolderMove = new ToolStripMenuItem("New folder (move here)...");
            ctxNewFolderMove.Click += (s, e) => NewFolderAndMoveSelected();

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
                new ToolStripSeparator(),
                ctxRename,
                ctxCreateFolder,
                ctxNewFolderMove,
                new ToolStripSeparator(),
                ctxDelete
            });

            listMedia.ContextMenuStrip = mediaContextMenu;
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
                // Status update removed as requested
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
            if (isLivePreviewEnabled)
                RefreshLivePreview();
        }

        private void ComboMonitors_DropDown(object? sender, EventArgs e)
        {
            // Auto-enable live preview when dropdown opens
            isLivePreviewEnabled = true;
            livePreviewTimer.Interval = settings.LivePreviewIntervalMs;
            livePreviewTimer.Start();
        }

        private void BtnCapture_Click(object? sender, EventArgs e)
        {
            // Auto-disable live preview when capture starts
            isLivePreviewEnabled = false;
            livePreviewTimer.Stop();

            if (comboMonitors.SelectedItem is not MonitorItem mi)
                return;

            CaptureMonitor(mi.Screen);
        }

        private void BtnCaptureRegion_Click(object? sender, EventArgs e)
        {
            // Auto-disable live preview when capture starts
            isLivePreviewEnabled = false;
            livePreviewTimer.Stop();

            if (comboMonitors.SelectedItem is not MonitorItem mi)
                return;

            using var regionForm = new RegionCaptureForm(mi.Screen);
            if (regionForm.ShowDialog(this) == DialogResult.OK && regionForm.HasRegion)
            {
                var rectLocal = regionForm.SelectedRegionLocal;
                if (rectLocal.Width <= 0 || rectLocal.Height <= 0) return;

                // Convert to absolute
                var absolute = new Rectangle(
                    mi.Screen.Bounds.X + rectLocal.X,
                    mi.Screen.Bounds.Y + rectLocal.Y,
                    rectLocal.Width,
                    rectLocal.Height
                );

                try
                {
                    string file = _screenshotService.CaptureRegion(mi.Screen, absolute, screenshotsFolder);
                    PostCapture(file);
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
                string file = _screenshotService.CaptureScreen(screen, screenshotsFolder);
                PostCapture(file);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error capturing screen: " + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PostCapture(string filePath)
        {
            _fileService.RunAutoCleanup(screenshotsFolder, settings.MaxFileCount, settings.MaxAgeDays);
            RefreshMediaList();
            if (currentView == MediaViewMode.Screenshots)
                SelectLatestInList();
            SetFileStatus($"Saved {Path.GetFileName(filePath)}");
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
            if (!_recordingService.IsRecording)
                StartRecordingSelectedMonitor();
            else
                StopRecording();
        }

        private void StartRecordingSelectedMonitor()
        {
            if (_recordingService.IsRecording)
                return;

            // Auto-disable live preview when recording starts
            isLivePreviewEnabled = false;
            livePreviewTimer.Stop();

            if (comboMonitors.SelectedItem is not MonitorItem mi)
            {
                MessageBox.Show("Please select a monitor first.", "Recording",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnRecord.Text = "Stop";
            SetFileStatus($"Recording Monitor {mi.Index + 1}…");

            _recordingService.StartRecording(
                mi.Screen,
                recordingsFolder,
                settings.RecordingIncludeAudio,
                settings.VideoBitrate,
                settings.VideoFramerate,
                onComplete: (filePath) =>
                {
                    if (!IsHandleCreated) return;
                    BeginInvoke(new Action(() =>
                    {
                        btnRecord.Text = "Record";
                        RefreshMediaList();
                        if (currentView == MediaViewMode.Recordings)
                            SelectLatestInList();
                        SetFileStatus($"Recording saved: {Path.GetFileName(filePath)}");
                    }));
                },
                onError: (error) =>
                {
                    if (!IsHandleCreated) return;
                    BeginInvoke(new Action(() =>
                    {
                        btnRecord.Text = "Record";
                        MessageBox.Show("Recording failed:\n" + error,
                            "Recording Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }));
                });
        }

        private void StopRecording()
        {
            _recordingService.StopRecording();
            SetFileStatus("Stopping recording…");
        }

        #endregion

        #region Media list

        private void RefreshMediaList()
        {
            currentMediaList.Clear();

            bool isScreenshots = (currentView == MediaViewMode.Screenshots);
            string folder = isScreenshots ? screenshotsFolder : recordingsFolder;
            string pattern = isScreenshots ? "*.png" : "*.mp4";

            var items = _fileService.LoadMedia(folder, pattern, isScreenshots);
            currentMediaList.AddRange(items);

            ApplySearchFilter();
        }

        private void ApplySearchFilter()
        {
            string filter = txtSearch.Text?.Trim().ToLowerInvariant() ?? "";

            listMedia.BeginUpdate();
            listMedia.Items.Clear();

            foreach (var item in currentMediaList)
            {
                if (!string.IsNullOrEmpty(filter) &&
                    !item.FileName.ToLowerInvariant().Contains(filter))
                    continue;

                var lvi = new ListViewItem(item.DisplayName) { Tag = item };
                lvi.SubItems.Add(item.MonitorShortLabel);
                lvi.SubItems.Add(item.FolderLabel);
                lvi.SubItems.Add(item.Created.ToString("yyyy-MM-dd HH:mm"));
                listMedia.Items.Add(lvi);
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

                try { mediaPlayer?.Player?.controls.stop(); } catch { }
                mediaPlayer.Visible = false;
                picturePreview.Visible = true;

                SetFileStatus("");
                return;
            }

            if (isLivePreviewEnabled)
            {
                isLivePreviewEnabled = false;
                livePreviewTimer.Stop();
            }

            UpdateFileStatusDetails(item.FullPath);

            if (currentView == MediaViewMode.Screenshots)
            {
                ShowPreviewForFile(item.FullPath);
            }
            else
            {
                ShowPreviewForFile(item.FullPath);
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

                if (isLivePreviewEnabled)
                    return;

                if (ext is ".png" or ".jpg" or ".jpeg" or ".bmp" or ".gif")
                {
                    try { mediaPlayer?.Player?.controls.stop(); } catch { }
                    mediaPlayer.Visible = false;
                    picturePreview.Visible = true;

                    picturePreview.Image?.Dispose();
                    picturePreview.Image = Image.FromFile(path);
                }
                else if (ext is ".mp4" or ".avi" or ".mov" or ".wmv")
                {
                    picturePreview.Visible = false;
                    mediaPlayer.Visible = true;

                    var player = mediaPlayer.Player;
                    if (player != null)
                    {
                        player.URL = path;
                        player.settings.volume = 100;
                        player.controls.play();
                    }
                }
                else
                {
                    try { mediaPlayer?.Player?.controls.stop(); } catch { }
                    mediaPlayer.Visible = false;
                    picturePreview.Visible = true;

                    picturePreview.Image?.Dispose();
                    picturePreview.Image = null;
                }
            }
            catch
            {
                // ignore
            }
        }

        private void RefreshLivePreview()
        {
            if (!isLivePreviewEnabled)
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

                try { mediaPlayer?.Player?.controls.stop(); } catch { }
                mediaPlayer.Visible = false;
                picturePreview.Visible = true;

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



        private void ComboView_SelectedIndexChanged(object? sender, EventArgs e)
        {
            currentView = comboView.SelectedIndex == 1
                ? MediaViewMode.Recordings
                : MediaViewMode.Screenshots;

            settings.LastMediaView = currentView == MediaViewMode.Screenshots
                ? "Screenshots"
                : "Recordings";
            _settingsService.Save(settings);

            lblList.Text = currentView == MediaViewMode.Screenshots ? "Screenshots" : "Recordings";

            if (isLivePreviewEnabled)
            {
                livePreviewTimer.Interval = settings.LivePreviewIntervalMs;
                livePreviewTimer.Start();
            }
            else
            {
                livePreviewTimer.Stop();
            }

            bool screenshots = (currentView == MediaViewMode.Screenshots);
            ctxCopyImage.Enabled = screenshots;
            ctxEditPaint.Enabled = screenshots;

            RefreshMediaList();
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
                // Fallback attempt
                try
                {
                    string args = "/select,\"" + item.FullPath + "\"";
                    Process.Start("explorer.exe", args);
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

            _fileService.DeleteFiles(items.Select(i => i.FullPath));
            RefreshMediaList();
        }

        private void DeleteAllVisibleFiles()
        {
            if (listMedia.Items.Count == 0) return;

            if (MessageBox.Show($"Delete ALL {listMedia.Items.Count} visible file(s)?",
                    "Delete All", MessageBoxButtons.OKCancel,
                    MessageBoxIcon.Warning) != DialogResult.OK)
                return;

            var paths = new List<string>();
            foreach (ListViewItem item in listMedia.Items)
            {
                if (item.Tag is IMediaItem m)
                    paths.Add(m.FullPath);
            }

            _fileService.DeleteFiles(paths);
            RefreshMediaList();
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
            _fileService.MoveFiles(items.Select(i => i.FullPath), destRoot);
            RefreshMediaList();
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
            if (string.IsNullOrEmpty(folderName)) return;
            foreach (char c in Path.GetInvalidFileNameChars())
                folderName = folderName.Replace(c, '_');

            try
            {
                string baseMonitorPath = GetBaseMonitorPathForFile(baseItem.FullPath);
                _fileService.CreateFolder(baseMonitorPath, folderName);

                string newDir = Path.Combine(baseMonitorPath, folderName);
                _fileService.MoveFiles(items.Select(i => i.FullPath), newDir);
                RefreshMediaList();
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

            using var dlg = new RenameForm(currentName);
            if (dlg.ShowDialog(this) != DialogResult.OK)
                return;

            string newName = dlg.NewName.Trim();
            foreach (char c in Path.GetInvalidFileNameChars())
                newName = newName.Replace(c, '_');

            if (string.IsNullOrEmpty(newName)) return;

            try
            {
                _fileService.RenameFile(item.FullPath, newName);
                RefreshMediaList();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error renaming file:\n" + ex.Message,
                    "Rename", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CreateNewFolder()
        {
            using var dlg = new RenameForm("NewFolder");
            dlg.Text = "Create New Folder";
            if (dlg.ShowDialog(this) != DialogResult.OK)
                return;

            string folderName = dlg.NewName.Trim();
            if (string.IsNullOrEmpty(folderName)) return;
            foreach (char c in Path.GetInvalidFileNameChars())
                folderName = folderName.Replace(c, '_');

            try
            {
                string basePath = currentView == MediaViewMode.Screenshots ? screenshotsFolder : recordingsFolder;

                var item = GetFirstSelectedItem();
                if (item != null)
                {
                    string dir = Path.GetDirectoryName(item.FullPath) ?? basePath;
                    basePath = dir;
                }

                _fileService.CreateFolder(basePath, folderName);
                RefreshMediaList();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error creating folder:\n" + ex.Message,
                    "Create New Folder", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                _settingsService.Save(settings);
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

            _recordingService.StopRecording();

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
