using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DataHeist
{
    /// <summary>The six chapters of the ending. Each builds its own set, directs it, and returns.</summary>
    public partial class EndingController
    {
        // ================================================================== 1. THE TRUTH IS REVEALED
        IEnumerator TruthRevealed()
        {
            const float X = 0f;
            NewStage("Truth");
            _ui.SetChapter("CHAPTER 1  //  THE TRUTH IS REVEALED");
            Music("music_truth");
            _parallax.SetTint(OppressedSky, OppressedCity);
            _parallax.SetRain(true);
            Rooftop(X - 30, 70);

            // the Halcyon tower and two office blocks whose facades carry giant screens
            var tower = Prop("Story/tower", X + 7f, -5f, DH.SortNear + 2);
            var beacon = Prop("FX/glow", X + 7f, 8.85f, DH.SortNear + 3, 0.9f);
            beacon.color = DH.Red;
            var screens = new List<SpriteRenderer>
            {
                Building(X - 6f, 4.8f, 7.5f, true),
                Building(X + 15f, 5.2f, 8.2f, true),
            };
            var towerScreen = Prop("Ending/screen_off", X + 7f, 2.4f, DH.SortNear + 5);
            screens.Add(towerScreen);
            var searchlights = new List<SpriteRenderer>();
            foreach (var bx in new[] { X - 12f, X + 22f })
            {
                var s = Prop("Story/searchlight", bx, 0.5f, DH.SortFar + 1);
                s.transform.rotation = Quaternion.Euler(0, 0, 180f);
                s.color = new Color(DH.Red.r, DH.Red.g, DH.Red.b, 0.2f);
                searchlights.Add(s);
            }
            var drones = new List<SpriteRenderer> { Drone(X - 2f, 6.2f, true), Drone(X + 4f, 5.0f, true), Drone(X + 11f, 6.6f, true) };

            // people on the rooftop, looking up; the hero among them
            var crowd = new List<Actor> { Npc("c1", X - 5f), Npc("c4", X - 3.6f), Npc("k1", X - 2.8f), Npc("c6", X - 1.4f, false), Npc("c2", X + 0.4f) };
            var hero = Hero(X - 8.5f);
            hero.FaceRight(true);

            SetCam(X - 2f, GroundCamY);
            yield return _ui.Fade(0f, 0.8f);
            PanAsync(X + 3f, GroundCamY + 0.8f, 18f);
            yield return _ui.Say("Midnight, Sector 7. Every screen in the city flickered at once.");

            // the leak goes live on every screen
            AudioManager.I.Sfx("screens_on", 0.9f);
            var evidence = SpriteLibrary.Anim("Ending/evidence");
            for (int f = 0; f < 6; f++)
            {
                foreach (var s in screens) s.sprite = f % 2 == 0 && evidence.Length > 0 ? evidence[0] : SpriteLibrary.Get("Ending/screen_off");
                yield return Wait(0.08f);
            }
            foreach (var s in screens)
            {
                var anim = s.gameObject.AddComponent<SpriteAnimator>();
                anim.unscaled = false;
                anim.timeOffset = s.transform.position.x;
                anim.Play(evidence, 1.2f);
            }
            foreach (var a in crowd) a.PlayFor("talk", 3f, "idle");
            _ui.Bubble("Is that... Halcyon?", () => crowd[0].transform.position + Vector3.up * 1.6f, 3f);
            yield return Wait(0.6f);
            _ui.Bubble("They were spying on all of us!", () => crowd[3].transform.position + Vector3.up * 1.6f, 3f, DH.Pink);
            yield return _ui.Say("The Halcyon files. Every bribe, every hidden camera, every buried secret, out in the open.");

            // surveillance shuts down: beams out, drones fall dark, the beacon and searchlights die
            AudioManager.I.Sfx("power_down", 1f);
            StartCoroutine(Retint(OppressedSky, OppressedCity, FreeSky * 0.85f, FreeCity * 0.9f, 3f));
            foreach (var d in drones)
            {
                foreach (Transform c in d.transform) c.gameObject.SetActive(false);
                var anim = d.GetComponent<SpriteAnimator>();
                if (anim != null) anim.enabled = false;
                d.color = new Color(0.45f, 0.45f, 0.55f);
            }
            float fall = 0;
            while (fall < 2.2f)
            {
                fall += Time.deltaTime;
                for (int i = 0; i < drones.Count; i++)
                {
                    var d = drones[i];
                    d.transform.position += new Vector3(Mathf.Sin(fall * 3 + i) * 0.4f, -1.6f - i * 0.4f, 0) * Time.deltaTime;
                    d.transform.rotation = Quaternion.Euler(0, 0, fall * (i % 2 == 0 ? 25f : -30f));
                }
                beacon.color = new Color(DH.Red.r, DH.Red.g, DH.Red.b, Mathf.Clamp01(1f - fall / 1.2f) * (Mathf.Repeat(fall * 8, 1) < 0.5f ? 1 : 0.3f));
                foreach (var s in searchlights) s.color = new Color(DH.Red.r, DH.Red.g, DH.Red.b, 0.2f * Mathf.Clamp01(1f - fall / 1.6f));
                yield return null;
            }
            foreach (var a in crowd) a.PlayFor("cheer", 3f, "idle");
            hero.PlayFor("win", 3f, "idle");
            yield return _ui.Say("One by one, Halcyon's drones went dark.");
            yield return _ui.Title("The truth can no longer be hidden.", null, DH.Ink, 3f, 12);
        }

        // ================================================================== 2. THE CITY TRANSFORMS
        IEnumerator CityTransforms()
        {
            const float X = 300f;
            NewStage("Transform");
            _ui.SetChapter("CHAPTER 2  //  BEFORE");
            Music("music_truth");
            _parallax.SetTint(OppressedSky, OppressedCity);
            Street(X - 30, 60);

            // BEFORE: shutters down, red lamps, a drone patrol, an oppressive billboard, people hurrying home
            var shops = new List<(SpriteRenderer sr, int v)>();
            float[] shopX = { X - 12.5f, X - 1.5f, X + 9.5f };
            for (int i = 0; i < 3; i++) shops.Add((Prop($"Ending/shop_closed_{i}", shopX[i], 0.06f, DH.SortDecor), i));
            var lamps = new List<SpriteRenderer>();
            foreach (var lx in new[] { X - 17f, X - 7f, X + 4f, X + 15f }) lamps.Add(Prop("Ending/lamp_red", lx, 0.06f, DH.SortDecor + 2));
            var ad = Prop("Ending/ad_obey", X + 19.5f, 0.06f, DH.SortDecor + 1);
            var drones = new List<SpriteRenderer> { Drone(X - 8f, 5.2f, true), Drone(X + 6f, 6.0f, true) };
            var hurried = new List<Actor> { Npc("c3", X - 14f), Npc("c5", X + 12f, false), Npc("c8", X - 4f) };
            foreach (var a in hurried) { a.SetTint(new Color(0.55f, 0.45f, 0.55f)); a.WalkTo(a.X + (a.X < X ? 30f : -30f), 3.4f); }

            SetCam(X, GroundCamY);
            yield return _ui.Fade(0f, 0.8f);
            float patrol = 0;
            IEnumerator Patrol(float seconds)
            {
                for (float t = 0; t < seconds; t += Time.deltaTime)
                {
                    patrol += Time.deltaTime;
                    for (int i = 0; i < drones.Count; i++)
                        drones[i].transform.position = new Vector3(SR.Snap(X + (i == 0 ? -8f : 6f) + Mathf.Sin(patrol * 0.6f + i * 2) * 4f), drones[i].transform.position.y, 0);
                    yield return null;
                }
            }
            yield return Patrol(0.5f);
            yield return _ui.Say("Under Halcyon, the streets emptied at sunset. Shutters down. Eyes in the sky.", hold: 1.6f);
            yield return Patrol(0.6f);

            // the transformation: a wave of light sweeps the street and everything it touches changes
            _ui.SetChapter("CHAPTER 2  //  AFTER");
            AudioManager.I.Sfx("transform", 1f);
            Music("music_hope");
            var wave = Prop("FX/glow", X - 22f, 4f, DH.SortFX + 1);
            wave.transform.localScale = new Vector3(2.2f, 18f, 1);
            wave.color = new Color(0.75f, 1f, 1f, 0.75f);
            StartCoroutine(Retint(OppressedSky, OppressedCity, FreeSky, FreeCity, 3f));
            var done = new HashSet<Object>();
            for (float t = 0; t < 3f; t += Time.deltaTime)
            {
                float wx = Mathf.Lerp(X - 22f, X + 22f, t / 3f);
                wave.transform.position = new Vector3(wx, 4f, 0);
                foreach (var (sr, v) in shops)
                    if (sr.transform.position.x < wx && done.Add(sr)) { sr.sprite = SpriteLibrary.Get($"Ending/shop_open_{v}"); }
                foreach (var l in lamps)
                    if (l.transform.position.x < wx && done.Add(l)) l.sprite = SpriteLibrary.Get("Ending/lamp_warm");
                if (ad.transform.position.x < wx && done.Add(ad)) ad.sprite = SpriteLibrary.Get("Ending/ad_free");
                foreach (var d in drones)
                    if (d.transform.position.x < wx && done.Add(d)) foreach (Transform c in d.transform) c.gameObject.SetActive(false);
                foreach (var d in drones)
                    if (done.Contains(d)) { d.transform.position += new Vector3(1.5f, 4f, 0) * Time.deltaTime; d.color = new Color(1, 1, 1, Mathf.Max(0, d.color.a - Time.deltaTime)); }
                foreach (var a in hurried) if (a.X < wx) { a.SetTint(Color.white); a.speed = 2.0f; }
                yield return null;
            }
            Destroy(wave.gameObject);

            // AFTER: decorations, families, a tram, flying cars
            for (float bx = X - 16f; bx < X + 16f; bx += 2f)
            {
                Prop("Ending/bunting", bx, 4.4f, DH.SortDecor + 3);
                var l = Prop("Ending/lights", bx, 3.4f, DH.SortDecor + 3);
            }
            var walkers = new List<Actor>
            {
                Npc("c2", X - 12f), Npc("k1", X - 11f), Npc("c7", X - 15f), Npc("c4", X + 12f, false), Npc("k2", X + 13.2f, false),
                Npc("c1", X + 16f, false), Npc("c6", X - 19f),
            };
            foreach (var w in walkers) w.WalkTo(w.X + (w.X < X ? 34f : -34f), w.Id.StartsWith("k") ? 1.9f : 1.8f);
            // shopkeepers and neighbours step out of the reopened doors and wave
            foreach (var (sx, id) in new[] { (shopX[0] + 1.6f, "c3"), (shopX[1] + 1.6f, "c5"), (shopX[2] + 1.6f, "c8") })
            {
                var a = Npc(id, sx, sx < X);
                a.PlayFor("wave", 3f, "talk");
            }
            var tram = Prop("Ending/tram", X - 30f, 2.9f, DH.SortDecor - 2);
            var cars = new List<SpriteRenderer> { Prop("Ending/car_0", X + 24f, 7.2f, DH.SortMid + 2), Prop("Ending/car_1", X - 26f, 6.4f, DH.SortMid + 2) };
            cars[0].flipX = true;
            StartCoroutine(Glide(tram.transform, 9f, 7f));
            StartCoroutine(Glide(cars[0].transform, -6f, 9f));
            StartCoroutine(Glide(cars[1].transform, 5f, 11f));
            yield return _ui.Say("Now the lights stay on. The shops opened their doors. And the people came back.", hold: 2.5f);
            yield return Wait(1.5f);
        }

        IEnumerator Glide(Transform t, float speed, float seconds)
        {
            for (float s = 0; s < seconds && t != null; s += Time.deltaTime)
            {
                t.position += new Vector3(speed * Time.deltaTime, 0, 0);
                yield return null;
            }
        }

        // ================================================================== 3. THE HERO RETURNS (playable)
        static readonly string[] Cheers =
        {
            "You saved our city!", "Thank you for exposing the truth!", "We are finally free!", "You're our hero!",
            "My shop is open again because of you!", "I can see the stars without the drones!", "It's the runner from the news!",
            "Can I get your autograph?", "Thank you, " + "runner!",
        };

        IEnumerator HeroReturns()
        {
            const float X = 600f, End = X + 64f;
            NewStage("Walk");
            _ui.SetChapter("CHAPTER 3  //  THE HERO RETURNS");
            Music("music_celebrate");
            _parallax.SetTint(FreeSky, FreeCity);
            _parallax.SetRain(false);
            Street(X - 30, 110);
            var rr = new System.Random(9);
            for (float sx = X - 8f; sx < End + 10f; sx += 11f)
            {
                Prop($"Ending/shop_open_{rr.Next(3)}", sx, 0.06f, DH.SortDecor);
                Prop("Ending/lamp_warm", sx + 5.5f, 0.06f, DH.SortDecor + 2);
            }
            for (float bx = X - 8f; bx < End + 10f; bx += 2f)
            {
                Prop("Ending/bunting", bx, 4.4f, DH.SortDecor + 3);
                Prop("Ending/lights", bx, 3.4f, DH.SortDecor + 3);
            }
            Prop("Ending/ad_free", X + 30f, 0.06f, DH.SortDecor + 1);

            // citizens along the street, waiting to thank the hero
            string[] ids = { "c1", "c2", "k1", "c3", "c4", "c5", "k2", "c6", "c7", "c8", "c2", "c5", "c1", "k1" };
            var people = new List<Actor>();
            for (int i = 0; i < ids.Length; i++)
            {
                float px = X + 4f + i * 4.3f + (float)rr.NextDouble();
                var a = Npc(ids[i], px, rr.NextDouble() < 0.5);
                a.Play(rr.NextDouble() < 0.5 ? "talk" : "idle");
                people.Add(a);
            }
            // the crowd waiting at the square
            for (int i = 0; i < 6; i++) { var a = Npc(i % 3 == 0 ? "k2" : "c" + (i + 2), End + 2f + i * 1.1f, false); a.Play("cheer"); people.Add(a); }

            var hero = Hero(X - 5f);
            hero.FaceRight(true);
            _ui.Interactive = true;
            var input = new RunnerInput();
            SetCam(X, GroundCamY);
            yield return _ui.Fade(0f, 0.6f);
            _ui.Bubble("LEFT / RIGHT: WALK      " + RunnerInput.KeyLabel(input.Attack) + ": WAVE", () => hero.transform.position + Vector3.up * 1.8f, 4.5f, DH.Amber);

            float idle = 0, cheerCD = 0, nextBubble = 0;
            int line = 0;
            while (hero.X < End)
            {
                float dir = 0;
                var kb = Keyboard.current; var pad = Gamepad.current;
                if (kb != null && (kb.dKey.isPressed || kb.rightArrowKey.isPressed)) dir += 1;
                if (kb != null && (kb.aKey.isPressed || kb.leftArrowKey.isPressed)) dir -= 1;
                if (pad != null) dir += Mathf.Abs(pad.leftStick.x.ReadValue()) > 0.3f ? Mathf.Sign(pad.leftStick.x.ReadValue()) : pad.dpad.x.ReadValue();
                dir = Mathf.Clamp(dir, -1, 1);
                idle = dir != 0 ? 0 : idle + Time.deltaTime;
                if (AutoWalk || idle > 10f) dir = 1;          // never blocks: after 10 s idle the hero strolls on
                bool wave = input.Attack.WasPressedThisFrame() || (kb != null && kb.eKey.wasPressedThisFrame);

                var pos = hero.transform.position;
                if (dir != 0)
                {
                    pos.x = Mathf.Max(X - 6f, pos.x + dir * 3.2f * Time.deltaTime);
                    hero.transform.position = pos;
                    hero.FaceRight(dir > 0);
                    if (!hero.Walking) hero.Play("walk", 9f);
                }
                else if (!wave && !hero.Busy) hero.Play("idle");
                if (wave)
                {
                    hero.PlayFor("wave", 0.9f, "idle");
                    cheerCD -= 1;
                    foreach (var a in people) if (Mathf.Abs(a.X - pos.x) < 6f) { a.Face(pos.x); a.PlayFor("cheer", 1.4f, "clap"); }
                    if (cheerCD <= 0) { AudioManager.I.Sfx("crowd_cheer", 0.5f); cheerCD = 3; }
                }
                cheerCD = Mathf.Max(0, cheerCD - Time.deltaTime);

                // citizens notice the hero as they pass
                foreach (var a in people)
                {
                    if (a.Talked || Mathf.Abs(a.X - pos.x) > 2.8f || Time.time < nextBubble) continue;
                    nextBubble = Time.time + 1.3f;          // one bubble at a time, so they never overlap
                    a.Talked = true;
                    a.Face(pos.x);
                    a.PlayFor(line % 2 == 0 ? "wave" : "cheer", 2.2f, "clap");
                    var who = a;
                    string say = line == 8 ? "Thank you, " + HeroShort + "!" : Cheers[line % Cheers.Length];
                    _ui.Bubble(say, () => who.transform.position + Vector3.up * (who.transform.localScale.y * 1.6f + 0.2f), 2.8f,
                        line % 3 == 0 ? DH.Pink : line % 3 == 1 ? DH.Cyan : DH.Amber);
                    AudioManager.I.Sfx("npc_talk", 0.5f, Random.Range(0.9f, 1.2f));
                    line++;
                }
                float camX = Mathf.Clamp(pos.x + 3f, X, End - 2f);
                SetCam(Mathf.Lerp(_camPos.x, camX, Time.deltaTime * 4f), GroundCamY);
                yield return null;
            }
            input.Dispose();
            hero.Play("wave");
            AudioManager.I.Sfx("crowd_cheer", 0.8f);
            yield return Wait(1.4f);
        }

        // ================================================================== 4. THE AWARD CEREMONY
        const float SquareX = 900f, StageTop = 1.06f, StatueOffset = 13f;
        Actor _mayor, _ceremonyHero;
        readonly List<Actor> _crowd = new List<Actor>();
        SpriteRenderer _cloth, _statue;

        void BuildSquare()
        {
            const float X = SquareX;
            NewStage("Square");
            _crowd.Clear();
            _parallax.SetTint(FreeSky, FreeCity);
            _parallax.SetRain(false);
            Street(X - 30, 60);
            Prop("Ending/stage", X, 0.06f, DH.SortProps);
            Prop("Ending/podium", X + 1.6f, StageTop, DH.SortProps + 1);
            Prop("Ending/banner", X, 4.6f, DH.SortDecor + 4);
            for (float bx = X - 14f; bx < X + 16f; bx += 2f) { Prop("Ending/bunting", bx, 5.6f, DH.SortDecor + 3); Prop("Ending/lights", bx, 3.9f, DH.SortDecor + 3); }
            foreach (var (sx, ang) in new[] { (X - 6f, 18f), (X + 6f, -18f) })
            {
                var s = Prop("Story/searchlight", sx, 9f, DH.SortDecor + 5);
                s.transform.rotation = Quaternion.Euler(0, 0, ang);
                s.color = new Color(1f, 0.95f, 0.8f, 0.12f);
            }
            Prop("Ending/shop_open_0", X - 20f, 0.06f, DH.SortDecor);
            Prop("Ending/shop_open_2", X + 22f, 0.06f, DH.SortDecor);
            // the statue, still covered, to the right of the stage
            Prop("Ending/pedestal", X + StatueOffset, 0.06f, DH.SortProps);
            _statue = Prop("Ending/statue_" + _heroId, X + StatueOffset, 1.8f, DH.SortProps + 1, 3f);
            _statue.enabled = false;
            _cloth = Prop("Ending/cloth", X + StatueOffset, 1.8f, DH.SortProps + 2);
            // the crowd on both sides of the stage
            string[] left = { "c1", "k1", "c3", "c5", "c7", "k2" }, right = { "c2", "c4", "k1", "c6", "c8" };
            for (int i = 0; i < left.Length; i++) _crowd.Add(Npc(left[i], X - 5.2f - i * 1.15f, true));
            for (int i = 0; i < right.Length; i++) _crowd.Add(Npc(right[i], X + 4.8f + i * 1.05f, false));
            foreach (var a in _crowd) a.Play(Random.value < 0.5f ? "idle" : "clap");
            _mayor = Npc("mayor", X + 1.2f, false, StageTop);
            _mayor.Order = DH.SortProps + 3;
            _ceremonyHero = Hero(X - 14f);
            _ceremonyHero.FaceRight(true);
        }

        void Crowd(string anim, float seconds, string after = "idle")
        {
            foreach (var a in _crowd) a.PlayFor(anim, seconds * Random.Range(0.85f, 1.15f), after);
        }

        IEnumerator AwardCeremony()
        {
            const float X = SquareX;
            BuildSquare();
            _ui.SetChapter("CHAPTER 4  //  THE AWARD CEREMONY");
            Music("music_ceremony");
            SetCam(X - 3f, GroundCamY);
            yield return _ui.Fade(0f, 0.8f);

            // 1. the hero approaches the stage
            var hero = _ceremonyHero;
            hero.WalkTo(X - 3.6f, 2.4f);
            Crowd("cheer", 2.5f, "clap");
            AudioManager.I.Sfx("crowd_cheer", 0.9f);
            PanAsync(X, GroundCamY, 3f);
            while (hero.Walking) yield return null;
            hero.transform.position = new Vector3(hero.X, StageTop, 0);        // up the steps
            hero.WalkTo(X - 0.4f, 2f);
            while (hero.Walking) yield return null;
            hero.FaceRight(true);

            // 2. the mayor welcomes the hero; 3. the crowd cheers
            _mayor.Play("talk");
            yield return _ui.Say("Welcome, " + HeroShort + ". Please, come and stand with me.", "MAYOR", MayorPortrait, 1.2f);
            _mayor.Play("idle");
            Crowd("cheer", 2.2f, "clap");
            AudioManager.I.Sfx("crowd_cheer", 0.8f);
            yield return Wait(1.6f);

            // 4. the speech
            _mayor.Play("talk");
            yield return _ui.Say("Today, we honor a hero who stood against corruption and fought for the truth.", "MAYOR", MayorPortrait, 1.6f);
            yield return _ui.Say("Because of your courage, our city has regained its freedom.", "MAYOR", MayorPortrait, 1.6f);
            yield return _ui.Say("You have given our people hope, and your bravery will never be forgotten.", "MAYOR", MayorPortrait, 1.8f);
            _ui.ClearCaption();

            // 5. the mayor presents the medal; 6. the award
            _mayor.Play("present");
            var medal = AnimProp("Ending/medal", X + 0.75f, StageTop + 0.7f, DH.SortPlayer + 2, 5);
            medal.transform.localScale = Vector3.one * 0.5f;
            yield return Wait(0.6f);
            for (float t = 0; t < 0.6f; t += Time.deltaTime)
            {
                medal.transform.position = Vector3.Lerp(new Vector3(X + 0.75f, StageTop + 0.7f), new Vector3(X - 0.3f, StageTop + 0.65f), t / 0.6f);
                yield return null;
            }
            yield return _ui.AwardCard(SpriteLibrary.Anim("Ending/medal"), "GUARDIAN OF THE CITY", "awarded to " + _heroDef.displayName);
            medal.transform.position = new Vector3(X - 0.32f, StageTop + 0.55f, 0);
            medal.transform.localScale = Vector3.one * 0.35f;

            // 7. the handshake, under camera flashes
            hero.Play("shake");
            _mayor.Play("shake");
            var flashes = new List<SpriteRenderer>();
            for (int i = 0; i < 6; i++)
            {
                var f = Prop("FX/glow", 0, 0, DH.SortFX, 0.5f);
                f.enabled = false;
                flashes.Add(f);
            }
            for (float t = 0; t < 2.4f; t += Time.deltaTime)
            {
                foreach (var f in flashes)
                {
                    if (!f.enabled && Random.value < 0.03f)
                    {
                        var who = _crowd[Random.Range(0, _crowd.Count)];
                        f.transform.position = who.transform.position + new Vector3(0.2f, 1.1f, 0);
                        f.enabled = true;
                        f.color = Color.white;
                    }
                    else if (f.enabled) { var c = f.color; c.a -= Time.deltaTime * 6f; f.color = c; if (c.a <= 0) f.enabled = false; }
                }
                yield return null;
            }
            // 8. applause; 9. confetti
            AudioManager.I.Sfx("applause", 1f);
            Crowd("clap", 4f, "cheer");
            _confetti.Active = true;
            hero.Play("win");
            _mayor.Play("idle");
            yield return Wait(3.5f);
        }

        // ================================================================== 5. THE STATUE
        IEnumerator StatueReveal()
        {
            const float X = SquareX;
            if (_stage == null || _mayor == null) { BuildSquare(); _ceremonyHero.transform.position = new Vector3(X - 0.4f, StageTop, 0); }
            _ui.SetChapter("CHAPTER 5  //  THE GUARDIAN");
            Music("music_ceremony");
            _confetti.Active = false;
            SetCam(X, GroundCamY);
            yield return _ui.Fade(0f, 0.6f);
            _mayor.Play("talk");
            yield return _ui.Say("But words are not enough for what you did.", "MAYOR", MayorPortrait, 1.2f);
            yield return _ui.Say("So the city made something that will stand for as long as Sector 7 does.", "MAYOR", MayorPortrait, 1.2f);
            _mayor.Play("idle");
            // everyone turns to the covered statue
            foreach (var a in _crowd) a.Face(X + StatueOffset);
            _ceremonyHero.WalkTo(X + 3.2f, 2f);
            yield return Pan(X + StatueOffset - 3f, GroundCamY + 0.6f, 2.2f);
            while (_ceremonyHero.Walking) yield return null;
            _ceremonyHero.Face(X + StatueOffset);
            yield return _ui.Say("Citizens of Sector 7... I give you our Guardian!", "MAYOR", MayorPortrait, 0.8f);

            // the cloth comes off
            AudioManager.I.Sfx("reveal", 1f);
            _statue.enabled = true;
            var glow = Prop("FX/glow", X + StatueOffset, 4.4f, DH.SortProps, 4f);
            glow.color = new Color(1f, 0.82f, 0.4f, 0f);
            Vector3 c0 = _cloth.transform.position;
            for (float t = 0; t < 1.6f; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0, 1, t / 1.6f);
                _cloth.transform.position = c0 + new Vector3(k * 1.2f, k * 7f, 0);
                _cloth.color = new Color(1, 1, 1, 1f - k);
                glow.color = new Color(1f, 0.82f, 0.4f, 0.45f * k);
                yield return null;
            }
            _cloth.enabled = false;
            Crowd("cheer", 4f, "clap");
            AudioManager.I.Sfx("crowd_cheer", 1f);
            _confetti.Active = true;
            // slow push toward the statue, then the plaque
            _ui.ClearCaption();
            PanAsync(X + StatueOffset, GroundCamY + 1.0f, 5f);
            yield return Wait(2.2f);
            // the plaque's words, set above the statue so they never cover it
            yield return _ui.Title("GUARDIAN OF THE CITY", "In honor of the hero who restored freedom\nand revealed the truth.",
                new Color(1f, 0.86f, 0.4f), 4f, 14, false, 118f);
        }

        // ================================================================== 6. THE CITY IS FREE
        IEnumerator CityIsFree()
        {
            const float X = 1500f;
            NewStage("Finale");
            _ui.SetChapter("EPILOGUE  //  THE CITY IS FREE");
            Music("music_finale");
            _parallax.SetTint(FreeSky, FreeCity);
            _parallax.SetRain(false);
            Rooftop(X - 30, 70);
            Prop("Decor/antenna", X - 9f, 0f, DH.SortDecor);
            Prop("Decor/water_tank", X + 9f, 0f, DH.SortDecor);
            var hero = Hero(X - 1f);
            hero.FaceRight(true);
            hero.Play("win");
            var friends = new List<Actor> { Npc("c5", X - 5.5f), Npc("k2", X - 4.4f), Npc("c1", X - 3.3f) };
            foreach (var a in friends) a.Play("cheer");
            _fireworks.area = new Rect(X - 6f, 5.4f, 18f, 3.6f);
            _fireworks.Active = true;
            _fireworks.rate = 1.6f;
            SetCam(X - 6f, GroundCamY);
            yield return _ui.Fade(0f, 1f);
            for (int i = 0; i < 4; i++) { _fireworks.Launch(); yield return Wait(0.25f); }
            PanAsync(X + 2f, GroundCamY + 0.5f, 9f);
            yield return Wait(2f);
            yield return _ui.Title("THE CITY IS FREE", null, DH.Cyan, 3f, 22, false);
            yield return _ui.Title("", "The data was stolen. The truth was restored.\nThe future belongs to the people.", DH.Ink, 4f, 18, false);
            for (int i = 0; i < 6; i++) { _fireworks.Launch(); yield return Wait(0.15f); }
            yield return _ui.Title("THANK YOU FOR PLAYING", "THE DATA HEIST", DH.Amber, 3.5f, 16, true);
        }
    }
}
