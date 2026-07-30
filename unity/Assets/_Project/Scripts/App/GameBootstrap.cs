using FlyingFox.Presentation;
using UnityEngine;

namespace FlyingFox.App
{
    /// <summary>
    /// Builds a playable Game hierarchy at runtime so you can press Play
    /// without hand-authoring scene references in the Editor.
    /// Add this to an empty scene (or open Scenes/Game.unity).
    /// </summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoBootstrapIfEmpty()
        {
            if (FindAny<GameSession>() != null) return;
            if (FindAny<GameBootstrap>() != null) return;

            // Only hijack empty/default/Game scenes
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
            // Camera
            Camera cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera");
                cam = camGo.AddComponent<Camera>();
                camGo.tag = "MainCamera";
            }
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.orthographic = true;
            var mapCam = cam.GetComponent<MapCameraController>();
            if (mapCam == null) mapCam = cam.gameObject.AddComponent<MapCameraController>();

            // Light (optional for sprites)
            if (FindAny<Light>() == null)
            {
                var lightGo = new GameObject("Directional Light");
                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Directional;
                light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            }

            // Map root
            var mapGo = GameObject.Find("HexMap") ?? new GameObject("HexMap");
            var map = mapGo.GetComponent<HexMapView>() ?? mapGo.AddComponent<HexMapView>();
            map.EnsureRoots();

            var ghostGo = GameObject.Find("Ghost") ?? new GameObject("Ghost");
            var ghost = ghostGo.GetComponent<GhostPlacementView>() ?? ghostGo.AddComponent<GhostPlacementView>();

            var inputGo = gameObject;
            var input = inputGo.GetComponent<MapInputController>() ?? inputGo.AddComponent<MapInputController>();
            var hud = inputGo.GetComponent<GameplayHudImgui>() ?? inputGo.AddComponent<GameplayHudImgui>();
            var session = inputGo.GetComponent<GameSession>() ?? inputGo.AddComponent<GameSession>();

            session.Wire(map, mapCam, ghost, input, hud);
            gameObject.name = "FlyingFox_Game";

            Debug.Log("[FlyingFox] GameBootstrap ready — Classic run starts via GameSession.Start.");
        }
    }
}
