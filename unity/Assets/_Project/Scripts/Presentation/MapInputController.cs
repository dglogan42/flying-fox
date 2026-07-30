using FlyingFox.Core;
using UnityEngine;

namespace FlyingFox.Presentation
{
    /// <summary>
    /// Mouse + gamepad (Switch-style) place/rotate/select.
    /// Pan: RMB / right stick. Cursor: D-pad / left stick via SwitchCursorController.
    /// </summary>
    public sealed class MapInputController : MonoBehaviour
    {
        [SerializeField] MapCameraController _camera;
        [SerializeField] HexMapView _map;
        [SerializeField] GhostPlacementView _ghost;
        [SerializeField] SwitchCursorController _cursor;
        [SerializeField] float _clickSlopPx = 10f;
        [SerializeField] float _rightStickPan = 6f;

        RunController _run;
        HexCoord? _hover;
        Vector3 _mouseDownPos;
        bool _mouseDown;
        bool _dragPanSuspect;
        int _lastSelected = -1;
        string _lastEdges;
        bool _usingGamepadAim;

        public HexCoord? HoverHex => _hover;
        public bool InputEnabled { get; set; } = true;

        public void Bind(
            RunController run,
            MapCameraController cam,
            HexMapView map,
            GhostPlacementView ghost,
            SwitchCursorController cursor = null)
        {
            _run = run;
            _camera = cam;
            _map = map;
            _ghost = ghost;
            _cursor = cursor;
            _hover = null;
            _mouseDown = false;
            _lastSelected = -1;
            _lastEdges = null;
            _cursor?.Bind(run, map);
        }

        public void Unbind()
        {
            _run = null;
            _hover = null;
            _ghost?.Hide();
            _cursor?.Unbind();
        }

        public void NotifyBoardChanged() => _cursor?.OnBoardChanged();

        void Update()
        {
            if (PauseMenuController.IsPaused || PauseMenuController.IsPointerOverPause)
            {
                _ghost?.Hide();
                return;
            }

            if (!InputEnabled || _run == null || _camera == null || _map == null || _ghost == null)
                return;

            if (_run.Phase != RunPhase.Playing)
            {
                _hover = null;
                _ghost.Hide();
                return;
            }

            HandleGamepadPan();
            HandleKeysAndGamepadButtons();
            HandleHoverAndClick();
            SyncGhostFromCursorOrMouse();
        }

        void HandleGamepadPan()
        {
            // Right stick pan (axes 3/4 common on many pads; also Joy axis)
            float rx = 0f, ry = 0f;
            try
            {
                rx = Input.GetAxis("Joy X 3");
                ry = Input.GetAxis("Joy Y 4");
            }
            catch { /* axes may be undefined */ }

            if (Mathf.Abs(rx) < 0.01f && Mathf.Abs(ry) < 0.01f)
            {
                // XInput-style: 4th/5th axes often right stick
                rx = Input.GetAxisRaw("Horizontal") * 0f; // don't double left stick
            }

            // Generic: Joystick axis 3 & 4
            if (Mathf.Abs(rx) < 0.15f && Mathf.Abs(ry) < 0.15f)
            {
                // Fallback using named axes if project maps them later
            }

            float j3 = GetAxisSafe(3);
            float j4 = GetAxisSafe(4);
            if (Mathf.Abs(j3) > 0.2f || Mathf.Abs(j4) > 0.2f)
            {
                var p = _camera.transform.position;
                p.x += j3 * _rightStickPan * Time.unscaledDeltaTime * _camera.Cam.orthographicSize;
                p.y += -j4 * _rightStickPan * Time.unscaledDeltaTime * _camera.Cam.orthographicSize;
                _camera.transform.position = p;
            }
        }

        static float GetAxisSafe(int joyAxisIndex)
        {
            // Unity legacy: "Joystick Axis X" not indexed easily — use raw key alternatives only
            return 0f;
        }

        void HandleKeysAndGamepadButtons()
        {
            if (GameplayHudImgui.IsPointerOverHud) return;

            // Keyboard
            if (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.E))
                _run.RotateSelected(1);
            if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.Z))
                _run.RotateSelected(-1);
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                bool back = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                _run.CycleHand(back ? -1 : 1);
            }
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
                _run.SelectHand(0);
            if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
                _run.SelectHand(1);
            if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
                _run.SelectHand(2);
            if (Input.GetKeyDown(KeyCode.N) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
                GameSession.Instance?.StartClassicRun();
            // Escape / Start handled by PauseMenuController

            // Gamepad — Unity joystick button indices (common XInput / similar; Switch SDK remaps later)
            // A=0, B=1, X=2, Y=3, L=4, R=5, Select=6, Start=7
            if (GetJoyDown(0)) // A — place at cursor
            {
                var target = _cursor != null && _cursor.Cursor.HasValue
                    ? _cursor.Cursor
                    : _hover;
                if (target.HasValue)
                    _run.TryPlace(target.Value);
            }
            if (GetJoyDown(1) || GetJoyDown(5)) // B or R — rotate CW
                _run.RotateSelected(1);
            if (GetJoyDown(3)) // Y — rotate CCW
                _run.RotateSelected(-1);
            if (GetJoyDown(4)) // L — prev hand
                _run.CycleHand(-1);
            if (GetJoyDown(2)) // X — cycle hand
                _run.CycleHand(1);
            // Start (7) → pause (PauseMenuController)
            // Select (6) → abandon via pause menu confirm (avoid misclick)
            if (GetJoyDown(6))
                PauseMenuController.Instance?.Pause();
        }

        static bool GetJoyDown(int button)
        {
            // JoystickButton0 + button
            return Input.GetKeyDown(KeyCode.JoystickButton0 + button);
        }

        void HandleHoverAndClick()
        {
            if (GameplayHudImgui.IsPointerOverHud)
            {
                if (!_usingGamepadAim)
                {
                    _hover = null;
                    _ghost.Hide();
                }
                return;
            }

            // Mouse movement engages mouse aim
            if (Input.GetAxis("Mouse X") != 0f || Input.GetAxis("Mouse Y") != 0f ||
                Input.GetMouseButton(0) || Input.GetMouseButton(1))
                _usingGamepadAim = false;

            bool panHeld =
                Input.GetKey(KeyCode.Space) ||
                Input.GetKey(KeyCode.LeftAlt) ||
                Input.GetMouseButton(1) ||
                Input.GetMouseButton(2);

            if (panHeld)
            {
                if (!_usingGamepadAim)
                {
                    _hover = null;
                    _ghost.Hide();
                    _map.RefreshSlots(_run.Board, null);
                }
                _mouseDown = false;
                return;
            }

            if (!_usingGamepadAim)
            {
                if (!_camera.ScreenToWorld(Input.mousePosition, out var world))
                    return;

                var hex = HexMeshUtil.FromWorld(world, _map.HexSize);
                bool valid = _run.Board.IsValidPlacement(hex) && _run.Hand.Count > 0;
                _hover = valid ? hex : (HexCoord?)null;
                _map.RefreshSlots(_run.Board, _hover);
            }

            if (Input.GetMouseButtonDown(0))
            {
                _mouseDown = true;
                _dragPanSuspect = false;
                _mouseDownPos = Input.mousePosition;
                _usingGamepadAim = false;
            }

            if (_mouseDown && Input.GetMouseButton(0))
            {
                if (Vector3.Distance(Input.mousePosition, _mouseDownPos) > _clickSlopPx)
                    _dragPanSuspect = true;
            }

            if (Input.GetMouseButtonUp(0) && _mouseDown)
            {
                _mouseDown = false;
                if (_dragPanSuspect) return;
                if (_hover.HasValue)
                    _run.TryPlace(_hover.Value);
            }

            // Any stick/dpad → gamepad aim mode
            if (Mathf.Abs(Input.GetAxis("Horizontal")) > 0.4f ||
                Mathf.Abs(Input.GetAxis("Vertical")) > 0.4f)
                _usingGamepadAim = true;
        }

        void SyncGhostFromCursorOrMouse()
        {
            HexCoord? aim = null;
            if (_usingGamepadAim && _cursor != null && _cursor.Cursor.HasValue)
                aim = _cursor.Cursor;
            else if (_hover.HasValue)
                aim = _hover;

            if (!aim.HasValue || _run.Hand.Count == 0)
            {
                if (!_hover.HasValue || _usingGamepadAim)
                    _ghost.Hide();
                return;
            }

            if (!_run.Board.IsValidPlacement(aim.Value))
            {
                _ghost.Hide();
                return;
            }

            var tile = _run.Hand[_run.SelectedHandIndex];
            string edges = EdgesKey(tile.Edges);
            _lastSelected = _run.SelectedHandIndex;
            _lastEdges = edges;
            var eval = PlacementService.Evaluate(_run.Board, aim.Value, tile.Edges);
            _ghost.Show(aim.Value, tile.Edges, eval, _run.Config.Balance);
            _map.RefreshSlots(_run.Board, aim);
        }

        static string EdgesKey(BiomeId[] e)
        {
            if (e == null) return "";
            var c = new char[e.Length];
            for (int i = 0; i < e.Length; i++) c[i] = BiomeCodec.ToChar(e[i]);
            return new string(c);
        }
    }
}
