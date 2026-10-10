using System;
using UnityEngine;

namespace DataHeist
{
    /// <summary>
    /// Plain data mirror of Resources/DataHeist/Levels/levelXX.json.
    /// The same JSON drives the browser prototype, so field names must match it exactly.
    /// </summary>
    [Serializable]
    public class LevelData
    {
        public string id;
        public string name;
        public string subtitle;
        public PhysicsData physics;
        public float killY = -9f;
        public float spawnX;
        public float exitX;
        public float parTime = 80f;
        public RoofData[] roofs;
        public RoofData[] platforms;
        public EntityData[] entities;
        public PromptData[] prompts;
        public ZoneData[] zones;
        public DecorData[] decor;

        public static LevelData Load(string levelId)
        {
            var asset = Resources.Load<TextAsset>(DH.LevelPath + levelId);
            if (asset == null)
            {
                Debug.LogError($"[DataHeist] Level json not found at Resources/{DH.LevelPath}{levelId}");
                return null;
            }
            var data = JsonUtility.FromJson<LevelData>(asset.text);
            data.platforms ??= Array.Empty<RoofData>();
            data.zones ??= Array.Empty<ZoneData>();
            data.prompts ??= Array.Empty<PromptData>();
            data.decor ??= Array.Empty<DecorData>();
            return data;
        }
    }

    [Serializable]
    public class PhysicsData
    {
        public float runSpeed = 7.5f;
        public float gravity = 55f;
        public float jumpVelocity = 15.5f;
        public float jumpCutMultiplier = 0.45f;
        public float coyoteTime = 0.1f;
        public float jumpBuffer = 0.12f;
        public float slideMin = 0.45f;
        public float slideMax = 1.1f;
        public float padVelocity = 23f;
        public float stompBounce = 13f;
        public float maxFall = 26f;
        public float standHeight = 1.1f;
        public float slideHeight = 0.5f;
        public float width = 0.55f;
    }

    [Serializable]
    public class RoofData
    {
        public float x0, x1, y;
        public string style = "violet";
    }

    [Serializable]
    public class EntityData
    {
        public string t;      // byte, can, barrier, drone, gunner, laser, pad, repair, checkpoint, shard, exit,
                              // sniper, shock, sign (Level 2)
        public float x, y;
        public string kind;   // drone kind
        public string move;   // bob | still
        public float h;       // laser height
        public string mode;   // laser: low | high
        public float w;       // shock: floor strip width
    }

    [Serializable]
    public class PromptData
    {
        public float x;
        public string key;
        public string title;
        public string body;
        public bool ability;
    }

    [Serializable]
    public class ZoneData
    {
        public float x0, x1, speed = 1f;
        public bool alarm;
    }

    [Serializable]
    public class DecorData
    {
        public string k;
        public float x, y;
        public int layer;
    }
}
