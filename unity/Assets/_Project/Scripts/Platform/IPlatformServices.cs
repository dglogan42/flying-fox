namespace FlyingFox.Platform
{
    /// <summary>Thin platform façade — Steam / Switch / desktop.</summary>
    public interface IPlatformServices
    {
        string PlatformId { get; }
        bool SupportsGamepadCursor { get; }
        void Init();
        void Shutdown();
        /// <summary>Show system-native confirm if available; else true.</summary>
        bool ConfirmQuitOrAbandon(string message);
    }

    public static class PlatformServices
    {
        public static IPlatformServices Current { get; private set; } = new DesktopPlatformServices();

        public static void Set(IPlatformServices services)
        {
            Current?.Shutdown();
            Current = services ?? new DesktopPlatformServices();
            Current.Init();
        }
    }

    public sealed class DesktopPlatformServices : IPlatformServices
    {
        public string PlatformId => "desktop";
        public bool SupportsGamepadCursor => true;
        public void Init() { }
        public void Shutdown() { }
        public bool ConfirmQuitOrAbandon(string message) => true;
    }

    /// <summary>
    /// Switch implementation body is compiled only with Nintendo SDK + UNITY_SWITCH.
    /// Without the SDK this type still exists as a stub for Editor simulation via FF_SWITCH_SIM.
    /// </summary>
    public sealed class SwitchPlatformServices : IPlatformServices
    {
        public string PlatformId =>
#if UNITY_SWITCH
            "switch";
#else
            "switch-sim";
#endif

        public bool SupportsGamepadCursor => true;

        public void Init()
        {
#if UNITY_SWITCH
            // NintendoSDK: nn.fs save mount, account, etc. — fill when SDK available.
#endif
        }

        public void Shutdown()
        {
#if UNITY_SWITCH
            // Unmount save data.
#endif
        }

        public bool ConfirmQuitOrAbandon(string message)
        {
#if UNITY_SWITCH
            // Optional: software keyboard / system dialog
            return true;
#else
            return true;
#endif
        }
    }

    /// <summary>
    /// Generic China-market Android sideload channel for tablets and phones
    /// (Xiaoxin, Huawei, Xiaomi, Oppo, Vivo, Honor, etc.).
    /// No Google Play, no GMS, no Play Billing — APK install only.
    /// Active when built with <c>FF_CHINA_SIDELOAD</c> (or legacy <c>FF_XIAOXIN</c>).
    /// </summary>
    public sealed class ChinaSideloadPlatformServices : IPlatformServices
    {
        public string PlatformId =>
#if FF_CHINA_SIDELOAD || FF_XIAOXIN
            "china-sideload";
#else
            "china-sideload-sim";
#endif

        /// <summary>Touch-first; gamepad still works via Unity Input if present.</summary>
        public bool SupportsGamepadCursor => false;

        public void Init()
        {
            // Offline-first: local storage only. No Google Play, GMS, or Play Billing.
            // Safe on pure AOSP / OEM ROMs without GMS (common in mainland China).
        }

        public void Shutdown() { }

        public bool ConfirmQuitOrAbandon(string message) => true;
    }

    /// <summary>Obsolete name — use <see cref="ChinaSideloadPlatformServices"/>.</summary>
    [System.Obsolete("Use ChinaSideloadPlatformServices (generic China sideload APK).")]
    public sealed class XiaoxinProPlatformServices : IPlatformServices
    {
        readonly ChinaSideloadPlatformServices _inner = new ChinaSideloadPlatformServices();
        public string PlatformId => _inner.PlatformId;
        public bool SupportsGamepadCursor => _inner.SupportsGamepadCursor;
        public void Init() => _inner.Init();
        public void Shutdown() => _inner.Shutdown();
        public bool ConfirmQuitOrAbandon(string message) => _inner.ConfirmQuitOrAbandon(message);
    }
}
