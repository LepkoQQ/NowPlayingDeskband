using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;

namespace NowPlayingDeskband
{
    class SettingsForm : Form
    {
        public event EventHandler SettingsChanged;

        public SettingsForm() {
            // Sync
            SimpleLogger.DefaultLog("SettingsForm::constructor called...");
            InitializeComponent();
            SimpleLogger.DefaultLog("SettingsForm::constructor DONE");
        }

        private void InitializeComponent() {
            SimpleLogger.DefaultLog("SettingsForm::InitializeComponent called...");
            SuspendLayout();

            Name = "Now Playing Deskband Settings";
            Text = "Now Playing Deskband Settings";
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowIcon = false;
            StartPosition = FormStartPosition.Manual;
            Location = new Point(Screen.PrimaryScreen.WorkingArea.Right - Width, Screen.PrimaryScreen.WorkingArea.Bottom - Height);
            //Size = new Size(78, 78 + 12 + 12);

            var panel = new TableLayoutPanel {
                Dock = DockStyle.Fill,
                Padding = new Padding(6),
            };
            Controls.Add(panel);

            var appearanceGroupBox = new GroupBox {
                Text = "Appearance",
                Dock = DockStyle.Fill,
                Padding = new Padding(7),
                AutoSize = true,
            };
            panel.Controls.Add(appearanceGroupBox, 0, 0);

            var rowPanel = new TableLayoutPanel {
                Dock = DockStyle.Top,
                AutoSize = true,
            };
            appearanceGroupBox.Controls.Add(rowPanel);

            var fontSizeLabel = new Label {
                Text = "Font size:",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Width = 100,
                Height = 20,
                Margin = new Padding(0),
            };
            rowPanel.Controls.Add(fontSizeLabel, 0, 0);

            var numberInput = new NumericUpDown {
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                Value = 8,
                DecimalPlaces = 2,
                Increment = 0.25M,
            };
            rowPanel.Controls.Add(numberInput, 1, 0);

            var otherGroupBox = new GroupBox {
                Text = "Other",
                Dock = DockStyle.Fill,
                //AutoSize = true,
            };
            panel.Controls.Add(otherGroupBox, 0, 1);


            ResumeLayout(false);
            SimpleLogger.DefaultLog("SettingsForm::InitializeComponent DONE");
        }

        protected override void OnFormClosing(FormClosingEventArgs e) {
            SettingsChanged?.Invoke(this, null);
            base.OnFormClosing(e);
        }
    }
}
