﻿using System;
using System.Drawing;
using System.Drawing.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NowPlayingDeskband
{
    class NowPlayingDeskbandControl : UserControl
    {
        private ContextMenuStrip contextMenuStrip;
        private PictureBox albumArtPictureBox;
        private Label artistLabel;
        private Label titleLabel;

        private MediaSessionManager SessionManager = null;

        public NowPlayingDeskbandControl()
        {
            InitializeAll();
        }

        private void InitializeAll() {
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

            if (contextMenuStrip == null) {
                contextMenuStrip = new ContextMenuStrip();

                var settingsItem = new ToolStripMenuItem {
                    Text = "Settings",
                };
                settingsItem.Click += OnOpenSettingsWindow;
                contextMenuStrip.Items.Add(settingsItem);

                var forceUpdateSessionsItem = new ToolStripMenuItem {
                    Text = "Force Update",
                };
                forceUpdateSessionsItem.Click += OnForceUpdateSessions;
                contextMenuStrip.Items.Add(forceUpdateSessionsItem);

                var reinitializeItem = new ToolStripMenuItem {
                    Text = "Reinitialize Component",
                };
                reinitializeItem.Click += OnReinitializeComponent;
                contextMenuStrip.Items.Add(reinitializeItem);

                ContextMenuStrip = contextMenuStrip;
            }

            albumArtPictureBox = new PictureBox {
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
                Location = new Point(1, 77),
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
                Location = new Point(1, 77 + 12),
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

        private void OnOpenSettingsWindow(object sender, EventArgs e) {
            var form = new SettingsForm();
            form.SettingsChanged += OnSettingsChanged;
            form.Show();
        }

        private void OnForceUpdateSessions(object sender, EventArgs e) {
            SessionManager?.ForceUpdate();
        }

        private void OnReinitializeComponent(object sender, EventArgs e) {
            Controls.Clear();
            albumArtPictureBox = null;
            artistLabel = null;
            titleLabel = null;

            SessionManager.CurrentSongChanged -= OnCurrentSongChanged;
            SessionManager.Destroy();
            SessionManager = null;

            InitializeAll();
        }

        private void OnSettingsChanged(object sender, EventArgs e) {
            SimpleLogger.DefaultLog("OnSettingsChanged");
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
                    albumArtPictureBox.Image = null;
                    SimpleLogger.DefaultLog("    PlaybackData is null, resetting display DONE");
                    return;
                }

                SimpleLogger.DefaultLog("    PlaybackData received, setting display...");
                var data = args.PlaybackData.Value;
                artistLabel.Text = data.Artist;
                titleLabel.Text = data.Title;
                if (albumArtPictureBox.Image != data.AlbumArt) {
                    if (data.AlbumArt.Width > data.AlbumArt.Height) {
                        var ratio = data.AlbumArt.Height / (double)data.AlbumArt.Width;
                        albumArtPictureBox.Height = (int)Math.Ceiling(albumArtPictureBox.Width * ratio);
                        albumArtPictureBox.Top = 2 + albumArtPictureBox.Width - albumArtPictureBox.Height;
                    } else {
                        albumArtPictureBox.Height = albumArtPictureBox.Width;
                        albumArtPictureBox.Top = 2;
                    }
                    albumArtPictureBox.Image = data.AlbumArt;
                }
                SimpleLogger.DefaultLog("    PlaybackData received, setting display DONE");
            });

            SimpleLogger.DefaultLog("NowPlayingDeskbandControl::OnCurrentSongChanged DONE");
        }
    }
}
