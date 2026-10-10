using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DataHeist
{
    /// <summary>
    /// Fade-to-black scene transitions. SceneFader.Load(name) fades out, loads, then fades the
    /// new scene in. Lives on a persistent overlay canvas above every screen.
    /// </summary>
    public class SceneFader : MonoBehaviour
    {
        static SceneFader _instance;
        Image _black;
        bool _busy;

        static SceneFader I
        {
            get
            {
                if (_instance != null) return _instance;
                var canvas = UIKit.Canvas("[DataHeist Fader]", 1000);
                DontDestroyOnLoad(canvas.gameObject);
                _instance = canvas.gameObject.AddComponent<SceneFader>();
                _instance._black = UIKit.Image("Black", canvas.transform, DH.Night);
                _instance._black.rectTransform.Stretch(-4);
                _instance._black.raycastTarget = true;
                _instance.SetAlpha(0);
                return _instance;
            }
        }

        /// <summary>True when the scene is in Build Settings (logs how to fix it when not).</summary>
        public static bool CanLoad(string scene)
        {
            if (Application.CanStreamedLevelBeLoaded(scene)) return true;
            Debug.LogWarning($"[DataHeist] Scene '{scene}' is not in Build Settings. Run Data Heist > Setup Project.");
            return false;
        }

        public static void Load(string scene, float fadeOut = 0.35f, float fadeIn = 0.45f)
        {
            if (!CanLoad(scene)) return;
            var f = I;
            if (f._busy) return;
            f.StartCoroutine(f.Run(scene, fadeOut, fadeIn));
        }

        /// <summary>Fades in from black; call from a screen's Start for a soft entrance.</summary>
        public static void FadeIn(float seconds = 0.5f)
        {
            var f = I;
            if (f._busy) return;
            f.StopAllCoroutines();
            f.StartCoroutine(f.Fade(1, 0, seconds));
        }

        IEnumerator Run(string scene, float fadeOut, float fadeIn)
        {
            _busy = true;
            yield return Fade(_black.color.a, 1, fadeOut);
            var op = SceneManager.LoadSceneAsync(scene);
            while (!op.isDone) yield return null;
            Time.timeScale = 1f;
            yield return null;
            _busy = false;
            yield return Fade(1, 0, fadeIn);
        }

        IEnumerator Fade(float from, float to, float seconds)
        {
            _black.raycastTarget = true;
            for (float t = 0; t < seconds; t += Time.unscaledDeltaTime)
            {
                SetAlpha(Mathf.Lerp(from, to, Mathf.SmoothStep(0, 1, t / seconds)));
                yield return null;
            }
            SetAlpha(to);
            _black.raycastTarget = to > 0.5f;
        }

        void SetAlpha(float a)
        {
            var c = _black.color; c.a = a; _black.color = c;
        }
    }
}
