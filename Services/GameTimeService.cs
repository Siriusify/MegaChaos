using System;

namespace MegaChaos.Services;

internal static class GameTimeService
{
    private static string _lastSource;
    private static bool _warnedMissing;

    public static float? TryGetElapsedStageSeconds()
    {
        try
        {
            var gameManagerType = GameReflection.FindType("GameManager", "Il2CppGameManager");
            var gameManager = GameReflection.GetStaticMember(gameManagerType, "Instance");

            var gameTimer = GameReflection.GetMember(gameManager, "gameTimer");
            if (TryConvertToPositiveFloat(gameTimer, out var gameTimerValue))
            {
                LogSourceOnce("GameManager.gameTimer");
                return gameTimerValue;
            }

            var stageTimeline = GetStageTimeline(gameManager);
            if (stageTimeline != null)
            {
                var stageTime = GameReflection.InvokeInstance(stageTimeline, "GetStageTime", Type.EmptyTypes);
                if (TryConvertToPositiveFloat(stageTime, out var stageTimeValue))
                {
                    LogSourceOnce("StageTimeline.GetStageTime");
                    return stageTimeValue;
                }

                var stageTimeMember = GameReflection.GetMember(stageTimeline, "stageTime");
                if (TryConvertToPositiveFloat(stageTimeMember, out var stageTimeFieldValue))
                {
                    LogSourceOnce("StageTimeline.stageTime");
                    return stageTimeFieldValue;
                }
            }

            var gameTimerType = GameReflection.FindType("GameTimer", "Il2CppGameTimer");
            var stageTimer = GameReflection.GetStaticMember(gameTimerType, "stageTimer");
            if (TryConvertToPositiveFloat(stageTimer, out var stageTimerValue))
            {
                LogSourceOnce("GameTimer.stageTimer");
                return stageTimerValue;
            }
        }
        catch (Exception ex)
        {
            Main.Error($"Failed to read stage time: {ex.GetBaseException().Message}");
        }

        if (!_warnedMissing)
        {
            _warnedMissing = true;
            Main.Warn("Could not resolve a stage timer yet. Time rules are waiting for a valid game timer.");
        }

        return null;
    }

    private static object GetStageTimeline(object gameManager)
    {
        var timeline = GameReflection.GetMember(gameManager, "stageTimeline");
        if (timeline != null)
            return timeline;

        return GameReflection.GetMember(gameManager, "timeline");
    }

    private static bool TryConvertToPositiveFloat(object value, out float converted)
    {
        converted = 0f;
        if (value == null)
            return false;

        try
        {
            converted = Convert.ToSingle(value);
            return converted >= 0f;
        }
        catch
        {
            return false;
        }
    }

    private static void LogSourceOnce(string source)
    {
        _warnedMissing = false;
        if (_lastSource == source)
            return;

        _lastSource = source;
        Main.Msg($"Using game timer source: {source}");
    }
}
