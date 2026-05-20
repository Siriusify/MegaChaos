using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace MegaChaos.Services;

public class RuleProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "Default";
    public string Rules { get; set; } = string.Empty;
    public bool ChaosEnabled { get; set; } = false;
    public float ChaosInterval { get; set; } = 30f;
    public float ChaosDurationMultiplier { get; set; } = 1f;
}

public class ProfileDataStore
{
    public string ActiveProfileId { get; set; }
    public List<RuleProfile> Profiles { get; set; } = new();
}

public static class ProfileManager
{
    private static ProfileDataStore _store = new();
    private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public static IReadOnlyList<RuleProfile> Profiles => _store.Profiles;
    
    public static RuleProfile ActiveProfile
    {
        get
        {
            var profile = _store.Profiles.FirstOrDefault(p => p.Id == _store.ActiveProfileId);
            if (profile == null && _store.Profiles.Count > 0)
            {
                profile = _store.Profiles[0];
                _store.ActiveProfileId = profile.Id;
                Save();
            }
            return profile;
        }
    }

    private static string GetFilePath()
    {
        var dir = Path.Combine(MelonLoader.Utils.MelonEnvironment.UserDataDirectory, Constants.MODNAME);
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        return Path.Combine(dir, "profiles.json");
    }

    public static void Initialize()
    {
        var path = GetFilePath();
        if (File.Exists(path))
        {
            try
            {
                var json = File.ReadAllText(path);
                _store = JsonSerializer.Deserialize<ProfileDataStore>(json, _jsonOptions) ?? new ProfileDataStore();
            }
            catch (Exception ex)
            {
                Main.Error($"Failed to load profiles.json: {ex.Message}");
            }
        }

        if (_store.Profiles.Count == 0)
        {
            var defaultProfile = new RuleProfile 
            { 
                Name = "Default", 
                Rules = "time:60:Key:1;kills:100:BeefyRing:1" 
            };
            _store.Profiles.Add(defaultProfile);
            _store.ActiveProfileId = defaultProfile.Id;
            Save();
        }
    }

    public static void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(_store, _jsonOptions);
            File.WriteAllText(GetFilePath(), json);
        }
        catch (Exception ex)
        {
            Main.Error($"Failed to save profiles.json: {ex.Message}");
        }
    }

    public static void SetActiveProfile(string id)
    {
        if (_store.Profiles.Any(p => p.Id == id))
        {
            _store.ActiveProfileId = id;
            Save();
        }
    }

    public static void CreateProfile(string name)
    {
        var profile = new RuleProfile { Name = string.IsNullOrWhiteSpace(name) ? "New Profile" : name };
        _store.Profiles.Add(profile);
        Save();
    }

    public static void DeleteProfile(string id)
    {
        if (_store.Profiles.Count <= 1) return; // Cannot delete last profile
        var profileToRemove = _store.Profiles.FirstOrDefault(p => p.Id == id);
        if (profileToRemove != null)
        {
            _store.Profiles.Remove(profileToRemove);
            if (_store.ActiveProfileId == id)
                _store.ActiveProfileId = _store.Profiles[0].Id;
            Save();
        }
    }

    public static void RenameProfile(string id, string newName)
    {
        var profile = _store.Profiles.FirstOrDefault(p => p.Id == id);
        if (profile != null)
        {
            profile.Name = string.IsNullOrWhiteSpace(newName) ? "Unnamed Profile" : newName;
            Save();
        }
    }
}
