using System;
using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// Editor tool: builds the four scenes (MainMenu, Shop, Options, Level1) and places every object of Level 1
// from the original layout file. Run it from the menu: Tools > Build Simple Game Scenes.
public static class SimpleSceneBuilder
{
    const string Art = "Assets/DataHeist/Art/";
    const string ScenesFolder = "Assets/DataHeist/Scenes/";
    const string PrefabsFolder = "Assets/DataHeist/Prefabs/";
    const int GroundLayer = 6;
    const int ObstacleLayer = 7;

    static Font font;
    static readonly Color ButtonColor = new Color(0.10f, 0.13f, 0.28f, 0.95f);
    static readonly Color Cyan = new Color(0.18f, 0.95f, 1f);
    static readonly Color Pink = new Color(1f, 0.25f, 0.65f);

    [Serializable] class LevelFile { public float exitX; public RoofData[] roofs; public EntityData[] entities; }
    [Serializable] class RoofData { public float x0, x1, y; public string style; }
    [Serializable] class EntityData { public string t, kind, move; public float x, y; }

    [MenuItem("Tools/Build Simple Game Scenes")]
    public static void Build()
    {
        SetupLayers();
        font = AssetDatabase.LoadAssetAtPath<Font>(Art + "Fonts/PressStart2P-Regular.ttf");
        Directory.CreateDirectory(ScenesFolder);
        Directory.CreateDirectory(PrefabsFolder);

        BuildMainMenu();
        BuildShop();
        BuildOptions();
        BuildLevel1();

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(ScenesFolder + "MainMenu.unity", true),
            new EditorBuildSettingsScene(ScenesFolder + "Level1.unity", true),
            new EditorBuildSettingsScene(ScenesFolder + "Shop.unity", true),
            new EditorBuildSettingsScene(ScenesFolder + "Options.unity", true),
        };
        AssetDatabase.SaveAssets();
        Debug.Log("[Builder] scenes built");
    }

    public static void BuildBatch()
    {
        try { Build(); EditorApplication.Exit(0); }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }

    // ------------------------------------------------------------------ menus
    static void BuildMainMenu()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        MakeCamera();
        MakeEventSystem();
        MakeMusic("music_menu.ogg");
        var buttons = new GameObject("ButtonManager").AddComponent<ButtonManagerScript>();
        var canvas = MakeCanvas("Canvas");
        MakeMenuBackground(canvas);
        var title = MakeText(canvas, "Title", "THE DATA HEIST", 48, new Vector2(0.5f, 1f), new Vector2(0, -150), new Vector2(1200, 80), Cyan);
        title.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(4, -4);

        AddLoadScene(MakeButton(canvas, "PlayButton", "PLAY", new Vector2(0, 60)), buttons, "Level1");
        AddLoadScene(MakeButton(canvas, "ShopButton", "SHOP", new Vector2(0, -20)), buttons, "Shop");
        AddLoadScene(MakeButton(canvas, "OptionsButton", "OPTIONS", new Vector2(0, -100)), buttons, "Options");
        UnityEventTools.AddPersistentListener(MakeButton(canvas, "ExitButton", "EXIT", new Vector2(0, -180)).onClick,
            new UnityAction(buttons.QuitGame));
        EditorSceneManager.SaveScene(scene, ScenesFolder + "MainMenu.unity");
    }

    static void BuildShop()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        MakeCamera();
        MakeEventSystem();
        MakeMusic("music_menu.ogg");
        var buttons = new GameObject("ButtonManager").AddComponent<ButtonManagerScript>();
        var canvas = MakeCanvas("Canvas");
        MakeMenuBackground(canvas);
        MakeText(canvas, "Title", "SHOP", 40, new Vector2(0.5f, 1f), new Vector2(0, -110), new Vector2(1000, 60), Cyan);

        var shop = new GameObject("ShopMenu").AddComponent<ShopMenu>();
        shop.soundEffects = shop.gameObject.AddComponent<AudioSource>();
        shop.soundEffects.playOnAwake = false;
        shop.buySound = Clip("SFX/sfx_ui_select.wav");
        shop.coinsText = MakeText(canvas, "CoinsText", "Coins: 0", 24, new Vector2(0.5f, 0.5f), new Vector2(0, 150), new Vector2(900, 40), Color.white);
        MakeText(canvas, "ItemText", "Extra Heart: +1 health at the start of each run", 16, new Vector2(0.5f, 0.5f), new Vector2(0, 70), new Vector2(1100, 40), Color.white);
        MakeText(canvas, "PriceText", "Price: 100 coins", 16, new Vector2(0.5f, 0.5f), new Vector2(0, 35), new Vector2(900, 30), Pink);
        var buy = MakeButton(canvas, "BuyButton", "BUY", new Vector2(0, -30));
        UnityEventTools.AddPersistentListener(buy.onClick, new UnityAction(shop.BuyExtraHeart));
        shop.heartsText = MakeText(canvas, "HeartsText", "Extra hearts: 0 / 2", 16, new Vector2(0.5f, 0.5f), new Vector2(0, -95), new Vector2(900, 30), Color.white);
        shop.messageText = MakeText(canvas, "MessageText", "", 16, new Vector2(0.5f, 0.5f), new Vector2(0, -135), new Vector2(1100, 30), Cyan);
        AddLoadScene(MakeButton(canvas, "BackButton", "BACK", new Vector2(0, -220)), buttons, "MainMenu");
        EditorSceneManager.SaveScene(scene, ScenesFolder + "Shop.unity");
    }

    static void BuildOptions()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        MakeCamera();
        MakeEventSystem();
        var music = MakeMusic("music_menu.ogg");
        var buttons = new GameObject("ButtonManager").AddComponent<ButtonManagerScript>();
        var canvas = MakeCanvas("Canvas");
        MakeMenuBackground(canvas);
        MakeText(canvas, "Title", "OPTIONS", 40, new Vector2(0.5f, 1f), new Vector2(0, -110), new Vector2(1000, 60), Cyan);

        var options = new GameObject("OptionsMenu").AddComponent<OptionsMenu>();
        options.music = music;
        options.soundPreview = options.gameObject.AddComponent<AudioSource>();
        options.soundPreview.playOnAwake = false;
        options.previewSound = Clip("SFX/sfx_byte.wav");

        MakeText(canvas, "MusicLabel", "Music Volume", 20, new Vector2(0.5f, 0.5f), new Vector2(0, 130), new Vector2(800, 40), Color.white);
        options.musicSlider = MakeSlider(canvas, "MusicSlider", new Vector2(0, 85));
        UnityEventTools.AddPersistentListener(options.musicSlider.onValueChanged, new UnityAction<float>(options.SetMusicVolume));
        MakeText(canvas, "SoundLabel", "Sound Volume", 20, new Vector2(0.5f, 0.5f), new Vector2(0, 10), new Vector2(800, 40), Color.white);
        options.soundSlider = MakeSlider(canvas, "SoundSlider", new Vector2(0, -35));
        UnityEventTools.AddPersistentListener(options.soundSlider.onValueChanged, new UnityAction<float>(options.SetSoundVolume));
        AddLoadScene(MakeButton(canvas, "BackButton", "BACK", new Vector2(0, -180)), buttons, "MainMenu");
        EditorSceneManager.SaveScene(scene, ScenesFolder + "Options.unity");
    }

    // ------------------------------------------------------------------ level 1
    static void BuildLevel1()
    {
        var data = JsonUtility.FromJson<LevelFile>(File.ReadAllText(Art + "Levels/level01.json"));
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var noFriction = NoFrictionMaterial();
        var bolt = MakeBoltPrefab();

        // rooftops (ground)
        var rooftops = new GameObject("Rooftops").transform;
        foreach (var r in data.roofs)
        {
            float width = r.x1 - r.x0;
            var roof = new GameObject("Rooftop");
            roof.layer = GroundLayer;
            roof.transform.SetParent(rooftops);
            roof.transform.position = new Vector3(r.x0, r.y, 0);
            var col = roof.AddComponent<BoxCollider2D>();
            col.size = new Vector2(width, 16f);
            col.offset = new Vector2(width / 2f, -8f);
            TiledSprite(roof.transform, "Wall", Sprite("Tiles/facade_" + r.style + "_0"), new Vector2(0, -14f / 32f), new Vector2(width, 16f), -10);
            TiledSprite(roof.transform, "Top", Sprite("Tiles/rooftop_" + r.style), Vector2.zero, new Vector2(width, 0.5f), -5);
        }

        var coins = new GameObject("Coins").transform;
        var obstacles = new GameObject("Obstacles").transform;
        var enemies = new GameObject("Enemies").transform;
        Transform player = MakePlayer(noFriction);

        foreach (var e in data.entities)
        {
            float ground = RoofTop(data, e.x);
            bool tooHigh = e.y - ground > 3.8f;     // only reachable with the removed jump pads
            switch (e.t)
            {
                case "byte":
                    if (tooHigh) break;
                    var coin = MakeSpriteObject("Coin", coins, new Vector2(e.x, e.y), Sprite("Pickups/byte_00"), 5);
                    coin.AddComponent<CircleCollider2D>().isTrigger = true;
                    coin.GetComponent<CircleCollider2D>().radius = 0.35f;
                    coin.AddComponent<Coin>();
                    Animate(coin, Frames("Pickups/byte_", 6), 10);
                    break;
                case "can":
                    if (tooHigh) break;
                    var can = MakeSpriteObject("TrashCan", obstacles, new Vector2(e.x, e.y), Sprite("Obstacles/garbage_can"), 4);
                    can.layer = ObstacleLayer;
                    AddTriggerBox(can, new Vector2(0.6f, 0.8f), new Vector2(0, 0.4f));
                    can.AddComponent<Obstacle>();
                    break;
                case "barrier":
                    var barrier = MakeSpriteObject("Barrier", obstacles, new Vector2(e.x, e.y), Sprite("Obstacles/barrier"), 4);
                    barrier.layer = ObstacleLayer;
                    AddTriggerBox(barrier, new Vector2(1.6f, 2.5f), new Vector2(0, 2.0f));
                    barrier.AddComponent<Obstacle>();
                    break;
                case "drone":
                    if (tooHigh) break;
                    var drone = MakeSpriteObject("ScoutDrone", enemies, new Vector2(e.x, e.y), Sprite("Enemies/drone_scout_00"), 6);
                    AddTriggerBox(drone, new Vector2(0.8f, 0.6f), Vector2.zero);
                    drone.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
                    var droneAI = drone.AddComponent<EnemyController>();
                    droneAI.enemyType = EnemyType.ScoutDrone;
                    droneAI.patrolDistance = e.move == "still" ? 0f : 1f;
                    droneAI.bobHeight = e.move == "still" ? 0.05f : 0.15f;
                    Animate(drone, Frames("Enemies/drone_scout_", 4), 8);
                    break;
                case "gunner":
                    var gunner = MakeSpriteObject("Gunner", enemies, new Vector2(e.x, e.y), Sprite("Enemies/drone_gunner_00"), 6);
                    AddTriggerBox(gunner, new Vector2(0.8f, 0.6f), Vector2.zero);
                    gunner.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
                    var gun = gunner.AddComponent<EnemyController>();
                    gun.enemyType = EnemyType.Gunner;
                    gun.projectilePrefab = bolt;
                    gun.shootSound = Clip("SFX/sfx_shoot.wav");
                    var firePoint = new GameObject("FirePoint").transform;
                    firePoint.SetParent(gunner.transform);
                    firePoint.position = new Vector3(e.x - 0.5f, ground + 0.8f, 0);   // chest height of a standing player
                    gun.firePoint = firePoint;
                    Animate(gunner, Frames("Enemies/drone_gunner_", 4), 8);
                    break;
                case "shard":
                    var shard = MakeSpriteObject("DataShard", null, new Vector2(e.x, e.y), Sprite("Pickups/shard_00"), 6);
                    AddTriggerBox(shard, new Vector2(1f, 3.5f), new Vector2(0, ground + 1.75f - e.y));   // tall, so it can't be jumped over
                    shard.AddComponent<DataShard>();
                    Animate(shard, Frames("Pickups/shard_", 4), 6);
                    break;
            }
        }

        // finish line
        float exitGround = RoofTop(data, data.exitX);
        var finish = MakeSpriteObject("FinishLine", null, new Vector2(data.exitX, exitGround), Sprite("Obstacles/extraction_pad"), -4);

        // camera with a static background
        var cam = MakeCamera();
        cam.transform.position = new Vector3(4, 2, -10);
        var follow = cam.gameObject.AddComponent<CameraFollow>();
        follow.target = player;
        CameraBackground(cam.transform);

        // music and game manager
        MakeMusic("music_level01.ogg");
        var manager = new GameObject("GameManager").AddComponent<GameManager>();
        manager.player = player;
        manager.finishLine = finish.transform;
        manager.soundEffects = manager.gameObject.AddComponent<AudioSource>();
        manager.soundEffects.playOnAwake = false;
        manager.levelName = "Level1";
        manager.coinSound = Clip("SFX/sfx_byte.wav");
        manager.dataShardSound = Clip("SFX/sfx_shard.wav");
        manager.gameOverSound = Clip("SFX/sfx_game_over.wav");
        manager.levelCompleteSound = Clip("SFX/sfx_level_complete.wav");

        manager.uiManager = MakeHud(manager, player.GetComponent<PlayerHealth>());
        MakeEventSystem();
        EditorSceneManager.SaveScene(scene, ScenesFolder + "Level1.unity");
    }

    static Transform MakePlayer(PhysicsMaterial2D material)
    {
        var go = new GameObject("Player");
        go.tag = "Player";
        go.transform.position = new Vector3(0f, 0.05f, 0f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Sprite("Characters/kai/kai_run_00");
        sr.sortingOrder = 10;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 55f / Mathf.Abs(Physics2D.gravity.y);     // the original game's gravity (55)
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        var box = go.AddComponent<BoxCollider2D>();
        box.size = new Vector2(0.55f, 1.1f);
        box.offset = new Vector2(0f, 0.55f);
        box.sharedMaterial = material;

        var groundCheck = new GameObject("GroundCheck").transform;
        groundCheck.SetParent(go.transform, false);

        var controller = go.AddComponent<PlayerController>();
        controller.groundCheck = groundCheck;
        controller.groundLayer = 1 << GroundLayer;
        controller.ceilingLayers = (1 << GroundLayer) | (1 << ObstacleLayer);
        controller.runFrames = Frames("Characters/kai/kai_run_", 8);
        controller.jumpFrames = Frames("Characters/kai/kai_jump_", 2);
        controller.fallFrames = Frames("Characters/kai/kai_fall_", 2);
        controller.slideFrames = Frames("Characters/kai/kai_slide_", 2);
        controller.attackFrames = Frames("Characters/kai/kai_attack_", 4);
        controller.jumpSound = Clip("SFX/sfx_jump.wav");
        controller.doubleJumpSound = Clip("SFX/sfx_air_jump.wav");
        controller.slideSound = Clip("SFX/sfx_slide.wav");

        var attack = go.AddComponent<PlayerAttack>();
        attack.attackSound = Clip("SFX/sfx_attack.wav");
        attack.enemyDestroyedSound = Clip("SFX/sfx_explode.wav");
        var health = go.AddComponent<PlayerHealth>();
        health.hurtSound = Clip("SFX/sfx_hurt.wav");
        return go.transform;
    }

    static GameObject MakeBoltPrefab()
    {
        var go = new GameObject("Bolt");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Sprite("Enemies/bolt_00");
        sr.sortingOrder = 7;
        AddTriggerBox(go, new Vector2(0.5f, 0.25f), Vector2.zero);
        go.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
        go.AddComponent<Projectile>();
        Animate(go, Frames("Enemies/bolt_", 2), 12);
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabsFolder + "Bolt.prefab");
        UnityEngine.Object.DestroyImmediate(go);
        return prefab;
    }

    static UIManager MakeHud(GameManager manager, PlayerHealth health)
    {
        var canvas = MakeCanvas("HUD");
        var ui = canvas.gameObject.AddComponent<UIManager>();
        ui.playerHealth = health;
        ui.statusText = MakeText(canvas, "StatusText", "Coins: 0     Health: 3     Data Shard: 0/1", 20,
            new Vector2(0.5f, 1f), new Vector2(0, -36), new Vector2(1240, 40), Color.white);
        ui.statusText.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.9f);

        // pause
        ui.pausePanel = MakePanel(canvas, "PausePanel", out var pause);
        MakeText(pause, "Title", "PAUSED", 36, new Vector2(0.5f, 0.5f), new Vector2(0, 110), new Vector2(560, 60), Cyan);
        UnityEventTools.AddPersistentListener(MakeButton(pause, "ResumeButton", "RESUME", new Vector2(0, 10)).onClick, new UnityAction(manager.ResumeGame));
        UnityEventTools.AddPersistentListener(MakeButton(pause, "MainMenuButton", "MAIN MENU", new Vector2(0, -80)).onClick, new UnityAction(manager.LoadMainMenu));

        // game over
        ui.gameOverPanel = MakePanel(canvas, "GameOverPanel", out var over);
        MakeText(over, "Title", "GAME OVER", 36, new Vector2(0.5f, 0.5f), new Vector2(0, 130), new Vector2(560, 60), Pink);
        ui.gameOverReasonText = MakeText(over, "ReasonText", "", 16, new Vector2(0.5f, 0.5f), new Vector2(0, 70), new Vector2(560, 30), Color.white);
        ui.gameOverCoinsText = MakeText(over, "CoinsText", "", 20, new Vector2(0.5f, 0.5f), new Vector2(0, 25), new Vector2(560, 30), Color.white);
        UnityEventTools.AddPersistentListener(MakeButton(over, "RetryButton", "RETRY", new Vector2(0, -55)).onClick, new UnityAction(manager.RetryLevel));
        UnityEventTools.AddPersistentListener(MakeButton(over, "MainMenuButton", "MAIN MENU", new Vector2(0, -135)).onClick, new UnityAction(manager.LoadMainMenu));

        // level complete
        ui.levelCompletePanel = MakePanel(canvas, "LevelCompletePanel", out var done);
        MakeText(done, "Title", "LEVEL COMPLETE", 32, new Vector2(0.5f, 0.5f), new Vector2(0, 140), new Vector2(560, 60), Cyan);
        ui.levelCompleteCoinsText = MakeText(done, "CoinsText", "", 18, new Vector2(0.5f, 0.5f), new Vector2(0, 85), new Vector2(560, 30), Color.white);
        ui.levelCompleteTotalText = MakeText(done, "TotalText", "", 18, new Vector2(0.5f, 0.5f), new Vector2(0, 50), new Vector2(560, 30), Color.white);
        ui.levelCompleteShardText = MakeText(done, "ShardText", "", 18, new Vector2(0.5f, 0.5f), new Vector2(0, 15), new Vector2(560, 30), Pink);
        var next = MakeButton(done, "NextLevelButton", "NEXT LEVEL", new Vector2(0, -55));
        UnityEventTools.AddPersistentListener(next.onClick, new UnityAction(manager.LoadNextLevel));
        ui.nextLevelButton = next.gameObject;
        UnityEventTools.AddPersistentListener(MakeButton(done, "MainMenuButton", "MAIN MENU", new Vector2(0, -135)).onClick, new UnityAction(manager.LoadMainMenu));
        return ui;
    }

    // ------------------------------------------------------------------ small helpers
    static float RoofTop(LevelFile data, float x)
    {
        float best = float.NegativeInfinity;
        foreach (var r in data.roofs)
            if (x >= r.x0 - 4f && x <= r.x1 + 4f) best = Mathf.Max(best, (x >= r.x0 && x <= r.x1) ? r.y : r.y - 0.001f);
        return float.IsNegativeInfinity(best) ? 0f : best;
    }

    static Sprite Sprite(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(Art + path + ".png");

    static Sprite[] Frames(string prefix, int count)
    {
        var frames = new Sprite[count];
        for (int i = 0; i < count; i++) frames[i] = Sprite(prefix + i.ToString("00"));
        return frames;
    }

    static AudioClip Clip(string path) => AssetDatabase.LoadAssetAtPath<AudioClip>(Art + "Audio/" + path);

    static GameObject MakeSpriteObject(string name, Transform parent, Vector2 position, Sprite sprite, int order)
    {
        var go = new GameObject(name);
        if (parent != null) go.transform.SetParent(parent);
        go.transform.position = position;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = order;
        return go;
    }

    static void TiledSprite(Transform parent, string name, Sprite sprite, Vector2 localPosition, Vector2 size, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = size;
        sr.sortingOrder = order;
    }

    static void AddTriggerBox(GameObject go, Vector2 size, Vector2 offset)
    {
        var box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = size;
        box.offset = offset;
    }

    static void Animate(GameObject go, Sprite[] frames, float fps)
    {
        var anim = go.AddComponent<SimpleAnimation>();
        anim.frames = frames;
        anim.framesPerSecond = fps;
    }

    static PhysicsMaterial2D NoFrictionMaterial()
    {
        const string path = "Assets/DataHeist/Prefabs/NoFriction.physicsMaterial2D";
        var mat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(path);
        if (mat == null)
        {
            mat = new PhysicsMaterial2D("NoFriction") { friction = 0f, bounciness = 0f };
            AssetDatabase.CreateAsset(mat, path);
        }
        return mat;
    }

    static Camera MakeCamera()
    {
        var go = new GameObject("Main Camera");
        go.tag = "MainCamera";
        go.transform.position = new Vector3(0, 0, -10);
        var cam = go.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5.625f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.06f, 0.04f, 0.14f);
        go.AddComponent<AudioListener>();
        return cam;
    }

    // Sky and city pictures that move with the camera, so the background stays still on screen
    static void CameraBackground(Transform cam)
    {
        float viewWidth = 5.625f * 2f * 16f / 9f;
        float bottom = -5.625f;
        AddBackgroundLayer(cam, "Sky", Sprite("Parallax/sky"), viewWidth, bottom, -100, true);
        AddBackgroundLayer(cam, "CityFar", Sprite("Parallax/city_far"), viewWidth, bottom, -90, false);
        AddBackgroundLayer(cam, "CityMid", Sprite("Parallax/city_mid"), viewWidth, bottom, -80, false);
    }

    static void AddBackgroundLayer(Transform cam, string name, Sprite sprite, float viewWidth, float bottom, int order, bool fill)
    {
        var go = new GameObject(name);
        go.transform.SetParent(cam, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = order;
        float scale = (viewWidth + 0.5f) / sprite.bounds.size.x;
        if (fill) scale = Mathf.Max(scale, 11.5f / sprite.bounds.size.y);
        go.transform.localScale = new Vector3(scale, scale, 1);
        // put the picture's bottom-left corner at the bottom-left of the view
        Vector3 min = sprite.bounds.min * scale;
        go.transform.localPosition = new Vector3(-viewWidth / 2f - 0.25f - min.x, bottom - min.y, 10f);
    }

    static AudioSource MakeMusic(string file)
    {
        var go = new GameObject("Music");
        var source = go.AddComponent<AudioSource>();
        source.clip = Clip("Music/" + file);
        source.loop = true;
        source.playOnAwake = true;
        go.AddComponent<MusicPlayer>();
        return source;
    }

    static void MakeEventSystem()
    {
        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<InputSystemUIInputModule>();
    }

    static Transform MakeCanvas(string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        return go.transform;
    }

    static void MakeMenuBackground(Transform canvas)
    {
        MakeImage(canvas, "Sky", Sprite("Parallax/sky"), Vector2.zero, Vector2.one, Vector2.zero, Color.white);
        MakeImage(canvas, "CityFar", Sprite("Parallax/city_far"), Vector2.zero, new Vector2(1, 0), new Vector2(0, 520), Color.white);
        MakeImage(canvas, "CityMid", Sprite("Parallax/city_mid"), Vector2.zero, new Vector2(1, 0), new Vector2(0, 440), new Color(0.75f, 0.75f, 0.85f));
    }

    static void MakeImage(Transform parent, string name, Sprite sprite, Vector2 anchorMin, Vector2 anchorMax, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = size;
        var image = go.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
    }

    static Text MakeText(Transform parent, string name, string text, int size, Vector2 anchor, Vector2 position, Vector2 box, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.anchoredPosition = position;
        rt.sizeDelta = box;
        var t = go.AddComponent<Text>();
        t.font = font;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }

    static Button MakeButton(Transform parent, string name, string label, Vector2 position)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = new Vector2(340, 60);
        var image = go.AddComponent<Image>();
        image.color = ButtonColor;
        go.AddComponent<Outline>().effectColor = Cyan;
        var button = go.AddComponent<Button>();
        var colors = button.colors;
        colors.highlightedColor = new Color(0.55f, 0.85f, 1f);
        colors.pressedColor = new Color(0.4f, 0.6f, 0.8f);
        colors.selectedColor = Color.white;
        button.colors = colors;
        MakeText(go.transform, "Text", label, 20, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(340, 60), Color.white);
        return button;
    }

    static void AddLoadScene(Button button, ButtonManagerScript buttons, string sceneName)
    {
        UnityEventTools.AddStringPersistentListener(button.onClick, new UnityAction<string>(buttons.LoadGameScene), sceneName);
    }

    static GameObject MakePanel(Transform canvas, string name, out Transform box)
    {
        var dim = new GameObject(name, typeof(RectTransform));
        dim.transform.SetParent(canvas, false);
        var rt = (RectTransform)dim.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        dim.AddComponent<Image>().color = new Color(0, 0, 0, 0.6f);

        var panel = new GameObject("Box", typeof(RectTransform));
        panel.transform.SetParent(dim.transform, false);
        var prt = (RectTransform)panel.transform;
        prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0.5f);
        prt.sizeDelta = new Vector2(620, 400);
        panel.AddComponent<Image>().color = new Color(0.07f, 0.06f, 0.16f, 0.96f);
        panel.AddComponent<Outline>().effectColor = Cyan;
        box = panel.transform;
        return dim;
    }

    static Slider MakeSlider(Transform parent, string name, Vector2 position)
    {
        var resources = new DefaultControls.Resources
        {
            standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
            background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
            knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"),
        };
        var go = DefaultControls.CreateSlider(resources);
        go.name = name;
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = new Vector2(460, 26);
        var slider = go.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0.7f;
        return slider;
    }

    // Adds the "Ground" and "Obstacle" layers to the project settings
    static void SetupLayers()
    {
        var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layers = tagManager.FindProperty("layers");
        layers.GetArrayElementAtIndex(GroundLayer).stringValue = "Ground";
        layers.GetArrayElementAtIndex(ObstacleLayer).stringValue = "Obstacle";
        tagManager.ApplyModifiedProperties();
    }
}
