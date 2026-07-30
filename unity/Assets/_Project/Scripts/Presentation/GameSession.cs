using FlyingFox.Core;
using UnityEngine;

namespace FlyingFox.Presentation
{
    /// <summary>
    /// Wires Core RunController to map view, camera, input, and HUD.
    /// First playable Classic loop.
    /// </summary>
    public sealed class GameSession : MonoBehaviour
    {
        [SerializeField] int _debugSeed;
        [SerializeField] bool _useDebugSeed;
        [SerializeField] float _hexSize = HexMeshUtil.DefaultSize;

        [SerializeField] HexMapView _map;
        [SerializeField] MapCameraController _camera;
        [SerializeField] GhostPlacementView _ghost;
        [SerializeField] MapInputController _input;
        [SerializeField] GameplayHudImgui _hud;

        RunController _run;
        public RunController Run => _run;
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
            GameplayHudImgui hud)
        {
            _map = map;
            _camera = camera;
            _ghost = ghost;
            _input = input;
            _hud = hud;
        }

        void Start()
        {
            if (_map == null || _camera == null)
            {
                Debug.LogError("GameSession missing map/camera — use GameBootstrap.");
                return;
            }

            _map.SetHexSize(_hexSize);
            _ghost.SetHexSize(_hexSize);
            _camera.ConfigureFromBalance(GameBalance.WebParity);
            StartClassicRun();
        }

        public void StartClassicRun()
        {
            int seed = _useDebugSeed ? _debugSeed : System.Environment.TickCount;
            _run = new RunController();
            _run.Changed += OnRunChanged;
            _run.Start(new RunConfig
            {
                Mode = RunMode.Classic,
                Seed = seed,
                Balance = GameBalance.WebParity,
            }, new SplitMix64Rng(seed));

            _input.Bind(_run, _camera, _map, _ghost);
            _hud.Bind(_run);
            _camera.FocusWorld(Vector3.zero);
            OnRunChanged();
            Debug.Log($"[FlyingFox] Classic run started seed={seed}");
        }

        void OnDestroy()
        {
            if (_run != null)
                _run.Changed -= OnRunChanged;
        }

        void OnRunChanged()
        {
            if (_run == null) return;
            _map.Rebuild(_run.Board);
            if (_run.Phase == RunPhase.Ended)
            {
                _ghost.Hide();
                var r = _run.BuildResult();
                Debug.Log($"[FlyingFox] Run ended natural={r.NaturalEnd} score={r.Score} medal={r.Medal}");
            }
        }
    }
}
