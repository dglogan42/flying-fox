using FlyingFox.Core;
using UnityEngine;

namespace FlyingFox.Presentation
{
    /// <summary>
    /// Map click-to-place, hover ghost, keyboard rotate/select.
    /// Pan is handled by MapCameraController (RMB / MMB / Space+LMB).
    /// </summary>
    public sealed class MapInputController : MonoBehaviour
    {
        [SerializeField] MapCameraController _camera;
        [SerializeField] HexMapView _map;
        [SerializeField] GhostPlacementView _ghost;
        [SerializeField] float _clickSlopPx = 10f;

        RunController _run;
        HexCoord? _hover;
        Vector3 _mouseDownPos;
        bool _mouseDown;
        bool _dragPanSuspect;
        int _lastSelected = -1;
        string _lastEdges;

        public HexCoord? HoverHex => _hover;
        public bool InputEnabled { get; set; } = true;

        public void Bind(RunController run, MapCameraController cam, HexMapView map, GhostPlacementView ghost)
        {
            _run = run;
            _camera = cam;
            _map = map;
            _ghost = ghost;
            _hover = null;
            _mouseDown = false;
            _lastSelected = -1;
            _lastEdges = null;
        }

        public void Unbind()
        {
            _run = null;
            _hover = null;
            _ghost?.Hide();
        }

        void Update()
        {
            if (!InputEnabled || _run == null || _camera == null || _map == null || _ghost == null)
                return;

            if (_run.Phase != RunPhase.Playing)
            {
                _hover = null;
                _ghost.Hide();
                return;
            }

            HandleKeys();
            HandleHoverAndClick();
        }

        void HandleKeys()
        {
            // Don't steal typing if ever we add text fields
            if (GameplayHudImgui.IsPointerOverHud) return;

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
            if (Input.GetKeyDown(KeyCode.Escape))
                _run.Abandon();
        }

        void HandleHoverAndClick()
        {
            if (GameplayHudImgui.IsPointerOverHud)
            {
                _hover = null;
                _ghost.Hide();
                return;
            }

            bool panHeld =
                Input.GetKey(KeyCode.Space) ||
                Input.GetKey(KeyCode.LeftAlt) ||
                Input.GetMouseButton(1) ||
                Input.GetMouseButton(2);

            if (panHeld)
            {
                _hover = null;
                _ghost.Hide();
                _map.RefreshSlots(_run.Board, null);
                _mouseDown = false;
                return;
            }

            if (!_camera.ScreenToWorld(Input.mousePosition, out var world))
                return;

            var hex = HexMeshUtil.FromWorld(world, _map.HexSize);
            bool valid = _run.Board.IsValidPlacement(hex) && _run.Hand.Count > 0;
            _hover = valid ? hex : (HexCoord?)null;

            _map.RefreshSlots(_run.Board, _hover);

            if (valid)
            {
                var tile = _run.Hand[_run.SelectedHandIndex];
                // Refresh ghost when selection/rotation changes
                string edges = EdgesKey(tile.Edges);
                if (_lastSelected != _run.SelectedHandIndex || _lastEdges != edges || _hover != null)
                {
                    _lastSelected = _run.SelectedHandIndex;
                    _lastEdges = edges;
                }

                var eval = PlacementService.Evaluate(_run.Board, hex, tile.Edges);
                _ghost.Show(hex, tile.Edges, eval, _run.Config.Balance);
            }
            else
            {
                _ghost.Hide();
            }

            if (Input.GetMouseButtonDown(0))
            {
                _mouseDown = true;
                _dragPanSuspect = false;
                _mouseDownPos = Input.mousePosition;
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
