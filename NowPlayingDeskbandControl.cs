using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Windows.Media.Control;
using WindowsMediaController;

namespace NowPlayingDeskband
{
    using MediaProperties = GlobalSystemMediaTransportControlsSessionMediaProperties;
    using PlaybackInfo = GlobalSystemMediaTransportControlsSessionPlaybackInfo;
    
    struct MediaSessionWithData
    {
        public MediaManager.MediaSession session;
        public MediaProperties props;
        public PlaybackInfo info;
        public DateTime updatedAt;

        public static implicit operator MediaSessionWithData((MediaManager.MediaSession session, MediaProperties props, PlaybackInfo info, DateTime updatedAt) tuple)
        {
            return new MediaSessionWithData
            {
                session = tuple.session,
                props = tuple.props,
                info = tuple.info,
                updatedAt = tuple.updatedAt
            };
        }
    }

    class NowPlayingDeskbandControl : UserControl
    {
        private PictureBox albumArtPictureBox;
        private Label artistLabel;
        private Label titleLabel;

        private Dictionary<MediaManager.MediaSession, MediaSessionWithData> sessionData = new Dictionary<MediaManager.MediaSession, MediaSessionWithData>();

        public NowPlayingDeskbandControl()
        {
            SimpleLogger.DefaultLog("Creating NowPlayingDeskbandControl...");
            InitializeComponent();
            GetMediaInfo();
            SimpleLogger.DefaultLog("Creating NowPlayingDeskbandControl DONE");
        }

        private void InitializeComponent()
        {
            SimpleLogger.DefaultLog("Initializing components...");
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
            SimpleLogger.DefaultLog("Initializing components DONE");
        }

        private async void GetMediaInfo() {
            SimpleLogger.DefaultLog("MediaManager - Adding Listeners...");
            MediaManager.OnSongChanged += OnSongChanged;
            MediaManager.OnPlaybackStateChanged += OnPlaybackStateChanged;
            MediaManager.OnRemovedSource += OnRemovedSource;
            SimpleLogger.DefaultLog("MediaManager - Adding Listeners DONE");

            while (!IsHandleCreated) {
                SimpleLogger.DefaultLog("No handle yet, waiting...");
                await Task.Delay(250);
            }

            SimpleLogger.DefaultLog("MediaManager - Starting...");
            MediaManager.Start();
            SimpleLogger.DefaultLog("MediaManager - Starting DONE");
        }

        private void OnSongChanged(MediaManager.MediaSession session, MediaProperties props) {
            SimpleLogger.DefaultLog("OnSongChanged called...");
            if (IsDisposed) {
                return;
            }

            SimpleLogger.DefaultLog("OnSongChanged session:");
            SimpleLogger.DefaultLog("    SourceAppUserModelId: " + session.ControlSession.SourceAppUserModelId);

            SimpleLogger.DefaultLog("OnSongChanged props:");
            SimpleLogger.DefaultLog("    Artist: " + props.Artist);
            SimpleLogger.DefaultLog("    Title: " + props.Title);
            SimpleLogger.DefaultLog("    AlbumTitle: " + props.AlbumTitle);

            var info = session.ControlSession.GetPlaybackInfo();
            SimpleLogger.DefaultLog("OnSongChanged info:");
            SimpleLogger.DefaultLog("    PlaybackStatus: " + info.PlaybackStatus);

            Invoke((MethodInvoker)delegate {
                sessionData[session] = (session, props, info, DateTime.Now);
                UpdateSongDisplay();
            });
        }

        private void OnPlaybackStateChanged(MediaManager.MediaSession session, PlaybackInfo info) {
            SimpleLogger.DefaultLog("OnPlaybackStateChanged called...");
            if (IsDisposed) {
                return;
            }

            SimpleLogger.DefaultLog("OnPlaybackStateChanged session:");
            SimpleLogger.DefaultLog("    SourceAppUserModelId: " + session.ControlSession.SourceAppUserModelId);

            SimpleLogger.DefaultLog("OnPlaybackStateChanged info:");
            SimpleLogger.DefaultLog("    PlaybackStatus: " + info.PlaybackStatus);

            Invoke((MethodInvoker)delegate {
                if (sessionData.ContainsKey(session)) {
                    var props = sessionData[session].props;
                    sessionData[session] = (session, props, info, DateTime.Now);
                    UpdateSongDisplay();
                }
            });
        }

        private void OnRemovedSource(MediaManager.MediaSession session) {
            SimpleLogger.DefaultLog("OnRemovedSource called...");
            if (IsDisposed) {
                return;
            }

            SimpleLogger.DefaultLog("OnRemovedSource session:");
            SimpleLogger.DefaultLog("    SourceAppUserModelId: " + session.ControlSession.SourceAppUserModelId);

            Invoke((MethodInvoker)delegate {
                if (sessionData.ContainsKey(session)) {
                    sessionData.Remove(session);
                    UpdateSongDisplay();
                }
            });
        }

        private async void UpdateSongDisplay()
        {
            SimpleLogger.DefaultLog("UpdateSongDisplay called...");
            var playing = sessionData.Values.Where(value => value.info.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing).ToList();
            var sorted = playing.OrderByDescending(value => value.updatedAt).ToList();
            SimpleLogger.DefaultLog("UpdateSongDisplay - Have " + sorted.Count + " sorted sessions");

            if (sorted.Count > 0)
            {
                SimpleLogger.DefaultLog("UpdateSongDisplay - Trying to display...");
                try
                {
                    var first = sorted[0];
                    artistLabel.Text = first.props.Artist;
                    titleLabel.Text = first.props.Title;

                    using (var randomAccessStream = await first.props.Thumbnail.OpenReadAsync())
                    {
                        using (var stream = randomAccessStream.AsStream())
                        {
                            var oldImage = albumArtPictureBox.Image;
                            albumArtPictureBox.Image = Image.FromStream(stream);
                            oldImage?.Dispose();
                        }
                    }
                }
                catch (Exception e)
                {
                    SimpleLogger.DefaultLog("UpdateSongDisplay - Exception: " + e.Message + "\n" + e.StackTrace);
                    artistLabel.Text = "Exception";
                    titleLabel.Text = ":(";
                    albumArtPictureBox.Image?.Dispose();
                    albumArtPictureBox.Image = null;
                    SimpleLogger.DefaultLog("UpdateSongDisplay - Exception DONE");
                }
                SimpleLogger.DefaultLog("UpdateSongDisplay - Trying to display DONE");
            }
            else
            {
                SimpleLogger.DefaultLog("UpdateSongDisplay - No sessions, resetting display...");
                artistLabel.Text = "";
                titleLabel.Text = "";
                albumArtPictureBox.Image?.Dispose();
                albumArtPictureBox.Image = null;
                SimpleLogger.DefaultLog("UpdateSongDisplay - No sessions, resetting display DONE");
            }
            SimpleLogger.DefaultLog("UpdateSongDisplay DONE");
        }
    }
}
