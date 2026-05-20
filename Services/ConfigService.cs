using MelonLoader;

namespace MegaChaos.Services;

internal static class ConfigService
{
    public static MelonPreferences_Entry<bool> Enabled { get; private set; }
    public static MelonPreferences_Entry<float> CheckIntervalSeconds { get; private set; }
    public static MelonPreferences_Entry<bool> ChaosModeEnabled { get; private set; }
    public static MelonPreferences_Entry<float> ChaosModeInterval { get; private set; }

    public static bool IsInitialized => Enabled != null && CheckIntervalSeconds != null && ChaosModeEnabled != null;

    public static string CurrentRules
    {
        get => ProfileManager.ActiveProfile?.Rules ?? string.Empty;
        set
        {
            var p = ProfileManager.ActiveProfile;
            if (p != null)
            {
                p.Rules = value;
                ProfileManager.Save();
            }
        }
    }

    public static void Initialize()
    {
        var category = MelonPreferences.CreateCategory(Constants.MODNAME, Constants.MODNAME);
        Enabled = category.CreateEntry("Enabled", true, "Enabled", "Enable item rewards.");
        CheckIntervalSeconds = category.CreateEntry("CheckIntervalSeconds", 1.0f, "Check Interval Seconds", "How often the mod checks time and kill rules.");
        ChaosModeEnabled = category.CreateEntry("ChaosModeEnabled", false, "Chaos Mode Enabled", "Enable random chaos effects.");
        ChaosModeInterval = category.CreateEntry("ChaosModeInterval", 30.0f, "Chaos Mode Interval", "Time between chaos effects in seconds.");

        ProfileManager.Initialize();

        ClampValues();
    }

    public static void ClampValues()
    {
        if (!IsInitialized)
            return;

        if (CheckIntervalSeconds.Value < 0.1f)
            CheckIntervalSeconds.Value = 0.1f;
        
        if (ChaosModeInterval.Value < 5.0f)
            ChaosModeInterval.Value = 5.0f;
    }
}
