# The Data Heist: Level 1 + Runner Roster

Everything for Level 1 "Neon Rooftops", the four playable runners and the full game loop lives in `Assets/DataHeist`. Nothing in the team's existing folders was removed. Two small changes were made: the Main Menu **Play** button now loads Character Select, and two scenes were added to Build Settings.

## Quick start (Unity 6000.3.22f1)

1. Pull the repo and open it in Unity. Wait for the import to finish.
2. Run the menu command **Data Heist > Setup Project**. It does four things:
   - re-imports the art with pixel settings (32 PPU, point filter, no compression)
   - creates the editable character assets
   - checks Build Settings
   - wires the Main Menu Play button
3. Open `Assets/Scenes/MainMenuScreenScene` and press Play. The flow is **Play → Choose Your Runner → Level 1**.
   - Shortcut: **Data Heist > Play Level 1**.
   - Debug commands: **Data Heist > Debug > Unlock All Runners + 1000 Bytes** and **Reset Save Data**.
4. After editing the level, run **Data Heist > Validate Level 1 (Playtest Bot)**. It proves every runner can still finish with zero hits.

## Controls (GDD, all remappable in Pause → Controls)

| Action | Keyboard / Mouse | Gamepad |
|---|---|---|
| Jump (hold for higher) | Space, ↑ | South |
| Slide (hold to extend). In mid-air it dives into a slide | S, ↓ | East |
| Attack | Left click, J | West |
| Character ability | W, Left Shift | North |
| Pause | Esc, P | Start |

## The runners

| Runner | Role | HP | Speed | Reach | Ability (W) | Cooldown | Unlock |
|---|---|---|---|---|---|---|---|
| **Kai Okafor** (m) | The Courier | 3 | 1.00× | 1.8 | **Kick Thrusters**: a second jump in mid-air (GDD "Double Jump") | Recharges on landing | Free |
| **Nova Reyes** (f) | The Runner | 3 | 1.06× | 2.0 | **Phase Dash**: a 2.6× speed burst. She passes through hazards and clears big gaps (GDD "Sprint") | 3 s | Clear Level 1 |
| **Jax Mercer** (m) | The Breaker | 4 | 0.95× | 2.3 | **EMP Pulse**: destroys every drone and bolt within 11 m and shuts off lasers for 4 s | 8 s | 300 Data Bytes |
| **Lin Sato** (f) | The Ghost | 3 | 1.00× | 1.9 | **Time Glitch**: the world runs at half speed for 3 s and Data Bytes fly to her | 10 s | 500 Data Bytes |

Every stat lives on a `CharacterDefinition` ScriptableObject in `Resources/DataHeist/CharacterDefs`, so designers can tune them in the Inspector. Each runner has 28 animation frames: idle 4, run 8, jump 2, fall 2, slide 2, attack 4, hurt 2, ability 2 and win 2. There is also a portrait.

## Level 1: "Neon Rooftops" (580 m, about 75 s)

The level follows the GDD rule of teaching one verb at a time, then combining them.

| Section | Metres | Teaches | Beats |
|---|---|---|---|
| 0 Wake-up | 0–60 | **Jump** | Byte arcs trace the jump curve over garbage cans, ending with a double can |
| 1 Gaps | 60–150 | **Gaps & step-ups** | 2–3 m gaps, a 1 m step up, a drop down, a repair kit |
| 2 Billboards | 150–215 | **Slide** | Three neon billboards, slide then jump, the first jump pad |
| 3 Drones | 215–292 | **Attack** | Scout drones at head height and low. Stomping a drone also kills it. **Checkpoint 1**, then the ability prompt (personalised per runner) |
| 4 Mixed | 292–455 | **Combos** | Can → billboard → drone runs, a 3.5 m drop gap, the first Gunner drone (slash its bolt), a jump pad to a neon catwalk with a bonus route. **Checkpoint 2** |
| 5 Security Sweep | 455–560 | **Pressure** | Alarm zone with a 12% speed boost, red pulse and siren. Low and high lasers, a gunner, dense combos |
| Extraction | 560–580 | **Payoff** | Data Shard pickup, the EXIT tower and the results screen |

There are 305 Data Bytes, 2 repair kits, 2 checkpoints and 1 Data Shard. `Docs/level01_map.png` shows the whole layout.

**Win / lose (GDD):** you win by reaching the extraction point. You lose when health hits zero or you fall. After a loss you can **Retry from Checkpoint**, **Restart** or **Quit**. Collected bytes are always banked to the wallet.

**Rank:** S needs ≥85% bytes, no hits and no retries. A needs ≥70% bytes, at most 2 hits and no retries. B needs ≥45% bytes. Everything else is C. Score adds up bytes ×10, the shard (+500), drones ×25, a time bonus and a no-hit bonus (+300).

**Fairness rules built into the content:**
- 120 Hz fixed step
- coyote time of 0.10 s and a jump buffer of 0.12 s
- a slide never stands you up under a ceiling
- gunner bolts always fly at standing-chest height, so you can slide, jump or slash them
- i-frames after every hit

## Architecture

```
Scripts/
  Core/          DH (constants), LevelData (JSON model), CharacterDefinition (+ roster),
                 SaveSystem (PlayerPrefs JSON), RunnerInput (Input System + rebinding),
                 SpriteLibrary (Resources loader), AudioManager (pooled SFX + music)
  Gameplay/      RunnerSim  - deterministic simulation: physics, collisions, abilities, scoring
                 LevelController - scene entry point: fixed-step loop, input latching, event routing, game flow
  Presentation/  WorldBuilder, EntityViews, PlayerView, CameraRig, Parallax, FXSystem, SpriteAnimator
  UI/            UIKit (runtime UGUI/TMP factory), LevelHUD, LevelMenus, CharacterSelectController
Editor/          DataHeistSetup (menu), DataHeistArtImporter (pixel import rules), PlaytestBot
Resources/DataHeist/
  Characters/  Tiles/  Obstacles/  Enemies/  Pickups/  Decor/  FX/  UI/  Parallax/
  Audio/SFX (28 wav)  Audio/Music (2 ogg loops)  Levels/level01.json  Fonts/Press Start 2P (OFL)
Scenes/          CharacterSelectScene, Level01_NeonRooftops (one GameObject each; everything else is built at runtime)
```

**Simulation / presentation split.** `RunnerSim` is plain C# with no MonoBehaviour and no physics engine. Views only read from it, and gameplay reports what happened as events (Jump, Hurt, DroneDown and so on), which drive the sound, particles and HUD. This split gives three things:
- **Determinism.** A run plays the same way on every machine.
- **Testability.** The editor `PlaytestBot` beam-searches inputs through the real sim and proves the level can be cleared.
- **One source of truth.** The same rules and the same `level01.json` drive the browser prototype (`Tools/WebPrototype`), so a level change shows up in both builds.

**Data-driven levels.** Levels are JSON. `Tools/DataHeistGenerators/gen_level.py` authors them with helpers such as `byte_arc` (bytes that follow the real jump parabola) and `prompt`. To make Level 2, copy the script, change the sections, regenerate the JSON, then run the bot.

## Regenerating the art and audio

All art and audio are original and procedurally generated. The scripts are in `Tools/DataHeistGenerators`:
- `gen_characters.py`: skeleton-posed pixel runners
- `gen_env.py`: tiles, props, drones, FX, UI, parallax
- `gen_audio.py`: synthwave tracks and SFX
- `gen_level.py`: the level JSON
- `build_all.sh`: rebuilds everything

They need Python 3 with numpy, scipy and Pillow, plus ffmpeg.

## Ideas for Level 2+

The hooks already exist:
- `zones` handle speed and alarm areas.
- `gunner` handles projectile enemies.
- `laser` modes and `platforms` handle vertical routes.
- `CharacterDefinition.unlock` supports `ClearLevel` for per-level unlocks.

The Shop scene can call `SaveSystem.TryUnlock(def)` to sell runners in the shopkeeper booth.
