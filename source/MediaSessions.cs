namespace OrbitIsland;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Media.Control;
using Windows.Storage.Streams;
public record MediaSource(string Id, string Name, bool Playing);
public record MediaSnapshot(bool Available, string Title, string Artist, string Source, string Status, double Position, double Duration, string Art, bool CanToggle, bool CanNext, bool CanPrevious, bool CanSeek, List<MediaSource> Sources, string Token, string SourceKey);
public sealed class MediaSessions : IDisposable
{
    GlobalSystemMediaTransportControlsSessionManager? manager;
    GlobalSystemMediaTransportControlsSession? selected;
    readonly Dictionary<string, GlobalSystemMediaTransportControlsSession> targets = new(); readonly Queue<string> tokens = new();
    string selectedId = "", cachedSource = "", cachedTitle = "", cachedArtist = "", cachedArt = "";
    DateTime metadataAt = DateTime.MinValue;
    public event Action? Changed;
    string? boundToken;
    public void BindToken(string? token)
    {
        if (boundToken != null && boundToken != token && !tokens.Contains(boundToken))
            targets.Remove(boundToken);
        boundToken = token;
    }
    public string SelectedId { get; set; } = "";
    public string? PlayingSourceId
    {
        get; private set;
    }
    public async Task Initialize()
    {
        if (manager != null)
            return;
        manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
        manager.SessionsChanged += SessionsChanged;
        manager.CurrentSessionChanged += CurrentChanged;
    }
    void SessionsChanged(GlobalSystemMediaTransportControlsSessionManager sender, SessionsChangedEventArgs args)
    {
        metadataAt = DateTime.MinValue;
        Changed?.Invoke();
    }
    void CurrentChanged(GlobalSystemMediaTransportControlsSessionManager sender, CurrentSessionChangedEventArgs args)
    {
        metadataAt = DateTime.MinValue;
        Changed?.Invoke();
    }
    static string Label(string source)
    {
        string s = source.ToLowerInvariant();
        if (s.Contains("chrome"))
            return "Google Chrome";
        if (s.Contains("msedge"))
            return "Microsoft Edge";
        if (s.Contains("firefox"))
            return "Firefox";
        if (s.Contains("spotify"))
            return "Spotify";
        if (s.Contains("brave"))
            return "Brave";
        if (s.Contains("vlc"))
            return "VLC";
        if (s.Contains("zunemusic") || s.Contains("mediaplayer"))
            return "Media Player";
        return source;
    }
    public async Task<MediaSnapshot> Read()
    {
        await Initialize();
        var sessions = manager!.GetSessions().ToList();
        var sources = sessions.Select((s, i) => new MediaSource(s.SourceAppUserModelId + "#" + i, Label(s.SourceAppUserModelId), s.GetPlaybackInfo().PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing)).ToList();
        int index = sources.FindIndex(s => s.Id == SelectedId);
        selected = index >= 0 ? sessions[index] : manager.GetCurrentSession() ?? sessions.FirstOrDefault(s => s.GetPlaybackInfo().PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing) ?? sessions.FirstOrDefault();
        if (selected == null)
        {
            PlayingSourceId = null;
            return new(false, "", "", "", "", 0, 0, "", false, false, false, false, sources, "", "");
        }
        int selectedIndex = sessions.FindIndex(s => s == selected);
        selectedId = selectedIndex >= 0 ? sources[selectedIndex].Id : selected.SourceAppUserModelId;
        var playback = selected.GetPlaybackInfo();
        var timeline = selected.GetTimelineProperties();
        var controls = playback.Controls;
        bool playing = playback.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;
        PlayingSourceId = playing ? selected.SourceAppUserModelId : null;
        if (cachedSource != selectedId || (DateTime.UtcNow - metadataAt).TotalSeconds >= 3)
        {
            if (cachedSource != selectedId)
                cachedArt = "";
            cachedSource = selectedId;
            metadataAt = DateTime.UtcNow;
            try
            {
                var props = await selected.TryGetMediaPropertiesAsync();
                bool newArt = cachedTitle != props.Title || cachedArtist != props.Artist;
                cachedTitle = string.IsNullOrWhiteSpace(props.Title) ? Label(selected.SourceAppUserModelId) : props.Title;
                cachedArtist = props.Artist;
                if (newArt || cachedArt.Length == 0)
                {
                    cachedArt = "";
                    if (props.Thumbnail != null)
                    {
                        using var stream = await props.Thumbnail.OpenReadAsync();
                        if (stream.Size > 0 && stream.Size <= 2097152)
                        {
                            using var reader = new DataReader(stream.GetInputStreamAt(0));
                            await reader.LoadAsync((uint)stream.Size);
                            var bytes = new byte[reader.UnconsumedBufferLength];
                            reader.ReadBytes(bytes);
                            cachedArt = Convert.ToBase64String(bytes);
                        }
                    }
                }
            }
            catch { cachedTitle = Label(selected.SourceAppUserModelId); cachedArtist = "Metadata is not available from this source"; cachedArt = ""; }
        }
        double duration = Math.Max(0, (timeline.EndTime - timeline.StartTime).TotalSeconds);
        double position = (timeline.Position - timeline.StartTime).TotalSeconds;
        if (playing && duration > 0)
        {
            double seconds = (DateTimeOffset.UtcNow - timeline.LastUpdatedTime).TotalSeconds;
            if (seconds >= 0 && seconds < 86400)
                position += seconds * (playback.PlaybackRate ?? 1);
        }
        string token = Guid.NewGuid().ToString("N");
        targets[token] = selected;
        tokens.Enqueue(token);
        while (tokens.Count > 32)
        {
            var expired = tokens.Dequeue();
            if (expired != boundToken)
                targets.Remove(expired);
        }
        return new(true, cachedTitle, cachedArtist, Label(selected.SourceAppUserModelId), playback.PlaybackStatus.ToString(), Math.Clamp(position, 0, Math.Max(0, duration)), duration, cachedArt, controls.IsPlayPauseToggleEnabled || controls.IsPlayEnabled || controls.IsPauseEnabled, controls.IsNextEnabled, controls.IsPreviousEnabled, controls.IsPlaybackPositionEnabled && duration > 0, sources, token, selectedId);
    }
    public async Task Execute(string command, double seconds = 0, string? token = null)
    {
        if (selected == null)
            await Read();
        var session = (token != null ? targets.GetValueOrDefault(token) : selected) ?? throw new InvalidOperationException("No Windows media session is available. Start playback in your browser or media app first.");
        bool ok = command switch
        {
            "toggle" => await Toggle(session),
            "next" => await session.TrySkipNextAsync(),
            "previous" => await session.TrySkipPreviousAsync(),
            "seek" => await session.TryChangePlaybackPositionAsync((long)((session.GetTimelineProperties().StartTime.TotalSeconds + Math.Max(0, seconds)) * TimeSpan.TicksPerSecond)),
            _ => false
        };
        if (!ok)
            throw new InvalidOperationException("This browser or app did not accept the " + command + " command. Its Windows media-session controls may not support that action.");
        metadataAt = DateTime.MinValue;
    }
    static async Task<bool> Toggle(GlobalSystemMediaTransportControlsSession session)
    {
        var state = session.GetPlaybackInfo();
        if (state.Controls.IsPlayPauseToggleEnabled)
            return await session.TryTogglePlayPauseAsync();
        return state.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing ? await session.TryPauseAsync() : await session.TryPlayAsync();
    }
    public void Dispose()
    {
        if (manager != null)
        {
            manager.SessionsChanged -= SessionsChanged;
            manager.CurrentSessionChanged -= CurrentChanged;
        }
    }
}
