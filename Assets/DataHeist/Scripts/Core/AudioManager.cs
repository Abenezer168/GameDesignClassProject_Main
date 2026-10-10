using System.Collections.Generic;
using UnityEngine;

namespace DataHeist
{
    /// <summary>
    /// Persistent audio service: pooled one-shot SFX + one looping music track.
    /// Clips are loaded lazily from Resources/DataHeist/Audio.
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
            if (_music != null)
                _music.pitch = Mathf.MoveTowards(_music.pitch, _targetPitch, Time.unscaledDeltaTime * 2f);
        }

        public void ApplyVolumes()
        {
            if (_music != null) _music.volume = SaveSystem.Data.musicVolume * 0.55f * _musicDuck;
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

        public void Music(string name, bool restart = false)
        {
            if (_musicName == name && !restart && _music.isPlaying) return;
            var clip = Clip(DH.MusicPath + name);
            if (clip == null) return;
            _musicName = name;
            _music.clip = clip;
            _music.pitch = _targetPitch = 1f;
            _music.Play();
        }

        public void MusicPitch(float p) => _targetPitch = p;

        public void MusicDuck(bool duck)
        {
            _musicDuck = duck ? 0.35f : 1f;
            ApplyVolumes();
        }
    }
}
