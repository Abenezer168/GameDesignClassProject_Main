using System.Collections.Generic;
using UnityEngine;

namespace DataHeist
{
    /// <summary>
    /// A person in the ending: a citizen, the mayor, or the hero. Plays frame animations
    /// (walk, idle, wave, cheer, clap, talk, present, shake, win), walks to targets and faces
    /// whoever it reacts to. Sprites: Ending/npc/&lt;id&gt;_&lt;anim&gt;_NN, or the runner's own folder.
    /// </summary>
    public class Actor : MonoBehaviour
    {
        public string Id { get; private set; }
        public bool IsHero { get; private set; }
        public float speed = 2.2f;
        public bool Talked;                              // has already reacted to the hero (walk scene)

        SpriteRenderer _sr;
        readonly Dictionary<string, Sprite[]> _anims = new Dictionary<string, Sprite[]>();
        string _anim = "idle";
        float _fps = 5f, _t, _targetX = float.NaN, _oneShotUntil;
        string _after = "idle";

        public static Actor Spawn(string id, Transform parent, float x, float y, bool hero = false, float scale = 1f, int order = 0)
        {
            var a = new GameObject(id).AddComponent<Actor>();
            a.transform.SetParent(parent, false);
            a.Id = id; a.IsHero = hero;
            a._sr = a.gameObject.AddComponent<SpriteRenderer>();
            a._sr.sortingOrder = (hero ? DH.SortPlayer : DH.SortPlayer - 4) + order;
            a.transform.position = new Vector3(x, y, 0);
            a.transform.localScale = new Vector3(scale, scale, 1);
            a._t = Random.value * 3f;
            a.Play("idle");
            return a;
        }

        Sprite[] Frames(string anim)
        {
            if (_anims.TryGetValue(anim, out var f)) return f;
            f = IsHero ? SpriteLibrary.CharacterAnim(Id, anim) : SpriteLibrary.Anim($"Ending/npc/{Id}_{anim}");
            if (f.Length == 0 && anim != "idle") f = Frames("idle");
            _anims[anim] = f;
            return f;
        }

        public void Play(string anim, float fps = -1)
        {
            _anim = anim;
            _fps = fps > 0 ? fps : anim == "walk" ? 9f : anim == "cheer" || anim == "clap" ? 6f : anim == "wave" ? 5f : 4f;
            _oneShotUntil = 0;
        }

        /// <summary>Play an animation for a while, then fall back to another.</summary>
        public void PlayFor(string anim, float seconds, string after = "idle")
        {
            Play(anim);
            _oneShotUntil = Time.time + seconds;
            _after = after;
        }

        public void Face(float x) => _sr.flipX = x < transform.position.x;
        public void FaceRight(bool right) => _sr.flipX = !right;
        public bool Walking => !float.IsNaN(_targetX);
        /// <summary>True while a one-shot (wave, cheer...) is still playing.</summary>
        public bool Busy => _oneShotUntil > Time.time;
        public float X => transform.position.x;

        public void WalkTo(float x, float walkSpeed = -1)
        {
            _targetX = x;
            if (walkSpeed > 0) speed = walkSpeed;
            Face(x);
            Play("walk");
        }

        public void Stop(string anim = "idle")
        {
            _targetX = float.NaN;
            Play(anim);
        }

        public void SetAlpha(float a) => _sr.color = new Color(_sr.color.r, _sr.color.g, _sr.color.b, a);
        public void SetTint(Color c) => _sr.color = c;
        public int Order { get => _sr.sortingOrder; set => _sr.sortingOrder = value; }

        void Update()
        {
            float dt = Time.deltaTime;
            if (!float.IsNaN(_targetX))
            {
                var p = transform.position;
                float step = speed * dt;
                if (Mathf.Abs(_targetX - p.x) <= step) { p.x = _targetX; _targetX = float.NaN; Play("idle"); }
                else p.x += Mathf.Sign(_targetX - p.x) * step;
                transform.position = p;
            }
            if (_oneShotUntil > 0 && Time.time > _oneShotUntil) { Play(_after); }
            _t += dt;
            var f = Frames(_anim);
            if (f.Length > 0) _sr.sprite = f[Mathf.FloorToInt(_t * _fps) % f.Length];
        }
    }
}
