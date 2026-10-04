using System;
using PugMod;
using UnityEngine;
using UnityEngine.Networking;

namespace StimHits
{
    /// <summary>
    /// Loads the replacement sounds and plays them.
    /// Built-in sounds are WAVs shipped in the mod's Sounds/ folder, read through the loader's
    /// sandbox-safe <see cref="LoadedMod.GetFile"/> and decoded here. Local sounds (hit/hurt/kill
    /// .mp3, .ogg or .wav, plus numbered variants kill2..kill8 picked at random) live in a folder outside the mod so updates never delete them and they
    /// are never distributed: <c>%USERPROFILE%\AppData\LocalLow\Pugstorm\Core Keeper\StimHits\</c>.
    /// Unity decodes those via a file:// UnityWebRequest. "Auto" plays the local file when there is
    /// one, otherwise the slot's built-in sound. Named game sounds play through AudioManager.
    /// Everything goes through the Effects mixer, so the game's SFX volume applies.
    /// </summary>
    public static class SoundBank
    {
        public enum Kind { Hit, Hurt, Kill }

        private static readonly string[] FileNames = { "hit", "hurt", "kill" };
        private static readonly string[] Extensions = { "mp3", "ogg", "wav" };
        private const int MaxVariants = 8; // kill, kill2 .. kill8
        private const int Kinds = 3;
        private const int Voices = 8;
        private const float MinGap = 0.045f; // seconds between two sounds of the same kind

        private static LoadedMod _mod;
        private static AudioClip _ting, _clang, _coin;
        private static readonly System.Collections.Generic.List<AudioClip>[] _local =
            { new System.Collections.Generic.List<AudioClip>(), new System.Collections.Generic.List<AudioClip>(), new System.Collections.Generic.List<AudioClip>() };
        private static AudioSource[] _sources;
        private static int _next;
        private static readonly float[] _lastPlayed = { -1f, -1f, -1f };

        // Local file loading: one request at a time, walking (kind, variant, extension) candidates.
        private static UnityWebRequest _request;
        private static int _candidate = -1;
        private static bool _variantFound; // the current (kind, variant) already loaded with an earlier extension

        private static int CandidatesPerKind => MaxVariants * Extensions.Length;
        private static string CandidateName(int c)
        {
            var kind = c / CandidatesPerKind;
            var variant = c % CandidatesPerKind / Extensions.Length;
            return FileNames[kind] + (variant == 0 ? "" : (variant + 1).ToString()) + "." + Extensions[c % Extensions.Length];
        }

        public static string LocalFolder => Application.persistentDataPath + "/StimHits";

        public static void Init(LoadedMod mod)
        {
            _mod = mod;
            _ting = LoadWav("Sounds/stim_hit.wav");
            _clang = LoadWav("Sounds/stim_hurt.wav");
            _coin = LoadWav("Sounds/stim_coin.wav");
            ReloadLocal();
        }

        /// <summary>Rescans the local folder (startup, and whenever a slot is switched to Auto).</summary>
        public static void ReloadLocal()
        {
            _request?.Dispose();
            _request = null;
            for (var i = 0; i < Kinds; i++) _local[i].Clear();
            _variantFound = false;
            _candidate = 0;
            StartCandidate();
        }

        /// <summary>Called every frame from <see cref="StimHitsMod.Update"/> to finish local loads.</summary>
        public static void Update()
        {
            if (_request == null || !_request.isDone) return;
            var kind = _candidate / CandidatesPerKind;
            if (_request.result == UnityWebRequest.Result.Success)
            {
                var clip = DownloadHandlerAudioClip.GetContent(_request);
                if (clip != null && clip.length > 0f)
                {
                    clip.name = "StimHits local " + CandidateName(_candidate);
                    _local[kind].Add(clip);
                    _variantFound = true;
                    Debug.Log($"[{StimHitsMod.Name}] Local sound loaded: {CandidateName(_candidate)} ({clip.length:0.00}s)");
                }
            }
            _request.Dispose();
            _request = null;
            Advance();
            StartCandidate();
        }

        private static void Advance()
        {
            _candidate++;
            if (_candidate % Extensions.Length == 0) _variantFound = false; // next variant
        }

        private static void StartCandidate()
        {
            while (_candidate >= 0 && _candidate < Kinds * CandidatesPerKind)
            {
                if (_variantFound) { Advance(); continue; } // e.g. kill.mp3 loaded: skip kill.ogg/kill.wav
                var ext = Extensions[_candidate % Extensions.Length];
                var type = ext == "mp3" ? AudioType.MPEG : ext == "ogg" ? AudioType.OGGVORBIS : AudioType.WAV;
                var url = "file:///" + LocalFolder.Replace('\\', '/') + "/" + CandidateName(_candidate);
                try
                {
                    _request = UnityWebRequestMultimedia.GetAudioClip(url, type);
                    ((DownloadHandlerAudioClip)_request.downloadHandler).streamAudio = false;
                    _request.SendWebRequest();
                    return;
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[{StimHitsMod.Name}] Could not request {url}: {e.Message}");
                    _request = null;
                    Advance();
                }
            }
            _candidate = -1;
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

            AudioClip clip;
            switch (choice)
            {
                case "Ting": clip = _ting; break;
                case "Clang": clip = _clang; break;
                case "Coin": clip = _coin; break;
                default: // Auto: the local file if there is one, else this slot's built-in sound
                    var local = _local[(int)kind];
                    clip = local.Count > 0
                        ? local[UnityEngine.Random.Range(0, local.Count)]
                        : kind == Kind.Hit ? _ting : kind == Kind.Hurt ? _clang : _coin;
                    break;
            }
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

        /// <summary>Decodes a PCM WAV (8/16/24/32-bit int or 32-bit float) from the mod folder.</summary>
        private static AudioClip LoadWav(string path)
        {
            try
            {
                var data = _mod?.GetFile(path);
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
