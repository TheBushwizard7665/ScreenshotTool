using System.Windows.Forms;
using System.Drawing;

namespace ScreenshotTool
{
    public partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
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
            tooltips.SetToolTip(comboMonitors, "Select which monitor to capture or record.");

            btnCapture = new Button
            {
                Text = "Capture",
                Width = 90,
                Margin = new Padding(8, 0, 0, 0)
            };
            tooltips.SetToolTip(btnCapture,
                "Capture a full screenshot of the selected monitor.");

            btnCaptureRegion = new Button
            {
                Text = "Region",
                Width = 90,
                Margin = new Padding(6, 0, 0, 0)
            };
            tooltips.SetToolTip(btnCaptureRegion,
                "Capture a custom rectangle by dragging on the selected monitor.");

            btnRecord = new Button
            {
                Text = "Record",
                Width = 90,
                Margin = new Padding(6, 0, 0, 0)
            };
            tooltips.SetToolTip(btnRecord,
                "Start/stop screen recording of the selected monitor.");

            btnOpenFolder = new Button
            {
                Text = "Open folder",
                Width = 100,
                Margin = new Padding(6, 0, 0, 0)
            };
            tooltips.SetToolTip(btnOpenFolder,
                "Open the screenshot folder for the selected monitor.");

            btnOpenAllFolders = new Button
            {
                Text = "Open all",
                Width = 90,
                Margin = new Padding(6, 0, 0, 0)
            };
            tooltips.SetToolTip(btnOpenAllFolders,
                "Open the root folder that contains all screenshots.");

            btnSettings = new Button
            {
                Text = "Settings",
                Width = 90,
                Margin = new Padding(6, 0, 0, 0)
            };
            tooltips.SetToolTip(btnSettings,
                "Adjust cleanup, preview, recording, and hotkey behavior.");

            topFlow.Controls.Add(lblMonitors);
            topFlow.Controls.Add(comboMonitors);
            topFlow.Controls.Add(btnCapture);
            topFlow.Controls.Add(btnCaptureRegion);
            topFlow.Controls.Add(btnRecord);
            topFlow.Controls.Add(btnOpenFolder);
            topFlow.Controls.Add(btnOpenAllFolders);
            topFlow.Controls.Add(btnSettings);

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
            tooltips.SetToolTip(comboView, "Switch between screenshot list and recording list.");

            txtSearch = new TextBox
            {
                Dock = DockStyle.Top,
                PlaceholderText = "Search by name..."
            };
            tooltips.SetToolTip(txtSearch, "Filter files by name. Type to search.");

            listMedia = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                HideSelection = false,
                MultiSelect = true
            };
            // Use -2 for auto-size width (header)
            listMedia.Columns.Add("Name", -2);
            listMedia.Columns.Add("Monitor", 70);
            listMedia.Columns.Add("Folder", 80);
            listMedia.Columns.Add("Date", 120);

            tooltips.SetToolTip(listMedia,
                "Click to select. Delete = remove, Enter = open.");

            leftBottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 36
            };

            btnDelete = new Button
            {
                Text = "Delete",
                Width = 60,
                Left = 6,
                Top = 6
            };
            tooltips.SetToolTip(btnDelete, "Delete the selected file(s).");

            btnDeleteAll = new Button
            {
                Text = "Delete All",
                Width = 70,
                Left = 72,
                Top = 6
            };
            tooltips.SetToolTip(btnDeleteAll, "Delete ALL visible files in the list.");

            btnMove = new Button
            {
                Text = "Move...",
                Width = 60,
                Left = 148,
                Top = 6
            };
            tooltips.SetToolTip(btnMove, "Move selected file(s) to another folder.");

            leftBottomPanel.Controls.Add(btnDelete);
            leftBottomPanel.Controls.Add(btnDeleteAll);
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
            // Location is set in resize event or initial

            tooltips.SetToolTip(btnPopout,
                "Pop out preview window (Shift+Click anywhere on the preview).");

            previewHost.Controls.Add(btnPopout);
            btnPopout.BringToFront();

            var rightBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60
            };

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

            this.Controls.Add(rightPanel);
            this.Controls.Add(mainSplitter);
            this.Controls.Add(leftPanel);
            this.Controls.Add(topBar);

            this.Text = "Screenshot Tool";
            this.Font = new Font("Segoe UI", 9F);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(900, 500);
        }
    }
}
