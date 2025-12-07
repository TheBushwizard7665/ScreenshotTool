using System;
using System.Drawing;
using System.Windows.Forms;

namespace ScreenshotTool
{
    public sealed class RenameForm : Form
    {
        private TextBox txtName = null!;
        private Button btnOk = null!;
        private Button btnCancel = null!;

        public string NewName => txtName.Text.Trim();

        public RenameForm(string initialName)
        {
            Text = "Rename";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(360, 120);

            var lbl = new Label
            {
                Text = "New name:",
                AutoSize = true,
                Left = 12,
                Top = 15
            };

            txtName = new TextBox
            {
                Left = 12,
                Top = 35,
                Width = 330,
                Text = initialName
            };

            btnOk = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Left = 190,
                Top = 75,
                Width = 70
            };

            btnCancel = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Left = 272,
                Top = 75,
                Width = 70
            };

            Controls.Add(lbl);
            Controls.Add(txtName);
            Controls.Add(btnOk);
            Controls.Add(btnCancel);

            AcceptButton = btnOk;
            CancelButton = btnCancel;
        }
    }
}
