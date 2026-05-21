using HarmonyLib;
using MelonLoader;
using UnityEngine;

using System;
using System.Reflection;

using MegaChaos.Services;
using MegaChaos.Services.Chaos;
using MegaChaos.UI;

[assembly: MelonInfo(typeof(MegaChaos.Main), MegaChaos.Constants.MODNAME, MegaChaos.Constants.VERSION, MegaChaos.Constants.AUTHOR)]
[assembly: MelonGame(null, null)]

namespace MegaChaos;

public sealed class Main : MelonMod
{
    private const string DebugLoggingEnvVar = "MEGA_CHAOS_DEBUG";
    private static Main _instance;
    private RewardSchedulerWindow _window;
    private ChaosDebugWindow _chaosDebugWindow;
    private float _nextTickTime;

    internal static MelonLogger.Instance Log => _instance?.LoggerInstance;
    internal static bool DebugLoggingEnabled { get; } = IsTruthy(Environment.GetEnvironmentVariable(DebugLoggingEnvVar));

    public override void OnInitializeMelon()
    {
        _instance = this;

        ConfigService.Initialize();
        RuleScheduler.ReloadRules();
        RuleScheduler.ResetRun(Time.unscaledTime);
        _window = new RewardSchedulerWindow();


        // Örnek etkiyi kaydet
        ChaosEngine.Instance.RegisterEffect(new MegaChaos.Services.Chaos.Effects.DummySizeChangeEffect());
        ChaosEngine.Instance.RegisterEffect(new MegaChaos.Services.Chaos.Effects.MiniModeEffect());
        ChaosEngine.Instance.RegisterEffect(new MegaChaos.Services.Chaos.Effects.TimeBendEffect());
        ChaosEngine.Instance.RegisterEffect(new MegaChaos.Services.Chaos.Effects.HyperSpeedEffect());
        ChaosEngine.Instance.RegisterEffect(new MegaChaos.Services.Chaos.Effects.SlowMotionEffect());
        ChaosEngine.Instance.RegisterEffect(new MegaChaos.Services.Chaos.Effects.BlindnessEffect());
        ChaosEngine.Instance.RegisterEffect(new MegaChaos.Services.Chaos.Effects.NoHudEffect());
        ChaosEngine.Instance.RegisterEffect(new MegaChaos.Services.Chaos.Effects.UpsideDownEffect());
        ChaosEngine.Instance.RegisterEffect(new MegaChaos.Services.Chaos.Effects.MirrorXEffect());
        ChaosEngine.Instance.RegisterEffect(new MegaChaos.Services.Chaos.Effects.FovDistortionEffect());
        ChaosEngine.Instance.RegisterEffect(new MegaChaos.Services.Chaos.Effects.ZoomInEffect());
        ChaosEngine.Instance.RegisterEffect(new MegaChaos.Services.Chaos.Effects.ScreenShakeEffect());
        ChaosEngine.Instance.RegisterEffect(new MegaChaos.Services.Chaos.Effects.DrunkEffect());
        ChaosEngine.Instance.RegisterEffect(new MegaChaos.Services.Chaos.Effects.DvdScreensaverEffect());
        ChaosEngine.Instance.RegisterEffect(new MegaChaos.Services.Chaos.Effects.PacketLossEffect());
        ChaosEngine.Instance.RegisterEffect(new MegaChaos.Services.Chaos.Effects.HighGravityEffect());
        ChaosEngine.Instance.RegisterEffect(new MegaChaos.Services.Chaos.Effects.LowGravityEffect());
        ChaosEngine.Instance.RegisterEffect(new MegaChaos.Services.Chaos.Effects.ColorTintEffect());
        ChaosEngine.Instance.RegisterEffect(new MegaChaos.Services.Chaos.Effects.FakeGoldEffect());
        ChaosEngine.Instance.RegisterEffect(new MegaChaos.Services.Chaos.Effects.GoldHeistEffect());
        ChaosEngine.Instance.RegisterEffect(new MegaChaos.Services.Chaos.Effects.RandomItemEffect());
        ChaosEngine.Instance.RegisterEffect(new MegaChaos.Services.Chaos.Effects.FakeItemLotteryEffect());
        ChaosEngine.Instance.RegisterEffect(new MegaChaos.Services.Chaos.Effects.RandomTomeEffect());
        ChaosEngine.Instance.RegisterEffect(new MegaChaos.Services.Chaos.Effects.FakeTomeLotteryEffect());
        ChaosEngine.Instance.RegisterEffect(new MegaChaos.Services.Chaos.Effects.RandomStatEffect());
        ChaosEngine.Instance.RegisterEffect(new MegaChaos.Services.Chaos.Effects.ExpCleanerEffect());
        ChaosEngine.Instance.RegisterEffect(new MegaChaos.Services.Chaos.Effects.CantAttackEffect());
        ChaosEngine.Instance.RegisterEffect(new MegaChaos.Services.Chaos.Effects.OneHitKOEffect());
        // --- New effects ---
        ChaosEngine.Instance.RegisterEffect(new MegaChaos.Services.Chaos.Effects.LightsOutEffect());
        ChaosEngine.Instance.RegisterEffect(new MegaChaos.Services.Chaos.Effects.RainbowEffect());
        ChaosEngine.Instance.RegisterEffect(new MegaChaos.Services.Chaos.Effects.ConfusionEffect());
        ChaosEngine.Instance.RegisterEffect(new MegaChaos.Services.Chaos.Effects.ReverseGravityEffect());
        ChaosEngine.Instance.RegisterEffect(new MegaChaos.Services.Chaos.Effects.VignetteEffect());
        ChaosEngine.Instance.RegisterEffect(new MegaChaos.Services.Chaos.Effects.TvStaticEffect());

        if (DebugLoggingEnabled)
        {
            _chaosDebugWindow = new ChaosDebugWindow();
        }

        var harmony = new HarmonyLib.Harmony(Constants.GUID);
        PatchStartNewMap(harmony);

        Msg($"Loaded {Constants.MODNAME} v{Constants.VERSION}");
    }

    public override void OnUpdate()
    {
        _window?.Update();
        _chaosDebugWindow?.Update();
        ChaosEngine.Instance.Update();

        var now = Time.unscaledTime;
        if (now < _nextTickTime)
            return;

        _nextTickTime = now + ConfigService.CheckIntervalSeconds.Value;
        RuleScheduler.Tick(now);
    }

    public override void OnGUI()
    {
        _window?.OnGUI();
        _chaosDebugWindow?.OnGUI();
        ChaosEngine.Instance.OnGUI();
        NotificationService.Draw();
    }

    public override void OnPreferencesSaved()
    {
        if (!ConfigService.IsInitialized)
            return;

        ConfigService.ClampValues();
        RuleScheduler.ReloadRules();
    }

    public override void OnPreferencesLoaded()
    {
        if (!ConfigService.IsInitialized)
            return;

        ConfigService.ClampValues();
        RuleScheduler.ReloadRules();
    }

    private static void PatchStartNewMap(HarmonyLib.Harmony harmony)
    {
        var targetType = GameReflection.FindType(
            "Il2CppAssets.Scripts.Managers.MapController",
            "Assets.Scripts.Managers.MapController",
            "MapController");

        if (targetType == null)
        {
            Warn("Could not find MapController. Time rules will start when the mod loads.");
            return;
        }

        var targetMethod = GameReflection.FindAnyMethod(targetType, "StartNewMap");
        var postfix = typeof(MapController_StartNewMap_Patch).GetMethod(
            nameof(MapController_StartNewMap_Patch.Postfix),
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

        if (targetMethod == null || postfix == null)
        {
            Warn("Could not patch MapController.StartNewMap. Time rules will not reset on new map.");
            return;
        }

        harmony.Patch(targetMethod, postfix: new HarmonyMethod(postfix));
        Msg($"Patched {targetType.FullName}.StartNewMap");
    }

    internal static void Msg(string message)
    {
        if (DebugLoggingEnabled)
            Log?.Msg(message);
    }

    internal static void Warn(string message)
    {
        if (DebugLoggingEnabled)
            Log?.Warning(message);
    }

    internal static void Error(string message) => Log?.Error(message);

    private static bool IsTruthy(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        switch (value.Trim().ToLowerInvariant())
        {
            case "1":
            case "true":
            case "yes":
            case "on":
            case "debug":
                return true;
            default:
                return false;
        }
    }
}
