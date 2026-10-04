using System;
using PugMod;
using UnityEngine;
using UnityEngine.Networking;

namespace StimHits
{
    /// <summary>
    /// Loads the replacement sounds and plays them.
    /// Built-in "Stim" sounds are WAVs shipped in the mod's Sounds/ folder, read through the
    /// loader's sandbox-safe <see cref="LoadedMod.GetFile"/> and decoded here. Custom sounds
    /// (hit/hurt .mp3, .ogg or .wav) live in a folder outside the mod so updates never delete them:
    /// <c>%USERPROFILE%\AppData\LocalLow\Pugstorm\Core Keeper\StimHits\</c>; Unity decodes those via
    /// a file:// UnityWebRequest. The named game sounds play through the game's AudioManager.
    /// Everything goes through the Effects mixer, so the game's SFX volume applies.
    /// </summary>
    public static class SoundBank
    {
        public enum Kind { Hit, Hurt }

        private const int Voices = 8;
        private const float MinGap = 0.045f; // seconds between two dings of the same kind
        private static readonly string[] Extensions = { "mp3", "ogg", "wav" };

        private static LoadedMod _mod;
        private static AudioClip _stimHit, _stimHurt, _customHit, _customHurt;
        private static AudioSource[] _sources;
        private static int _next;
        private static readonly float[] _lastPlayed = { -1f, -1f };

        // Custom loading: one request at a time, walking (kind, extension) candidates.
        private static UnityWebRequest _request;
        private static int _candidate = -1;
        private static bool _customHitFound, _customHurtFound, _warnedMissing;

        public static string CustomFolder => Application.persistentDataPath + "/StimHits";

        public static void Init(LoadedMod mod)
        {
            _mod = mod;
            _stimHit = LoadWav("Sounds/stim_hit.wav");
            _stimHurt = LoadWav("Sounds/stim_hurt.wav");
            ReloadCustom();
        }

        public static void ReloadCustom()
        {
            _request?.Dispose();
            _request = null;
            _customHitFound = _customHurtFound = false;
            _warnedMissing = false;
            _candidate = 0;
            StartCandidate();
        }

        /// <summary>Called every frame from <see cref="StimHitsMod.Update"/> to finish custom loads.</summary>
        public static void Update()
        {
            if (_request == null || !_request.isDone) return;
            var kind = (Kind)(_candidate / Extensions.Length);
            var alreadyFound = kind == Kind.Hit ? _customHitFound : _customHurtFound;
            if (!alreadyFound && _request.result == UnityWebRequest.Result.Success)
            {
                var clip = DownloadHandlerAudioClip.GetContent(_request);
                if (clip != null && clip.length > 0f)
                {
                    clip.name = "StimHits custom " + kind;
                    if (kind == Kind.Hit) { _customHit = clip; _customHitFound = true; }
                    else { _customHurt = clip; _customHurtFound = true; }
                    Debug.Log($"[{StimHitsMod.Name}] Custom {kind} sound loaded: {_request.url} ({clip.length:0.00}s)");
                }
            }
            _request.Dispose();
            _request = null;
            _candidate++;
            StartCandidate();
        }

        private static void StartCandidate()
        {
            var total = Extensions.Length * 2;
            while (_candidate >= 0 && _candidate < total)
            {
                var kind = (Kind)(_candidate / Extensions.Length);
                if ((kind == Kind.Hit && _customHitFound) || (kind == Kind.Hurt && _customHurtFound))
                {
                    _candidate++;
                    continue;
                }
                var ext = Extensions[_candidate % Extensions.Length];
                var type = ext == "mp3" ? AudioType.MPEG : ext == "ogg" ? AudioType.OGGVORBIS : AudioType.WAV;
                var url = "file:///" + CustomFolder.Replace('\\', '/') + "/" + (kind == Kind.Hit ? "hit." : "hurt.") + ext;
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
                    _candidate++;
                }
            }
            _candidate = -1;
        }

        /// <summary>Plays the configured replacement for <paramref name="kind"/> (nothing when the setting is Off).</summary>
        public static void Play(Kind kind, Transform at)
        {
            var hit = kind == Kind.Hit;
            var choice = (hit ? StimHitsMod.HitSound : StimHitsMod.HurtSound)?.Value ?? "Off";
            if (choice == "Off") return;
            var now = Time.unscaledTime;
            if (now - _lastPlayed[(int)kind] < MinGap) return;
            _lastPlayed[(int)kind] = now;

            var volume = Mathf.Clamp01((hit ? StimHitsMod.HitVolume : StimHitsMod.HurtVolume)?.Value ?? 0.8f);
            var pitch = (hit ? StimHitsMod.HitPitch : StimHitsMod.HurtPitch)?.Value ?? 1f;
            pitch *= UnityEngine.Random.Range(0.97f, 1.03f); // a little variation so repeats don't drone

            if (TryGameSound(choice, out var sfx))
            {
                AudioManager.SfxFollowTransform(sfx, at, volume, pitch, 0f, reuse: false, AudioManager.MixerGroupEnum.EFFECTS,
                    ignoreAudioIfOutsideOfViewport: false, useSpatialSound: false, playOnGamepad: !hit);
                return;
            }

            AudioClip clip = null;
            if (choice == "Custom")
            {
                clip = hit ? _customHit : _customHurt;
                if (clip == null && !_warnedMissing && _candidate < 0)
                {
                    _warnedMissing = true;
                    Debug.LogWarning($"[{StimHitsMod.Name}] No custom {kind} sound in {CustomFolder} (hit/hurt .mp3/.ogg/.wav); using Stim.");
                }
            }
            if (clip == null) clip = hit ? _stimHit : _stimHurt;
            if (clip != null) PlayClip(clip, volume, pitch);
        }

        private static bool TryGameSound(string choice, out SfxID sfx)
        {
            switch (choice)
            {
                case "Clang": sfx = SfxID.metalImpact; return true;
                case "Small Clang": sfx = SfxID.metalImpactSmall; return true;
                case "Ding": sfx = SfxID.inventory_ding; return true;
                case "Anvil": sfx = SfxID.anvil; return true;
                case "Bell": sfx = SfxID.Bell; return true;
                case "Shield": sfx = SfxID.shieldBlock; return true;
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
