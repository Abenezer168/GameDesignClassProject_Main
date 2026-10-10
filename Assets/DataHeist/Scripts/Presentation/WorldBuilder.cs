using UnityEngine;

namespace DataHeist
{
    /// <summary>Builds the static scenery of a level (rooftops, catwalks, decor, extraction) from LevelData.</summary>
    public static class WorldBuilder
    {
        public static Transform Build(LevelData L, RunnerSim sim)
        {
            var root = new GameObject("World").transform;

            var roofs = new GameObject("Rooftops").transform;
            roofs.SetParent(root, false);
            for (int i = 0; i < L.roofs.Length; i++) BuildRoof(L.roofs[i], i, roofs);

            var cat = new GameObject("Catwalks").transform;
            cat.SetParent(root, false);
            foreach (var p in L.platforms) BuildCatwalk(p, cat);

            var decor = new GameObject("Decor").transform;
            decor.SetParent(root, false);
            foreach (var d in L.decor)
            {
                // animated decor (screens, beacons, server LEDs) ships as numbered frames
                var frames = SpriteLibrary.Anim("Decor/" + d.k);
                var sr = SR.Make(d.k, decor, frames.Length > 0 ? frames[0] : SpriteLibrary.Get("Decor/" + d.k), DH.SortDecor, new Vector3(d.x, d.y - 1f / DH.PPU, 0));
                if (frames.Length > 1)
                {
                    var anim = sr.gameObject.AddComponent<SpriteAnimator>();
                    anim.timeOffset = d.x * 0.37f;
                    anim.unscaled = false;
                    anim.Play(frames, d.k == "beacon" ? 8f : d.k == "screen" ? 3f : 1.5f);
                }
                if (d.k.StartsWith("sign")) sr.sortingOrder = DH.SortDecor + 1;
            }

            // extraction point: tower behind, landing pad in front
            float gy = sim.GroundAt(L.exitX);
            var tower = SR.Make("ExtractionTower", root, null, DH.SortDecor + 2, new Vector3(L.exitX + 3.5f, gy - 1f / DH.PPU, 0));
            tower.gameObject.AddComponent<SpriteAnimator>().Play(SpriteLibrary.Anim("Obstacles/extraction"), 3f);
            SR.Make("ExtractionPad", root, SpriteLibrary.Get("Obstacles/extraction_pad"), DH.SortProps - 1,
                new Vector3(L.exitX, gy - 12f / DH.PPU, 0));
            return root;
        }

        static void BuildRoof(RoofData r, int index, Transform parent)
        {
            var t = new GameObject($"Roof_{index}_{r.style}").transform;
            t.SetParent(parent, false);
            float w = r.x1 - r.x0;
            const float depth = 16f;
            float top = r.y;
            // facade: one tiled column per unit so window variants mix
            int cols = Mathf.CeilToInt(w);
            for (int c = 0; c < cols; c++)
            {
                float cw = Mathf.Min(1f, w - c);
                int v = ((c * 7 + Mathf.FloorToInt(r.x0)) % 5 + 5) % 5;
                var sprite = SpriteLibrary.Get($"Tiles/facade_{r.style}_{(v == 4 ? 2 : v % 2)}");
                SR.Tiled("Facade", t, sprite, DH.SortFacade, new Vector3(r.x0 + c, top - 14f / DH.PPU, 0), new Vector2(cw, depth));
            }
            // ledge cap
            SR.Tiled("Ledge", t, SpriteLibrary.Get($"Tiles/rooftop_{r.style}"), DH.SortRoofTop,
                new Vector3(r.x0, top + 2f / DH.PPU, 0), new Vector2(w, 0.5f));
            // edge shading
            SR.Box("EdgeL", t, new Color(0.04f, 0.02f, 0.08f, 0.55f), DH.SortRoofTop, r.x0, top - depth, 3f / DH.PPU, depth - 14f / DH.PPU);
            SR.Box("EdgeR", t, new Color(1, 1, 1, 0.08f), DH.SortRoofTop, r.x1 - 2f / DH.PPU, top - depth, 2f / DH.PPU, depth - 14f / DH.PPU);
        }

        static void BuildCatwalk(RoofData p, Transform parent)
        {
            var t = new GameObject("Catwalk").transform;
            t.SetParent(parent, false);
            float w = p.x1 - p.x0, px = 1f / DH.PPU;
            SR.Box("Deck", t, DH.Hex("3b3456"), DH.SortPlatform, p.x0, p.y - 6 * px, w, 6 * px);
            SR.Box("DeckTop", t, DH.Hex("6a5f8e"), DH.SortPlatform + 1, p.x0, p.y - px, w, px);
            SR.Box("Neon", t, DH.Pink, DH.SortPlatform + 1, p.x0, p.y - 8 * px, w, 2 * px);
            var glow = DH.Pink; glow.a = 0.25f;
            SR.Box("NeonGlow", t, glow, DH.SortPlatform, p.x0, p.y - 14 * px, w, 6 * px);
            for (float x = p.x0 + 0.4f; x < p.x1; x += 3f)
                SR.Box("Cable", t, DH.Hex("241e38"), DH.SortPlatform - 1, x, p.y, 2 * px, 20f);
        }
    }
}
