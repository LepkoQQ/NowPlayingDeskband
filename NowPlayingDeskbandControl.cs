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
            InitializeComponent();
            GetMediaInfo();
        }

        private void InitializeComponent()
        {
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
                Text = "",
            };
            Controls.Add(titleLabel);

            ResumeLayout(false);
        }

        private async void UpdateSongDisplay()
        {
            //System.Diagnostics.Debug.WriteLine(ms.ControlSession.SourceAppUserModelId);

            var playing = sessionData.Values.Where(value => value.info.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing).ToList();
            var sorted = playing.OrderByDescending(value => value.updatedAt).ToList();

            if (sorted.Count > 0)
            {
                var first = sorted[0];
                artistLabel.Text = first.props.Artist;
                titleLabel.Text = first.props.Title;

                using (var randomAccessStream = await first.props.Thumbnail.OpenReadAsync())
                {
                    using (var stream = randomAccessStream.AsStream())
                    {
                        var oldImage = albumArtPictureBox.Image;
                        albumArtPictureBox.Image = CropSpotifyImage(Image.FromStream(stream));
                        oldImage?.Dispose();
                    }
                }
            }
            else
            {
                artistLabel.Text = "";
                titleLabel.Text = "";
                albumArtPictureBox.Image?.Dispose();
                albumArtPictureBox.Image = null;
            }
        }

        private Image CropSpotifyImage(Image image)
        {
            if (image.Width != 300 || image.Height != 300)
            {
                return image;
            }

            using (var bitmap = new Bitmap(image))
            {
                if (bitmap.GetPixel(0, 0).A != 0 || bitmap.GetPixel(32, 0).A != 0 || bitmap.GetPixel(299 - 32, 0).A != 0 || bitmap.GetPixel(299, 0).A != 0)
                {
                    return image;
                }

                var croppedBitmap = bitmap.Clone(new Rectangle(32, 0, 300 - 64, 300 - 64), bitmap.PixelFormat);
                image.Dispose();
                return croppedBitmap;
            }
        }

        private async void GetMediaInfo()
        {
            MediaManager.OnSongChanged += OnSongChanged;
            MediaManager.OnPlaybackStateChanged += OnPlaybackStateChanged;
            MediaManager.OnRemovedSource += OnRemovedSource;

            while (!IsHandleCreated)
            {
                await Task.Delay(250);
            }

            MediaManager.Start();
        }

        private void OnSongChanged(MediaManager.MediaSession session, MediaProperties props)
        {
            if (IsDisposed) return;

            var info = session.ControlSession.GetPlaybackInfo();
            Invoke((MethodInvoker)delegate
            {
                sessionData[session] = (session, props, info, DateTime.Now);
                UpdateSongDisplay();
            });
        }

        private void OnPlaybackStateChanged(MediaManager.MediaSession session, PlaybackInfo info)
        {
            if (IsDisposed) return;

            Invoke((MethodInvoker)delegate
            {
                if (sessionData.ContainsKey(session))
                {
                    var props = sessionData[session].props;
                    sessionData[session] = (session, props, info, DateTime.Now);
                    UpdateSongDisplay();
                }
            });
        }

        private void OnRemovedSource(MediaManager.MediaSession session)
        {
            if (IsDisposed) return;

            Invoke((MethodInvoker)delegate
            {
                if (sessionData.ContainsKey(session))
                {
                    sessionData.Remove(session);
                    UpdateSongDisplay();
                }
            });
        }
    }
}
