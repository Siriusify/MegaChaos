using System;
using System.Collections;
using System.Text.RegularExpressions;
using MelonLoader;
using UnityEngine.Networking;

namespace MegaChaos.Services;

internal static class VersionCheckService
{
    private const string GitHubConstantsUrl = "https://raw.githubusercontent.com/Siriusify/MegaChaos/main/Constants.cs";

    public static void CheckForUpdates()
    {
        MelonCoroutines.Start(CheckRoutine());
    }

    private static IEnumerator CheckRoutine()
    {
        var webRequest = UnityWebRequest.Get(GitHubConstantsUrl);
        yield return webRequest.SendWebRequest();

        if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError)
        {
            Main.Warn($"Failed to check for updates: {webRequest.error}");
            webRequest.Dispose();
            yield break;
        }

        string content = webRequest.downloadHandler.text;
        webRequest.Dispose();
        
        var match = Regex.Match(content, @"public const string VERSION = ""(.*?)"";");
        if (match.Success)
        {
            string latestVersionStr = match.Groups[1].Value;
            
            if (Version.TryParse(Constants.VERSION, out Version currentVersion) &&
                Version.TryParse(latestVersionStr, out Version latestVersion))
            {
                if (latestVersion > currentVersion)
                {
                    Main.Warn($"A new version of MegaChaos is available! (Current: v{Constants.VERSION}, Latest: v{latestVersion})");
                    NotificationService.Show($"Update Available: v{latestVersionStr}!\nYou are using v{Constants.VERSION}", type: NotificationService.NotificationType.Warning);
                }
                else
                {
                    Main.Msg($"MegaChaos is up to date (v{Constants.VERSION}).");
                }
            }
            else
            {
                // Fallback if Version parsing fails for some reason
                if (latestVersionStr != Constants.VERSION && !Constants.VERSION.Contains(latestVersionStr))
                {
                    Main.Warn($"A different version of MegaChaos is available! (Current: v{Constants.VERSION}, Latest: v{latestVersionStr})");
                    NotificationService.Show($"Update Available: v{latestVersionStr}!", type: NotificationService.NotificationType.Warning);
                }
            }
        }
        else
        {
            Main.Warn("Could not parse version from GitHub.");
        }
    }
}
