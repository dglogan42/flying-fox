using FlyingFox.Presentation;
using UnityEngine;

namespace FlyingFox.App
{
    /// <summary>
    /// Builds a playable Game hierarchy at runtime.
    /// Place on a GameObject in <c>Game.unity</c>, or rely on auto-bootstrap
    /// for empty/SampleScene/Untitled scenes.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] float _hexSize = 1f;
        [SerializeField] bool _useDebugSeed;
        [SerializeField] int _debugSeed = 42;

        bool _built;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoBootstrapIfEmpty()
        {
            if (FindAny<GameSession>() != null) return;
            if (FindAny<GameBootstrap>() != null) return;

            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.name != "Game" && scene.name != "Boot" && scene.name != "SampleScene" &&
                scene.name != "Untitled")
                return;

            var host = new GameObject("FlyingFox_Bootstrap");
            host.AddComponent<GameBootstrap>();
        }

        static T FindAny<T>() where T : Object
        {
#if UNITY_2023_1_OR_NEWER
            return Object.FindFirstObjectByType<T>();
#else
            return Object.FindObjectOfType<T>();
#endif
        }

        void Awake()
        {
            var existing = FindAny<GameSession>();
            if (existing != null && existing.gameObject != gameObject)
            {
                Destroy(gameObject);
                return;
            }

            Build();
        }

        public void Build()
        {
            if (_built) return;
            _built = true;

            var root = new GameObject("— Flying Fox —");

            // ── Camera ─────────────────────────────────────
            Camera cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera");
                cam = camGo.AddComponent<Camera>();
                camGo.tag = "MainCamera";
                camGo.AddComponent<AudioListener>();
            }
            else if (cam.GetComponent<AudioListener>() == null)
            {
                cam.gameObject.AddComponent<AudioListener>();
            }

            cam.transform.SetParent(root.transform, true);
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.transform.rotation = Quaternion.identity;
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.04f, 0.09f, 0.06f);
            cam.nearClipPlane = -50f;
            cam.farClipPlane = 100f;

            var mapCam = cam.GetComponent<MapCameraController>()
                         ?? cam.gameObject.AddComponent<MapCameraController>();

            // ── Light ──────────────────────────────────────
            if (FindAny<Light>() == null)
            {
                var lightGo = new GameObject("Directional Light");
                lightGo.transform.SetParent(root.transform, false);
                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Directional;
                light.color = new Color(1f, 0.98f, 0.92f);
                light.intensity = 1f;
                lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            }

            // ── Map + ghost ────────────────────────────────
            var mapGo = new GameObject("HexMap");
            mapGo.transform.SetParent(root.transform, false);
            var map = mapGo.AddComponent<HexMapView>();
            map.SetHexSize(_hexSize);
            map.EnsureRoots();

            var ghostGo = new GameObject("GhostPlacement");
            ghostGo.transform.SetParent(root.transform, false);
            var ghost = ghostGo.AddComponent<GhostPlacementView>();
            ghost.SetHexSize(_hexSize);
            ghost.Ensure();

            // ── Session host ───────────────────────────────
            transform.SetParent(root.transform, false);
            gameObject.name = "GameSession";

            var input = gameObject.GetComponent<MapInputController>()
                        ?? gameObject.AddComponent<MapInputController>();
            var hud = gameObject.GetComponent<GameplayHudImgui>()
                      ?? gameObject.AddComponent<GameplayHudImgui>();
            var session = gameObject.GetComponent<GameSession>()
                          ?? gameObject.AddComponent<GameSession>();

            session.Configure(_hexSize, _useDebugSeed, _debugSeed);
            session.Wire(map, mapCam, ghost, input, hud);

            Debug.Log("[FlyingFox] GameBootstrap ready — Classic run starts on GameSession.Start.");
        }
    }
}
