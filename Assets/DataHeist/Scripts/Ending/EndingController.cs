using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DataHeist
{
    /// <summary>
    /// The ending (EndingScene), played after Level 3's escape and replayable from the completion
    /// screen and the home page. Six chapters, each staged at its own x along one world under the
    /// shared night-city parallax:
    ///   1 The Truth Is Revealed  2 The City Transforms  3 The Hero Returns (playable walk)
    ///   4 The Award Ceremony      5 The Statue           6 The City Is Free (fireworks)
    /// then the completion screen. ESC / SKIP SCENE skips the current chapter.
    /// Built entirely at runtime, like every Data Heist scene; chapters live in EndingChapters.cs.
    /// </summary>
    public partial class EndingController : MonoBehaviour
    {
        /// <summary>Test hooks: start at a chapter, and let the playable walk drive itself.</summary>
        public static int StartChapter;
        public static bool AutoWalk;

        public const int Chapters = 6;
        public int Chapter { get; private set; } = -1;
        public bool Finished { get; private set; }

        const float GroundCamY = 2.25f;             // ground at y = 0 sits just above the caption bar

        Transform _cam;
        Parallax _parallax;
        CinematicUI _ui;
        Fireworks _fireworks;
        Confetti _confetti;
        Transform _stage;                           // the current chapter's set
        string _heroId;
        CharacterDefinition _heroDef;
        bool _skip;
        Vector2 _camPos;

        void Start()
        {
            _heroId = GameSession.SelectedCharacter;
            _heroDef = CharacterDefinition.Get(_heroId);
            var camGo = Camera.main != null ? Camera.main.gameObject : new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            if (!camGo.TryGetComponent<Camera>(out var cam)) cam = camGo.AddComponent<Camera>();
            cam.orthographic = true; cam.orthographicSize = DH.OrthoSize;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = DH.Night;
            cam.nearClipPlane = -50; cam.farClipPlane = 50;
            if (camGo.GetComponent<AudioListener>() == null) camGo.AddComponent<AudioListener>();
            _cam = camGo.transform;
            _parallax = Parallax.Create(_cam, 0);
            _ui = CinematicUI.Create();
            _ui.OnSkip += () => _skip = true;
            ScreenFX.Add(60, 0.6f, 0.06f);
            _fireworks = Fireworks.Create(transform, new Rect(0, 0, 1, 1));
            _confetti = Confetti.Create(transform);
            SceneFader.FadeIn(0.8f);
            StartCoroutine(Run());
        }

        IEnumerator Run()
        {
            Func<IEnumerator>[] chapters = { TruthRevealed, CityTransforms, HeroReturns, AwardCeremony, StatueReveal, CityIsFree };
            int first = Mathf.Clamp(StartChapter, 0, Chapters - 1);
            StartChapter = 0;
            for (int i = first; i < chapters.Length; i++)
            {
                Chapter = i;
                _skip = false;
                yield return Skippable(chapters[i]());
                _ui.ClearCaption();
                _fireworks.Active = false;
                _confetti.Active = false;
                if (i < chapters.Length - 1) yield return _ui.Fade(1f, 0.5f);
                // the ceremony's square stays standing for the statue reveal
                if (i != 3 && _stage != null) { Destroy(_stage.gameObject); _stage = null; }
                _ui.Interactive = false;
            }
            Finished = true;
            SaveSystem.Data.endingSeen = true;
            SaveSystem.Save();
            SceneFader.Load(DH.SceneCompletion, 1.2f, 0.8f);
        }

        /// <summary>Runs a chapter (and every coroutine it yields) so that SKIP can cut it anywhere.</summary>
        IEnumerator Skippable(IEnumerator root)
        {
            var stack = new Stack<IEnumerator>();
            stack.Push(root);
            while (stack.Count > 0)
            {
                if (_skip) { AudioManager.I.Sfx("ui_back"); yield break; }
                var top = stack.Peek();
                if (!top.MoveNext()) { stack.Pop(); continue; }
                if (top.Current is IEnumerator sub) { stack.Push(sub); continue; }
                yield return top.Current;
            }
        }

        // ------------------------------------------------------------------ camera
        void SetCam(float x, float y)
        {
            _camPos = new Vector2(x, y);
            _cam.position = new Vector3(SR.Snap(x), SR.Snap(y), -10);
        }

        IEnumerator Pan(float x, float y, float seconds)
        {
            Vector2 from = _camPos, to = new Vector2(x, y);
            for (float t = 0; t < seconds; t += Time.deltaTime)
            {
                var p = Vector2.Lerp(from, to, Mathf.SmoothStep(0, 1, t / seconds));
                SetCam(p.x, p.y);
                yield return null;
            }
            SetCam(x, y);
        }

        /// <summary>Starts a pan without waiting for it (the scene keeps playing underneath).</summary>
        void PanAsync(float x, float y, float seconds) => StartCoroutine(Pan(x, y, seconds));

        static IEnumerator Wait(float seconds)
        {
            for (float t = 0; t < seconds; t += Time.deltaTime) yield return null;
        }

        // ------------------------------------------------------------------ staging helpers
        Transform NewStage(string name)
        {
            if (_stage != null) Destroy(_stage.gameObject);
            _stage = new GameObject("Set_" + name).transform;
            _stage.SetParent(transform, false);
            return _stage;
        }

        SpriteRenderer Prop(string path, float x, float y, int order, float scale = 1f)
        {
            var sr = SR.Make(path, _stage, SpriteLibrary.Get(path), order, new Vector3(SR.Snap(x), SR.Snap(y), 0));
            sr.transform.localScale = new Vector3(scale, scale, 1);
            return sr;
        }

        SpriteRenderer AnimProp(string basePath, float x, float y, int order, float fps)
        {
            var frames = SpriteLibrary.Anim(basePath);
            var sr = SR.Make(basePath, _stage, frames.Length > 0 ? frames[0] : null, order, new Vector3(SR.Snap(x), SR.Snap(y), 0));
            if (frames.Length > 1) sr.gameObject.AddComponent<SpriteAnimator>().Play(frames, fps);
            return sr;
        }

        void Rooftop(float x0, float width)
        {
            SR.Tiled("Ledge", _stage, SpriteLibrary.Get("Tiles/rooftop_violet"), DH.SortRoofTop, new Vector3(x0, 0.06f, 0), new Vector2(width, 0.5f));
            SR.Tiled("Facade", _stage, SpriteLibrary.Get("Tiles/facade_violet_0"), DH.SortFacade, new Vector3(x0, -0.44f, 0), new Vector2(width, 8));
        }

        void Street(float x0, float width)
        {
            SR.Tiled("Sidewalk", _stage, SpriteLibrary.Get("Tiles/rooftop_street"), DH.SortRoofTop, new Vector3(x0, 0.06f, 0), new Vector2(width, 0.5f));
            SR.Tiled("Road", _stage, SpriteLibrary.Get("Ending/road"), DH.SortFacade, new Vector3(x0 + width / 2f, -7.44f, 0), new Vector2(width, 7f));
        }

        Actor Npc(string id, float x, bool faceRight = true, float y = 0)
        {
            bool kid = id.StartsWith("k");
            var a = Actor.Spawn(id, _stage, x, y, false, kid ? 0.75f : 1f, kid ? 1 : 0);
            a.FaceRight(faceRight);
            return a;
        }

        Actor Hero(float x, float y = 0) => Actor.Spawn(_heroId, _stage, x, y, true);

        SpriteRenderer Drone(float x, float y, bool beam)
        {
            var d = AnimProp("Enemies/drone_scout", x, y, DH.SortEnemies, 12);
            if (beam)
            {
                var b = SR.Make("Beam", d.transform, SpriteLibrary.Get("Story/searchlight"), DH.SortEnemies - 1, new Vector3(0, -0.15f, 0));
                b.color = new Color(DH.Red.r, DH.Red.g, DH.Red.b, 0.3f);
            }
            return d;
        }

        /// <summary>A dark mid-distance building, optionally carrying a giant screen on its face.</summary>
        SpriteRenderer Building(float x, float w, float h, bool screen)
        {
            var body = SR.Box("Building", _stage, DH.Hex("120c22"), DH.SortNear + 3, x - w / 2f, -1f, w, h + 1f);
            var rr = new System.Random((int)(x * 10));
            for (float wy = 0.5f; wy < h - 0.6f; wy += 0.6f)
                for (float wx = x - w / 2f + 0.3f; wx < x + w / 2f - 0.3f; wx += 0.5f)
                    if (rr.NextDouble() < 0.3)
                        SR.Box("Window", _stage, new Color(1f, 0.85f, 0.55f, 0.35f), DH.SortNear + 4, wx, wy, 0.18f, 0.25f);
            if (!screen) return body;
            return SR.Make("Screen", _stage, SpriteLibrary.Get("Ending/screen_off"), DH.SortNear + 5, new Vector3(SR.Snap(x), SR.Snap(h - 2.4f), 0));
        }

        // ------------------------------------------------------------------ moods
        static readonly Color OppressedSky = new Color(0.62f, 0.32f, 0.42f), OppressedCity = new Color(0.62f, 0.4f, 0.52f);
        static readonly Color FreeSky = new Color(1.0f, 0.95f, 1.0f), FreeCity = Color.white;

        IEnumerator Retint(Color skyFrom, Color cityFrom, Color skyTo, Color cityTo, float seconds)
        {
            for (float t = 0; t < seconds; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0, 1, t / seconds);
                _parallax.SetTint(Color.Lerp(skyFrom, skyTo, k), Color.Lerp(cityFrom, cityTo, k));
                yield return null;
            }
            _parallax.SetTint(skyTo, cityTo);
        }

        void Music(string track) => AudioManager.I.Music(track);

        Sprite MayorPortrait => SpriteLibrary.Get("Ending/npc/mayor_portrait");
        string HeroShort => _heroDef.displayName.Split(' ')[0];
    }
}
