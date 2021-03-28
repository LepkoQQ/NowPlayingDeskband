using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
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
            public Image AlbumArt;

            public override bool Equals(object obj) {
                if (obj is PlaybackData other) {
                    return IsPlaying == other.IsPlaying && Artist == other.Artist && Title == other.Title && AlbumArt == other.AlbumArt;
                }
                return false;
            }

            public override int GetHashCode() {
                int hash = 17;
                hash = hash * 23 + IsPlaying.GetHashCode();
                hash = hash * 23 + Artist.GetHashCode();
                hash = hash * 23 + Title.GetHashCode();
                hash = hash * 23 + AlbumArt.GetHashCode();
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
            foreach (var entry in CurrentSessions) {
                entry.Key.MediaPropertiesChanged -= OnMediaPropertiesChanged;
                entry.Key.PlaybackInfoChanged -= OnPlaybackInfoChanged;
                entry.Value.AlbumArt?.Dispose();
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
            SimpleLogger.DefaultLog("MediaSessionManager::OnMediaPropertiesChanged called...");
            try {
                if (CurrentSessions.ContainsKey(session)) {
                    var props = session.TryGetMediaPropertiesAsync().GetAwaiter().GetResult();
                    var title = props.Title;
                    var artist = props.Artist;
                    Image albumArt = null;
                    if (props.Thumbnail != null) {
                        using (var winStream = props.Thumbnail.OpenReadAsync().GetAwaiter().GetResult()) {
                            using (var stream = winStream.AsStream()) {
                                albumArt = Image.FromStream(stream);
                            }
                        }
                    }

                    var data = CurrentSessions[session];
                    if (data.Title != title || data.Artist != artist || data.AlbumArt != albumArt) {
                        data.Title = title;
                        data.Artist = artist;
                        data.AlbumArt = albumArt;
                        CurrentSessions[session] = data;
                        UpdateCurrentSong();
                    }
                }
            } catch (Exception e) {
                SimpleLogger.DefaultLog($"MediaSessionManager::OnMediaPropertiesChanged - Exception - {e.Message}\n{e.StackTrace}");
            }
            SimpleLogger.DefaultLog("MediaSessionManager::OnMediaPropertiesChanged DONE");
        }

        private void OnPlaybackInfoChanged(GlobalSystemMediaTransportControlsSession session, PlaybackInfoChangedEventArgs args = null) {
            if (CurrentSessions.ContainsKey(session)) {
                var info = session.GetPlaybackInfo();
                var isPlaying = info.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;

                var data = CurrentSessions[session];
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

            var sessions = CurrentSessions.Values.ToList();
            SimpleLogger.DefaultLog("> All sessions:");
            foreach (var data in sessions) {
                SimpleLogger.DefaultLog($"    > Artist={data.Artist}; Title={data.Title}; IsPlaying={data.IsPlaying}");
            }

            sessions = sessions.Where(value => value.IsPlaying).ToList();
            SimpleLogger.DefaultLog("> Playing sessions:");
            foreach (var data in sessions) {
                SimpleLogger.DefaultLog($"    > Artist={data.Artist}; Title={data.Title}; IsPlaying={data.IsPlaying}");
            }

            sessions = sessions.Where(value => value.Artist != "" || value.Title != "").ToList();
            SimpleLogger.DefaultLog("> Sessions with info:");
            foreach (var data in sessions) {
                SimpleLogger.DefaultLog($"    > Artist={data.Artist}; Title={data.Title}; IsPlaying={data.IsPlaying}");
            }

            if (sessions.Count == 0) {
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
            // var sorted = playing.OrderByDescending(value => value.updatedAt).ToList();
            var playbackData = sessions.Last();
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
