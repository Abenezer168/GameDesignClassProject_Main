using UnityEngine;

namespace DataHeist
{
    /// <summary>
    /// Living menu background: the camera cruises over an endless rooftop with the night-city
    /// parallax and rain, a runner keeps pace, and security drones sweep past now and then.
    /// Shared by the main menu and character select so the two screens feel continuous.
    /// </summary>
    public class MenuBackdrop : MonoBehaviour
    {
        /// <summary>Runner shown on the rooftop (character id); empty hides it.</summary>
        public string runnerId = "kai";
        /// <summary>Horizontal position of the runner as a fraction of the screen width.</summary>
        public float runnerScreenX = 0.18f;
        public Color runnerTint = Color.white;
        public float cruiseSpeed = 7.5f;

        const float StripWidth = 96f;

        Transform _cam;
        SpriteRenderer _runner, _drone, _beam, _ledge, _facade;
        SpriteRenderer[] _props;
        Sprite[] _droneFrames;
        float _camX, _droneT = 3f, _droneX, _droneY, _droneDir;

        public static MenuBackdrop Create()
        {
            var b = new GameObject("MenuBackdrop").AddComponent<MenuBackdrop>();
            b.Build();
            return b;
        }

        void Build()
        {
            var camGo = Camera.main != null ? Camera.main.gameObject : new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            if (!camGo.TryGetComponent<Camera>(out var cam)) cam = camGo.AddComponent<Camera>();
            cam.orthographic = true; cam.orthographicSize = DH.OrthoSize;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = DH.Night;
            cam.nearClipPlane = -50; cam.farClipPlane = 50;
            if (camGo.GetComponent<AudioListener>() == null) camGo.AddComponent<AudioListener>();
            _cam = camGo.transform;
            Parallax.Create(_cam, 0);

            // short tiled strips that hop forward with the camera (a single huge tiled sprite
            // exceeds Unity's 9-slice vertex limit), plus a recycled set of rooftop props
            _ledge = SR.Tiled("Ledge", transform, SpriteLibrary.Get("Tiles/rooftop_violet"), DH.SortRoofTop, new Vector3(0, 0.06f, 0), new Vector2(StripWidth, 0.5f));
            _facade = SR.Tiled("Facade", transform, SpriteLibrary.Get("Tiles/facade_violet_0"), DH.SortFacade, new Vector3(0, -0.44f, 0), new Vector2(StripWidth, 8));
            string[] props = { "ac_unit", "antenna", "water_tank", "vent", "sign_pink", "sign_cyan" };
            _props = new SpriteRenderer[8];
            float px = 6;
            for (int i = 0; i < _props.Length; i++)
            {
                _props[i] = SR.Make("Prop", transform, SpriteLibrary.Get("Decor/" + props[i % props.Length]), DH.SortDecor, new Vector3(SR.Snap(px), 0, 0));
                px += Random.Range(7f, 14f);
            }

            _runner = SR.Make("Runner", transform, null, DH.SortPlayer, Vector3.zero);
            _droneFrames = SpriteLibrary.Anim("Enemies/drone_scout");
            _drone = SR.Make("Drone", transform, null, DH.SortEnemies, Vector3.zero);
            _beam = SR.Make("Searchlight", _drone.transform, SpriteLibrary.Get("FX/glow"), DH.SortEnemies - 1, new Vector3(0, -1.6f, 0));
            _beam.color = new Color(DH.Red.r, DH.Red.g, DH.Red.b, 0.35f);
            _beam.transform.localScale = new Vector3(1.2f, 3f, 1);
            _drone.gameObject.SetActive(false);
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            _camX += dt * cruiseSpeed;
            float halfH = DH.OrthoSize, halfW = halfH * Camera.main.aspect;
            _cam.position = new Vector3(SR.Snap(_camX), SR.Snap(halfH - 1.7f), -10);
            Wrap(_ledge, 0.06f);
            Wrap(_facade, -0.44f);
            float left = _camX - halfW - 2f, furthest = left;
            foreach (var p in _props) furthest = Mathf.Max(furthest, p.transform.position.x);
            foreach (var p in _props)
                if (p.transform.position.x < left)
                {
                    furthest += Random.Range(7f, 14f);
                    p.transform.position = new Vector3(SR.Snap(furthest), 0, 0);
                }

            if (string.IsNullOrEmpty(runnerId)) _runner.enabled = false;
            else
            {
                _runner.enabled = true;
                var run = SpriteLibrary.CharacterAnim(runnerId, "run");
                if (run.Length > 0) _runner.sprite = run[Mathf.FloorToInt(Time.unscaledTime * 14) % run.Length];
                _runner.transform.position = new Vector3(SR.Snap(_camX - halfW + halfW * 2 * runnerScreenX), 1f / DH.PPU, 0);
                _runner.color = runnerTint;
            }
            UpdateDrone(dt, halfW, halfH);
        }

        void Wrap(SpriteRenderer strip, float y)
        {
            float period = strip.sprite != null ? strip.sprite.bounds.size.x : 1f;
            float x = _camX - 30f;
            strip.transform.position = new Vector3(SR.Snap(x - Mathf.Repeat(x, period)), y, 0);
        }

        void UpdateDrone(float dt, float halfW, float halfH)
        {
            if (_droneFrames.Length == 0) return;
            if (!_drone.gameObject.activeSelf)
            {
                _droneT -= dt;
                if (_droneT > 0) return;
                _droneDir = Random.value < 0.5f ? 1 : -1;
                _droneX = -_droneDir * (halfW + 2);
                _droneY = Random.Range(4.5f, 7.5f);
                _drone.flipX = _droneDir < 0;
                _drone.gameObject.SetActive(true);
            }
            _droneX += _droneDir * dt * 3.2f;
            float bob = Mathf.Sin(Time.unscaledTime * 3f) * 0.15f;
            _drone.sprite = _droneFrames[Mathf.FloorToInt(Time.unscaledTime * 10) % _droneFrames.Length];
            _drone.transform.position = new Vector3(SR.Snap(_camX + _droneX), SR.Snap(_droneY + bob), 0);
            _beam.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.unscaledTime * 1.3f) * 18f);
            if (Mathf.Abs(_droneX) > halfW + 3)
            {
                _drone.gameObject.SetActive(false);
                _droneT = Random.Range(6f, 12f);
            }
        }
    }
}
