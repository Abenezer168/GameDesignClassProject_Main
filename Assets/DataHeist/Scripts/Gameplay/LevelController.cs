using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DataHeist
{
    /// <summary>
    /// Scene entry point for a Data Heist level. Owns the simulation, steps it at a fixed
    /// 120 Hz, feeds it input, and routes its events to views, FX, audio and UI.
    /// Drop this on one GameObject in an empty scene; everything else is built at runtime.
    /// </summary>
    public class LevelController : MonoBehaviour
    {
        [Tooltip("Level json in Resources/DataHeist/Levels")]
        public string levelId = "level01";
        [Tooltip("Optional override for testing in the editor (kai, nova, jax, lin). Empty = saved selection.")]
        public string characterOverride = "";
        [Tooltip("Seconds of 3-2-1 before the run starts")]
        public float introCountdown = 3.2f;

        public RunnerSim Sim { get; private set; }
        /// <summary>Applies saved abilities to this run and unlocks double jump when its goal is reached.</summary>
        public PlayerAbilityManager Abilities { get; private set; }

        string LevelMusic => "music_" + levelId;

        // the track this moment should have (null = deliberate silence); Update keeps the music on it,
        // so a crossfade interrupted by a scene change can never leave a level silent
        string _musicWanted;

        void WantMusic(string track, bool restart = false)
        {
            _musicWanted = track;
            if (track == null) AudioManager.I.StopMusic();
            else AudioManager.I.Music(track, restart);
        }

        LevelData _level;
        CharacterDefinition _ch;
        RunnerInput _input;
        CameraRig _cam;
        PlayerView _player;
        EntityViews _entities;
        FXSystem _fx;
        LevelHUD _hud;
        LevelMenus _menus;

        float _acc, _countdown, _hitStop, _glitch;
        int _lastCount = -1;
        bool _paused, _alarmOn, _banked, _endShown;
        int _byteChain; float _byteChainT;
        // latched edge inputs so a press is never lost between fixed steps
        bool _jumpLatch, _slideLatch, _attackLatch, _abilityLatch;

        void Awake()
        {
            Application.targetFrameRate = 120;
            GameSession.CurrentLevel = levelId;
            _level = LevelData.Load(levelId);
            if (_level == null) { enabled = false; return; }
            var id = string.IsNullOrEmpty(characterOverride) ? GameSession.SelectedCharacter : characterOverride;
            _ch = CharacterDefinition.Get(id);
            _input = new RunnerInput();

            Sim = new RunnerSim(_level, _ch);
            Abilities = new PlayerAbilityManager(Sim);       // carries a saved double jump into this run
            Abilities.DoubleJumpUnlocked += OnDoubleJumpUnlocked;
            _cam = CameraRig.Create();
            Parallax.Create(_cam.transform, Sim.p.y, _level.theme);
            WorldBuilder.Build(_level, Sim);

            _entities = new GameObject("Entities").AddComponent<EntityViews>();
            _entities.Init(Sim);
            _player = new GameObject("Runner_" + _ch.id).AddComponent<PlayerView>();
            _player.Init(Sim);
            _fx = new GameObject("FX").AddComponent<FXSystem>();

            _hud = new GameObject("HUD").AddComponent<LevelHUD>();
            _hud.Init(Sim, _input);
            _menus = new GameObject("Menus").AddComponent<LevelMenus>();
            _menus.Init(_input);
            _menus.OnResume += Resume;
            _menus.OnRestart += Restart;
            _menus.OnRetryCheckpoint += RetryCheckpoint;
            _menus.OnQuitToRunners += () => Leave(DH.SceneCharacterSelect);
            _menus.OnQuitToMenu += () => Leave(DH.SceneMainMenu);
            _menus.OnNextLevel += NextLevel;
            _menus.OnBindingsChanged += _hud.RefreshControls;

            _cam.Bind(Sim);
            _countdown = introCountdown;
            // the menu track fades out during 3-2-1; the level track starts on GO
            AudioManager.I.StopMusic();
        }

        void OnDestroy()
        {
            _input?.Dispose();
            Time.timeScale = 1f;
        }

        // ------------------------------------------------------------------ loop
        void Update()
        {
            if (Sim == null) return;
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);

            if (_input.Pause.WasPressedThisFrame())
            {
                if (_paused && _menus.PauseOpen) Resume();
                else if (!_paused && Sim.state == SimState.Running && _countdown <= 0) Pause();
            }
            if (_paused) return;

            if (_input.Jump.WasPressedThisFrame()) _jumpLatch = true;
            if (_input.Slide.WasPressedThisFrame()) _slideLatch = true;
            if (_input.Attack.WasPressedThisFrame()) _attackLatch = true;
            if (_input.Ability.WasPressedThisFrame()) _abilityLatch = true;

            _byteChainT -= dt;
            if (_byteChainT <= 0) _byteChain = 0;

            if (_countdown > 0)
            {
                _countdown -= dt;
                int c = Mathf.CeilToInt(_countdown - 0.2f);
                if (c != _lastCount)
                {
                    _lastCount = c;
                    if (c > 0 && c <= 3) AudioManager.I.Sfx("count", 0.6f);
                    else if (c <= 0)
                    {
                        AudioManager.I.Sfx("go", 0.7f);
                        if (AudioManager.I.CurrentMusic != LevelMusic) WantMusic(LevelMusic, true);
                        else _musicWanted = LevelMusic;
                    }
                }
                _hud.SetCountdown(_countdown, Sim.stats.deaths == 0, _level);
                _jumpLatch = _slideLatch = _attackLatch = _abilityLatch = false;
                return;
            }
            _hud.SetCountdown(0, false, _level);
            if (_musicWanted != null && !_endShown && Sim.state == SimState.Running && AudioManager.I.CurrentMusic != _musicWanted)
                AudioManager.I.Music(_musicWanted);

            if (_hitStop > 0) { _hitStop -= dt; return; }

            float scale = 1f;
            if (_glitch > 0)
            {
                _glitch -= dt; scale = 0.5f;
                if (_glitch <= 0) AudioManager.I.MusicPitch(1f);
            }
            _player.GlitchTime = _glitch;
            _hud.GlitchT = _glitch;

            _acc += dt * scale;
            int guard = 0;
            while (_acc >= DH.SimStep && guard++ < 20)
            {
                var frame = new RunnerInputFrame
                {
                    jumpPressed = _jumpLatch, jumpHeld = _input.Jump.IsPressed(),
                    slidePressed = _slideLatch, slideHeld = _input.Slide.IsPressed(),
                    attackPressed = _attackLatch, abilityPressed = _abilityLatch,
                };
                _jumpLatch = _slideLatch = _attackLatch = _abilityLatch = false;
                Sim.Step(DH.SimStep, frame);
                _acc -= DH.SimStep;
                HandleEvents();
                Abilities.Tick();
                if (Sim.state != SimState.Running) { _acc = 0; break; }
            }
            // keep Unity's Time in step with the glitch so particles/animators slow too
            Time.timeScale = scale;

            bool alarm = Sim.InAlarmZone || Sim.escaping;
            if (alarm && !_alarmOn) AudioManager.I.Sfx("alarm", 0.6f);
            _alarmOn = alarm;
            _hud.Alarm = alarm;
        }

        // ------------------------------------------------------------------ events
        void OnDoubleJumpUnlocked()
        {
            var at = new Vector2(Sim.p.x, Sim.p.y + 1.2f);
            AudioManager.I.Sfx("unlock", 0.9f);
            _fx.Burst(at, 24, new[] { DH.Green, DH.Cyan, Color.white }, 6, 0.7f, -6);
            _hud.ShowDoubleJumpUnlocked();
        }

        void HandleEvents()
        {
            var a = AudioManager.I;
            var p = Sim.p;
            foreach (var ev in Sim.events)
            {
                var at = new Vector2(ev.x, ev.y);
                switch (ev.type)
                {
                    case SimEventType.Jump:
                        a.Sfx("jump", 0.7f);
                        _fx.Burst(at, 6, new[] { DH.Hex("b8a8e0"), DH.Hex("6a5f8e") }, 3, 0.35f, -6);
                        break;
                    case SimEventType.AirJump:
                        a.Sfx("air_jump", 0.8f);
                        _fx.Burst(at + new Vector2(-0.1f, 0), 14, new[] { _ch.color, DH.Orange, Color.white }, 5, 0.4f, -18);
                        break;
                    case SimEventType.Land:
                        a.Sfx("land", ev.flag ? 0.8f : 0.45f);
                        _fx.Burst(at, ev.flag ? 10 : 5, new[] { DH.Hex("8a7fb0"), DH.Hex("4b4268") }, 3, 0.3f, -8);
                        if (ev.flag) _cam.Shake(0.15f);
                        break;
                    case SimEventType.Slide: a.Sfx("slide", 0.6f); break;
                    case SimEventType.Dive: a.Sfx("dash", 0.35f, 1.6f); break;
                    case SimEventType.Attack: a.Sfx("attack", 0.7f); _player.OnAttack(); break;
                    case SimEventType.Byte:
                        a.Sfx("byte", 0.45f, 1f + Mathf.Min(0.5f, _byteChain++ * 0.03f));
                        _byteChainT = 0.5f;
                        _fx.Burst(at, 4, new[] { DH.Cyan, Color.white }, 2.5f, 0.3f, 0);
                        break;
                    case SimEventType.Repair:
                        a.Sfx("repair", 0.8f);
                        _fx.FloatText(at + Vector2.up * 0.6f, "+1 HP", DH.Green);
                        _fx.Burst(at, 14, new[] { DH.Green, Color.white }, 4, 0.6f, 0);
                        break;
                    case SimEventType.Shard:
                        a.Sfx("shard", 1f);
                        _fx.FloatText(at + Vector2.up, "DATA SHARD SECURED", DH.Pink);
                        _fx.Burst(at, 40, new[] { DH.Pink, DH.Cyan, Color.white }, 7, 1f, 0);
                        _hud.Flash(0.25f);
                        break;
                    case SimEventType.Pad:
                        a.Sfx("pad", 0.8f);
                        _fx.Burst(at + Vector2.up * 0.3f, 12, new[] { DH.Amber, DH.Orange }, 5, 0.4f, -10);
                        break;
                    case SimEventType.DroneDown:
                        a.Sfx("explode", 0.7f);
                        _cam.Shake(0.22f);
                        if (ev.info != "emp") _hitStop = Mathf.Max(_hitStop, 0.05f);
                        _fx.Explosion(at);
                        _fx.Burst(at, 16, new[] { DH.Amber, DH.Orange, DH.Pink, DH.Hex("565f7e") }, 7, 0.7f, -14);
                        _fx.FloatText(at + Vector2.up * 0.5f, ev.info == "stomp" ? "STOMP +25" : "+25", DH.Amber);
                        break;
                    case SimEventType.CanHit:
                        _fx.Burst(at + Vector2.up * 0.4f, 14, new[] { DH.Hex("4a5a78"), DH.Green, DH.Hex("7d90b4") }, 6, 0.6f, -20);
                        break;
                    case SimEventType.Hurt:
                        a.Sfx("hurt", 1f);
                        _cam.Shake(0.35f); _hitStop = 0.08f; _hud.Flash(0.18f);
                        _fx.FloatText(new Vector2(p.x, p.y + 1.6f), ev.info == "time" ? "-1   -3s" : "-1", DH.Red);
                        break;
                    case SimEventType.Dead:
                        a.Sfx("game_over", 1f);
                        _cam.Shake(0.4f);
                        AudioManager.I.MusicPitch(0.6f);
                        StartCoroutine(ShowOverAfter(0.9f, ev.info));
                        break;
                    case SimEventType.Checkpoint:
                        a.Sfx("checkpoint", 0.9f);
                        _fx.FloatText(new Vector2(ev.x, ev.y + 2.4f), "CHECKPOINT", DH.Green);
                        break;
                    case SimEventType.Dash: a.Sfx("dash", 0.9f); _cam.Shake(0.1f); break;
                    case SimEventType.Emp:
                        a.Sfx("emp", 1f); _player.OnEmp(); _cam.Shake(0.3f); _hud.Flash(0.2f);
                        break;
                    case SimEventType.Glitch:
                        a.Sfx("glitch", 1f); _glitch = 3f; AudioManager.I.MusicPitch(0.75f);
                        break;
                    case SimEventType.AbilityDenied: a.Sfx("denied", 0.4f); break;
                    case SimEventType.Shoot: a.Sfx("shoot", 0.5f); break;
                    case SimEventType.Deflect:
                        a.Sfx("deflect", 0.9f);
                        _fx.Burst(at, 10, new[] { DH.Orange, Color.white }, 5, 0.35f, 0);
                        break;
                    case SimEventType.Respawn:
                        _countdown = 1.6f; _lastCount = -1; AudioManager.I.MusicPitch(1f);
                        // back to the right track for where the retry starts (level, or the escape)
                        WantMusic(Sim.escaping ? "music_escape" : LevelMusic);
                        break;
                    case SimEventType.Win:
                        StartCoroutine(Win());
                        break;
                    // ---- Level 3: data, vault, Guardian, escape
                    case SimEventType.Fragment:
                        a.Sfx("fragment", 1f);
                        _hud.Flash(0.15f);
                        _fx.Burst(at, 24, new[] { DH.Amber, DH.Cyan, Color.white }, 6, 0.7f, 0);
                        _fx.FloatText(at + Vector2.up * 0.8f, ev.flag ? "ALL FRAGMENTS: VAULT UNLOCKED" : $"DATA FRAGMENT {Sim.stats.fragments}/{Sim.totalFragments}", DH.Amber);
                        break;
                    case SimEventType.VaultOpen:
                        a.Sfx("vault_open", 1f);
                        _cam.Shake(0.3f);
                        _fx.FloatText(at + Vector2.up * 1.8f, "VAULT OPEN", DH.Green);
                        break;
                    case SimEventType.BossStart:
                        a.Sfx("boss_roar", 1f);
                        WantMusic("music_boss", true);
                        _cam.Shake(0.4f);
                        break;
                    case SimEventType.BossTelegraph:
                        a.Sfx(ev.info == "slam" ? "boss_charge" : "turret_aim", ev.info == "slam" ? 0.7f : 0.6f, ev.info == "orbs" ? 0.8f : 1f);
                        break;
                    case SimEventType.BossFire:
                        if (ev.info == "laser") a.Sfx("sniper_shot", 0.9f);
                        else if (ev.info == "orb") a.Sfx("boss_orb", 0.7f);
                        else
                        {
                            a.Sfx("press_slam", 1f);
                            _cam.Shake(0.45f);
                            _fx.Burst(at, 20, new[] { DH.Cyan, DH.Hex("9a5cff"), Color.white }, 7, 0.5f, -12);
                        }
                        break;
                    case SimEventType.BossHit:
                        a.Sfx("boss_hit", 1f);
                        _entities.FlashBoss();
                        _cam.Shake(0.3f); _hitStop = Mathf.Max(_hitStop, 0.06f);
                        _fx.Explosion(at);
                        _fx.FloatText(at + Vector2.up * 0.6f, "CRITICAL HIT", DH.Cyan);
                        break;
                    case SimEventType.BossPhase:
                        a.Sfx("boss_roar", 1f, 1.15f);
                        _hud.Flash(0.25f);
                        _fx.FloatText(at + Vector2.up, "GUARDIAN ENRAGED // PHASE " + ev.info, DH.Red);
                        break;
                    case SimEventType.BossDefeated:
                        StartCoroutine(BossDeath(at));
                        break;
                    case SimEventType.Evidence:
                        a.Sfx("shard", 1f, 0.9f);
                        _hud.Flash(0.3f);
                        _fx.Burst(at, 40, new[] { DH.Pink, DH.Cyan, Color.white }, 7, 1f, 0);
                        _fx.FloatText(at + Vector2.up, "EVIDENCE SECURED", DH.Pink);
                        break;
                    case SimEventType.Lockdown:
                        WantMusic("music_escape", true);     // the alarm-zone check plays the siren
                        _cam.Shake(0.3f);
                        break;
                    // ---- Level 3 hazards
                    case SimEventType.TurretAim: a.Sfx("turret_aim", 0.55f); break;
                    case SimEventType.TurretShot:
                        a.Sfx("turret_shot", 0.7f);
                        _fx.Burst(new Vector2(ev.x - 0.6f, ev.y), 6, new[] { DH.Red, DH.Amber }, 3, 0.2f, 0);
                        break;
                    case SimEventType.LaserOn: a.Sfx("laser_on", 0.35f); break;
                    case SimEventType.PressWarn: a.Sfx("press_warn", 0.5f); break;
                    case SimEventType.PressSlam:
                        a.Sfx("press_slam", 0.8f);
                        _cam.Shake(0.2f);
                        _fx.Burst(at, 12, new[] { DH.Amber, DH.Hex("8a7fb0") }, 5, 0.35f, -16);
                        break;
                    // ---- Level 2 hazards
                    case SimEventType.SniperAim: a.Sfx("sniper_aim", 0.7f); break;
                    case SimEventType.SniperShot:
                        a.Sfx("sniper_shot", 0.9f);
                        _cam.Shake(0.12f);
                        _fx.Burst(new Vector2(ev.x - 0.8f, ev.y), 8, new[] { DH.Red, Color.white }, 4, 0.25f, 0);
                        break;
                    case SimEventType.ShockOn:
                        a.Sfx("shock", 0.4f, UnityEngine.Random.Range(0.95f, 1.08f));
                        break;
                    case SimEventType.SignShake:
                        a.Sfx("sign_creak", 0.7f);
                        _fx.Burst(at + new Vector2(0, 1.3f), 6, new[] { DH.Amber, Color.white }, 2.5f, 0.3f, -14);
                        break;
                    case SimEventType.SignLand:
                        a.Sfx("sign_crash", 0.9f);
                        _cam.Shake(0.25f);
                        _fx.Burst(at + Vector2.up * 0.3f, 18, new[] { DH.Pink, DH.Cyan, DH.Amber, DH.Hex("3c2766") }, 6, 0.6f, -18);
                        break;
                    case SimEventType.SignSmash:
                        a.Sfx("sign_crash", 0.7f, 1.15f);
                        _fx.Burst(at, 22, new[] { DH.Pink, DH.Cyan, Color.white, DH.Hex("3c2766") }, 7, 0.6f, -16);
                        _fx.FloatText(at + Vector2.up * 0.7f, "SMASH", DH.Pink);
                        break;
                }
            }
            Sim.events.Clear();
        }

        /// <summary>The Guardian comes apart in a chain of explosions, then the arena falls quiet.</summary>
        IEnumerator BossDeath(Vector2 at)
        {
            var a = AudioManager.I;
            WantMusic(null);                         // a beat of silence before the lockdown
            a.Sfx("boss_explode", 1f);
            _hud.Flash(0.4f);
            _fx.FloatText(at + Vector2.up * 1.5f, "GUARDIAN DESTROYED", DH.Amber);
            for (int i = 0; i < 6; i++)
            {
                var pos = at + new Vector2(UnityEngine.Random.Range(-1.4f, 1.4f), UnityEngine.Random.Range(-1.5f, 1.5f));
                _fx.Explosion(pos);
                _fx.Burst(pos, 14, new[] { DH.Amber, DH.Orange, DH.Pink }, 7, 0.6f, -14);
                _cam.Shake(0.35f);
                if (i % 2 == 0) a.Sfx("explode", 0.8f, 0.8f + i * 0.05f);
                yield return new WaitForSeconds(0.28f);
            }
        }

        // ------------------------------------------------------------------ flow
        void Pause()
        {
            _paused = true; Time.timeScale = 0f;
            AudioManager.I.MusicDuck(true);
            AudioManager.I.Sfx("ui_back");
            _menus.ShowPause();
        }

        void Resume()
        {
            _paused = false; Time.timeScale = 1f;
            AudioManager.I.MusicDuck(false);
            _menus.HideAll();
            _jumpLatch = _slideLatch = _attackLatch = _abilityLatch = false;
        }

        IEnumerator ShowOverAfter(float delay, string cause)
        {
            Time.timeScale = 1f; _glitch = 0;
            yield return new WaitForSecondsRealtime(delay);
            if (Sim.state != SimState.Dead) yield break;
            _paused = true;
            _menus.ShowOver(cause, Sim.checkpointX > _level.spawnX);
        }

        void RetryCheckpoint()
        {
            _menus.HideAll();
            _paused = false; Time.timeScale = 1f;
            Sim.Respawn();
            HandleEvents();
            _player.ResetRun();
            _cam.Snap();
        }

        void Restart()
        {
            Time.timeScale = 1f;
            BankBytes();
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        void Leave(string scene)
        {
            Time.timeScale = 1f;
            BankBytes();
            if (Application.CanStreamedLevelBeLoaded(scene)) SceneManager.LoadScene(scene);
            else
            {
                Debug.LogWarning($"[DataHeist] Scene '{scene}' is not in Build Settings. Run Data Heist > Setup Project.");
                SceneManager.LoadScene(DH.SceneMainMenu);
            }
        }

        /// <summary>Bytes collected are always banked to the wallet, even on a failed run.</summary>
        void BankBytes()
        {
            if (_banked || Sim == null) return;
            _banked = true;
            SaveSystem.Data.bytes += Sim.stats.bytes;
            SaveSystem.Data.totalDeaths += Sim.stats.deaths;
            SaveSystem.Data.totalPlayTime += Sim.time;
            SaveSystem.Save();
        }

        IEnumerator Win()
        {
            if (_endShown) yield break;
            _endShown = true;
            Time.timeScale = 1f; _glitch = 0;
            AudioManager.I.Sfx("level_complete", 1f);
            AudioManager.I.Music("music_menu", true);
            yield return new WaitForSecondsRealtime(1.4f);

            var s = Sim.stats;
            string rank = Sim.Rank();
            int score = Sim.Score();
            BankBytes();
            var unlocks = new StringBuilder();
            var data = SaveSystem.Data;
            var next = LevelCatalog.Next(_level.id);
            if (!data.cleared.Contains(_level.id))
            {
                data.cleared.Add(_level.id);
                if (next.HasValue) unlocks.AppendLine($"> LEVEL {next.Value.number} {next.Value.name} UNLOCKED");
                foreach (var c in CharacterDefinition.All)
                    if (c.unlock == UnlockType.ClearLevel && c.unlockLevelId == _level.id && !data.unlocked.Contains(c.id))
                    {
                        data.unlocked.Add(c.id);
                        unlocks.AppendLine($"> {c.displayName} JOINED THE CREW");
                    }
            }
            if (s.evidence && !data.guardianAward)
            {
                data.guardianAward = true;           // the final mission is done: Guardian of the City
                unlocks.AppendLine("> AWARD EARNED: GUARDIAN OF THE CITY");
            }
            bool best = SaveSystem.SubmitRecord(new LevelRecord { levelId = _level.id, score = score, rank = rank, time = Sim.time, bytes = s.bytes, fragments = s.fragments });
            SaveSystem.Save();
            foreach (var c in CharacterDefinition.All)
                if (c.unlock == UnlockType.Bytes && !data.unlocked.Contains(c.id) && data.bytes >= c.unlockCost)
                    unlocks.AppendLine($"> {c.displayName} CAN BE RECRUITED ({c.unlockCost})");

            string rows =
                $"DATA BYTES   <color=#2ff3ff>{s.bytes} / {Sim.totalBytes}</color>\n" +
                $"DRONES DOWN  <color=#2ff3ff>{s.drones}</color>\n" +
                $"HITS TAKEN   <color=#2ff3ff>{s.hits}</color>\n" +
                $"RETRIES      <color=#2ff3ff>{s.deaths}</color>\n" +
                (Sim.totalFragments > 0 ? $"DATA FRAGS   <color=#2ff3ff>{s.fragments} / {Sim.totalFragments}</color>\n" : "") +
                $"TIME         <color=#2ff3ff>{DH.FormatTime(Sim.time)}</color>\n" +
                $"SCORE        <color=#2ff3ff>{score}</color>{(best ? "  <color=#ffd23a>NEW BEST</color>" : "")}";
            _paused = true;
            if (!next.HasValue && s.evidence)
            {
                // the final mission: no results screen, the escape rolls straight into the ending
                AudioManager.I.StopMusic();
                _hud.Flash(0.6f);
                yield return new WaitForSecondsRealtime(1.2f);
                LoadingScreen.Run(DH.SceneEnding, null, sceneLabel: "THE FILES GO PUBLIC");
                yield break;
            }
            _menus.ShowResults(rank, rows, unlocks.ToString(), data.bytes, next.HasValue ? next.Value.name : null);
        }

        /// <summary>Results screen NEXT LEVEL: same runner, straight into the next level through the loading screen.</summary>
        void NextLevel()
        {
            var next = LevelCatalog.Next(_level.id);
            if (!next.HasValue) return;
            Time.timeScale = 1f;
            BankBytes();
            GameSession.CurrentLevel = next.Value.id;
            AudioManager.I.StopMusic();
            LoadingScreen.Run(next.Value.scene, GamePreloader.Steps(story: false, levelId: next.Value.id, onlyRunner: _ch.id),
                sceneLabel: "DROPPING INTO " + next.Value.name);
        }
    }
}
