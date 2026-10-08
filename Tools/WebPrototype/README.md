# Web prototype of Level 1

Open `the-data-heist.html` in any browser to play. It is self-contained.

- `sim.js`: the gameplay rules. They match `Assets/DataHeist/Scripts/Gameplay/RunnerSim.cs` 1:1.
- `game.js` and `shell.html`: the renderer, input, audio and screens.
- `bot.js`: the headless playtest bot. Run `node bot.js ../../Assets/DataHeist/Resources/DataHeist/Levels/level01.json`.
