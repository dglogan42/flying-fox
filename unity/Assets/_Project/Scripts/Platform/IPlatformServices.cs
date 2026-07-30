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
}
