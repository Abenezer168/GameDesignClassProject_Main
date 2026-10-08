using System.Collections.Generic;
using UnityEngine;

namespace DataHeist
{
    /// <summary>
    /// Loads and caches the generated pixel art from Resources/DataHeist.
    /// Sprites are addressed by folder + file name, e.g. Get("Obstacles/garbage_can").
    /// Animations are numbered files: kai_run_00..07 -> Anim("Characters/kai/kai_run").
    /// </summary>
    public static class SpriteLibrary
    {
        static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();
        static readonly Dictionary<string, Sprite[]> AnimCache = new Dictionary<string, Sprite[]>();

        public static Sprite Get(string path)
        {
            if (Cache.TryGetValue(path, out var s)) return s;
            s = Resources.Load<Sprite>(DH.Root + path);
            if (s == null)
            {
                // Texture imported as Default instead of Sprite? build one on the fly so the game still runs.
                var tex = Resources.Load<Texture2D>(DH.Root + path);
                if (tex != null)
                {
                    tex.filterMode = FilterMode.Point;
                    s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), PivotFor(path), DH.PPU, 0, SpriteMeshType.FullRect);
                    Debug.LogWarning($"[DataHeist] {path} is not imported as a Sprite. Run Data Heist > Reimport Art.");
                }
                else Debug.LogWarning($"[DataHeist] Missing sprite Resources/{DH.Root}{path}");
            }
            Cache[path] = s;
            return s;
        }

        /// <summary>Loads base_00, base_01, ... until a frame is missing.</summary>
        public static Sprite[] Anim(string basePath)
        {
            if (AnimCache.TryGetValue(basePath, out var a)) return a;
            var list = new List<Sprite>();
            for (int i = 0; i < 64; i++)
            {
                var path = $"{basePath}_{i:00}";
                if (Resources.Load<Texture2D>(DH.Root + path) == null) break;
                list.Add(Get(path));
            }
            a = list.ToArray();
            AnimCache[basePath] = a;
            return a;
        }

        public static Sprite[] CharacterAnim(string charId, string anim) => Anim($"Characters/{charId}/{charId}_{anim}");
        public static Sprite Portrait(string charId) => Get($"Characters/{charId}/{charId}_portrait");

        /// <summary>Pivot convention shared with the editor importer (DataHeistArtImporter).</summary>
        public static Vector2 PivotFor(string path)
        {
            if (path.StartsWith("Characters") || path.StartsWith("Obstacles") || path.StartsWith("Decor"))
                return new Vector2(0.5f, 0f);
            if (path.StartsWith("Tiles")) return new Vector2(0f, 1f);
            if (path.StartsWith("Parallax")) return new Vector2(0f, 0f);
            return new Vector2(0.5f, 0.5f);
        }
    }
}
