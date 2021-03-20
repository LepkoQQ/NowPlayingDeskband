using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Windows.Media.Control;

namespace NowPlayingDeskband
{
    class NowPlayingDeskbandControl : UserControl
    {
        private PictureBox albumArtPictureBox;
        private Label artistLabel;
        private Label titleLabel;

        private MediaSessionManager mediaSessionManager = null;

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
                Font = new Font("Segoe UI", 7F),
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
                Font = new Font("Segoe UI", 7F),
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

            mediaSessionManager = await MediaSessionManager.CreateAsync();

            SimpleLogger.DefaultLog("NowPlayingDeskbandControl::InitializeMediaSessionManager DONE");
        }

        //private async void UpdateSongDisplay()
        //{
        //    if (IsDisposed) {
        //        return;
        //    }
        //    SimpleLogger.DefaultLog("UpdateSongDisplay called...");
        //    var playing = sessionData.Values.Where(value => value.info.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing).ToList();
        //    var sorted = playing.OrderByDescending(value => value.updatedAt).ToList();
        //    SimpleLogger.DefaultLog("UpdateSongDisplay - Have " + sorted.Count + " sorted sessions");

        //    if (sorted.Count > 0)
        //    {
        //        SimpleLogger.DefaultLog("UpdateSongDisplay - Trying to display...");
        //        try
        //        {
        //            var first = sorted[0];
        //            artistLabel.Text = first.props.Artist;
        //            titleLabel.Text = first.props.Title;

        //            using (var randomAccessStream = await first.props.Thumbnail.OpenReadAsync())
        //            {
        //                using (var stream = randomAccessStream.AsStream())
        //                {
        //                    var oldImage = albumArtPictureBox.Image;
        //                    albumArtPictureBox.Image = Image.FromStream(stream);
        //                    oldImage?.Dispose();
        //                }
        //            }
        //        }
        //        catch (Exception e)
        //        {
        //            SimpleLogger.DefaultLog("UpdateSongDisplay - Exception: " + e.Message + "\n" + e.StackTrace);
        //            artistLabel.Text = "Exception";
        //            titleLabel.Text = ":(";
        //            albumArtPictureBox.Image?.Dispose();
        //            albumArtPictureBox.Image = null;
        //            SimpleLogger.DefaultLog("UpdateSongDisplay - Exception DONE");
        //        }
        //        SimpleLogger.DefaultLog("UpdateSongDisplay - Trying to display DONE");
        //    }
        //    else
        //    {
        //        SimpleLogger.DefaultLog("UpdateSongDisplay - No sessions, resetting display...");
        //        artistLabel.Text = "";
        //        titleLabel.Text = "";
        //        albumArtPictureBox.Image?.Dispose();
        //        albumArtPictureBox.Image = null;
        //        SimpleLogger.DefaultLog("UpdateSongDisplay - No sessions, resetting display DONE");
        //    }
        //    SimpleLogger.DefaultLog("UpdateSongDisplay DONE");
        //}
    }
}
