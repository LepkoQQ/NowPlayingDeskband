using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NowPlayingDeskband
{
    class NowPlayingDeskbandControl : UserControl
    {
        private PictureBox albumArtPictureBox;
        private Label artistLabel;
        private Label titleLabel;

        private MediaSessionManager SessionManager = null;

        public NowPlayingDeskbandControl()
        {
            // Sync
            SimpleLogger.DefaultLog("NowPlayingDeskbandControl::constructor called...");
            InitializeComponent();
            SimpleLogger.DefaultLog("NowPlayingDeskbandControl::constructor DONE");

            // Async
            InitializeAsync();
        }

        private async void InitializeAsync() {
            SimpleLogger.DefaultLog("NowPlayingDeskbandControl::InitializeAsync called...");
            await InitializeMediaSessionManager();
            SimpleLogger.DefaultLog("NowPlayingDeskbandControl::InitializeAsync DONE");
        }

        private void InitializeComponent()
        {
            SimpleLogger.DefaultLog("NowPlayingDeskbandControl::InitializeComponent called...");
            SuspendLayout();

            Name = "Now Playing Deskband";
            Size = new Size(78, 78 + 12 + 12);
            BackColor = Color.Black;

            albumArtPictureBox = new PictureBox
            {
                Name = "Album Art Picture",
                Location = new Point(2, 2),
                Size = new Size(74, 74),
                SizeMode = PictureBoxSizeMode.Zoom,
                Image = null,
            };
            Controls.Add(albumArtPictureBox);

            artistLabel = new Label
            {
                Name = "Artist Label",
                Location = new Point(0, 78),
                Size = new Size(78, 12),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 6.5F),
                TextAlign = ContentAlignment.MiddleLeft,
                UseMnemonic = false,
                Text = "",
            };
            Controls.Add(artistLabel);

            titleLabel = new Label
            {
                Name = "Title Label",
                Location = new Point(0, 78 + 12),
                Size = new Size(78, 12),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 6.5F),
                TextAlign = ContentAlignment.MiddleLeft,
                UseMnemonic = false,
                Text = "",
            };
            Controls.Add(titleLabel);

            ResumeLayout(false);
            SimpleLogger.DefaultLog("NowPlayingDeskbandControl::InitializeComponent DONE");
        }

        private async Task InitializeMediaSessionManager() {
            SimpleLogger.DefaultLog("NowPlayingDeskbandControl::InitializeMediaSessionManager called...");

            while (!IsHandleCreated) {
                SimpleLogger.DefaultLog("    No handle yet, waiting...");
                await Task.Delay(250);
            }

            SessionManager = await MediaSessionManager.CreateAsync();
            SessionManager.CurrentSongChanged += OnCurrentSongChanged;
            SessionManager.ForceUpdate();

            SimpleLogger.DefaultLog("NowPlayingDeskbandControl::InitializeMediaSessionManager DONE");
        }

        private void OnCurrentSongChanged(object sender, MediaSessionManager.CurrentSongChangedEventArgs args) {
            if (IsDisposed) {
                return;
            }
            
            SimpleLogger.DefaultLog("NowPlayingDeskbandControl::OnCurrentSongChanged called...");

            Invoke((MethodInvoker)delegate {
                if (!args.PlaybackData.HasValue) {
                    SimpleLogger.DefaultLog("    PlaybackData is null, resetting display...");
                    artistLabel.Text = "";
                    titleLabel.Text = "";
                    albumArtPictureBox.Image?.Dispose();
                    albumArtPictureBox.Image = null;
                    SimpleLogger.DefaultLog("    PlaybackData is null, resetting display DONE");
                    return;
                }

                SimpleLogger.DefaultLog("    PlaybackData received, setting display...");
                var data = args.PlaybackData.Value;
                artistLabel.Text = data.Artist;
                titleLabel.Text = data.Title;
                if (albumArtPictureBox.Image != data.AlbumArt) {
                    albumArtPictureBox.Image?.Dispose();
                    albumArtPictureBox.Image = data.AlbumArt;
                }
                SimpleLogger.DefaultLog("    PlaybackData received, setting display DONE");
            });

            SimpleLogger.DefaultLog("NowPlayingDeskbandControl::OnCurrentSongChanged DONE");
        }
    }
}
