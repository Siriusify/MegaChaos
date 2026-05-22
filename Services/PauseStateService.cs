using UnityEngine;

namespace MegaChaos.Services;

internal static class PauseStateService
{
    private static readonly string[] PauseMenuPaths =
    {
        "PauseUI/Main",
        "PauseUI/Main/Inventory",
        "PauseUI/Main/W_Stats"
    };

    private static readonly string[] MenuPaths =
    {
        "PauseUI/Main",
        "PauseUI/Main/Inventory",
        "PauseUI/Main/W_Stats",
        "InventoryOverlay/W_Inventory",
        "InventoryOverlay/W_Stats (1)",
        "InventoryOverlay/W_Stats"
    };

    public static bool IsPauseMenuOpen()
    {
        return IsAnyPathActive(PauseMenuPaths);
    }

    public static bool IsMenuOpen()
    {
        return IsAnyPathActive(MenuPaths);
    }

    public static bool IsTimePaused()
    {
        if (Time.timeScale == 0f)
            return true;

        return IsPauseMenuOpen();
    }

    private static bool IsAnyPathActive(string[] paths)
    {
        foreach (var path in paths)
        {
            if (IsActivePath(path))
                return true;
        }
        return false;
    }

    private static bool IsActivePath(string path)
    {
        var obj = GameObject.Find(path);
        return obj != null && obj.activeInHierarchy;
    }
}
