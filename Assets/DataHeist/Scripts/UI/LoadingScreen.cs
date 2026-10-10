using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DataHeist
{
    /// <summary>
    /// Full-screen loading screen: the equipped runner sprints along the progress bar while real
    /// work happens (asset preloading, then an async scene load held until ready), with status
    /// lines and gameplay tips. Shown for at least MinSeconds so it never just flickers.
    /// Usage: LoadingScreen.Run(sceneName, GamePreloader.Steps(...)) or with scene = null and
    /// an onReady callback that builds the next screen in place.
    /// </summary>
    public class LoadingScreen : MonoBehaviour
    {
        public const float MinSeconds = 2.2f;

        static readonly string[] Tips =
        {
            "Hold JUMP for a higher jump. Tap it for a short hop.",
            "Slide under neon billboards. Hold SLIDE to stay low longer.",
            "Land on a drone to stomp it, or slash it out of the air.",
            "Gunner bolts fly at chest height: slide, jump or slash them.",
            "Checkpoints save your run. Data Bytes you collect are always banked.",
            "Spend Data Bytes in the SHOP to recruit new runners.",
            "Every runner has a unique ability on W. Learn the cooldowns.",
            "Rank S needs 85% of the bytes, no hits and no retries.",
        };

        /// <summary>True while a loading screen is on screen.</summary>
        public static bool Active { get; private set; }

        CanvasGroup _group;
        RectTransform _fill, _runner, _bar;
        TextMeshProUGUI _status, _percent, _tip;
        SpriteAnimator _runAnim;
        float _shown, _target, _tipT;
        int _tipIndex;
        string _sceneLabel;

        /// <param name="sceneLabel">What the player reads while the scene itself loads, e.g. "DROPPING INTO NEON ROOFTOPS".</param>
        public static void Run(string scene, IEnumerable<(string status, Action work)> steps, Action onReady = null, string sceneLabel = null)
        {
            if (Active) return;
            if (scene != null && !SceneFader.CanLoad(scene)) return;
            var canvas = UIKit.Canvas("[DataHeist Loading]", 900);
            DontDestroyOnLoad(canvas.gameObject);
            var ls = canvas.gameObject.AddComponent<LoadingScreen>();
            ls.Build((RectTransform)canvas.transform);
            ls._sceneLabel = sceneLabel ?? "ENTERING";
            ls.StartCoroutine(ls.Work(scene, steps, onReady));
        }

        void Build(RectTransform root)
        {
            Active = true;
            _group = gameObject.AddComponent<CanvasGroup>();
            _group.alpha = 0;
            _group.blocksRaycasts = true;

            var bg = UIKit.Image("Bg", root, DH.Night);
            bg.rectTransform.Stretch(-4);
            bg.raycastTarget = true;
            var art = UIKit.Image("KeyArt", root, new Color(0.55f, 0.5f, 0.75f, 0.28f), SpriteLibrary.Get("Story/keyart"));
            art.rectTransform.Stretch();

            var brand = UIKit.Text("Brand", root, "THE DATA HEIST", 6, DH.Dim);
            brand.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -18), new Vector2(200, 10));

            // tip card
            var card = UIKit.Panel("Tip", root, DH.Cyan);
            card.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 30), new Vector2(380, 64));
            var tipHead = UIKit.Text("TipHead", card.rectTransform, "RUNNER TIP", 6, DH.Pink);
            tipHead.characterSpacing = 20;
            tipHead.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(16, -12), new Vector2(200, 10));
            _tip = UIKit.Text("TipText", card.rectTransform, "", 7, DH.Ink);
            _tip.lineSpacing = 30;
            _tip.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(16, -28), new Vector2(348, 30));
            _tipIndex = UnityEngine.Random.Range(0, Tips.Length);
            _tip.text = Tips[_tipIndex];

            // progress bar with the runner sprinting along it
            _bar = UIKit.Rect("Bar", root);
            _bar.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 44), new Vector2(520, 4));
            UIKit.Image("Track", _bar, DH.PurpleLine).rectTransform.Stretch();
            var fill = UIKit.Image("Fill", _bar, DH.Pink);
            _fill = fill.rectTransform;
            _fill.anchorMin = new Vector2(0, 0); _fill.anchorMax = new Vector2(0, 1); _fill.pivot = new Vector2(0, 0.5f);
            _fill.sizeDelta = new Vector2(0, 0);
            var head = UIKit.Image("Head", _fill, DH.Cyan);
            head.rectTransform.anchorMin = head.rectTransform.anchorMax = new Vector2(1, 0.5f);
            head.rectTransform.sizeDelta = new Vector2(3, 8);

            var runner = UIKit.Image("Runner", _bar, Color.white);
            _runner = runner.rectTransform;
            _runner.anchorMin = _runner.anchorMax = new Vector2(0, 1);
            _runner.pivot = new Vector2(0.5f, 0);
            _runner.sizeDelta = new Vector2(48, 48);
            _runAnim = runner.gameObject.AddComponent<SpriteAnimator>();
            _runAnim.Play(SpriteLibrary.CharacterAnim(GameSession.SelectedCharacter, "run"), 14);

            _status = UIKit.Text("Status", root, "", 6, DH.Cyan);
            _status.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-60, 24), new Vector2(400, 10));
            _percent = UIKit.Text("Percent", root, "0%", 7, DH.Ink, TextAlignmentOptions.Right);
            _percent.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(220, 24), new Vector2(80, 10));
        }

        IEnumerator Work(string scene, IEnumerable<(string status, Action work)> steps, Action onReady)
        {
            float started = Time.unscaledTime;
            for (float t = 0; t < 0.25f; t += Time.unscaledDeltaTime) { _group.alpha = t / 0.25f; yield return null; }
            _group.alpha = 1;

            // 1. preload (real work, one step per frame so the bar moves)
            var list = new List<(string status, Action work)>(steps ?? Array.Empty<(string, Action)>());
            float preloadShare = scene != null ? 0.6f : 0.9f;
            for (int i = 0; i < list.Count; i++)
            {
                _status.text = list[i].status + "...";
                yield return null;
                try { list[i].work?.Invoke(); }
                catch (Exception e) { Debug.LogException(e); }
                _target = preloadShare * (i + 1) / list.Count;
            }

            // 2. the next scene loads in the background and activates only once we are ready
            if (scene != null)
            {
                _status.text = _sceneLabel + "...";
                var op = SceneManager.LoadSceneAsync(scene);
                op.allowSceneActivation = false;
                while (op.progress < 0.9f)
                {
                    _target = preloadShare + (1 - preloadShare) * (op.progress / 0.9f) * 0.95f;
                    yield return null;
                }
                _target = 0.97f;
                while (Time.unscaledTime - started < MinSeconds) yield return null;
                _target = 1f;
                while (_shown < 0.999f) yield return null;
                op.allowSceneActivation = true;
                while (!op.isDone) yield return null;
                Time.timeScale = 1f;
            }
            else
            {
                _target = 1f;
                while (Time.unscaledTime - started < MinSeconds || _shown < 0.999f) yield return null;
            }

            _status.text = "READY";
            yield return new WaitForSecondsRealtime(0.15f);
            for (float t = 0; t < 0.4f; t += Time.unscaledDeltaTime) { _group.alpha = 1 - t / 0.4f; yield return null; }
            Active = false;
            onReady?.Invoke();   // e.g. start the menu music once the screen is visible
            Destroy(gameObject);
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            // the bar eases toward the real progress (never past it)
            _shown = Mathf.MoveTowards(_shown, _target, dt * 1.4f);
            float w = _bar.rect.width;
            _fill.sizeDelta = new Vector2(w * _shown, 0);
            _runner.anchoredPosition = new Vector2(Mathf.Round(w * _shown), 1);
            _percent.text = Mathf.RoundToInt(_shown * 100) + "%";

            _tipT += dt;
            if (_tipT > 3.5f)
            {
                _tipT = 0;
                _tipIndex = (_tipIndex + 1) % Tips.Length;
                _tip.text = Tips[_tipIndex];
            }
        }

        void OnDestroy() => Active = false;
    }

    /// <summary>
    /// Real preloading work for the loading screen: warms the sprite and audio caches and
    /// parses level data, so the next screen starts without hitches.
    /// </summary>
    public static class GamePreloader
    {
        static readonly string[] Anims = { "idle", "run", "jump", "fall", "slide", "attack", "hurt", "ability", "win" };

        public static IEnumerable<(string status, Action work)> Steps(bool story, string levelId = "level01", string onlyRunner = null)
        {
            var runners = new List<CharacterDefinition>();
            foreach (var c in CharacterDefinition.All)
                if (onlyRunner == null || c.id == onlyRunner) runners.Add(c);
            foreach (var c in runners)
            {
                var id = c.id;
                yield return ($"SYNCING RUNNER  {c.displayName}", () =>
                {
                    foreach (var a in Anims) SpriteLibrary.CharacterAnim(id, a);
                    SpriteLibrary.Portrait(id);
                });
            }
            yield return (levelId == "level01" ? "DECRYPTING SECTOR 7 MAP" : "DECRYPTING SECTOR 9 MAP", () => LevelData.Load(levelId));
            yield return ("RENDERING ROOFTOPS", () =>
            {
                foreach (var s in new[] { "violet", "teal", "brick" })
                {
                    SpriteLibrary.Get($"Tiles/rooftop_{s}");
                    for (int i = 0; i < 3; i++) SpriteLibrary.Get($"Tiles/facade_{s}_{i}");
                }
                foreach (var p in new[] { "sky", "city_far", "city_mid", "city_near" }) SpriteLibrary.Get("Parallax/" + p);
                foreach (var d in new[] { "ac_unit", "antenna", "water_tank", "vent", "sign_pink", "sign_cyan" }) SpriteLibrary.Get("Decor/" + d);
            });
            yield return ("ARMING SECURITY DRONES", () =>
            {
                SpriteLibrary.Anim("Enemies/drone_scout");
                SpriteLibrary.Anim("Enemies/drone_gunner");
                SpriteLibrary.Anim("Enemies/bolt");
                SpriteLibrary.Anim("Obstacles/laser_beam");
                SpriteLibrary.Anim("Pickups/byte");
                SpriteLibrary.Anim("Pickups/shard");
                SpriteLibrary.Anim("FX/explosion");
                SpriteLibrary.Anim("FX/slash");
                if (levelId != "level01")
                {
                    SpriteLibrary.Anim("Enemies/sniper_window");
                    SpriteLibrary.Anim("Enemies/sniper_bolt");
                    SpriteLibrary.Anim("Obstacles/shock_on");
                    SpriteLibrary.Get("Obstacles/shock_off");
                    SpriteLibrary.Get("Obstacles/sign_hang");
                    SpriteLibrary.Get("Obstacles/sign_fallen");
                    foreach (var v in new[] { "0", "1", "2" }) SpriteLibrary.Get("Tiles/facade_glass_" + v);
                    SpriteLibrary.Get("Tiles/rooftop_glass");
                }
            });
            yield return ("TUNING THE SYNTHWAVE", () => AudioManager.I.PreloadSfx());
            if (story)
                yield return ("RESTORING ARCHIVE FOOTAGE", () =>
                {
                    foreach (var s in new[] { "tower", "vault", "searchlight", "holo_frame" }) SpriteLibrary.Get("Story/" + s);
                });
        }
    }
}
