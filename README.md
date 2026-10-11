# The Data Heist: Cyberpunk 2D Autorunner

This is a Geometry Dash-style 2D autorunner we're building for our Game Design and Development class. You play a crew of runners in Sector 7, 2089, a city owned by Halcyon Corp. Over three missions you steal Halcyon's Data Shards, break into its vault, beat its security Guardian and leak the evidence that sets the city free.

## What we've built so far:
* **The Look:** Pixel-art cyberpunk cities (rooftops, glass towers and an underground vault) with parallax backgrounds, plus an HD key-art splash screen.
* **Menus & Screens:** A key-art splash with PRESS START, a real loading screen, and a home page that plays a recorded run of Level 1 behind the menu (PLAY, STORY, SHOP, OPTIONS, QUIT). There's also Character Select with a mission picker, the Shop, Options (audio, video, key rebinding, credits), results, game over, a Mission Complete screen and scrolling credits.
* **UI & Navigation:** Keyboard, mouse and gamepad work everywhere, and screens fade into each other. The team's original Options, Shop, Game Screen and Game Over scenes now open the new screens.
* **The full campaign:** a story intro, three levels, a boss fight, a timed escape and a cinematic ending (details below).

## Built With:
* **Unity 6.3** (Version 6000.3.22f1, URP 2D). Stick to this exact version if you're pulling from our repo!
* **C#**, the **Input System** (rebindable keys, gamepad) and **UGUI + TextMesh Pro**
* **Python** scripts in `Tools/DataHeistGenerators` that generate the pixel art, level files and audio mix
* **GitHub** for version control

## How to play
| Action | Keyboard / Mouse | Gamepad |
|---|---|---|
| Jump (hold for higher) | Space, ↑ | South (A / ✕) |
| Slide (in mid-air it dives into a slide) | S, ↓ | East (B / ○) |
| Attack (slash drones and bolts) | Left click, J | West (X / □) |
| Runner ability | W, Left Shift | North (Y / △) |
| Pause | Esc, P | Start |

All keys can be changed in **Pause > Controls** or **Options > Controls**. You win a level by reaching the extraction point, and you lose when your health hits zero or you fall. After a loss you can **retry from the last checkpoint**, restart or quit. Data Bytes you collected are always kept.

## Story intro
On first launch a six-shot cinematic story plays: the Sector 7 skyline, the Halcyon tower, the Data Shard vault, Kai and the job offer, the crew line-up and the rooftop run. The shards hold the proof of everything Halcyon has buried, so the crew's real job is to steal the truth and leak it. You can replay the intro any time from **STORY** on the home page. Between levels, each level's first countdown shows a one-line **mission briefing** that moves the story on: Shard 1 exposes a payroll of bribed officials, which leads to the Corporate District, and the shards then unlock Halcyon's archive vault.

## Level 1 + Runners (Data Heist update)
* **Level 1 "Neon Rooftops"** (580 m, about 75 s): a full auto-runner level that teaches jump, slide, attack and the runner's ability. It has gaps, billboards, Security Drones, Gunner drones, lasers, jump pads, a catwalk bonus route, 2 checkpoints, an alarm-zone finale, the Data Shard and the extraction point.
* **4 runners** (2 men, 2 women), each with a unique W ability: Kai (Kick Thrusters), Nova (Phase Dash), Jax (EMP Pulse) and Lin (Time Glitch). Runners unlock by clearing a level or by spending Data Bytes in the Shop.
* **The game loop:** Character Select → countdown → HUD (health, Data Bytes, Data Shard, progress, ability cooldown, controls) → pause with volume and **key rebinding** → game over with checkpoint retry → results with S/A/B/C rank, save data, unlocks and **NEXT LEVEL**.

## Level 2: "Corporate District"
* **644 m, about 85 s**, across glass corporate towers. It unlocks when Level 1 is cleared.
* **Sniper:** a penthouse window paints a red laser sight at chest height, then fires a fast tracer. Slide, jump or slash the round.
* **Live wires:** electric floor strips that are live 0.9 s of every 1.5 s. Jump them or cross while they're dark.
* **Falling signs:** Halcyon billboards shake, spark and crash onto the roof. Jump the wreck or slash it.
* It ends with a **Lockdown** section at 112% speed with every hazard together. There are 6 snipers, 7 live floors, 5 signs and 2 checkpoints.

## Level 3: "The Underground Data Vault" (final mission)
* **702 m**, in server halls under Halcyon's archive, with its own music for the infiltration, the boss and the escape.
* **New hazards:** security turrets (warning line, then chest or ankle shots), cycling laser barriers, live floors that crackle before turning on, crushers, falling machinery and patrolling security drones.
* **Data fragments:** collect all 3 to open the vault door. If you miss one, the retry sends you back to before the first one you missed, so you can never get stuck.
* **Boss: the Corporate Security Guardian.** It cycles a laser, an orb volley (low, chest, low) and a ground slam (jump the shockwave). **The only way to hurt it is to slash its lasers and orbs back into it.** It has 9 health over 3 phases, and each phase is faster.
* **Lockdown escape:** grabbing the evidence starts a 34-second countdown. Every hit costs 3 seconds.

## The ending
Clearing Level 3 plays a six-chapter cinematic ending: **The Truth Is Revealed** (the leaked files appear on every screen and Halcyon's drones fall), **The City Transforms** (before and after), **The Hero Returns** (a playable walk through the cheering city), **The Award Ceremony** (the mayor gives you the **Guardian of the City** award), **The Statue** (a statue of *your* runner) and **The City Is Free** (fireworks). Then come the **Mission Complete** screen with your campaign stats, and the credits. The home page then shows **MISSION REPORT**, so you can replay the ending any time.

## Audio
All sound is real CC0 (public-domain) audio: sound effects mixed from Kenney packs and music from OpenGameArt.org, with a different track for the menus, the story, each level, the boss, the escape and the ending. Music crossfades so two tracks never overlap. Volume sliders are in **Options > Audio** and in the pause menu. Full credits are in **Options > Credits**.

## Running it in Unity
1. Open the project in **Unity 6000.3.22f1** and wait for the import.
2. Run **Data Heist > Setup Project** once (pixel-art import settings, runner assets, Build Settings).
3. Open `Assets/DataHeist/Scenes/MainMenu` and press Play, or jump straight in with **Data Heist > Play Level 1 / 2 / 3 / Play Story / Play Ending**.
4. **Debug:** **Data Heist > Debug > Unlock All Runners, Levels + 1000 Bytes** and **Reset Save Data**.

## Testing
* **Data Heist > Validate Level 1 / 2 / 3 (Playtest Bot):** searches inputs through the real game simulation and proves every runner can clear each level with zero hits and no abilities.
* **Data Heist > Debug > Smoke Test:** plays through the whole game muted (title, options, story, all three levels, the boss and the ending), fails on any console error and saves screenshots to `Logs/Screens/`.

## More
* **Full design notes:** `Assets/DataHeist/README_DataHeist.md` (runner stats, level sections, hazards, rank rules, audio list, architecture).
* **Browser build:** `Tools/WebPrototype/the-data-heist.html` is a playable browser prototype of **Level 1**. Levels 2 and 3 are Unity-only for now.
* **Still to do:** levels for Data Shards 4 and 5, Levels 2 and 3 in the browser build, and our team's names in `Assets/DataHeist/Resources/DataHeist/Credits/team.txt`.
