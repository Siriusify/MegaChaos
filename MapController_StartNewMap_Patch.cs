using UnityEngine;

using MegaChaos.Services;

namespace MegaChaos;

internal static class MapController_StartNewMap_Patch
{
    public static void Postfix()
    {
        RuleScheduler.HandleStageStarted(Time.unscaledTime);
    }
}
