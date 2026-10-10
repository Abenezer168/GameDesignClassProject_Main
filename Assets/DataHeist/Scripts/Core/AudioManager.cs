using System.Collections.Generic;
using UnityEngine;

namespace DataHeist
{
    /// <summary>
    /// Persistent audio service: pooled one-shot SFX + one looping music track.
    /// Music changes crossfade (the old track fades out before the new one fades in), so two
    /// tracks are never heard at once. Clips are loaded lazily from Resources/DataHeist/Audio.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        static AudioManager _instance;
        public static AudioManager I
        {
            get
            {
                if (_instance != null) return _instance;
                var go = new GameObject("[DataHeist Audio]");
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<AudioManager>();
                return _instance;
            }
        }

        readonly Dictionary<string, AudioClip> _clips = new Dictionary<string, AudioClip>();
        readonly List<AudioSource> _pool = new List<AudioSource>();
        AudioSource _music;
        string _musicName;
        float _musicDuck = 1f;
        float _targetPitch = 1f;
        // crossfade state: _fade is the current gain of the music source (0..1)
        float _fade = 1f;
        AudioClip _pending;
        string _pendingName;
        bool _stopping;
        const float FadeOut = 0.35f, FadeIn = 0.6f;

        void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;
            _music = gameObject.AddComponent<AudioSource>();
            _music.loop = true;
            _music.playOnAwake = false;
            for (int i = 0; i < 12; i++)
            {
                var s = gameObject.AddComponent<AudioSource>();
                s.playOnAwake = false;
                _pool.Add(s);
            }
            ApplyVolumes();
        }

        void Update()
        {
            if (_music == null) return;
            float dt = Time.unscaledDeltaTime;
            _music.pitch = Mathf.MoveTowards(_music.pitch, _targetPitch, dt * 2f);
            if (_pending != null || _stopping)
            {
                _fade = _music.isPlaying ? _fade - dt / FadeOut : 0f;
                if (_fade <= 0f)
                {
                    _fade = 0f;
                    if (_stopping) { _music.Stop(); _musicName = null; _stopping = false; }
                    else
                    {
                        _music.clip = _pending;
                        _musicName = _pendingName;
                        _music.pitch = _targetPitch = 1f;
                        _music.Play();
                        _pending = null;
                    }
                }
            }
            else if (_fade < 1f) _fade = Mathf.Min(1f, _fade + dt / FadeIn);
            ApplyVolumes();
        }

        public void ApplyVolumes()
        {
            if (_music != null) _music.volume = SaveSystem.Data.musicVolume * 0.55f * _musicDuck * _fade;
        }

        AudioClip Clip(string path, bool warnIfMissing = true)
        {
            if (_clips.TryGetValue(path, out var c)) return c;
            c = Resources.Load<AudioClip>(path);
            if (c == null && warnIfMissing) Debug.LogWarning("[DataHeist] Missing audio Resources/" + path);
            _clips[path] = c;
            return c;
        }

        /// <summary>Plays a music track that may not exist yet, falling back to another one.</summary>
        public void MusicOr(string name, string fallback, bool restart = false)
        {
            Music(Clip(DH.MusicPath + name, false) != null ? name : fallback, restart);
        }

        public void Sfx(string name, float volume = 1f, float pitch = 1f)
        {
            var clip = Clip(DH.SfxPath + name);
            if (clip == null) return;
            AudioSource src = null;
            foreach (var s in _pool) if (!s.isPlaying) { src = s; break; }
            if (src == null) src = _pool[Random.Range(0, _pool.Count)];
            src.pitch = pitch;
            src.volume = volume * SaveSystem.Data.sfxVolume * 0.8f;
            src.clip = clip;
            src.Play();
        }

        /// <summary>Crossfades to a looping track. Asking for the track that is already playing does nothing unless restart is set.</summary>
        public void Music(string name, bool restart = false)
        {
            _stopping = false;
            if (!restart && _pending == null && _musicName == name && _music.isPlaying) return;
            if (!restart && _pendingName == name && _pending != null) return;
            var clip = Clip(DH.MusicPath + name);
            if (clip == null) return;
            _pending = clip;
            _pendingName = name;
        }

        /// <summary>Fades the current track out and stops it.</summary>
        public void StopMusic()
        {
            _pending = null; _pendingName = null;
            _stopping = _music.isPlaying;
        }

        public string CurrentMusic => _pending != null ? _pendingName : _music != null && _music.isPlaying ? _musicName : null;

        public void MusicPitch(float p) => _targetPitch = p;

        public void MusicDuck(bool duck)
        {
            _musicDuck = duck ? 0.35f : 1f;
            ApplyVolumes();
        }
    }
}
