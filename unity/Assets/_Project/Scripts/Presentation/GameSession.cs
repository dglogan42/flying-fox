using FlyingFox.Core;
using UnityEngine;

namespace FlyingFox.Presentation
{
    /// <summary>
    /// Wires Core <see cref="RunController"/> to map, camera, input, and HUD.
    /// First playable Classic loop (Daily later).
    /// </summary>
    public sealed class GameSession : MonoBehaviour
    {
        [Header("Run")]
        [SerializeField] int _debugSeed = 42;
        [SerializeField] bool _useDebugSeed;
        [SerializeField] float _hexSize = HexMeshUtil.DefaultSize;

        [Header("Wired by GameBootstrap")]
        [SerializeField] HexMapView _map;
        [SerializeField] MapCameraController _camera;
        [SerializeField] GhostPlacementView _ghost;
        [SerializeField] MapInputController _input;
        [SerializeField] GameplayHudImgui _hud;
        [SerializeField] SwitchCursorController _cursor;

        RunController _run;
        int _activeSeed;

        public RunController Run => _run;
        public int ActiveSeed => _activeSeed;
        public static GameSession Instance { get; private set; }

        void OnEnable() => Instance = this;

        void OnDisable()
        {
            if (Instance == this) Instance = null;
        }

        public void Wire(
            HexMapView map,
            MapCameraController camera,
            GhostPlacementView ghost,
            MapInputController input,
            GameplayHudImgui hud,
            SwitchCursorController cursor = null)
        {
            _map = map;
            _camera = camera;
            _ghost = ghost;
            _input = input;
            _hud = hud;
            _cursor = cursor;
        }

        /// <summary>Called by <see cref="FlyingFox.App.GameBootstrap"/> before first Start.</summary>
        public void Configure(float hexSize, bool useDebugSeed, int debugSeed)
        {
            _hexSize = hexSize;
            _useDebugSeed = useDebugSeed;
            _debugSeed = debugSeed;
        }

        void Start()
        {
            if (_map == null || _camera == null || _ghost == null || _input == null || _hud == null)
            {
                Debug.LogError("[FlyingFox] GameSession missing refs — add GameBootstrap to the scene.");
                enabled = false;
                return;
            }

            ApplyHexSize();
            _camera.ConfigureFromBalance(GameBalance.WebParity);
            StartClassicRun(_useDebugSeed ? _debugSeed : (int?)null);
        }

        void ApplyHexSize()
        {
            _map.SetHexSize(_hexSize);
            _ghost.SetHexSize(_hexSize);
        }

        /// <summary>Start Classic with random seed (or fixed if null and debug flag).</summary>
        public void StartClassicRun() => StartClassicRun(null);

        /// <summary>Start Classic with optional fixed seed (for “Same seed” / debug).</summary>
        public void StartClassicRun(int? seed)
        {
            if (_map == null) return;

            if (_run != null)
                _run.Changed -= OnRunChanged;

            _activeSeed = seed ?? (_useDebugSeed ? _debugSeed : unchecked(System.Environment.TickCount));
            _run = new RunController();
            _run.Changed += OnRunChanged;
            _run.Start(new RunConfig
            {
                Mode = RunMode.Classic,
                Seed = _activeSeed,
                Balance = GameBalance.WebParity,
            }, new SplitMix64Rng(_activeSeed));

            _input.Bind(_run, _camera, _map, _ghost, _cursor);
            _input.InputEnabled = true;
            _hud.Bind(_run, _activeSeed);
            _ghost.Hide();
            _camera.FocusWorld(Vector3.zero);
            OnRunChanged();

            Debug.Log($"[FlyingFox] Classic run seed={_activeSeed} platform-ready=switch-controls");
        }

        void OnDestroy()
        {
            if (_run != null)
                _run.Changed -= OnRunChanged;
            _input?.Unbind();
        }

        void OnRunChanged()
        {
            if (_run == null || _map == null) return;

            _map.Rebuild(_run.Board);
            _input.NotifyBoardChanged();

            if (_run.Phase == RunPhase.Ended)
            {
                _ghost.Hide();
                _cursor?.Unbind();
                _input.InputEnabled = false;
                var r = _run.BuildResult();
                Debug.Log(
                    $"[FlyingFox] End natural={r.NaturalEnd} score={r.Score} medal={r.Medal} " +
                    $"M={r.Breakdown.Matches} P={r.Breakdown.Perfects} T={r.Breakdown.Tiles} Q={r.Breakdown.Quests}");
            }
            else
            {
                _input.InputEnabled = true;
            }
        }
    }
}
