using FlyingFox.Core;
using UnityEngine;

namespace FlyingFox.Presentation
{
    public enum PauseMenuPage
    {
        Main,
        ConfirmAbandon,
        Controls,
    }

    /// <summary>
    /// Cert-critical pause: + / Esc / Start. Freezes gameplay (timeScale 0),
    /// blocks map input, resume / new run / abandon with confirm.
    /// </summary>
    public sealed class PauseMenuController : MonoBehaviour
    {
        public static PauseMenuController Instance { get; private set; }
        public static bool IsPaused { get; private set; }
        public static bool IsPointerOverPause { get; private set; }

        [SerializeField] bool _pauseOnFocusLoss = true;

        RunController _run;
        PauseMenuPage _page;
        GUIStyle _title;
        GUIStyle _body;
        GUIStyle _btn;
        GUIStyle _btnDanger;
        GUIStyle _muted;
        bool _styles;
        float _prevTimeScale = 1f;
        int _focusIndex;

        public void Bind(RunController run)
        {
            _run = run;
            if (IsPaused) Resume();
        }

        void OnEnable() => Instance = this;

        void OnDisable()
        {
            if (Instance == this) Instance = null;
            if (IsPaused)
            {
                Time.timeScale = _prevTimeScale > 0f ? _prevTimeScale : 1f;
                IsPaused = false;
            }
            IsPointerOverPause = false;
        }

        void Update()
        {
            if (_run == null) return;

            // Toggle pause when playing (not on end screen)
            bool toggle =
                Input.GetKeyDown(KeyCode.Escape) ||
                Input.GetKeyDown(KeyCode.P) ||
                Input.GetKeyDown(KeyCode.JoystickButton7); // Start / +

            if (toggle)
            {
                if (_run.Phase == RunPhase.Ended)
                    return;

                if (IsPaused)
                {
                    if (_page == PauseMenuPage.Controls || _page == PauseMenuPage.ConfirmAbandon)
                        _page = PauseMenuPage.Main;
                    else
                        Resume();
                }
                else if (_run.Phase == RunPhase.Playing)
                {
                    Pause();
                }
            }

            if (!IsPaused) return;

            // Simple D-pad / stick menu navigation for Switch cert
            if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S) ||
                Input.GetKeyDown(KeyCode.JoystickButton0) == false && NudgeDown())
                _focusIndex++;
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W) || NudgeUp())
                _focusIndex--;
        }

        bool NudgeDown()
        {
            return Input.GetAxisRaw("Vertical") < -0.7f && Input.anyKeyDown;
        }

        bool NudgeUp()
        {
            return Input.GetAxisRaw("Vertical") > 0.7f && Input.anyKeyDown;
        }

        public void Pause()
        {
            if (IsPaused) return;
            if (_run != null && _run.Phase != RunPhase.Playing) return;

            IsPaused = true;
            _page = PauseMenuPage.Main;
            _focusIndex = 0;
            _prevTimeScale = Time.timeScale;
            Time.timeScale = 0f;

            var session = GameSession.Instance;
            if (session != null)
                session.SetGameplayInputEnabled(false);

            Debug.Log("[FlyingFox] Paused");
        }

        public void Resume()
        {
            if (!IsPaused) return;
            IsPaused = false;
            _page = PauseMenuPage.Main;
            Time.timeScale = _prevTimeScale > 0f ? _prevTimeScale : 1f;
            IsPointerOverPause = false;

            var session = GameSession.Instance;
            if (session != null && _run != null && _run.Phase == RunPhase.Playing)
                session.SetGameplayInputEnabled(true);

            Debug.Log("[FlyingFox] Resumed");
        }

        void OnApplicationPause(bool pauseStatus)
        {
            if (!_pauseOnFocusLoss) return;
            if (pauseStatus && _run != null && _run.Phase == RunPhase.Playing && !IsPaused)
                Pause();
        }

        void OnApplicationFocus(bool hasFocus)
        {
            if (!_pauseOnFocusLoss) return;
            if (!hasFocus && _run != null && _run.Phase == RunPhase.Playing && !IsPaused)
                Pause();
        }

        void EnsureStyles(float uiScale)
        {
            // Rebuild each mode change cheaply via scale on sizes
            _title = new GUIStyle(GUI.skin.label)
            {
                fontSize = Scale(28, uiScale),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.96f, 0.64f, 0.38f) },
            };
            _body = new GUIStyle(GUI.skin.label)
            {
                fontSize = Scale(16, uiScale),
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                normal = { textColor = new Color(0.99f, 0.98f, 0.88f) },
            };
            _muted = new GUIStyle(GUI.skin.label)
            {
                fontSize = Scale(13, uiScale),
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                normal = { textColor = new Color(0.56f, 0.7f, 0.6f) },
            };
            _btn = new GUIStyle(GUI.skin.button)
            {
                fontSize = Scale(16, uiScale),
                fontStyle = FontStyle.Bold,
                fixedHeight = Scale(44, uiScale),
            };
            _btnDanger = new GUIStyle(_btn);
            _styles = true;
        }

        static int Scale(int v, float s) => Mathf.Max(11, Mathf.RoundToInt(v * s));

        void OnGUI()
        {
            if (!IsPaused)
            {
                IsPointerOverPause = false;
                return;
            }

            float ui = DisplayModeService.Instance != null
                ? DisplayModeService.Instance.UiScale
                : 1f;
            EnsureStyles(ui);

            // Full-screen dim
            Color prev = GUI.color;
            GUI.color = new Color(0.02f, 0.05f, 0.03f, 0.82f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = prev;

            Rect safe = DisplayModeService.Instance != null
                ? DisplayModeService.Instance.SafeGuiRect
                : new Rect(0, 0, Screen.width, Screen.height);

            float boxW = Mathf.Min(safe.width * 0.85f, 420f * ui);
            float boxH = Mathf.Min(safe.height * 0.75f, 460f * ui);
            var box = new Rect(
                safe.x + (safe.width - boxW) * 0.5f,
                safe.y + (safe.height - boxH) * 0.5f,
                boxW,
                boxH);

            GUI.color = new Color(0.08f, 0.14f, 0.1f, 0.96f);
            GUI.Box(box, GUIContent.none);
            GUI.color = Color.white;

            GUILayout.BeginArea(new Rect(box.x + 20f * ui, box.y + 16f * ui, box.width - 40f * ui, box.height - 32f * ui));

            switch (_page)
            {
                case PauseMenuPage.Main:
                    DrawMain();
                    break;
                case PauseMenuPage.ConfirmAbandon:
                    DrawConfirmAbandon();
                    break;
                case PauseMenuPage.Controls:
                    DrawControls();
                    break;
            }

            GUILayout.EndArea();

            Vector2 guiMouse = Event.current != null
                ? Event.current.mousePosition
                : new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
            IsPointerOverPause = box.Contains(guiMouse);
        }

        void DrawMain()
        {
            GUILayout.Label("Paused", _title);
            GUILayout.Label("Flying Fox", _muted);
            if (DisplayModeService.Instance != null)
            {
                var d = DisplayModeService.Instance;
                GUILayout.Label($"{d.FormFactor} · UI ×{d.UiScale:0.00} · {Screen.width}×{Screen.height}", _muted);
            }
            GUILayout.Space(16);

            if (GUILayout.Button("Resume  (+ / Esc)", _btn))
                Resume();
            GUILayout.Space(8);
            if (GUILayout.Button("Controls", _btn))
                _page = PauseMenuPage.Controls;
            GUILayout.Space(8);
            if (GUILayout.Button("New run", _btn))
            {
                Resume();
                GameSession.Instance?.StartClassicRun();
            }
            GUILayout.Space(8);
            if (GUILayout.Button("Abandon run…", _btnDanger))
                _page = PauseMenuPage.ConfirmAbandon;

            GUILayout.FlexibleSpace();
            GUILayout.Label("Lotcheck: pause must open from + / Start and return cleanly.", _muted);
        }

        void DrawConfirmAbandon()
        {
            GUILayout.Label("Abandon run?", _title);
            GUILayout.Label("Score will be kept for this run’s end screen. Progress is not saved mid-run in v1.", _body);
            GUILayout.Space(20);
            if (GUILayout.Button("Yes, abandon", _btnDanger))
            {
                var run = _run;
                Resume();
                run?.Abandon();
            }
            GUILayout.Space(8);
            if (GUILayout.Button("Cancel", _btn))
                _page = PauseMenuPage.Main;
        }

        void DrawControls()
        {
            GUILayout.Label("Controls", _title);
            GUILayout.Space(8);
            GUILayout.Label("A / LMB — Place\nB·Y / R·Q — Rotate\nX / Tab — Cycle hand\nStick / D-pad — Move cursor\nRight stick / RMB — Pan\n+ / Esc — Pause", _body);
            GUILayout.Space(16);
            if (GUILayout.Button("Back", _btn))
                _page = PauseMenuPage.Main;
        }
    }
}
