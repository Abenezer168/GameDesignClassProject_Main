# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

"The Data Heist" — a cyberpunk 2D auto-runner (Geometry Dash-style) for a Game Design class. Unity **6000.3.22f1** (pinned; don't upgrade), URP, new Input System, UGUI + TextMesh Pro. Detailed design notes (runner stats, level sections, rank rules, controls) live in `Assets/DataHeist/README_DataHeist.md` — read it before changing gameplay.

## Repository layout

- `Assets/DataHeist/` — essentially all game code and content (namespace `DataHeist`; editor code in `DataHeist.EditorTools`). No asmdefs, so everything compiles into `Assembly-CSharp` / `Assembly-CSharp-Editor`.
- `Assets/Scenes/` + `Assets/Scripts/ButtonManagerScript.cs` — the team's original Options/Shop/GameScreen/GameOver scenes (plain scene-name-string navigation). Their old main menu was replaced by `Assets/DataHeist/Scenes/MainMenu` (the boot scene). `LegacySceneRouter` (in `MainMenuController.cs`) reroutes the remaining old scenes to the new home page's Options/Shop or Character Select.
- `Tools/DataHeistGenerators/` — Python generators that produce all art, audio and level JSON.
- `Tools/WebPrototype/` — standalone browser build of Level 1 (`the-data-heist.html`).
- `Plugins/Libraries/` — a third-party helper library (BugFreeProductionsLibrary) sitting **outside `Assets/`**, so Unity does not compile it and the game doesn't use it.
- `GameDesignClassProject_Main/` (nested, untracked) is a second clone of the repo on `main`. The user's Unity editor has sometimes had *that* copy open, so changes made here won't show up in it until they open this folder instead.
- `*.csproj` / `*.slnx` are Unity-generated and gitignored.

## Commands

There is no CLI build/lint/test pipeline; work happens in the Unity Editor via the **Data Heist** menu (`Assets/DataHeist/Editor/DataHeistSetup.cs`):
- **Data Heist > Setup Project** — first run after pulling: pixel-art reimport (32 PPU, point filter, no compression), creates `CharacterDefinition` assets, adds scenes to Build Settings, wires Main Menu Play.
- **Data Heist > Play Level 1 / Play Level 2 / Play Story** — jump straight into a scene.
- **Data Heist > Validate Level 1 / Level 2 (Playtest Bot)** — the closest thing to a test suite. Beam-searches inputs through the real `RunnerSim` and must find a zero-hit, no-ability clear for every runner. Run it after any change to `RunnerSim` or the level JSON.
- **Data Heist > Debug > Unlock All Runners + 1000 Bytes** / **Reset Save Data**.
- **Data Heist > Debug > Smoke Test**: a play-mode run through the title screen, Settings, all story shots, Character Select and Level 1. It fails on any console error and writes screenshots to `Logs/Screens/`.

Both checks also run headless. This only works while the project is **not** open in an editor; the user's editor may have the nested clone open, which is fine:
```bash
UNITY=/Applications/Unity/Hub/Editor/6000.3.22f1/Unity.app/Contents/MacOS/Unity
$UNITY -batchmode -projectPath "$PWD" -executeMethod DataHeist.EditorTools.PlaytestBot.ValidateAllBatch -logFile /tmp/bot.log   # every level; exit code 1 on failure
$UNITY -batchmode -projectPath "$PWD" -executeMethod DataHeist.EditorTools.SmokeTest.RunBatch -logFile /tmp/smoke.log   # exit code 1 on failure
```
Compile errors show up in the log as `error CS`. Screenshots in `Logs/Screens/*.png` are the way to check layout.

Audio is real CC0 sound (Kenney SFX + OpenGameArt music) built by `Tools/DataHeistGenerators/build_audio.py`. It needs `pip install numpy scipy soundfile pyloudnorm` and internet access for the first download. File names are the contract with code (`Sfx("jump")` → `Audio/SFX/sfx_jump.wav`, `Music("music_level02")` → `Audio/Music/music_level02.ogg`), and the README's **Audio** section maps every file to where it plays. Levels play `music_<levelId>` from GO.

Asset regeneration (needs Python 3 + numpy, scipy, Pillow, and ffmpeg):
```bash
Tools/DataHeistGenerators/build_all.sh                 # rebuild all art/audio/level + web build assets
python3 Tools/DataHeistGenerators/gen_level.py Assets/DataHeist/Resources/DataHeist/Levels/level01.json   # level only
```

Headless level validation for the web sim:
```bash
cd Tools/WebPrototype && node bot.js ../../Assets/DataHeist/Resources/DataHeist/Levels/level01.json
```

## Architecture

**Simulation / presentation split.** `Scripts/Gameplay/RunnerSim.cs` is plain C# (no MonoBehaviour, no Unity physics): it owns player physics, collisions, abilities, pickups and scoring, advanced by `Step(dt, RunnerInputFrame)` at a fixed 120 Hz (`DH.SimStep`). It reports what happened by appending `SimEvent`s to `events`, and it supports `Clone()` (the playtest bot relies on this). Keep it deterministic and free of Unity scene/physics dependencies.

`Scripts/Gameplay/LevelController.cs` is the scene entry point. It runs the fixed-step loop, latches input from `RunnerInput` into `RunnerInputFrame`s, drains sim events to drive audio/FX/HUD, and handles game flow (countdown, pause via `Time.timeScale`, checkpoint retry, results, scene loads). Presentation (`Scripts/Presentation/*`) only *reads* sim state, and UI (`Scripts/UI/*`) is built at runtime through the `UIKit` factory. The two DataHeist scenes contain just one GameObject each, and everything else is constructed in code. Edit code, not scene hierarchies.

**Parallel JS implementation.** `Tools/WebPrototype/sim.js` mirrors `RunnerSim.cs` 1:1, and both read the same level JSON. A gameplay-rule change in one should be ported to the other.

**Data-driven content, loaded via `Resources`.** All paths are constants in `Scripts/Core/DH.cs` (also scene names, palette, sorting orders; there is a single sorting layer and order is controlled purely by `DH.Sort*`).
- Levels: `Resources/DataHeist/Levels/levelXX.json` → `LevelData.Load(id)`. These are generated by `gen_level.py` (helpers like `byte_arc` match the real jump parabola), so change the generator and regenerate rather than hand-editing the JSON. Level JSON supports `zones`, `gunner`, `laser` modes and `platforms` for future levels.
- Runners: `CharacterDefinition` ScriptableObjects in `Resources/DataHeist/CharacterDefs` (stats, ability, unlock rule) loaded via `CharacterDefinition.All`.
- Sprites: `SpriteLibrary` loads by path and auto-discovers numbered animation frames (`<char>_<anim>_NN.png`).
- Persistence: `SaveSystem` (wallet, unlocks, best results) and `RunnerInput` key rebinds are both JSON in `PlayerPrefs`.

**Home page attract demo.** `DemoPlayer` replays `Resources/DataHeist/Demo/level01_<runner>.txt` (one `DemoActions` index per 9 sim ticks) through the real `RunnerSim` behind the menu. Those routes are recorded by the playtest bot (**Data Heist > Record Title Screen Demo**). Any change to Level 1 or to sim physics desyncs them, so re-record after such a change.

**Menus and story.** The title screen (`MainMenuController`), Character Select and the story intro (`Scripts/Story/`, StoryScene) are all built at runtime with `UIKit`. They share `MenuBackdrop` and `Parallax` for the city, `ScreenFX` for the vignette and scanlines, and `SceneFader.Load` for transitions. Story shots are `StoryShot` subclasses staged side by side along x, and story art comes from `gen_story.py`. Overlay-canvas UI can't be positioned with `WorldToScreenPoint` in tests, so world-pinned UI uses viewport maths.

**Levels and hazards.** `LevelCatalog` lists the campaign in order (id, scene, name). A level unlocks when the previous id is in `SaveData.cleared`, and the results screen offers NEXT LEVEL. Each level scene is one GameObject: a `LevelController` with a serialized `levelId`. Level 2's hazards (`sniper`, `shock`, `sign`) live in `RunnerSim.UpdateLevel2`. They are triggered by distance, never by clock time, so retries stay deterministic. New `SimEventType` values go at the end of the enum, and `EntityViews` / `LevelController.HandleEvents` render them. `Tools/WebPrototype/sim.js` has **not** been ported for Level 2.

**Serialized fields go stale.** Scene files keep old values of public MonoBehaviour fields (this bit `StoryController` and `CharacterSelectController`). Prefer `const`s or catalog lookups for flow and tuning that shouldn't be per-scene.

**Level design invariants** the bot and README enforce: coyote time of 0.10 s, a jump buffer of 0.12 s, gunner bolts at standing-chest height, a slide never stands you up under a ceiling, and i-frames after every hit.
