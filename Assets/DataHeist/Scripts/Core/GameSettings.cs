using System.Collections.Generic;
using UnityEngine;

namespace DataHeist
{
    /// <summary>
    /// Applies the player's saved audio and display settings. Values live on SaveData;
    /// they are applied once at boot and again whenever the Settings screen changes them.
    /// </summary>
    public static class GameSettings
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void ApplyOnBoot()
        {
            ApplyAudio();
            if (!Application.isEditor) ApplyDisplay();   // don't resize the editor's Game view
            else ApplyVsync();
        }

        public static void ApplyAudio()
        {
            AudioListener.volume = SaveSystem.Data.masterVolume;
            AudioManager.I.ApplyVolumes();
        }

        public static void ApplyDisplay()
        {
            var d = SaveSystem.Data;
            int w = d.resolutionWidth > 0 ? d.resolutionWidth : Screen.currentResolution.width;
            int h = d.resolutionHeight > 0 ? d.resolutionHeight : Screen.currentResolution.height;
            var mode = d.fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
            Screen.SetResolution(w, h, mode);
            ApplyVsync();
        }

        static void ApplyVsync()
        {
            QualitySettings.vSyncCount = SaveSystem.Data.vsync ? 1 : 0;
        }

        /// <summary>Distinct 16:9-friendly resolutions the display supports, smallest first.</summary>
        public static List<Vector2Int> Resolutions()
        {
            var list = new List<Vector2Int>();
            foreach (var r in Screen.resolutions)
            {
                var v = new Vector2Int(r.width, r.height);
                if (v.x < 640 || list.Contains(v)) continue;
                list.Add(v);
            }
            if (list.Count == 0) list.Add(new Vector2Int(Screen.width, Screen.height));
            list.Sort((a, b) => (a.x * a.y).CompareTo(b.x * b.y));
            return list;
        }

        public static Vector2Int CurrentResolution()
        {
            var d = SaveSystem.Data;
            return d.resolutionWidth > 0
                ? new Vector2Int(d.resolutionWidth, d.resolutionHeight)
                : new Vector2Int(Screen.width, Screen.height);
        }
    }
}
