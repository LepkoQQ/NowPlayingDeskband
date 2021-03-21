using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Media.Control;

namespace NowPlayingDeskband
{
    class MediaSessionManager
    {
        public struct PlaybackData
        {
            public bool IsPlaying;
            public string Artist;
            public string Title;
            // TODO: thumbnail data
            // try
            // {
            //     using (var randomAccessStream = await first.props.Thumbnail.OpenReadAsync())
            //     {
            //         using (var stream = randomAccessStream.AsStream())
            //         {
            //             var oldImage = albumArtPictureBox.Image;
            //             albumArtPictureBox.Image = Image.FromStream(stream);
            //             oldImage?.Dispose();
            //         }
            //     }
            // }

            public override bool Equals(object obj) {
                if (obj is PlaybackData other) {
                    return IsPlaying == other.IsPlaying && Artist == other.Artist && Title == other.Title;
                }
                return false;
            }

            public override int GetHashCode() {
                int hash = 17;
                hash = hash * 23 + IsPlaying.GetHashCode();
                hash = hash * 23 + Artist.GetHashCode();
                hash = hash * 23 + Title.GetHashCode();
                return hash;
            }
        }

        public class CurrentSongChangedEventArgs : EventArgs
        {
            public PlaybackData? PlaybackData { get; set; }
        }

        public event EventHandler<CurrentSongChangedEventArgs> CurrentSongChanged;

        private GlobalSystemMediaTransportControlsSessionManager SystemSessionManager = null;

        private readonly Dictionary<GlobalSystemMediaTransportControlsSession, PlaybackData> CurrentSessions = new Dictionary<GlobalSystemMediaTransportControlsSession, PlaybackData>();

        private PlaybackData? LastPlaybackData;

        private bool DisableUpdates = false;

        private MediaSessionManager() {
        }

        public static async Task<MediaSessionManager> CreateAsync() {
            SimpleLogger.DefaultLog("MediaSessionManager::CreateAsync called...");
            var instance = new MediaSessionManager {
                SystemSessionManager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync(),
            };
            instance.SystemSessionManager.SessionsChanged += instance.OnSessionsChanged;
            SimpleLogger.DefaultLog("MediaSessionManager::CreateAsync DONE");
            return instance;
        }

        public void ForceUpdate() {
            OnSessionsChanged(SystemSessionManager);
        }

        private void OnSessionsChanged(GlobalSystemMediaTransportControlsSessionManager sender, SessionsChangedEventArgs args = null) {
            DisableUpdates = true;

            SimpleLogger.DefaultLog("MediaSessionManager::OnSessionsChanged - Clearing old sessions...");
            foreach (var oldSession in CurrentSessions.Keys) {
                oldSession.MediaPropertiesChanged -= OnMediaPropertiesChanged;
                oldSession.PlaybackInfoChanged -= OnPlaybackInfoChanged;
            }
            CurrentSessions.Clear();
            SimpleLogger.DefaultLog("MediaSessionManager::OnSessionsChanged - Clearing old sessions DONE");

            SimpleLogger.DefaultLog("MediaSessionManager::OnSessionsChanged - Adding new sessions...");
            var sessions = sender.GetSessions();
            foreach (var session in sessions) {
                CurrentSessions[session] = new PlaybackData {
                    IsPlaying = false,
                    Artist = "",
                    Title = "",
                };
                OnMediaPropertiesChanged(session);
                OnPlaybackInfoChanged(session);
                session.MediaPropertiesChanged += OnMediaPropertiesChanged;
                session.PlaybackInfoChanged += OnPlaybackInfoChanged;
                SimpleLogger.DefaultLog($"    {session.SourceAppUserModelId}");
            }
            SimpleLogger.DefaultLog("MediaSessionManager::OnSessionsChanged - Adding new sessions DONE");

            DisableUpdates = false;
            UpdateCurrentSong();
        }

        private void OnMediaPropertiesChanged(GlobalSystemMediaTransportControlsSession session, MediaPropertiesChangedEventArgs args = null) {
            try {
                if (CurrentSessions.ContainsKey(session)) {
                    var props = session.TryGetMediaPropertiesAsync().GetAwaiter().GetResult();
                    var data = CurrentSessions[session];
                    var title = props.Title;
                    var artist = props.Artist;
                    if (data.Title != title || data.Artist != artist) {
                        data.Title = title;
                        data.Artist = artist;
                        CurrentSessions[session] = data;
                        UpdateCurrentSong();
                    }
                }
            } catch (Exception e) {
                SimpleLogger.DefaultLog($"MediaSessionManager::OnMediaPropertiesChanged - Exception - {e.Message}\n{e.StackTrace}");
            }
        }

        private void OnPlaybackInfoChanged(GlobalSystemMediaTransportControlsSession session, PlaybackInfoChangedEventArgs args = null) {
            if (CurrentSessions.ContainsKey(session)) {
                var info = session.GetPlaybackInfo();
                var data = CurrentSessions[session];
                var isPlaying = info.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;
                if (data.IsPlaying != isPlaying) {
                    data.IsPlaying = isPlaying;
                    CurrentSessions[session] = data;
                    UpdateCurrentSong();
                }
            }
        }

        private void UpdateCurrentSong() {
            if (DisableUpdates) {
                SimpleLogger.DefaultLog("MediaSessionManager::UpdateCurrentSong called, but updates are disabled.");
                return;
            }

            SimpleLogger.DefaultLog("MediaSessionManager::UpdateCurrentSong called...");

            foreach (var data in CurrentSessions.Values) {
                SimpleLogger.DefaultLog($"    > Artist={data.Artist}; Title={data.Title}; IsPlaying={data.IsPlaying}");
            }

            if (CurrentSessions.Count == 0) {
                if (!LastPlaybackData.HasValue) {
                    SimpleLogger.DefaultLog("MediaSessionManager::UpdateCurrentSong DONE (both null)");
                } else {
                    LastPlaybackData = null;
                    CurrentSongChanged?.Invoke(this, new CurrentSongChangedEventArgs());
                    SimpleLogger.DefaultLog("MediaSessionManager::UpdateCurrentSong DONE (old is not null, new is null)");
                }
                return;
            }

            // TODO: Be more intelligent with the selection
            // var playing = sessionData.Values.Where(value => value.info.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing).ToList();
            // var sorted = playing.OrderByDescending(value => value.updatedAt).ToList();
            var playbackData = CurrentSessions.Values.Last();
            if (LastPlaybackData.Equals(playbackData)) {
                SimpleLogger.DefaultLog("MediaSessionManager::UpdateCurrentSong DONE (old and new are equal)");
                return;
            }

            LastPlaybackData = playbackData;
            CurrentSongChanged?.Invoke(this, new CurrentSongChangedEventArgs { PlaybackData = playbackData });
            SimpleLogger.DefaultLog("MediaSessionManager::UpdateCurrentSong DONE");
        }
    }
}
