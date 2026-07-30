using FlyingFox.Core;
using UnityEngine;

namespace FlyingFox.Presentation
{
    /// <summary>
    /// Orthographic pan (RMB / MMB / Space+LMB) and zoom (scroll).
    /// Zoom factor clamped to GameBalance.WebParity 0.45–2.4.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class MapCameraController : MonoBehaviour
    {
        [SerializeField] float _baseOrthoSize = 8f;
        [SerializeField] float _zoom = 1f;
        [SerializeField] float _zoomMin = 0.45f;
        [SerializeField] float _zoomMax = 2.4f;
        [SerializeField] float _zoomSpeed = 0.12f;
        [SerializeField] float _panSpeed = 1f;

        Camera _cam;
        bool _dragging;
        Vector3 _lastMouse;
        bool _blockPan;

        public Camera Cam => _cam != null ? _cam : (_cam = GetComponent<Camera>());
        public float Zoom => _zoom;

        public void ConfigureFromBalance(GameBalance bal)
        {
            if (bal == null) return;
            _zoomMin = bal.ZoomMin;
            _zoomMax = bal.ZoomMax;
            ApplyZoom();
        }

        public void SetPanBlocked(bool blocked) => _blockPan = blocked;

        /// <summary>Docked vs handheld base ortho (player zoom still applies on top).</summary>
        public void SetBaseOrthoSize(float baseOrtho)
        {
            _baseOrthoSize = Mathf.Max(2f, baseOrtho);
            ApplyZoom();
        }

        public void FocusWorld(Vector3 world, bool instant = true)
        {
            var p = transform.position;
            p.x = world.x;
            p.y = world.y;
            transform.position = p;
        }

        void Awake()
        {
            _cam = GetComponent<Camera>();
            _cam.orthographic = true;
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = new Color(0.04f, 0.09f, 0.06f, 1f);
            _cam.nearClipPlane = -10f;
            _cam.farClipPlane = 100f;
            ApplyZoom();
        }

        void Update()
        {
            if (PauseMenuController.IsPaused || _blockPan)
            {
                _dragging = false;
                return;
            }
            HandleZoom();
            HandlePan();
        }

        void HandleZoom()
        {
            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) < 0.01f) return;
            float factor = scroll > 0f ? 1f / (1f + _zoomSpeed) : (1f + _zoomSpeed);
            // Web: wheel up zooms in (smaller ortho). Design: 0.45–2.4 as zoom multiplier.
            // Higher zoom = closer = smaller ortho size.
            if (scroll > 0f) _zoom = Mathf.Min(_zoomMax, _zoom * 1.1f);
            else _zoom = Mathf.Max(_zoomMin, _zoom / 1.1f);
            ApplyZoom();
        }

        void HandlePan()
        {
            if (_blockPan) return;

            bool wantDrag =
                Input.GetMouseButton(1) ||
                Input.GetMouseButton(2) ||
                (Input.GetMouseButton(0) && (Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.LeftAlt)));

            if (wantDrag)
            {
                if (!_dragging)
                {
                    _dragging = true;
                    _lastMouse = Input.mousePosition;
                }
                else
                {
                    var delta = Input.mousePosition - _lastMouse;
                    _lastMouse = Input.mousePosition;
                    float worldPerPixel = (Cam.orthographicSize * 2f) / Screen.height;
                    transform.position -= new Vector3(delta.x * worldPerPixel * _panSpeed,
                        delta.y * worldPerPixel * _panSpeed, 0f);
                }
            }
            else
            {
                _dragging = false;
            }
        }

        void ApplyZoom()
        {
            _zoom = Mathf.Clamp(_zoom, _zoomMin, _zoomMax);
            if (Cam != null)
                Cam.orthographicSize = _baseOrthoSize / _zoom;
        }

        public bool ScreenToWorld(Vector3 screen, out Vector3 world)
        {
            var ray = Cam.ScreenPointToRay(screen);
            // Plane z=0
            if (Mathf.Abs(ray.direction.z) < 1e-5f)
            {
                world = default;
                return false;
            }
            float t = -ray.origin.z / ray.direction.z;
            world = ray.origin + ray.direction * t;
            return true;
        }
    }
}
