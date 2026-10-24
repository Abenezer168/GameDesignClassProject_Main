# Developer Guide

How the project is laid out and how to change it.

## Project

"The Data Heist" is a 2D auto-runner for a Game Design class. Unity **6000.3.22f1** (don't upgrade), the Input System package, and UGUI. The game is kept deliberately simple: a small number of short `MonoBehaviour` scripts that use Unity's own 2D physics.

## Repository layout

- `Assets/DataHeist/Scripts/`: all game code (about 15 short scripts, see `Assets/DataHeist/README_DataHeist.md`).
- `Assets/DataHeist/Scenes/`: `MainMenu` (first scene), `Level1`, `Shop`, `Options`.
- `Assets/DataHeist/Art/`: sprites, the pixel font, music and sound effects used by the scenes.
- `Assets/DataHeist/Prefabs/`: `Bolt` (the gunner's bullet) and the `NoFriction` physics material used by the player.
- `Assets/Scripts/ButtonManagerScript.cs`: the team's scene-navigation script. Every menu button uses it (`LoadGameScene("Level1")`, `QuitGame()`).
- `Assets/Scenes/`: the team's first Options/Shop/GameScreen/GameOver scenes. They are not in Build Settings.
- `Assets/Adventure UI Kit`, `Assets/Hyper_Casual_UI`, `Assets/Backgrounds`: UI asset packs the team imported.
- `Tools/`: Python art generators and a browser prototype from the earlier, larger version of the game. The simple version does not use them.

## Scenes and objects

Everything in a level is a normal GameObject you can see in the Hierarchy and edit in the Inspector:

- **Player:** `Rigidbody2D`, `BoxCollider2D` (with the `NoFriction` material), `PlayerController`, `PlayerAttack`, `PlayerHealth`. Its `GroundCheck` child sits at the feet.
- **Rooftops:** one object per roof with a `BoxCollider2D` on the **Ground** layer (layer 6).
- **Coins**, **Obstacles** (trash cans and barriers on the **Obstacle** layer, layer 7), **Enemies** (scout drones and gunners), the **DataShard** and the **FinishLine**.
- **GameManager**, **HUD** (canvas with `UIManager`), **Music**, **Main Camera** (with `CameraFollow` and the background pictures as children).

To add a coin, duplicate an existing coin and move it. The same works for trash cans, barriers and enemies. A gunner's `FirePoint` child sets where its bullets start.

## Conventions

- Keep scripts short and easy to explain. Prefer `Start()`, `Update()`, `OnTriggerEnter2D()`, simple fields and `if` statements.
- Pickups, obstacles, enemies and bullets use **trigger** colliders. Only the rooftops are solid.
- Damage always goes through `PlayerHealth.TakeDamage()`, which has the damage cooldown.
- Saved data always goes through `SaveManager` (PlayerPrefs keys: `TotalCoins`, `ExtraHearts`, `DataShard_<level>`, `MusicVolume`, `SoundVolume`).
- Coins from a run are saved once, when the run ends (Game Over or Level Complete).
- A `MonoBehaviour` must live in a `.cs` file with the same name as the class, or the scene loses the reference.

## Testing

Play the game from `MainMenu` in the editor and check the Console for errors. Things worth checking after a change: all four menu buttons, the HUD line, coins, the double jump at 30 coins, sliding under a barrier, attacking a drone, a gunner's bullet, the Data Shard, Game Over, Retry, Level Complete and that coins appear in the Shop.
