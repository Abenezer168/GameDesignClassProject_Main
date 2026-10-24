# The Data Heist: How the Code Works

This guide explains every script in `Assets/DataHeist/Scripts/` in plain language.

## The big picture

* The **player** has a `Rigidbody2D`, so Unity's physics engine handles gravity and landing on the rooftops.
* Coins, obstacles, enemies, bullets and the Data Shard have **trigger colliders**. When the player touches one, Unity calls `OnTriggerEnter2D()` on that object, and the object decides what happens (add a coin, take damage, and so on).
* The **GameManager** keeps track of the run (coins, Data Shard, game over), and the **UIManager** shows it on screen.
* **SaveManager** stores progress between runs with `PlayerPrefs`.

## Player scripts

### PlayerController.cs
Runs, jumps, double jumps and slides.
* `FixedUpdate()` sets the horizontal speed to `moveSpeed` every physics step, so the player always runs to the right.
* `Update()` checks the ground with `Physics2D.OverlapBox` at the `groundCheck` point, then reads the keyboard:
  * **Space on the ground** (or within `coyoteTime`, 0.1 s after running off an edge) calls `Jump(jumpForce)`.
  * **Space in the air** jumps again only if `canDoubleJump` is true and the double jump hasn't been used since landing.
  * **Letting go of Space early** halves the upward speed, so a tap gives a small hop.
  * **S / Down Arrow on the ground** starts a slide. The collider is made shorter so the player fits under barriers and bullets. When `slideTime` runs out, `CanStandUp()` checks the space above, and the player only stands up if it's empty.
* **Double jump unlock:** `canDoubleJump` becomes true once `GameManager.instance.coinCount` reaches `coinsForDoubleJump` (30). It stays true for the rest of the run, and a new run starts with it false again.
* `UpdateAnimation()` picks the run, jump, fall, slide or attack frames and flips through them.

### PlayerAttack.cs
Press **J** or **left-click** to attack.
* An attack lasts `attackDuration` (0.25 s), and you have to wait `attackCooldown` (0.35 s) before the next one.
* While attacking, `HitEnemies()` uses `Physics2D.OverlapBoxAll` to find everything in a box in front of the player. Any object with an `EnemyController` is destroyed.

### PlayerHealth.cs
* Starts with `maxHealth` (3) plus any extra hearts bought in the Shop.
* `TakeDamage()` removes health, starts a `damageCooldown` of 1.25 s and makes the player blink. During the cooldown, more damage is ignored, so one collision only costs one health point.
* Falling below `fallLimit` or reaching 0 health calls `GameManager.instance.GameOver()`.

## Things in the level

### Coin.cs
On `OnTriggerEnter2D` with the player: `GameManager.instance.AddCoin()`, then the coin destroys itself. The `collected` bool makes sure a coin can never count twice.

### Obstacle.cs
Used by trash cans and barriers. Touching one calls `PlayerHealth.TakeDamage(1)`. Trash cans are short (jump over them), and a barrier's collider starts above the ground (slide under it).

### EnemyController.cs
One script for both enemy types, chosen with the `enemyType` setting:
* **ScoutDrone:** moves left and right around its start position with `Mathf.Sin`, and bobs up and down.
* **Gunner:** stays in place. While the player is between `minShootDistance` (6) and `shootRange` (15) in front of it, it fires a bullet every `shootInterval` (1.45 s) from its `firePoint`.
* Touching either one hurts the player. `Die()` (called by `PlayerAttack`) destroys it.

### Projectile.cs
The gunner's bullet. It moves left at `speed` each frame, hurts the player on touch and removes itself after `lifeTime` seconds. Bullets fly at chest height, so you can jump over them or slide under them.

### DataShard.cs
On touch: `GameManager.instance.CollectDataShard()`. Its collider is tall, so you collect it even while jumping.

## Game flow

### GameManager.cs
* `coinCount`, `hasDataShard` and `isGameOver` describe the current run.
* **Escape** pauses (`Time.timeScale = 0`) and resumes.
* When the player passes the `finishLine`, the level is complete if the Data Shard was collected, otherwise it's game over.
* `GameOver()` and `LevelComplete()` save this run's coins **once** with `SaveManager.AddCoins()`. `LevelComplete()` also saves the Data Shard.
* `RetryLevel()`, `LoadNextLevel()` and `LoadMainMenu()` are called by the buttons on the end screens.
* `PlaySound()` plays a sound effect at the volume chosen in Options.

### UIManager.cs
* Every frame it writes the one line at the top of the screen: `Coins: 25     Health: 3     Data Shard: 0/1`.
* `ShowPause()`, `ShowGameOver()` and `ShowLevelComplete()` turn the panels on and fill in their text.

### SaveManager.cs
A `static` class (no GameObject needed) with small functions around `PlayerPrefs`:
`GetTotalCoins / AddCoins / SpendCoins`, `HasDataShard / SaveDataShard`, `GetExtraHearts / AddExtraHeart`, and the music and sound volumes.

### CameraFollow.cs
Follows the player sideways with a `lookAhead` so you can see what's coming, and smoothly follows up and down. The sky and city pictures are children of the camera, so the background stays still.

## Menus

* **ButtonManagerScript.cs** (in `Assets/Scripts/`, written by our team): `LoadGameScene("SceneName")` loads a scene and `QuitGame()` exits. Every menu button uses it.
* **ShopMenu.cs:** shows the saved coins and sells an extra heart for 100 coins (maximum 2).
* **OptionsMenu.cs:** two sliders that save the music and sound volume. The music changes right away.
* **MusicPlayer.cs:** sets each scene's music to the saved volume.
* **SimpleAnimation.cs:** flips through a list of sprites. Used by coins, drones, gunners, bullets and the Data Shard.

## Numbers you can change in the Inspector

| Object | Field | Default | Effect |
|---|---|---|---|
| Player | `moveSpeed` | 7.5 | Running speed |
| Player | `jumpForce` | 15.5 | Height of a normal jump |
| Player | `doubleJumpForce` | 14 | Height of the double jump |
| Player | `coinsForDoubleJump` | 30 | Coins needed to unlock the double jump |
| Player | `maxHealth` | 3 | Starting health |
| Player | `damageCooldown` | 1.25 | Seconds of safety after a hit |
| Player | `attackRange` | 1.8 | How far the attack reaches |
| Gunner | `shootInterval` | 1.45 | Seconds between shots |
| ShopMenu | `extraHeartPrice` | 100 | Price of an extra heart |
