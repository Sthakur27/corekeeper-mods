using System;
using System.Collections.Generic;
using PugMod;
using UnityEngine;
using UnityEngine.Networking;

namespace StimHits
{
    /// <summary>
    /// Loads the replacement sounds and plays them.
    ///
    /// Built-in sounds are WAVs shipped in the mod's Sounds/ folder, read through the loader's
    /// sandbox-safe <see cref="LoadedMod.GetFile"/> and decoded here.
    ///
    /// Local sounds are every .mp3/.ogg/.wav under <see cref="LocalRoot"/> in the game's mod config
    /// folder (<c>%USERPROFILE%\AppData\LocalLow\Pugstorm\Core Keeper\Steam\&lt;id&gt;\mods\StimHits\Sounds\</c>,
    /// subfolders included). They are listed through <see cref="API.ConfigFilesystem"/> (the sandbox
    /// blocks System.IO) and each one becomes a choice in every slot, named after the file. Files named
    /// hit/hurt/kill (+ numbered variants kill2..) in the folder root are what "Auto" plays, picked at
    /// random. WAVs are read as bytes; mp3/ogg are decoded by Unity via a file:// UnityWebRequest.
    /// They live outside the mod, so updates never delete them and they are never distributed.
    /// Everything goes through the Effects mixer, so the game's SFX volume applies.
    /// </summary>
    public static class SoundBank
    {
        public enum Kind { Hit, Hurt, Kill }

        public const string LocalRoot = "StimHits/Sounds";
        private static readonly string[] FileNames = { "hit", "hurt", "kill" };
        private const int Voices = 8;
        private const float MinGap = 0.045f; // seconds between two sounds of the same kind

        public static readonly string[] BuiltInChoices =
        {
            "Off", "Auto", "Ting", "Clang", "Coin",
            "Game Clang", "Game Small Clang", "Game Ding", "Game Anvil", "Game Bell", "Game Shield",
        };

        private static LoadedMod _mod;
        private static AudioClip _ting, _clang, _coin;
        private static AudioSource[] _sources;
        private static int _next;
        private static readonly float[] _lastPlayed = { -1f, -1f, -1f };

        // Local files: choice name -> config-relative path; Auto files per kind; loaded clips by path.
        private static Dictionary<string, string> _localByName;
        private static List<string> _localNames;
        private static readonly List<string>[] _autoPaths = { new List<string>(), new List<string>(), new List<string>() };
        private static readonly Dictionary<string, AudioClip> _clips = new Dictionary<string, AudioClip>();
        private static readonly Queue<string> _pending = new Queue<string>();
        private static UnityWebRequest _request;
        private static string _requestPath;

        public static void Init(LoadedMod mod)
        {
            _mod = mod;
            _ting = ParseWav(ReadModFile("Sounds/stim_hit.wav"), "Sounds/stim_hit.wav");
            _clang = ParseWav(ReadModFile("Sounds/stim_hurt.wav"), "Sounds/stim_hurt.wav");
            _coin = ParseWav(ReadModFile("Sounds/stim_coin.wav"), "Sounds/stim_coin.wav");
            if (_localNames == null) Discover();
            Prepare("Auto");
        }

        /// <summary>Every option a sound slot offers: built-ins, then one entry per local file.</summary>
        public static string[] Choices()
        {
            if (_localNames == null) Discover();
            var all = new List<string>(BuiltInChoices);
            all.AddRange(_localNames);
            return all.ToArray();
        }

        /// <summary>Preloads the file(s) behind <paramref name="choice"/> (called when a slot changes).</summary>
        public static void Prepare(string choice)
        {
            if (_localNames == null) Discover();
            if (choice == "Auto")
            {
                foreach (var list in _autoPaths)
                    foreach (var path in list) EnsureLoaded(path);
            }
            else if (_localByName.TryGetValue(choice, out var path)) EnsureLoaded(path);
        }

        /// <summary>Lists the local sound files. Runs once, before the settings pages are built.</summary>
        private static void Discover()
        {
            _localByName = new Dictionary<string, string>();
            _localNames = new List<string>();
            foreach (var list in _autoPaths) list.Clear();
            var fs = API.ConfigFilesystem;
            if (fs == null) return;
            var files = new List<string>();
            try
            {
                if (!fs.DirectoryExists(LocalRoot)) fs.CreateDirectory(LocalRoot);
                foreach (var f in fs.GetFiles(LocalRoot)) files.Add(f.Replace('\\', '/'));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[{StimHitsMod.Name}] Could not list {LocalRoot}: {e.Message}");
                return;
            }
            files.Sort(StringComparer.OrdinalIgnoreCase);
            foreach (var path in files)
            {
                var dot = path.LastIndexOf('.');
                if (dot < 0) continue;
                var ext = path.Substring(dot + 1).ToLowerInvariant();
                if (ext != "mp3" && ext != "ogg" && ext != "wav") continue;
                var rel = path.StartsWith(LocalRoot + "/") ? path.Substring(LocalRoot.Length + 1) : path;
                var stem = rel.Substring(0, rel.LastIndexOf('.'));
                var slash = stem.LastIndexOf('/');
                var name = slash >= 0 ? stem.Substring(slash + 1) : stem;
                if (_localByName.ContainsKey(name) || Array.IndexOf(BuiltInChoices, name) >= 0) name = stem;
                if (_localByName.ContainsKey(name)) continue;
                _localByName[name] = path;
                _localNames.Add(name);
                if (slash < 0)
                    for (var k = 0; k < FileNames.Length; k++)
                        if (IsAutoName(stem, FileNames[k])) _autoPaths[k].Add(path);
            }
            Debug.Log($"[{StimHitsMod.Name}] {_localNames.Count} local sound(s) in {LocalRoot}");
        }

        /// <summary>"kill", "kill2" .. "kill99" count as Auto files for the kill slot.</summary>
        private static bool IsAutoName(string stem, string kind)
        {
            if (!stem.StartsWith(kind, StringComparison.OrdinalIgnoreCase)) return false;
            for (var i = kind.Length; i < stem.Length; i++)
                if (!char.IsDigit(stem[i])) return false;
            return true;
        }

        private static void EnsureLoaded(string path)
        {
            if (_clips.ContainsKey(path) || _pending.Contains(path) || path == _requestPath) return;
            if (path.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
            {
                byte[] data = null;
                try { data = API.ConfigFilesystem.Read(path); }
                catch (Exception e) { Debug.LogWarning($"[{StimHitsMod.Name}] Could not read {path}: {e.Message}"); }
                _clips[path] = TrimLeadingSilence(ParseWav(data, path));
                return;
            }
            _pending.Enqueue(path);
            StartNext();
        }

        /// <summary>Absolute folder of API.ConfigFilesystem on Steam builds, for file:// requests.</summary>
        private static string AbsoluteConfigRoot()
        {
            if (!Steamworks.SteamClient.IsValid) return null;
            return Application.persistentDataPath.Replace('\\', '/') + "/Steam/" + Steamworks.SteamClient.SteamId.AccountId + "/mods/";
        }

        private static void StartNext()
        {
            while (_request == null && _pending.Count > 0)
            {
                var path = _pending.Dequeue();
                var root = AbsoluteConfigRoot();
                if (root == null)
                {
                    Debug.LogWarning($"[{StimHitsMod.Name}] Cannot locate {path} outside Steam; use .wav instead.");
                    _clips[path] = null;
                    continue;
                }
                var type = path.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase) ? AudioType.MPEG : AudioType.OGGVORBIS;
                try
                {
                    _request = UnityWebRequestMultimedia.GetAudioClip("file:///" + root + path, type);
                    var handler = (DownloadHandlerAudioClip)_request.downloadHandler;
                    handler.streamAudio = false;
                    handler.compressed = false; // decoded PCM, so the silence trim can read the samples
                    _request.SendWebRequest();
                    _requestPath = path;
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[{StimHitsMod.Name}] Could not request {path}: {e.Message}");
                    _request = null;
                    _clips[path] = null;
                }
            }
        }

        /// <summary>Called every frame from <see cref="StimHitsMod.Update"/> to finish mp3/ogg loads.</summary>
        public static void Update()
        {
            if (_request == null || !_request.isDone) return;
            AudioClip clip = null;
            if (_request.result == UnityWebRequest.Result.Success)
            {
                clip = DownloadHandlerAudioClip.GetContent(_request);
                if (clip != null) clip.name = _requestPath;
            }
            if (clip == null || clip.length <= 0f)
            {
                Debug.LogWarning($"[{StimHitsMod.Name}] Could not decode {_requestPath}: {_request.error}");
                clip = null;
            }
            else
            {
                clip = TrimLeadingSilence(clip);
                Debug.Log($"[{StimHitsMod.Name}] Loaded {_requestPath} ({clip.length:0.00}s)");
            }
            _clips[_requestPath] = clip;
            _request.Dispose();
            _request = null;
            _requestPath = null;
            StartNext();
        }

        /// <summary>The clip for a local file; starts loading it on first use (the built-in plays meanwhile).</summary>
        private static AudioClip Loaded(string path)
        {
            if (_clips.TryGetValue(path, out var clip)) return clip;
            EnsureLoaded(path);
            return _clips.TryGetValue(path, out clip) ? clip : null;
        }

        /// <summary>Plays the configured sound for <paramref name="kind"/> (nothing when the slot is Off).</summary>
        public static void Play(Kind kind, Transform at)
        {
            var slot = StimHitsMod.Slot(kind);
            var choice = slot.Sound?.Value ?? "Off";
            if (choice == "Off") return;
            var now = Time.unscaledTime;
            if (now - _lastPlayed[(int)kind] < MinGap) return;
            _lastPlayed[(int)kind] = now;

            var volume = Mathf.Clamp01(slot.Volume?.Value ?? 0.8f);
            var pitch = (slot.Pitch?.Value ?? 1f) * UnityEngine.Random.Range(0.97f, 1.03f); // repeats don't drone

            if (TryGameSound(choice, out var sfx))
            {
                AudioManager.SfxFollowTransform(sfx, at, volume, pitch, 0f, reuse: false, AudioManager.MixerGroupEnum.EFFECTS,
                    ignoreAudioIfOutsideOfViewport: false, useSpatialSound: false, playOnGamepad: kind == Kind.Hurt);
                return;
            }

            var builtIn = kind == Kind.Hit ? _ting : kind == Kind.Hurt ? _clang : _coin;
            AudioClip clip = null;
            switch (choice)
            {
                case "Ting": clip = _ting; break;
                case "Clang": clip = _clang; break;
                case "Coin": clip = _coin; break;
                case "Auto": // a random hit/hurt/kill file from the local folder, else the built-in sound
                    var loaded = new List<AudioClip>();
                    foreach (var path in _autoPaths[(int)kind])
                    {
                        var c = Loaded(path);
                        if (c != null) loaded.Add(c);
                    }
                    if (loaded.Count > 0) clip = loaded[UnityEngine.Random.Range(0, loaded.Count)];
                    break;
                default: // a local file picked by name
                    if (_localByName != null && _localByName.TryGetValue(choice, out var file)) clip = Loaded(file);
                    break;
            }
            clip = clip ?? builtIn;
            if (clip != null) PlayClip(clip, volume, pitch);
        }

        private static bool TryGameSound(string choice, out SfxID sfx)
        {
            switch (choice)
            {
                case "Game Clang": sfx = SfxID.metalImpact; return true;
                case "Game Small Clang": sfx = SfxID.metalImpactSmall; return true;
                case "Game Ding": sfx = SfxID.inventory_ding; return true;
                case "Game Anvil": sfx = SfxID.anvil; return true;
                case "Game Bell": sfx = SfxID.Bell; return true;
                case "Game Shield": sfx = SfxID.shieldBlock; return true;
                default: sfx = default; return false;
            }
        }

        private static void PlayClip(AudioClip clip, float volume, float pitch)
        {
            if (_sources == null)
            {
                var go = new GameObject("StimHitsAudio");
                UnityEngine.Object.DontDestroyOnLoad(go);
                _sources = new AudioSource[Voices];
                for (var i = 0; i < Voices; i++)
                {
                    var s = go.AddComponent<AudioSource>();
                    s.playOnAwake = false;
                    s.spatialBlend = 0f;
                    _sources[i] = s;
                }
            }
            // Prefer an idle voice; otherwise cut the oldest (round robin).
            AudioSource src = null;
            for (var i = 0; i < Voices && src == null; i++)
            {
                var s = _sources[(_next + i) % Voices];
                if (!s.isPlaying) src = s;
            }
            if (src == null) src = _sources[_next];
            _next = (_next + 1) % Voices;

            var audio = Manager.audio;
            if (audio != null) src.outputAudioMixerGroup = audio.effectsMixerGroup;
            src.clip = clip;
            src.volume = volume;
            src.pitch = pitch;
            src.Play();
        }

        /// <summary>
        /// Drops the quiet lead-in many downloaded clips have (up to ~0.4 s, plus mp3 encoder delay),
        /// so the sound lands on the hit instead of lagging behind it. Keeps 2 ms before the onset.
        /// </summary>
        private static AudioClip TrimLeadingSilence(AudioClip clip)
        {
            if (clip == null) return null;
            try
            {
                var ch = clip.channels;
                var data = new float[clip.samples * ch];
                if (!clip.GetData(data, 0)) return clip;
                var peak = 0f;
                foreach (var v in data) peak = Mathf.Max(peak, Mathf.Abs(v));
                if (peak <= 0f) return clip;
                var threshold = peak * 0.02f;
                var first = 0;
                while (first < data.Length && Mathf.Abs(data[first]) < threshold) first++;
                var startFrame = Mathf.Max(0, first / ch - clip.frequency / 500); // 2 ms pre-roll
                if (startFrame < clip.frequency / 100) return clip; // under 10 ms: leave it alone
                var frames = clip.samples - startFrame;
                var trimmed = new float[frames * ch];
                Array.Copy(data, startFrame * ch, trimmed, 0, trimmed.Length);
                var result = AudioClip.Create(clip.name, frames, ch, clip.frequency, false);
                result.SetData(trimmed, 0);
                Debug.Log($"[{StimHitsMod.Name}] Trimmed {startFrame * 1000 / clip.frequency} ms of silence from {clip.name}");
                return result;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[{StimHitsMod.Name}] Could not trim {clip.name}: {e.Message}");
                return clip;
            }
        }

        private static byte[] ReadModFile(string path)
        {
            try { return _mod?.GetFile(path); }
            catch (Exception e)
            {
                Debug.LogWarning($"[{StimHitsMod.Name}] Could not read {path}: {e.Message}");
                return null;
            }
        }

        /// <summary>Decodes a PCM WAV (8/16/24/32-bit int or 32-bit float).</summary>
        private static AudioClip ParseWav(byte[] data, string path)
        {
            try
            {
                if (data == null) throw new Exception("file not found");
                if (data.Length < 12 || data[0] != 'R' || data[1] != 'I' || data[8] != 'W') throw new Exception("not a RIFF/WAVE file");
                int channels = 0, rate = 0, bits = 0, format = 0, pos = 12;
                while (pos + 8 <= data.Length)
                {
                    var id = "" + (char)data[pos] + (char)data[pos + 1] + (char)data[pos + 2] + (char)data[pos + 3];
                    var size = BitConverter.ToInt32(data, pos + 4);
                    var body = pos + 8;
                    if (id == "fmt ")
                    {
                        format = BitConverter.ToInt16(data, body);
                        channels = BitConverter.ToInt16(data, body + 2);
                        rate = BitConverter.ToInt32(data, body + 4);
                        bits = BitConverter.ToInt16(data, body + 14);
                    }
                    else if (id == "data")
                    {
                        if (channels <= 0 || rate <= 0) throw new Exception("data before fmt");
                        var bytesPer = bits / 8;
                        var count = Math.Min(size, data.Length - body) / bytesPer;
                        var samples = new float[count];
                        for (var i = 0; i < count; i++)
                        {
                            var o = body + i * bytesPer;
                            switch (bits)
                            {
                                case 8: samples[i] = (data[o] - 128) / 128f; break;
                                case 16: samples[i] = BitConverter.ToInt16(data, o) / 32768f; break;
                                case 24: samples[i] = ((data[o] << 8 | data[o + 1] << 16 | data[o + 2] << 24) >> 8) / 8388608f; break;
                                case 32: samples[i] = format == 3 ? BitConverter.ToSingle(data, o) : BitConverter.ToInt32(data, o) / 2147483648f; break;
                                default: throw new Exception($"unsupported {bits}-bit");
                            }
                        }
                        var clip = AudioClip.Create(path, count / channels, channels, rate, false);
                        clip.SetData(samples, 0);
                        return clip;
                    }
                    pos = body + size + (size & 1);
                }
                throw new Exception("no data chunk");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[{StimHitsMod.Name}] Could not load {path}: {e.Message}");
                return null;
            }
        }
    }
}
