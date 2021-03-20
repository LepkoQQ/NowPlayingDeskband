using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Control;
using Windows.Foundation;

namespace NowPlayingDeskband
{
    class MediaSessionManager
    {
        struct PlaybackData
        {
            public bool IsPlaying;
            public string Artist;
            public string Title;
            // TODO: thumbnail data
        }

        private GlobalSystemMediaTransportControlsSessionManager SystemSessionManager = null;
        private readonly Dictionary<GlobalSystemMediaTransportControlsSession, PlaybackData> CurrentSessions = new Dictionary<GlobalSystemMediaTransportControlsSession, PlaybackData>();

        private bool PauseUpdates = false;

        private MediaSessionManager() {
        }

        public static async Task<MediaSessionManager> CreateAsync() {
            SimpleLogger.DefaultLog("MediaSessionManager::CreateAsync called...");
            var instance = new MediaSessionManager {
                SystemSessionManager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync(),
            };
            instance.OnSessionsChanged(instance.SystemSessionManager);
            instance.SystemSessionManager.SessionsChanged += instance.OnSessionsChanged;
            SimpleLogger.DefaultLog("MediaSessionManager::CreateAsync DONE");
            return instance;
        }

        private void OnSessionsChanged(GlobalSystemMediaTransportControlsSessionManager sender, SessionsChangedEventArgs args = null) {
            PauseUpdates = true;

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

            PauseUpdates = false;
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
            if (PauseUpdates) {
                SimpleLogger.DefaultLog("MediaSessionManager::UpdateCurrentSong called, but updates are paused.");
                return;
            }

            SimpleLogger.DefaultLog("MediaSessionManager::UpdateCurrentSong called...");
            foreach (var data in CurrentSessions.Values) {
                SimpleLogger.DefaultLog($"    - {data.Artist} - {data.Title} - {data.IsPlaying}");
            }
            SimpleLogger.DefaultLog("MediaSessionManager::UpdateCurrentSong DONE");
        }
    }
}
