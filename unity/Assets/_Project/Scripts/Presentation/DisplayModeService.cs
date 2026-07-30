using System;
using UnityEngine;

namespace FlyingFox.Presentation
{
    public enum DisplayFormFactor
    {
        /// <summary>Phone-like / Switch handheld (~720p, close viewing).</summary>
        Handheld,
        /// <summary>TV / Switch docked (~1080p, 10ft UI).</summary>
        Docked,
        /// <summary>PC window / editor — treated like docked if large.</summary>
        Desktop,
    }

    /// <summary>
    /// Dock / undock (and window resize) → UI scale + safe area + camera ortho hints.
    /// Switch: resolution changes on dock; desktop simulates via window size.
    /// </summary>
    public sealed class DisplayModeService : MonoBehaviour
    {
        public static DisplayModeService Instance { get; private set; }

        [Header("Thresholds")]
        [SerializeField] int _dockedMinWidth = 1600;
        [SerializeField] int _dockedMinHeight = 900;
        [Tooltip("Extra TV overscan margin as fraction of screen (docked only).")]
        [SerializeField] float _tvOverscan = 0.04f;

        [Header("UI scale")]
        [SerializeField] float _handheldUiScale = 1f;
        [SerializeField] float _dockedUiScale = 1.28f;
        [SerializeField] float _desktopUiScale = 1.1f;

        [Header("Camera base ortho (before player zoom)")]
        [SerializeField] float _handheldOrtho = 7.2f;
        [SerializeField] float _dockedOrtho = 8.5f;

        int _lastW;
        int _lastH;
        DisplayFormFactor _form;
        float _uiScale = 1f;
        Rect _safeGui;

        public DisplayFormFactor FormFactor => _form;
        public float UiScale => _uiScale;
        /// <summary>Safe rect in GUI (top-left) coordinates for layout.</summary>
        public Rect SafeGuiRect => _safeGui;
        public float RecommendedOrthoBase =>
            _form == DisplayFormFactor.Handheld ? _handheldOrtho : _dockedOrtho;

        public bool IsDockedLike =>
            _form == DisplayFormFactor.Docked || _form == DisplayFormFactor.Desktop;

        public event Action<DisplayFormFactor> FormFactorChanged;

        void OnEnable()
        {
            Instance = this;
            ForceRefresh(true);
        }

        void OnDisable()
        {
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            if (Screen.width != _lastW || Screen.height != _lastH)
                ForceRefresh(false);
        }

        void OnApplicationFocus(bool focus)
        {
            if (focus) ForceRefresh(false);
        }

        void OnApplicationPause(bool pauseStatus)
        {
            // Cert: wake from sleep — re-evaluate dock state
            if (!pauseStatus) ForceRefresh(false);
        }

        public void ForceRefresh(bool silent)
        {
            _lastW = Screen.width;
            _lastH = Screen.height;

            var next = DetectFormFactor(_lastW, _lastH);
            bool changed = next != _form;
            _form = next;
            _uiScale = next switch
            {
                DisplayFormFactor.Handheld => _handheldUiScale,
                DisplayFormFactor.Docked => _dockedUiScale,
                _ => _desktopUiScale,
            };
            _safeGui = ComputeSafeGuiRect(_lastW, _lastH, next);

            if (changed && !silent)
            {
                Debug.Log($"[FlyingFox] Display mode → {_form} {_lastW}x{_lastH} uiScale={_uiScale:0.00}");
                FormFactorChanged?.Invoke(_form);
            }
            else if (!silent && changed == false)
            {
                // Still notify listeners for safe-area resize
                FormFactorChanged?.Invoke(_form);
            }
        }

        DisplayFormFactor DetectFormFactor(int w, int h)
        {
#if UNITY_SWITCH
            // Prefer operation mode when SDK available
            // nn.oe / UnityEngine.Switch APIs vary by package — resolution fallback is safe
#endif
            // Handheld: 720p-class or short side ≤ 800
            int minSide = Mathf.Min(w, h);
            int maxSide = Mathf.Max(w, h);
            if (minSide <= 800 || (w <= 1280 && h <= 800) || (h <= 1280 && w <= 800))
                return DisplayFormFactor.Handheld;

            if (w >= _dockedMinWidth && h >= _dockedMinHeight)
            {
#if UNITY_EDITOR || UNITY_STANDALONE
                // Large editor game view / desktop monitor
                return maxSide >= 1920 ? DisplayFormFactor.Desktop : DisplayFormFactor.Docked;
#else
                return DisplayFormFactor.Docked;
#endif
            }

            // In-between (e.g. 1366×768) — treat as handheld UI density
            return DisplayFormFactor.Handheld;
        }

        Rect ComputeSafeGuiRect(int w, int h, DisplayFormFactor form)
        {
            // Screen.safeArea is bottom-left; convert to GUI top-left
            Rect sa = Screen.safeArea;
            float x = sa.x;
            float yTop = h - (sa.y + sa.height);
            float width = sa.width;
            float height = sa.height;

            if (form == DisplayFormFactor.Docked || form == DisplayFormFactor.Desktop)
            {
                float mx = w * _tvOverscan;
                float my = h * _tvOverscan;
                x += mx;
                yTop += my;
                width -= mx * 2f;
                height -= my * 2f;
            }

            return new Rect(x, yTop, Mathf.Max(64f, width), Mathf.Max(64f, height));
        }

        /// <summary>Scale a base font/size by current UI scale (rounded).</summary>
        public int ScaleInt(int baseSize) =>
            Mathf.Max(10, Mathf.RoundToInt(baseSize * _uiScale));

        public float Scale(float baseValue) => baseValue * _uiScale;
    }
}
