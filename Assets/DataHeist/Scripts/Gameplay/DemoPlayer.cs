using System;
using UnityEngine;

namespace DataHeist
{
    /// <summary>
    /// The playtest bot's input vocabulary: one decision every DecisionTicks sim steps, each an
    /// index into this table. Shared by the editor bot (which records routes) and DemoPlayer
    /// (which replays them), so a recorded route always means the same inputs.
    /// </summary>
    public static class DemoActions
    {
        public const int DecisionTicks = 9;
        public const int Count = 7;   // idle, jump, holdJ, tapJ, slide, holdS, attack

        public static RunnerInputFrame Frame(int a, int k)
        {
            bool first = k == 0;
            switch (a)
            {
                case 1: return new RunnerInputFrame { jumpPressed = first, jumpHeld = true };
                case 2: return new RunnerInputFrame { jumpHeld = true };
                case 3: return new RunnerInputFrame { jumpPressed = first };
                case 4: return new RunnerInputFrame { slidePressed = first, slideHeld = true };
                case 5: return new RunnerInputFrame { slideHeld = true };
                case 6: return new RunnerInputFrame { attackPressed = first };
                default: return default;
            }
        }
    }

    /// <summary>
    /// Attract mode: replays a recorded clean run of Level 1 (Resources/DataHeist/Demo, made by
    /// Data Heist > Record Title Screen Demo) through the real simulation and level views, so the
    /// title screen shows the actual game. Silent; loops when the run ends.
    /// </summary>
    public class DemoPlayer : MonoBehaviour
    {
        /// <summary>Raised for every simulation event (jump, slide, attack, ...) as it happens.</summary>
        public event Action<SimEventType> OnSimEvent;

        public RunnerSim Sim { get; private set; }
        public LevelData Level { get; private set; }
        public string CharacterId { get; private set; }

        CameraRig _cam;
        Transform _world;
        EntityViews _entities;
        PlayerView _player;
        FXSystem _fx;
        string _route;
        int _decision, _tick;
        float _acc, _restartIn = -1;

        /// <summary>Builds the demo, or returns null if the level or a recorded route is missing.</summary>
        public static DemoPlayer Create(string characterId, float leadX)
        {
            var level = LevelData.Load("level01");
            if (level == null) return null;
            var demo = new GameObject("AttractDemo").AddComponent<DemoPlayer>();
            demo.Level = level;
            demo._cam = CameraRig.Create();
            demo._cam.leadX = leadX;
            if (!demo.Begin(characterId)) { Destroy(demo.gameObject); return null; }
            Parallax.Create(demo._cam.transform, demo.Sim.p.y);
            return demo;
        }

        /// <summary>Switches the runner and restarts the run.</summary>
        public void SetCharacter(string id)
        {
            if (id != CharacterId) Begin(id);
        }

        bool Begin(string id)
        {
            var asset = Resources.Load<TextAsset>($"{DH.Root}Demo/level01_{id}") ?? Resources.Load<TextAsset>($"{DH.Root}Demo/level01_kai");
            if (asset == null) return false;
            CharacterId = asset.name.Substring("level01_".Length);
            _route = asset.text.Trim();
            Teardown();
            Sim = new RunnerSim(Level, CharacterDefinition.Get(CharacterId));
            _world = WorldBuilder.Build(Level, Sim);
            _world.SetParent(transform, true);
            _entities = new GameObject("Entities").AddComponent<EntityViews>();
            _entities.transform.SetParent(transform, false);
            _entities.Init(Sim);
            _player = new GameObject("DemoRunner").AddComponent<PlayerView>();
            _player.transform.SetParent(transform, false);
            _player.Init(Sim);
            _fx = new GameObject("FX").AddComponent<FXSystem>();
            _fx.transform.SetParent(transform, false);
            _cam.Bind(Sim);
            _decision = 0; _tick = 0; _acc = 0; _restartIn = -1;
            return true;
        }

        void Teardown()
        {
            foreach (Transform child in transform) Destroy(child.gameObject);
        }

        void Update()
        {
            if (Sim == null) return;
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            if (_restartIn >= 0)
            {
                _restartIn -= dt;
                if (_restartIn < 0) Begin(CharacterId);
                return;
            }
            _acc += dt;
            int guard = 0;
            while (_acc >= DH.SimStep && guard++ < 20)
            {
                _acc -= DH.SimStep;
                int a = _decision < _route.Length ? _route[_decision] - '0' : 0;
                Sim.Step(DH.SimStep, DemoActions.Frame(a, _tick));
                if (++_tick >= DemoActions.DecisionTicks) { _tick = 0; _decision++; }
                HandleEvents();
                if (Sim.state != SimState.Running) { _restartIn = 1.2f; break; }
            }
        }

        void HandleEvents()
        {
            foreach (var ev in Sim.events)
            {
                var at = new Vector2(ev.x, ev.y);
                switch (ev.type)
                {
                    case SimEventType.Jump:
                        _fx.Burst(at, 6, new[] { DH.Hex("b8a8e0"), DH.Hex("6a5f8e") }, 3, 0.35f, -6);
                        break;
                    case SimEventType.Land:
                        _fx.Burst(at, ev.flag ? 10 : 5, new[] { DH.Hex("8a7fb0"), DH.Hex("4b4268") }, 3, 0.3f, -8);
                        break;
                    case SimEventType.Attack: _player.OnAttack(); break;
                    case SimEventType.Byte: _fx.Burst(at, 4, new[] { DH.Cyan, Color.white }, 2.5f, 0.3f, 0); break;
                    case SimEventType.Pad: _fx.Burst(at + Vector2.up * 0.3f, 12, new[] { DH.Amber, DH.Orange }, 5, 0.4f, -10); break;
                    case SimEventType.Shard: _fx.Burst(at, 40, new[] { DH.Pink, DH.Cyan, Color.white }, 7, 1f, 0); break;
                    case SimEventType.DroneDown:
                        _cam.Shake(0.15f);
                        _fx.Explosion(at);
                        _fx.Burst(at, 16, new[] { DH.Amber, DH.Orange, DH.Pink, DH.Hex("565f7e") }, 7, 0.7f, -14);
                        break;
                    case SimEventType.Deflect: _fx.Burst(at, 10, new[] { DH.Orange, Color.white }, 5, 0.35f, 0); break;
                }
                OnSimEvent?.Invoke(ev.type);
            }
            Sim.events.Clear();
        }
    }
}
