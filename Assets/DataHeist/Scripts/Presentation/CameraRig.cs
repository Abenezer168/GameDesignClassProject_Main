using UnityEngine;

namespace DataHeist
{
    /// <summary>
    /// Runner camera: keeps the hero on the left third, eases vertically toward the last
    /// rooftop height (so jumps don't bob the screen), follows falls and big launches,
    /// adds trauma-style shake and snaps to the 32 px grid.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraRig : MonoBehaviour
    {
        public float leadX = 5.5f;
        public float heightAboveGround = 2.4f;
        RunnerSim _sim;
        float _lastGroundY, _shake, _x, _y;
        public Camera Cam { get; private set; }

        public static CameraRig Create()
        {
            var existing = Camera.main;
            GameObject go = existing != null ? existing.gameObject : new GameObject("Main Camera");
            go.tag = "MainCamera";
            if (!go.TryGetComponent<Camera>(out var cam)) cam = go.AddComponent<Camera>();
            if (go.GetComponent<AudioListener>() == null) go.AddComponent<AudioListener>();
            cam.orthographic = true;
            cam.orthographicSize = DH.OrthoSize;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = DH.Night;
            cam.nearClipPlane = -50; cam.farClipPlane = 50;
            var rig = go.GetComponent<CameraRig>();
            if (rig == null) rig = go.AddComponent<CameraRig>();
            rig.Cam = cam;
            return rig;
        }

        public void Bind(RunnerSim sim)
        {
            _sim = sim;
            Snap();
        }

        public void Snap()
        {
            _lastGroundY = _sim.p.y;
            _x = _sim.p.x + (10f - leadX);   // camera centre sits ahead of the runner
            _y = _sim.p.y + heightAboveGround;
            Apply(Vector2.zero);
        }

        public void Shake(float amount) => _shake = Mathf.Max(_shake, amount);

        void LateUpdate()
        {
            if (_sim == null) return;
            float dt = Time.unscaledDeltaTime;
            var p = _sim.p;
            if (p.onGround) _lastGroundY = p.y;
            float ty = _lastGroundY + heightAboveGround;
            if (p.y < _lastGroundY - 0.5f) ty = p.y + heightAboveGround;
            if (p.y > _lastGroundY + 3.2f) ty = p.y - 0.8f;
            _y += (ty - _y) * Mathf.Min(1, dt * 4f);
            float tx = p.x + (10f - leadX);
            _x += (tx - _x) * Mathf.Min(1, dt * 12f);

            Vector2 off = Vector2.zero;
            if (_shake > 0)
            {
                _shake -= dt;
                off = new Vector2(Random.value - 0.5f, Random.value - 0.5f) * (8f * _shake / DH.PPU);
            }
            Apply(off);
        }

        void Apply(Vector2 off)
        {
            transform.position = new Vector3(SR.Snap(_x + off.x), SR.Snap(_y + off.y), -10f);
        }
    }
}
