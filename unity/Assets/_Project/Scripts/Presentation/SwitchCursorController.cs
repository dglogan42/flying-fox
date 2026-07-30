using System.Collections.Generic;
using FlyingFox.Core;
using UnityEngine;

namespace FlyingFox.Presentation
{
    /// <summary>
    /// Gamepad / Switch-style placement cursor over valid empty hexes.
    /// D-pad and left stick move; used by MapInputController when no mouse aim.
    /// </summary>
    public sealed class SwitchCursorController : MonoBehaviour
    {
        [SerializeField] float _stickDeadzone = 0.45f;
        [SerializeField] float _stickRepeat = 0.22f;

        RunController _run;
        HexMapView _map;
        HexCoord? _cursor;
        float _stickCooldown;
        HexTileView _marker;

        public HexCoord? Cursor => _cursor;

        public void Bind(RunController run, HexMapView map)
        {
            _run = run;
            _map = map;
            _cursor = null;
            SnapToFirst();
        }

        public void Unbind()
        {
            _run = null;
            _cursor = null;
            if (_marker != null) _marker.gameObject.SetActive(false);
        }

        public void SnapToFirst()
        {
            if (_run == null || _map == null) return;
            var slots = _run.Board.GetEmptyAdjacent();
            if (slots.Count == 0)
            {
                _cursor = null;
                return;
            }
            _cursor = slots[0];
            RefreshMarker();
        }

        void Update()
        {
            if (_run == null || _run.Phase != RunPhase.Playing) return;
            if (_stickCooldown > 0f) _stickCooldown -= Time.unscaledDeltaTime;

            Vector2 dpad = new Vector2(
                Input.GetAxisRaw("Horizontal"),
                Input.GetAxisRaw("Vertical"));

            // Prefer joystick axes if present
            float jx = Input.GetAxisRaw("JoyX");
            float jy = Input.GetAxisRaw("JoyY");
            if (Mathf.Abs(jx) > 0.01f || Mathf.Abs(jy) > 0.01f)
                dpad = new Vector2(jx, jy);

            // Unity default often: Joy axis 1/2 — also try Joystick axes
            if (Mathf.Abs(dpad.x) < 0.01f && Mathf.Abs(dpad.y) < 0.01f)
            {
                dpad = new Vector2(
                    Input.GetAxis("Horizontal"),
                    Input.GetAxis("Vertical"));
            }

            if (dpad.magnitude >= _stickDeadzone && _stickCooldown <= 0f)
            {
                MoveCursor(dpad.normalized);
                _stickCooldown = _stickRepeat;
            }

            // Discrete D-pad via buttons (Switch / XInput style)
            if (Input.GetKeyDown(KeyCode.JoystickButton5)) { /* optional */ }
            if (WasPad(KeyCode.UpArrow) || GetButtonDown("DPadUp")) MoveCursor(Vector2.up);
            if (WasPad(KeyCode.DownArrow) || GetButtonDown("DPadDown")) MoveCursor(Vector2.down);
            if (WasPad(KeyCode.LeftArrow) || GetButtonDown("DPadLeft")) MoveCursor(Vector2.left);
            if (WasPad(KeyCode.RightArrow) || GetButtonDown("DPadRight")) MoveCursor(Vector2.right);
        }

        static bool WasPad(KeyCode k) => Input.GetKeyDown(k);

        static bool GetButtonDown(string name)
        {
            try { return Input.GetButtonDown(name); }
            catch { return false; }
        }

        void MoveCursor(Vector2 dir)
        {
            if (_run == null) return;
            var slots = _run.Board.GetEmptyAdjacent();
            if (slots.Count == 0)
            {
                _cursor = null;
                RefreshMarker();
                return;
            }

            if (!_cursor.HasValue || !Contains(slots, _cursor.Value))
            {
                _cursor = slots[0];
                RefreshMarker();
                return;
            }

            // Pick valid hex whose world offset best aligns with dir
            Vector3 from = HexMeshUtil.ToWorld(_cursor.Value, _map.HexSize);
            HexCoord best = _cursor.Value;
            float bestDot = -999f;
            foreach (var s in slots)
            {
                if (s == _cursor.Value) continue;
                Vector3 to = HexMeshUtil.ToWorld(s, _map.HexSize) - from;
                if (to.sqrMagnitude < 1e-6f) continue;
                float dot = Vector2.Dot(dir, new Vector2(to.x, to.y).normalized);
                if (dot > bestDot)
                {
                    bestDot = dot;
                    best = s;
                }
            }

            if (bestDot > 0.15f)
                _cursor = best;
            else
            {
                // Fallback: cycle list
                int i = IndexOf(slots, _cursor.Value);
                int n = (i + (dir.x > 0.2f || dir.y < -0.2f ? 1 : slots.Count - 1)) % slots.Count;
                _cursor = slots[n];
            }

            RefreshMarker();
        }

        void RefreshMarker()
        {
            if (_map == null) return;
            if (_marker == null)
            {
                _marker = HexTileView.Create(transform, "SwitchCursor");
            }

            if (!_cursor.HasValue)
            {
                _marker.gameObject.SetActive(false);
                return;
            }

            var edges = new[]
            {
                BiomeId.Neutral, BiomeId.Neutral, BiomeId.Neutral,
                BiomeId.Neutral, BiomeId.Neutral, BiomeId.Neutral,
            };
            _marker.Setup(_cursor.Value, edges, _map.HexSize * 1.02f, false, 0.15f);
            _marker.SetRing(BiomePalette.PerfectRing, 0.11f);
            _marker.gameObject.SetActive(true);
        }

        static bool Contains(List<HexCoord> list, HexCoord c)
        {
            for (int i = 0; i < list.Count; i++)
                if (list[i] == c) return true;
            return false;
        }

        static int IndexOf(List<HexCoord> list, HexCoord c)
        {
            for (int i = 0; i < list.Count; i++)
                if (list[i] == c) return i;
            return 0;
        }

        public void OnBoardChanged()
        {
            if (_run == null) return;
            var slots = _run.Board.GetEmptyAdjacent();
            if (slots.Count == 0) { _cursor = null; RefreshMarker(); return; }
            if (!_cursor.HasValue || !Contains(slots, _cursor.Value))
                _cursor = slots[0];
            RefreshMarker();
        }
    }
}
