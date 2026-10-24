# The Data Heist: Cyberpunk 2D Auto-Runner

A 2D side-scrolling auto-runner made in Unity for our Game Design and Development class. You play Kai, a runner crossing the neon rooftops of Sector 7. Collect coins, jump over trash cans, slide under barriers, fight scout drones and gunners, and grab the **Data Shard** before you reach the end of the level.

## What's in the game
* **Main menu** with four buttons: **Play**, **Shop**, **Options** and **Exit**.
* **Level 1 "Neon Rooftops":** the player runs automatically from left to right across ten rooftops.
* **Coins:** every coin you touch adds 1 to your coin count. Coins are saved when the run ends and can be spent in the Shop.
* **Double jump:** unlocks after collecting **30 coins** in a run and stays on until the run ends.
* **Enemies:** scout drones that fly back and forth, and gunners that shoot bullets at chest height.
* **Obstacles:** trash cans (jump over them) and barriers (slide under them). Touching one costs 1 health.
* **Data Shard:** the goal of the level. You must collect it before reaching the finish.
* **Shop:** buy an extra heart (+1 starting health) for 100 coins, up to 2.
* **Options:** music and sound volume sliders.
* **Screens:** Pause, Game Over (Retry / Main Menu) and Level Complete.

## How to play
| Action | Key |
|---|---|
| Jump | Space |
| Double jump | Space again in the air (after 30 coins) |
| Slide | S or Down Arrow |
| Attack | J or Left Mouse Click |
| Pause | Escape |

You start with 3 health. You lose a health point when you touch a trash can, a barrier, a drone, a gunner or a bullet, and after a hit you blink for a moment and can't be hurt again. The run ends when your health reaches 0 or you fall between the rooftops.

## Built with
* **Unity 6.3** (version 6000.3.22f1). Please use this exact version.
* **C#** with Unity's 2D physics (`Rigidbody2D`, `BoxCollider2D`, triggers) and the **Input System**
* **UGUI** for the menus and the HUD
* **GitHub** for version control

## Running it
1. Open the project in **Unity 6000.3.22f1**.
2. Open `Assets/DataHeist/Scenes/MainMenu` and press **Play**.

## More
* **How the code works:** `Assets/DataHeist/README_DataHeist.md` explains every script.
* **Project layout and conventions:** `DEVELOPER_GUIDE.md`.
* **Audio credits:** `Assets/DataHeist/Art/Audio/CREDITS.txt` (all sounds and music are CC0).
