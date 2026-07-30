using FlyingFox.Core;
using UnityEngine;

namespace FlyingFox.Presentation
{
    /// <summary>
    /// Map click-to-place, hover ghost, keyboard rotate/select.
    /// Pan is handled by MapCameraController (RMB / Space+LMB).
    /// </summary>
    public sealed class MapInputController : MonoBehaviour
    {
        [SerializeField] MapCameraController _camera;
        [SerializeField] HexMapView _map;
        [SerializeField] GhostPlacementView _ghost;

        RunController _run;
        HexCoord? _hover;
        Vector3 _mouseDownPos;
        bool _mouseDown;
        const float ClickSlopPx = 8f;

        public HexCoord? HoverHex => _hover;

        public void Bind(RunController run, MapCameraController cam, HexMapView map, GhostPlacementView ghost)
        {
            _run = run;
            _camera = cam;
            _map = map;
            _ghost = ghost;
        }

        void Update()
        {
            if (_run == null || _run.Phase != RunPhase.Playing) 
            {
                _ghost?.Hide();
                return;
            }

            HandleKeys();
            HandleHoverAndClick();
        }

        void HandleKeys()
        {
            if (Input.GetKeyDown(KeyCode.R))
                _run.RotateSelected(1);
            if (Input.GetKeyDown(KeyCode.Q))
                _run.RotateSelected(-1);
            if (Input.GetKeyDown(KeyCode.Tab))
                _run.CycleHand(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? -1 : 1);
            if (Input.GetKeyDown(KeyCode.Alpha1)) _run.SelectHand(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) _run.SelectHand(1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) _run.SelectHand(2);
        }

        void HandleHoverAndClick()
        {
            // Don't place while panning modifiers held
            bool panMod = Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.LeftAlt);
            if (panMod || Input.GetMouseButton(1) || Input.GetMouseButton(2))
            {
                _hover = null;
                _ghost.Hide();
                _map.RefreshSlots(_run.Board, null);
                return;
            }

            if (!_camera.ScreenToWorld(Input.mousePosition, out var world))
                return;

            var hex = HexMeshUtil.FromWorld(world, _map.HexSize);
            bool valid = _run.Board.IsValidPlacement(hex);
            _hover = valid ? hex : (HexCoord?)null;

            _map.RefreshSlots(_run.Board, _hover);

            if (valid && _run.Hand.Count > 0)
            {
                var tile = _run.Hand[_run.SelectedHandIndex];
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
                _mouseDownPos = Input.mousePosition;
            }

            if (Input.GetMouseButtonUp(0) && _mouseDown)
            {
                _mouseDown = false;
                if (Vector3.Distance(Input.mousePosition, _mouseDownPos) > ClickSlopPx)
                    return;
                if (_hover.HasValue)
                    _run.TryPlace(_hover.Value);
            }
        }
    }
}
