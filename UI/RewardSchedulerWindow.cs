using MelonLoader;
using UnityEngine;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using MegaChaos.Services.Chaos;

using MegaChaos.Services;

namespace MegaChaos.UI;

internal sealed class RewardSchedulerWindow
{
    private enum RuleFilterMode
    {
        All,
        Active,
        Disabled
    }

    
    private static GUIStyle _panelStyle;
    private static GUIStyle _cellStyleBg;
    private static GUIStyle _headerStyle;
    private static GUIStyle _dropdownStyle;
    private static GUIStyle _frameStyle;

    private static void DrawRoundedRect(Rect rect, GUIStyle style)
    {
        if (style != null && style.normal.background != null)
        {
            GUI.Box(rect, new GUIContent(string.Empty), style);
        }
    }

    private static GUIStyle CreateRoundedStyle(Color bgColor, Color borderColor, int radius, int borderSize)
    {
        var style = new GUIStyle();
        style.normal.background = GenerateRoundedTexture(bgColor, borderColor, radius, borderSize);
        int borderInset = radius + 2;
        var offset = new RectOffset();
        offset.left = borderInset;
        offset.right = borderInset;
        offset.top = borderInset;
        offset.bottom = borderInset;
        style.border = offset;
        return style;
    }

    private static Texture2D GenerateRoundedTexture(Color bgColor, Color borderColor, int radius, int borderSize)
    {
        int size = radius * 2 + 4;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.hideFlags = HideFlags.HideAndDontSave;
        
        float center = size / 2f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Abs(x + 0.5f - center);
                float dy = Mathf.Abs(y + 0.5f - center);
                float cornerDx = Mathf.Max(0, dx - 2f);
                float cornerDy = Mathf.Max(0, dy - 2f);
                float dist = Mathf.Sqrt(cornerDx * cornerDx + cornerDy * cornerDy);
                
                if (dist > radius)
                {
                    tex.SetPixel(x, y, Color.clear);
                }
                else if (dist > radius - 1f)
                {
                    Color c = borderSize > 0 ? borderColor : bgColor;
                    c.a *= (radius - dist);
                    tex.SetPixel(x, y, c);
                }
                else if (dist > radius - borderSize)
                {
                    tex.SetPixel(x, y, borderColor);
                }
                else if (dist > radius - borderSize - 1f)
                {
                    tex.SetPixel(x, y, Color.Lerp(bgColor, borderColor, dist - (radius - borderSize - 1f)));
                }
                else
                {
                    tex.SetPixel(x, y, bgColor);
                }
            }
        }
        tex.Apply();
        return tex;
    }

    private static Texture2D _panelTexture;
    private static Texture2D _panelBorderTexture;
    private static Texture2D _cellTexture;
    private static Texture2D _headerTexture;
    private static Texture2D _dropdownTexture;
    
    private static GUIStyle _titleStyle;
    private static GUIStyle _sectionStyle;
    private static GUIStyle _cellStyle;
    private static GUIStyle _headerCellStyle;
    private static GUIStyle _valueStyle;
    private static GUIStyle _statusStyle;
    private static GUIStyle _previewItemStyle;
    private static GUIStyle _statusEnabledStyle;
    private static GUIStyle _statusDisabledStyle;
    private static GUIStyle _mutedStyle;
    
    private static GUIStyle _buttonStyle;
    private static GUIStyle _accentButtonStyle;
    private static GUIStyle _dangerButtonStyle;
    private static GUIStyle _fieldButtonStyle;
    private static GUIStyle _closeButtonStyle;
    private static GUIStyle _checkboxStyle;

    private static readonly string[] FallbackKnownItems =
    {
        "Key", "Random", "BeefyRing", "Medkit", "CursedDoll", "IceCube", "Beer", "SpikyShield", "Bonker",
        "SlipperyRing", "CowardsCloak", "GymSauce", "Battery", "PhantomShroud", "ForbiddenJuice",
        "DemonBlade", "GrandmasSecretTonic", "GiantFork", "MoldyCheese", "GoldenSneakers",
        "SpicyMeatball", "Chonkplate", "LightningOrb", "DemonicBlood", "DemonicSoul", "Dragonfire",
        "GoldenGlove", "GoldenShield", "ZaWarudo", "OverpoweredLamp", "Feathers", "Ghost",
        "TurboSocks", "ShatteredWisdom", "EchoShard", "SuckyMagnet", "Backpack", "Clover", "Campfire",
        "Rollerblades", "Skuleg", "EagleClaw", "Scarf", "Anvil", "Oats", "EnergyCore", "ElectricPlug",
        "SoulHarvester", "Mirror", "JoesDagger", "SpeedBoi", "Gasmask", "ToxicBarrel", "HolyBook",
        "BrassKnuckles", "IdleJuice", "Kevin", "Borgar", "CreditCardRed", "CreditCardGreen",
        "BossBuster", "LeechingCrystal", "TacticalGlasses", "Cactus", "CageKey", "IceCrystal",
        "TimeBracelet", "Wrench", "Beacon", "GoldenRing", "QuinsMask", "CryptKey", "OldMask",
        "Snek", "Pot", "BobsLantern", "Pumpkin", "WizardsHat"
    };
    private static readonly List<string> KnownItems = new(FallbackKnownItems);
    private static bool _knownItemsLoaded;

    private readonly List<EditableRule> _rules = new();
    private Rect _windowRect = new(22, 20, 1120, 720);
    private bool _visible;
    private bool _editorOpen;
    private bool _profileDialogOpen;
    private string _profileDialogMode;
    private string _profileDialogText;
    private RewardTrigger _editorTrigger = RewardTrigger.Time;
    private bool _triggerDropdownVisible;
    private bool _itemDropdownVisible;
    private bool _randomAllowTime = true;
    private bool _randomAllowKills = true;
    private bool _randomAllowNewStage = true;
    private bool _randomAllowBossKill = true;
    private bool _editorEnabled = true;
    private int _editingIndex = -1;
    private Vector2 _itemDropdownScrollPos;
    private Vector2 _rulesListScrollPos;
    private int _selectedItemIndex;
    private int _selectedRuleIndex = -1;
    private int _rowMenuIndex = -1;
    private Rect _rowMenuRect;
    private int _rowMenuDrawIndex = -1;
    private string _itemText = "Key";
    private int _durationValue = 1;
    private int _killsValue = 100;
    private int _randomTimeValue = 30;
    private int _randomKillsValue = 100;
    private int _countValue = 1;
    private int _maxGrantsValue = 0;
    private int _comboTimeValue = 5;
    private int _cooldownValue;
    private RuleRepeatMode _editorRepeatMode = RuleRepeatMode.Repeat;
    private string _activeNumericField;
    private string _activeNumericBuffer = string.Empty;
    private RuleFilterMode _ruleFilterMode = RuleFilterMode.All;
    private RewardTrigger? _ruleTriggerFilter;
    private bool _searchFieldActive;
    private string _searchText = string.Empty;
    private bool _itemSearchFieldActive;
    private string _itemSearchText = string.Empty;
    private int _numericCursorPos = -1;
    private int _profileDialogCursorPos = -1;
    private int _searchCursorPos = -1;
    private int _itemSearchCursorPos = -1;
    private string _status = string.Empty;
    private string _uiStep = "idle";
    private bool _draggingMain;
    private Vector2 _dragOffset;
    private static bool _visualsReady;
    private int _activeTab = 0; // 0 = Rules, 1 = Chaos
    private Vector2 _chaosLogScrollPos;
    private static readonly string ExportPath = Path.Combine(
        MelonLoader.Utils.MelonEnvironment.UserDataDirectory,
        Constants.MODNAME,
        "Exports",
        "rules.json");

    public RewardSchedulerWindow()
    {
        _visualsReady = false; // renk değişikliklerinin yansıması için sıfırla
        EnsureKnownItemsLoaded();
        LoadFromConfig();
    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.F8) || Input.GetKeyDown(KeyCode.JoystickButton7))
        {
            _visible = !_visible;
            if (_visible)
            {
                EnsureKnownItemsLoaded();
                LoadFromConfig();
            }
        }

        if (_visible && (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.JoystickButton1)))
        {
            if (_editorOpen)
                CloseEditor();
            else
                _visible = false;
        }
    }

    public void OnGUI()
    {
        if (!_visible)
            return;

        try
        {
            _uiStep = "visuals";
            EnsureVisuals();
            GUI.depth = -10000;
            HandleNumericKeyboardInput();
            HandleSearchKeyboardInput();
            
            var oldMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(0.8f, 0.8f, 1f));

            _uiStep = "drag";
            HandleDragging();
            _uiStep = "layout";
            ClampWindowsToScreen();
            _uiStep = "main";
            DrawMainWindow();
            ConsumeWindowInput();

            GUI.matrix = oldMatrix;
            _uiStep = "idle";
        }
        catch (Exception ex)
        {
            _visible = false;
            Main.Error($"UI disabled after error at {_uiStep}:\n{ex.ToString()}");
        }
    }

    private void DrawMainWindow()
    {
        DrawRoundedRect(_windowRect, _panelStyle);

        var headerHeight = 54f;
        var headerRect = new Rect(_windowRect.x, _windowRect.y, _windowRect.width, headerHeight);
        DrawRoundedRect(headerRect, _headerStyle);
        GUI.Label(new Rect(_windowRect.x + 22, _windowRect.y + 12, 520, 30), "MEGA CHAOS", _titleStyle);

        if (GUI.Button(new Rect(_windowRect.x + _windowRect.width - 58, _windowRect.y + 8, 34, 34), "X", _closeButtonStyle))
            _visible = false;

        // Tab buttons
        var tabY = _windowRect.y + 60f;
        var tabX = _windowRect.x + 28f;
        if (GUI.Button(new Rect(tabX, tabY, 100, 32), "Rules", _activeTab == 0 ? _accentButtonStyle : _buttonStyle))
            _activeTab = 0;
        if (GUI.Button(new Rect(tabX + 108, tabY, 100, 32), "Chaos", _activeTab == 1 ? _accentButtonStyle : _buttonStyle))
            _activeTab = 1;

        var x = _windowRect.x + 28;
        var y = _windowRect.y + 102f;
        var width = _windowRect.width - 56;

        GUI.enabled = !_editorOpen && !_triggerDropdownVisible && !_itemDropdownVisible && !_profileDialogOpen;

        if (_activeTab == 0)
        {
            DrawToolbar(x, y, width);
            y += 60f;
            DrawRulesCard(x, y, width, _windowRect.height - (y - _windowRect.y) - 24);
        }
        else if (_activeTab == 1)
        {
            DrawChaosTab(x, y, width, _windowRect.height - (y - _windowRect.y) - 24);
        }

        GUI.enabled = true;

        var e = Event.current;
        var savedType = e.type;
        var mouseInDropdown = false;

        if (_editorOpen)
        {
            if (_triggerDropdownVisible && GetTriggerDropdownRect().Contains(e.mousePosition))
                mouseInDropdown = true;
            if (_itemDropdownVisible && GetItemDropdownRect().Contains(e.mousePosition))
                mouseInDropdown = true;
            if (_rowMenuIndex == -100 && GetModeDropdownRect().Contains(e.mousePosition))
                mouseInDropdown = true;
        }

        if (_editorOpen
            && (e.type == EventType.MouseDown || e.type == EventType.MouseUp)
            && !mouseInDropdown)
        {
            if (_triggerDropdownVisible && !GetTriggerDropdownRect().Contains(e.mousePosition))
                _triggerDropdownVisible = false;

            if (_itemDropdownVisible && !GetItemDropdownRect().Contains(e.mousePosition))
            {
                _itemDropdownVisible = false;
                _itemSearchFieldActive = false;
            }

            if (_rowMenuIndex == -100 && !GetModeDropdownRect().Contains(e.mousePosition))
                _rowMenuIndex = -1;
        }

        if (mouseInDropdown && (e.type == EventType.MouseDown || e.type == EventType.MouseUp || e.type == EventType.ScrollWheel))
            e.type = EventType.Ignore;

        if (_editorOpen)
            DrawEditorDialog();
        else if (_profileDialogOpen)
            DrawProfileDialog();

        if (mouseInDropdown)
            e.type = savedType;

        DrawOverlayDropdowns();

        if (!string.IsNullOrWhiteSpace(_status))
            GUI.Label(new Rect(x, _windowRect.y + _windowRect.height - 26, width - 30, 20), _status, _statusStyle);
    }

    private void DrawChaosTab(float x, float y, float width, float height)
    {
        var profile = ProfileManager.ActiveProfile;
        if (profile == null) return;

        float settingsWidth = width * 0.22f;
        float logWidth = width - settingsWidth - 12f;

        // ---- Left Panel: Settings ----
        DrawCard(x, y, settingsWidth, height, "Settings");
        var sx = x + 14f;
        var sy = y + 42f;
        var fieldW = settingsWidth - 28f;

        // Enable toggle
        var enableStyle = profile.ChaosEnabled ? _accentButtonStyle : _buttonStyle;
        var enableLabel = profile.ChaosEnabled ? "CHAOS: ON" : "CHAOS: OFF";
        if (GUI.Button(new Rect(sx, sy, fieldW, 36), enableLabel, enableStyle))
        {
            profile.ChaosEnabled = !profile.ChaosEnabled;
            if (!profile.ChaosEnabled) ChaosEngine.Instance.ClearAllEffects();
            ProfileManager.Save();
        }
        sy += 46f;

        // Interval
        GUI.Label(new Rect(sx, sy, fieldW, 20), "Interval (sec)", _mutedStyle);
        sy += 22f;
        int intervalVal = (int)profile.ChaosInterval;
        DrawNumericValueBox(ref intervalVal, new Rect(sx, sy, fieldW, 30), 5, 9999, "chaos_interval");
        if (intervalVal != (int)profile.ChaosInterval) { profile.ChaosInterval = intervalVal; ProfileManager.Save(); }
        sy += 38f;

        // Duration multiplier
        GUI.Label(new Rect(sx, sy, fieldW, 20), "Duration Multiplier", _mutedStyle);
        sy += 22f;
        // We store as float but edit as integer *10 to show 1 decimal (e.g. 15 = 1.5x)
        int multX10 = (int)Math.Round(profile.ChaosDurationMultiplier * 10f);
        DrawNumericValueBox(ref multX10, new Rect(sx, sy, fieldW, 30), 1, 100, "chaos_mult");
        float newMult = multX10 / 10f;
        if (Math.Abs(newMult - profile.ChaosDurationMultiplier) > 0.01f) { profile.ChaosDurationMultiplier = newMult; ProfileManager.Save(); }
        GUI.Label(new Rect(sx, sy + 32f, fieldW, 20), $"{newMult:F1}x duration", _mutedStyle);
        sy += 58f;

        // Clear log button
        if (GUI.Button(new Rect(sx, sy, fieldW, 28), "Clear Log", _dangerButtonStyle))
            ChaosEngine.Instance.ClearLog();

        // ---- Right Panel: Log ----
        float logX = x + settingsWidth + 12f;
        DrawCard(logX, y, logWidth, height, "Effect Log");

        var log = ChaosEngine.Instance.Log;
        const float rowH = 26f;
        float viewH = height - 50f;
        float contentH = Math.Max(viewH, log.Count * rowH);

        _chaosLogScrollPos = GUI.BeginScrollView(
            new Rect(logX + 8, y + 42, logWidth - 16, viewH),
            _chaosLogScrollPos,
            new Rect(0, 0, logWidth - 36, contentH));

        if (log.Count == 0)
        {
            GUI.Label(new Rect(8, 8, logWidth - 52, 28), "No effects triggered yet.", _mutedStyle);
        }
        else
        {
            for (int i = 0; i < log.Count; i++)
            {
                var entry = log[i];
                GUI.Label(new Rect(8, i * rowH + 4, 80, rowH - 4), $"[{entry.Time}]", _mutedStyle);
                GUI.Label(new Rect(92, i * rowH + 4, logWidth - 136, rowH - 4), entry.EffectName, _cellStyle);
            }
        }

        GUI.EndScrollView();
    }

    private void DrawToolbar(float x, float y, float width)
    {
        var buttonY = y;
        var cursorX = x;
        var newWidth = 132f;
        var gap = 10f;

        if (GUI.Button(new Rect(cursorX, buttonY, newWidth, 46), "+ NEW RULE", _buttonStyle))
            OpenAddEditor();
        cursorX += newWidth + gap;

        var profileWidth = 110f;
        var activeProfileId = ProfileManager.ActiveProfile?.Id;

        // Draw profile tabs
        foreach (var profile in ProfileManager.Profiles)
        {
            var isSelected = activeProfileId == profile.Id;
            var style = isSelected ? _accentButtonStyle : _buttonStyle;
            if (GUI.Button(new Rect(cursorX, buttonY, profileWidth, 46), profile.Name, style))
                SwitchProfile(profile.Id);
            cursorX += profileWidth + 4;
        }

        // Profile action buttons
        cursorX += 16f;
        try
        {
            if (GUI.Button(new Rect(cursorX, buttonY, 36, 46), "+", _buttonStyle))
            {
                _profileDialogMode = "create";
                _profileDialogText = $"Profile {ProfileManager.Profiles.Count + 1}";
                _profileDialogCursorPos = _profileDialogText.Length;
                _profileDialogOpen = true;
            }
            cursorX += 40f;

            if (ProfileManager.Profiles.Count > 1)
            {
                if (GUI.Button(new Rect(cursorX, buttonY, 36, 46), "-", _dangerButtonStyle))
                {
                    ProfileManager.DeleteProfile(activeProfileId);
                    LoadFromConfig();
                    RuleScheduler.ReloadRules();
                    _status = "Profile deleted.";
                }
                cursorX += 40f;
            }

            if (GUI.Button(new Rect(cursorX, buttonY, 46, 46), "Edit", _buttonStyle))
            {
                _profileDialogMode = "rename";
                _profileDialogText = ProfileManager.ActiveProfile?.Name ?? "";
                _profileDialogCursorPos = _profileDialogText.Length;
                _profileDialogOpen = true;
            }
        }
        catch (Exception ex)
        {
            Main.Error($"DrawToolbar Buttons Exception:\n{ex.ToString()}");
        }
    }

    private void SwitchProfile(string profileId)
    {
        if (ProfileManager.ActiveProfile?.Id == profileId) return;
        ProfileManager.SetActiveProfile(profileId);
        LoadFromConfig();
        RuleScheduler.ReloadRules();
        RuleScheduler.ResetRun(Time.unscaledTime);
        _status = $"Switched to Profile {ProfileManager.ActiveProfile?.Name}";
    }

    private void DrawProfileDialog()
    {
        try
        {
            var overlayRect = new Rect(_windowRect.x, _windowRect.y, _windowRect.width, _windowRect.height);
            var oldColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.45f);
            GUI.DrawTexture(overlayRect, Texture2D.whiteTexture);
            GUI.color = oldColor;

            var dialogWidth = 400f;
            var dialogHeight = 200f;
            var dialogRect = new Rect(
                _windowRect.x + (_windowRect.width - dialogWidth) / 2f,
                _windowRect.y + (_windowRect.height - dialogHeight) / 2f,
                dialogWidth,
                dialogHeight
            );

            var title = _profileDialogMode == "create" ? "CREATE PROFILE" : "RENAME PROFILE";
            DrawCard(dialogRect.x, dialogRect.y, dialogRect.width, dialogRect.height, title);

            var fieldX = dialogRect.x + 30f;
            var fieldY = dialogRect.y + 70f;
            GUI.Label(new Rect(fieldX, fieldY, dialogWidth - 60, 28), "Profile Name:", _cellStyle);
            
            var inputRect = new Rect(fieldX, fieldY + 30, dialogWidth - 60, 36);
            GUI.Box(inputRect, string.Empty, _frameStyle);
            var labelRect = new Rect(inputRect.x + 8, inputRect.y + 8, inputRect.width - 16, inputRect.height - 16);
            GUI.Label(labelRect, _profileDialogText, _valueStyle);
            if (_profileDialogOpen) DrawTextCursor(labelRect, _profileDialogText, _profileDialogCursorPos, _valueStyle);

            var footerY = dialogRect.y + dialogRect.height - 56f;
            if (GUI.Button(new Rect(dialogRect.x + dialogRect.width - 236f, footerY, 102f, 36f), "CANCEL", _buttonStyle))
            {
                _profileDialogOpen = false;
            }
            if (GUI.Button(new Rect(dialogRect.x + dialogRect.width - 122f, footerY, 102f, 36f), "SAVE", _accentButtonStyle))
            {
                if (_profileDialogMode == "create")
                    ProfileManager.CreateProfile(_profileDialogText);
                else
                    ProfileManager.RenameProfile(ProfileManager.ActiveProfile?.Id, _profileDialogText);
                _profileDialogOpen = false;
            }
        }
        catch (Exception ex)
        {
            Main.Error($"DrawProfileDialog Exception:\n{ex.ToString()}");
        }
    }

    private void DrawEditorDialog()
    {
        var dialogRect = GetEditorDialogRect();
        var overlayRect = new Rect(_windowRect.x, _windowRect.y, _windowRect.width, _windowRect.height);
        var oldColor = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.45f);
        GUI.DrawTexture(overlayRect, Texture2D.whiteTexture);
        GUI.color = oldColor;

        var editorWidth = dialogRect.width * 0.58f;
        var previewWidth = dialogRect.width - editorWidth - 18f;

        DrawCard(dialogRect.x, dialogRect.y, dialogRect.width, dialogRect.height, _editingIndex >= 0 ? "EDIT RULE" : "NEW RULE");
        DrawEditorCard(dialogRect.x + 18f, dialogRect.y + 18f, editorWidth - 18f, dialogRect.height - 92f);
        DrawPreviewCard(dialogRect.x + 18f + editorWidth, dialogRect.y + 18f, previewWidth - 18f, dialogRect.height - 92f);

        var footerY = dialogRect.y + dialogRect.height - 56f;
        if (GUI.Button(new Rect(dialogRect.x + dialogRect.width - 236f, footerY, 102f, 36f), "CANCEL", _buttonStyle))
            CloseEditor();
        if (GUI.Button(new Rect(dialogRect.x + dialogRect.width - 122f, footerY, 102f, 36f), "SAVE", _accentButtonStyle))
            ConfirmEditor();
    }

    private Rect _lastModeRect;
    private Rect _lastTriggerRect;
    private Rect _lastItemRect;

    private void DrawEditorCard(float x, float y, float width, float height)
    {
        DrawCard(x, y, width, height, "RULE EDITOR");

        var left = x + 22;
        var top = y + 58;
        var labelWidth = 110f;
        var fieldX = left + 130f;
        var fieldWidth = Math.Max(160f, width - 176f);
        var compactFieldWidth = Math.Min(250f, fieldWidth);
        var rowHeight = 48f;

        GUI.Label(new Rect(left, top, labelWidth, 28), "Enabled", _cellStyle);
        var enabledRect = new Rect(fieldX, top + 3, 20, 20);
        if (GUI.Button(enabledRect, _editorEnabled ? "X" : "", _checkboxStyle))
            _editorEnabled = !_editorEnabled;
        

        top += rowHeight;
        GUI.Label(new Rect(left, top, labelWidth, 28), "Trigger", _cellStyle);
        _lastTriggerRect = new Rect(fieldX, top - 8, compactFieldWidth, 40);
        if (DrawSelectField(_lastTriggerRect, GetTriggerLabel(_editorTrigger)))
        {
            _triggerDropdownVisible = !_triggerDropdownVisible;
            _itemDropdownVisible = false;
            _rowMenuIndex = -1;
        }

        top += rowHeight + 4f;
        GUI.Label(new Rect(left, top, labelWidth, 28), "Condition", _cellStyle);
        if (_editorTrigger == RewardTrigger.Time)
        {
            DrawStepper(ref _durationValue, fieldX, top - 8, 1, 999, 1, "time_duration");
            GUI.Label(new Rect(fieldX + 138, top, 110, 28), "seconds", _mutedStyle);
        }
        else if (_editorTrigger == RewardTrigger.Kills)
        {
            DrawKillStepper(ref _killsValue, fieldX, top - 8, Math.Max(320f, compactFieldWidth), 1, 99999, "kills_value");
        }
        else if (_editorTrigger == RewardTrigger.Gold)
        {
            DrawKillStepper(ref _killsValue, fieldX, top - 8, Math.Max(320f, compactFieldWidth), 1, 999999, "gold_value");
            GUI.Label(new Rect(fieldX + 338, top, 90, 28), "gold", _mutedStyle);
        }
        else if (_editorTrigger == RewardTrigger.Level)
        {
            DrawStepper(ref _killsValue, fieldX, top - 8, 1, 999, 1, "level_value");
            GUI.Label(new Rect(fieldX + 138, top, 110, 28), "levels", _mutedStyle);
        }
        else if (_editorTrigger == RewardTrigger.NewStage)
        {
            GUI.Label(new Rect(fieldX, top, 280, 28), "On every new stage", _cellStyle);
        }
        else if (_editorTrigger == RewardTrigger.Combo)
        {
            DrawAdvancedStepper(ref _killsValue, fieldX, top - 8, 1, 99999, "combo_kills");
            GUI.Label(new Rect(fieldX + 324, top, 90, 28), "kills", _headerCellStyle);

            top += rowHeight;
            GUI.Label(new Rect(left, top, labelWidth, 28), "Time Window", _cellStyle);
            DrawStepper(ref _comboTimeValue, fieldX, top - 8, 1, 999, 1, "combo_time");
            GUI.Label(new Rect(fieldX + 138, top, 90, 28), "seconds", _headerCellStyle);
        }
        else if (_editorTrigger == RewardTrigger.Health)
        {
            GUI.Label(new Rect(fieldX, top, 100, 28), "Health <", _cellStyle);
            DrawStepper(ref _durationValue, fieldX + 80, top - 8, 1, 99, 1, "health_percent");
            GUI.Label(new Rect(fieldX + 200, top, 40, 28), "%", _headerCellStyle);
        }
        else if (_editorTrigger == RewardTrigger.BossKill)
        {
            GUI.Label(new Rect(fieldX, top, 280, 28), "On every boss kill", _cellStyle);
        }
        else if (_editorTrigger == RewardTrigger.Random)
        {
            GUI.Label(new Rect(fieldX, top, 200, 28), "Sub-triggers pool:", _mutedStyle);
            top += 36f;
            DrawRandomToggle(ref _randomAllowTime, fieldX, top, "Time");
            DrawRandomToggle(ref _randomAllowKills, fieldX + 106, top, "Kills");
            top += 32f;
            DrawRandomToggle(ref _randomAllowNewStage, fieldX, top, "Stage");
            DrawRandomToggle(ref _randomAllowBossKill, fieldX + 106, top, "Boss");

            if (_randomAllowTime)
            {
                top += 44f;
                GUI.Label(new Rect(fieldX, top, 60, 28), "Time (s)", _mutedStyle);
                DrawStepper(ref _randomTimeValue, fieldX + 70, top - 8, 1, 999, 1, "random_time");
            }

            if (_randomAllowKills)
            {
                top += 44f;
                GUI.Label(new Rect(fieldX, top, 60, 28), "Kills", _mutedStyle);
                DrawKillStepper(ref _randomKillsValue, fieldX + 70, top - 8, Math.Max(420f, compactFieldWidth + 30f), 1, 99999, "random_kills");
            }
        }

        top += rowHeight + 12f;
        GUI.Label(new Rect(left, top, labelWidth, 28), "Item(s)", _cellStyle);
        
        var textFieldWidth = fieldWidth - 46f;
        _lastItemRect = new Rect(fieldX, top - 8, textFieldWidth, 40);
        DrawStringValueBox(ref _itemText, _lastItemRect, "editor_item_text");
        
        if (GUI.Button(new Rect(fieldX + textFieldWidth + 6, top - 8, 40, 40), "v", _buttonStyle))
        {
            _itemDropdownVisible = !_itemDropdownVisible;
            _triggerDropdownVisible = false;
            _rowMenuIndex = -1;
            if (_itemDropdownVisible)
            {
                _itemSearchText = string.Empty;
                _itemSearchFieldActive = false;
                _itemDropdownScrollPos = Vector2.zero;
            }
        }

        top += rowHeight + 4f;
        GUI.Label(new Rect(left, top, labelWidth, 28), "Count", _cellStyle);
        DrawStepper(ref _countValue, fieldX, top - 8, 1, 999, 1, "item_count");
        GUI.Label(new Rect(fieldX + 138, top, 90, 28), "count", _mutedStyle);

        top += rowHeight + 8f;
        GUI.Label(new Rect(left, top, labelWidth, 28), "Mode", _cellStyle);
        _lastModeRect = new Rect(fieldX, top - 8, compactFieldWidth, 40);
        if (DrawSelectField(_lastModeRect, GetRepeatModeLabel(_editorRepeatMode)))
        {
            _rowMenuIndex = -100;
            _triggerDropdownVisible = false;
            _itemDropdownVisible = false;
        }

        if (_editorRepeatMode == RuleRepeatMode.Cooldown)
        {
            top += rowHeight;
            GUI.Label(new Rect(left, top, labelWidth, 28), "Cooldown", _cellStyle);
            DrawStepper(ref _cooldownValue, fieldX, top - 8, 1, 999, 1, "rule_cooldown");
            GUI.Label(new Rect(fieldX + 138, top, 90, 28), "seconds", _mutedStyle);
        }

        top += rowHeight;
        GUI.Label(new Rect(left, top, labelWidth, 28), "Max Grants", _cellStyle);
        DrawStepper(ref _maxGrantsValue, fieldX, top - 8, 0, 999, 1, "max_grants");
        GUI.Label(new Rect(fieldX + 138, top, 180, 28), "0 = unlimited", _mutedStyle);
    }

    private void DrawProgressBar(Rect rect, float progress)
    {
        var bgRect = rect;
        var fgRect = new Rect(rect.x, rect.y, rect.width * progress, rect.height);

        DrawRect(bgRect, new Color(0.1f, 0.11f, 0.14f, 1f));
        DrawRect(fgRect, new Color(0.031f, 0.949f, 0.843f, 1f));
    }

    private void DrawPreviewCard(float x, float y, float width, float height)
    {
        DrawCard(x, y, width, height, "LIVE PREVIEW");

        var inner = new Rect(x + 32, y + 62, width - 64, height - 94);
        DrawRoundedRect(inner, _cellStyleBg);
        

        var rawItems = _itemText.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        var baseItems = new List<string>(rawItems.Length);
        var weights = new List<string>(rawItems.Length);
        int totalWeight = 0;

        for (var i = 0; i < rawItems.Length; i++)
        {
            var item = rawItems[i].Trim();
            if (!string.IsNullOrWhiteSpace(item))
            {
                ParseItemString(item, out string name, out int w);
                baseItems.Add(name);
                
                weights.Add($"{w}%");
                totalWeight += w;
            }
        }

        if (baseItems.Count == 0)
        {
            baseItems.Add("Key");
            weights.Add("100%");
            totalWeight = 100;
        }

        if (totalWeight > 0 && totalWeight < 100)
        {
            baseItems.Add("None");
            weights.Add($"{100 - totalWeight}%");
        }

        var icons = new List<ItemIconService.IconResult>(baseItems.Count);
        for (var i = 0; i < baseItems.Count; i++)
            icons.Add(ItemIconService.GetIcon(baseItems[i]));

        GUI.Label(new Rect(inner.x, inner.y + 16, inner.width, 28), $"TRIGGER: {GetCompactPreviewTriggerText()}", _previewItemStyle);

        var iconArea = new Rect(inner.x + 14, inner.y + 52, inner.width - 28, inner.height - 84);
        var center = iconArea.y + iconArea.height * 0.5f;
        if (baseItems.Count == 1)
        {
            DrawIconCentered(iconArea, baseItems[0], icons[0], center - 52, 104, 104, _countValue, weights[0]);
        }
        else
        {
            var count = baseItems.Count;
            var columns = count <= 2 ? 2 : count <= 4 ? 2 : count <= 9 ? 3 : 4;
            var rows = (count + columns - 1) / columns;
            const float gap = 8f;
            var cellWidth = (iconArea.width - (columns - 1) * gap) / columns;
            var cellHeight = (iconArea.height - (rows - 1) * gap) / rows;

            for (var i = 0; i < count; i++)
            {
                var row = i / columns;
                var col = i % columns;
                var cell = new Rect(
                    iconArea.x + col * (cellWidth + gap),
                    iconArea.y + row * (cellHeight + gap),
                    cellWidth,
                    cellHeight);

                DrawIconInRect(cell, baseItems[i], icons[i], _countValue, weights[i]);
            }
        }
    }

    private void DrawRulesCard(float x, float y, float width, float height)
    {
        DrawCard(x, y, width, height, "RULE LIST");
        
        var e = Event.current;
        var savedType = e.type;
        var mouseInRowMenu = _rowMenuIndex >= 0 && _rowMenuRect.Contains(e.mousePosition);

        if (mouseInRowMenu && (e.type == EventType.MouseDown || e.type == EventType.MouseUp || e.type == EventType.ScrollWheel))
            e.type = EventType.Ignore;

        _rowMenuDrawIndex = -1;

        var tableX = x + 26;
        var tableY = y + 62;
        var tableWidth = width - 52;
        DrawRuleFilters(tableX, tableY, tableWidth);
        tableY += 52;
        var headerHeight = 38f;
        var rowHeight = 44f;
        
        var visibleRuleIndexes = GetVisibleRuleIndexes();
        var contentHeight = visibleRuleIndexes.Count * rowHeight;
        var bodyHeight = Math.Max(rowHeight, height - 178f);
        
        var hasScrollbar = contentHeight > bodyHeight;
        var scrollPad = hasScrollbar ? 18f : 0f;
        var rowViewWidth = tableWidth - scrollPad;

        var selectWidth = 42f;
        var countWidth = 60f;
        var actionsWidth = 70f;
        var dynamicSpace = Math.Max(400f, rowViewWidth - selectWidth - countWidth - actionsWidth);
        var triggerWidth = dynamicSpace * 0.16f;
        var conditionWidth = dynamicSpace * 0.22f;
        var itemWidth = dynamicSpace * 0.26f;
        var statusWidth = dynamicSpace * 0.36f;

        DrawCell(tableX, tableY, selectWidth, "#", true);
        DrawCell(tableX + selectWidth, tableY, triggerWidth, "Trigger", true);
        DrawCell(tableX + selectWidth + triggerWidth, tableY, conditionWidth, "Condition", true);
        DrawCell(tableX + selectWidth + triggerWidth + conditionWidth, tableY, itemWidth, "Item", true);
        DrawCell(tableX + selectWidth + triggerWidth + conditionWidth + itemWidth, tableY, countWidth, "Count", true);
        DrawCell(tableX + selectWidth + triggerWidth + conditionWidth + itemWidth + countWidth, tableY, statusWidth, "Status & Progress", true);
        DrawCell(tableX + selectWidth + triggerWidth + conditionWidth + itemWidth + countWidth + statusWidth, tableY, actionsWidth, "Menu", true);
        
        if (hasScrollbar)
            DrawCell(tableX + rowViewWidth, tableY, scrollPad, "", true);

        var bodyRect = new Rect(tableX, tableY + headerHeight, tableWidth, bodyHeight);
        DrawRoundedRect(bodyRect, _cellStyleBg);
        

        if (visibleRuleIndexes.Count == 0)
        {
            GUI.Label(new Rect(tableX + 20, tableY + headerHeight + 20, 320, 24), "No rules match the current filters.", _cellStyle);
            return;
        }

        var viewRect = new Rect(0, 0, rowViewWidth, contentHeight);

        _rulesListScrollPos = GUI.BeginScrollView(bodyRect, _rulesListScrollPos, viewRect);

        int startIndex = Mathf.Max(0, (int)(_rulesListScrollPos.y / rowHeight));
        int endIndex = Mathf.Min(visibleRuleIndexes.Count, startIndex + (int)(bodyRect.height / rowHeight) + 2);

        for (var i = startIndex; i < endIndex; i++)
        {
            var rowY = i * rowHeight;
            var ruleIndex = visibleRuleIndexes[i];
            DrawRulesListRow(_rules[ruleIndex], ruleIndex, 0, rowY, rowViewWidth, selectWidth, triggerWidth, conditionWidth, itemWidth, countWidth, statusWidth, actionsWidth, bodyRect);
        }

        GUI.EndScrollView();

        if (mouseInRowMenu)
            e.type = savedType;

        if (_rowMenuDrawIndex >= 0)
            DrawRowMenu(_rowMenuRect, _rowMenuDrawIndex);
    }

    private void DrawItemDropdown(float x, float y, float width)
    {
        if (!_itemDropdownVisible)
            return;

        var searchStr = RewardRule.Normalize(_itemSearchText ?? string.Empty);
        var filteredItems = new List<string>();
        for (var i = 0; i < KnownItems.Count; i++)
        {
            var item = KnownItems[i];
            if (string.IsNullOrWhiteSpace(searchStr) || RewardRule.Normalize(item).Contains(searchStr))
                filteredItems.Add(item);
        }
        filteredItems.Sort();

        const float rowHeight = 30f;
        const float searchHeight = 36f;
        const float viewportHeight = 260f;
        var height = viewportHeight + searchHeight + 8f;
        var dropdownRect = new Rect(x, y, width, height);

        GUI.depth = -10001; // Ensure dropdown is on top
        DrawRoundedRect(dropdownRect, _dropdownStyle);
        

        var searchRect = new Rect(dropdownRect.x + 4, dropdownRect.y + 4, dropdownRect.width - 8, searchHeight);
        DrawRoundedRect(searchRect, _itemSearchFieldActive ? _headerStyle : _frameStyle);
        var searchClicked = GUI.Button(searchRect, new GUIContent(string.Empty), GUIStyle.none);
        if (searchClicked)
        {
            if (_activeNumericField == "editor_item_text") _itemText = FormatItemString(_activeNumericBuffer);
            _activeNumericField = null;
            _searchFieldActive = false;
            _itemSearchFieldActive = true;
            _itemSearchCursorPos = _itemSearchText.Length;
        }

        var searchText = string.IsNullOrWhiteSpace(_itemSearchText) && !_itemSearchFieldActive ? "Search item..." : _itemSearchText;
        var searchLabelRect = new Rect(searchRect.x + 12, searchRect.y + 8, searchRect.width - 24, searchRect.height - 16);
        GUI.Label(searchLabelRect, searchText, string.IsNullOrWhiteSpace(_itemSearchText) && !_itemSearchFieldActive ? _mutedStyle : _cellStyle);
        if (_itemSearchFieldActive) DrawTextCursor(searchLabelRect, _itemSearchText, _itemSearchCursorPos, _cellStyle);

        var contentHeight = filteredItems.Count * rowHeight;
        var viewRect = new Rect(0, 0, width - 20, contentHeight);

        _itemDropdownScrollPos = GUI.BeginScrollView(new Rect(dropdownRect.x + 4, dropdownRect.y + 4 + searchHeight, dropdownRect.width - 8, viewportHeight), _itemDropdownScrollPos, viewRect);

        int itemStartIndex = Mathf.Max(0, (int)(_itemDropdownScrollPos.y / rowHeight));
        int itemEndIndex = Mathf.Min(filteredItems.Count, itemStartIndex + (int)(viewportHeight / rowHeight) + 2);

        for (var i = itemStartIndex; i < itemEndIndex; i++)
        {
            var rowY = i * rowHeight;
            var itemText = filteredItems[i];

            var isItemSelected = _itemText.Split(',').Select(s => GetItemNameOnly(s)).Contains(itemText);
            var itemButtonStyle = isItemSelected ? _accentButtonStyle : _buttonStyle;

            int currentWeight = 100;
            if (isItemSelected)
            {
                var selectedStr = _itemText.Split(',').Select(s => s.Trim()).FirstOrDefault(s => GetItemNameOnly(s) == itemText);
                if (selectedStr != null)
                {
                    ParseItemString(selectedStr, out _, out currentWeight);
                }
            }

            var buttonWidth = isItemSelected ? viewRect.width - 110 : viewRect.width;

            if (GUI.Button(new Rect(0, rowY, buttonWidth, 26), FormatItemName(itemText), itemButtonStyle))
            {
                var currentItems = _itemText.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                           .Select(s => s.Trim())
                                           .ToList();

                var existingIndex = currentItems.FindIndex(s => GetItemNameOnly(s) == itemText);
                if (existingIndex >= 0)
                {
                    currentItems.RemoveAt(existingIndex);
                }
                else
                {
                    if (currentItems.Count == 1 && GetItemNameOnly(currentItems[0]) == "Key")
                        currentItems[0] = itemText;
                    else
                        currentItems.Add(itemText);
                }

                _itemText = string.Join(", ", currentItems);
                if (string.IsNullOrWhiteSpace(_itemText)) _itemText = "Key";
                
                _selectedItemIndex = FindItemIndex(itemText);
                
                if (_activeNumericField == "editor_item_text")
                    _activeNumericBuffer = _itemText;
            }

            if (isItemSelected)
            {
                if (GUI.Button(new Rect(viewRect.width - 106, rowY, 30, 26), "-", _buttonStyle))
                {
                    UpdateItemWeight(itemText, Mathf.Max(0, currentWeight - 5));
                }
                
                GUI.Label(new Rect(viewRect.width - 72, rowY, 34, 26), $"{currentWeight}%", _mutedStyle);
                
                if (GUI.Button(new Rect(viewRect.width - 34, rowY, 30, 26), "+", _buttonStyle))
                {
                    UpdateItemWeight(itemText, currentWeight + 5);
                }
            }
        }

        GUI.EndScrollView();

        GUI.depth = -10000;
    }

    private void UpdateItemWeight(string baseItemName, int newWeight)
    {
        var currentItems = _itemText.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();
        var index = currentItems.FindIndex(s => GetItemNameOnly(s) == baseItemName);
        if (index >= 0)
        {
            currentItems[index] = $"{baseItemName} ({newWeight}%)";
            _itemText = string.Join(", ", currentItems);
            if (_activeNumericField == "editor_item_text") _activeNumericBuffer = _itemText;
        }
    }

    private void DrawRuleFilters(float x, float y, float width)
    {
        if (GUI.Button(new Rect(x, y, 76, 36), GetFilterModeLabel(RuleFilterMode.All), _ruleFilterMode == RuleFilterMode.All ? _accentButtonStyle : _buttonStyle))
            _ruleFilterMode = RuleFilterMode.All;
        if (GUI.Button(new Rect(x + 84, y, 90, 36), GetFilterModeLabel(RuleFilterMode.Active), _ruleFilterMode == RuleFilterMode.Active ? _accentButtonStyle : _buttonStyle))
            _ruleFilterMode = RuleFilterMode.Active;
        if (GUI.Button(new Rect(x + 182, y, 104, 36), GetFilterModeLabel(RuleFilterMode.Disabled), _ruleFilterMode == RuleFilterMode.Disabled ? _accentButtonStyle : _buttonStyle))
            _ruleFilterMode = RuleFilterMode.Disabled;

        if (GUI.Button(new Rect(x + 300, y, 170, 36), $"TRIGGER: {GetTriggerFilterLabel()}", _buttonStyle))
            CycleTriggerFilter();

        DrawSearchField(new Rect(x + width - 250, y, 250, 36));
    }

    private void DrawSearchField(Rect rect)
    {
        DrawRoundedRect(rect, _searchFieldActive ? _headerStyle : _frameStyle);
        var clicked = GUI.Button(rect, new GUIContent(string.Empty), GUIStyle.none);
        if (clicked)
        {
            if (_activeNumericField == "editor_item_text") _itemText = FormatItemString(_activeNumericBuffer);
            _activeNumericField = null;
            _itemSearchFieldActive = false;
            _searchFieldActive = true;
            _searchCursorPos = _searchText.Length;
        }

        var text = string.IsNullOrWhiteSpace(_searchText) && !_searchFieldActive ? "Search item or trigger" : _searchText;
        var searchLabelRect = new Rect(rect.x + 12, rect.y + 8, rect.width - 24, rect.height - 16);
        GUI.Label(searchLabelRect, text, string.IsNullOrWhiteSpace(_searchText) && !_searchFieldActive ? _mutedStyle : _cellStyle);
        if (_searchFieldActive) DrawTextCursor(searchLabelRect, _searchText, _searchCursorPos, _cellStyle);
    }

    private List<int> GetVisibleRuleIndexes()
    {
        var result = new List<int>();
        for (var i = 0; i < _rules.Count; i++)
        {
            var rule = _rules[i];
            if (_ruleFilterMode == RuleFilterMode.Active && !rule.Enabled)
                continue;
            if (_ruleFilterMode == RuleFilterMode.Disabled && rule.Enabled)
                continue;
            if (_ruleTriggerFilter.HasValue && rule.Trigger != _ruleTriggerFilter.Value)
                continue;
            if (!string.IsNullOrWhiteSpace(_searchText))
            {
                var search = RewardRule.Normalize(_searchText);
                if (!RewardRule.Normalize(rule.Item).Contains(search) && !RewardRule.Normalize(GetTriggerLabel(rule.Trigger)).Contains(search))
                    continue;
            }

            result.Add(i);
        }

        return result;
    }

    private void OpenAddEditor()
    {
        _editorOpen = true;
        _rowMenuIndex = -1;
        _editingIndex = -1;
        _selectedRuleIndex = -1;
        _editorTrigger = RewardTrigger.Time;
        _durationValue = 1;
        _killsValue = 100;
        _randomTimeValue = 30;
        _randomKillsValue = 100;
        _randomAllowTime = true;
        _randomAllowKills = true;
        _randomAllowNewStage = true;
        _randomAllowBossKill = true;
        _editorEnabled = true;
        _itemText = "Key";
        _countValue = 1;
        _maxGrantsValue = 0;
        _comboTimeValue = 5;
        _cooldownValue = 0;
        _editorRepeatMode = RuleRepeatMode.Repeat;
        _selectedItemIndex = FindItemIndex(_itemText);
        _itemDropdownScrollPos = new Vector2(0, _selectedItemIndex * 30f);
        _triggerDropdownVisible = false;
        _itemDropdownVisible = false;
        _activeNumericField = null;
        _activeNumericBuffer = string.Empty;
        _status = string.Empty;
    }

    private void OpenEditEditor(int index)
    {
        if (index < 0 || index >= _rules.Count)
            return;

        _editorOpen = true;
        _rowMenuIndex = -1;
        var rule = _rules[index];
        _editingIndex = index;
        _selectedRuleIndex = index;
        _itemText = rule.Item;
        _countValue = rule.Count;
        _editorRepeatMode = rule.RepeatMode;
        _cooldownValue = rule.CooldownSeconds;
        _maxGrantsValue = rule.MaxGrants;
        _comboTimeValue = rule.ComboTimeSeconds;
        _selectedItemIndex = FindItemIndex(_itemText);
        _itemDropdownScrollPos = new Vector2(0, _selectedItemIndex * 30f);
        _triggerDropdownVisible = false;
        _itemDropdownVisible = false;
        _editorTrigger = rule.Trigger;
        _editorEnabled = rule.Enabled;
        _durationValue = Math.Max(1, rule.Interval);
        _killsValue = Math.Max(1, rule.Interval);
        _randomTimeValue = rule.RandomTimeSeconds;
        _randomKillsValue = rule.RandomKillCount;
        _randomAllowTime = rule.RandomAllowTime;
        _randomAllowKills = rule.RandomAllowKills;
        _randomAllowNewStage = rule.RandomAllowNewStage;
        _randomAllowBossKill = rule.RandomAllowBossKill;
        _activeNumericField = null;
        _activeNumericBuffer = string.Empty;

        _status = string.Empty;
    }

    private void ConfirmEditor()
    {
        if (!TryCreateRuleFromEditor(out var rule, out var error))
        {
            _status = error;
            return;
        }

        try
        {
            if (_editingIndex >= 0 && _editingIndex < _rules.Count)
            {
                _rules[_editingIndex] = EditableRule.FromRule(rule);
                SaveRules("Rule updated.");
            }
            else
            {
                _rules.Add(EditableRule.FromRule(rule));
                _selectedRuleIndex = _rules.Count - 1;
                SaveRules("Rule added.");
            }
        }
        catch (Exception ex)
        {
            _status = $"Save failed: {ex.GetBaseException().Message}";
        }
        finally
        {
            CloseEditor();
        }
    }

    private bool TryCreateRuleFromEditor(out RewardRule rule, out string error)
    {
        rule = null;
        error = null;

        if (string.IsNullOrWhiteSpace(_itemText))
        {
            error = "Item name cannot be empty.";
            return false;
        }

        if (_countValue <= 0)
        {
            error = "Count must be a positive number.";
            return false;
        }

        if (_editorRepeatMode == RuleRepeatMode.Cooldown && _cooldownValue <= 0)
        {
            error = "Cooldown mode requires a positive cooldown.";
            return false;
        }

        if (_editorTrigger == RewardTrigger.Time)
        {
            if (_durationValue <= 0)
            {
                error = "Time must be a positive number.";
                return false;
            }

            rule = new RewardRule(_editorEnabled, RewardTrigger.Time, _durationValue, _itemText.Trim(), _countValue, _editorRepeatMode, _cooldownValue, _maxGrantsValue, _comboTimeValue);
            return true;
        }

        if (_editorTrigger == RewardTrigger.Kills)
        {
            if (_killsValue <= 0)
            {
                error = "Kill count must be a positive number.";
                return false;
            }

            rule = new RewardRule(_editorEnabled, RewardTrigger.Kills, _killsValue, _itemText.Trim(), _countValue, _editorRepeatMode, _cooldownValue, _maxGrantsValue, _comboTimeValue);
            return true;
        }

        if (_editorTrigger == RewardTrigger.Combo)
        {
            if (_killsValue <= 0 || _comboTimeValue <= 0)
            {
                error = "Combo values must be positive.";
                return false;
            }
            rule = new RewardRule(_editorEnabled, RewardTrigger.Combo, _killsValue, _itemText.Trim(), _countValue, _editorRepeatMode, _cooldownValue, _maxGrantsValue, _comboTimeValue);
            return true;
        }

        if (_editorTrigger == RewardTrigger.Health)
        {
            if (_durationValue <= 0)
            {
                error = "Health percent must be positive.";
                return false;
            }
            rule = new RewardRule(_editorEnabled, RewardTrigger.Health, _durationValue, _itemText.Trim(), _countValue, _editorRepeatMode, _cooldownValue, _maxGrantsValue, _comboTimeValue);
            return true;
        }

        if (_editorTrigger == RewardTrigger.NewStage)
        {
            rule = new RewardRule(_editorEnabled, RewardTrigger.NewStage, 1, _itemText.Trim(), _countValue, _editorRepeatMode, _cooldownValue, _maxGrantsValue, _comboTimeValue);
            return true;
        }

        if (_editorTrigger == RewardTrigger.BossKill)
        {
            rule = new RewardRule(_editorEnabled, RewardTrigger.BossKill, 1, _itemText.Trim(), _countValue, _editorRepeatMode, _cooldownValue, _maxGrantsValue, _comboTimeValue);
            return true;
        }

        if (_editorTrigger == RewardTrigger.Gold)
        {
            if (_killsValue <= 0)
            {
                error = "Gold threshold must be a positive number.";
                return false;
            }

            rule = new RewardRule(_editorEnabled, RewardTrigger.Gold, _killsValue, _itemText.Trim(), _countValue, _editorRepeatMode, _cooldownValue, _maxGrantsValue, _comboTimeValue);
            return true;
        }

        if (_editorTrigger == RewardTrigger.Level)
        {
            if (_killsValue <= 0)
            {
                error = "Level count must be a positive number.";
                return false;
            }

            rule = new RewardRule(_editorEnabled, RewardTrigger.Level, _killsValue, _itemText.Trim(), _countValue, _editorRepeatMode, _cooldownValue, _maxGrantsValue, _comboTimeValue);
            return true;
        }

        if (!_randomAllowTime && !_randomAllowKills && !_randomAllowNewStage && !_randomAllowBossKill)
        {
            error = "Random needs at least one enabled option.";
            return false;
        }

        rule = new RewardRule(
            _editorEnabled,
            RewardTrigger.Random,
            0,
            _itemText.Trim(),
            _countValue,
            _editorRepeatMode,
            _cooldownValue,
            _maxGrantsValue,
            _comboTimeValue,
            _randomTimeValue,
            _randomKillsValue,
            _randomAllowTime,
            _randomAllowKills,
            _randomAllowNewStage,
            _randomAllowBossKill);
        return true;
    }

    private void CloseEditor()
    {
        _editorOpen = false;
        _itemDropdownVisible = false;
        _triggerDropdownVisible = false;
        _editingIndex = -1;
        _rowMenuIndex = -1;
        _selectedRuleIndex = -1;  // Kural edit kapandığında kalıcı hover görünümüü temizle
    }

    private void LoadFromConfig()
    {
        _rules.Clear();

        var rawRules = ConfigService.CurrentRules ?? string.Empty;
        var splitRules = rawRules.Split(new[] { ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var rawRule in splitRules)
        {
            if (!RewardRule.TryParse(rawRule.Trim(), out var rule, out _))
                continue;

            _rules.Add(EditableRule.FromRule(rule));
        }

        _selectedItemIndex = FindItemIndex(_itemText);
        _selectedRuleIndex = ClampInt(_selectedRuleIndex, -1, _rules.Count - 1);
        _status = "Settings loaded.";
    }

    private void SaveRules(string status)
    {
        var serializedRules = new List<string>();
        foreach (var editableRule in _rules)
            serializedRules.Add(ToConfigString(editableRule));

        ConfigService.CurrentRules = string.Join(";", serializedRules);
        MelonPreferences.Save();
        RuleScheduler.ReloadRules();
        _status = status;
    }

    private void ClampWindowsToScreen()
    {
        var screenWidth = Screen.width;
        var screenHeight = Screen.height;
        
        if (GUI.matrix.m00 > 0)
        {
            screenWidth = (int)(screenWidth / GUI.matrix.m00);
            screenHeight = (int)(screenHeight / GUI.matrix.m00);
        }

        _windowRect.width = ClampFloat(_windowRect.width, 800f, Math.Max(800f, screenWidth - 40f));
        _windowRect.height = ClampFloat(_windowRect.height, 600f, Math.Max(600f, screenHeight - 40f));

        _windowRect.x = ClampFloat(_windowRect.x, 10, Math.Max(10f, screenWidth - _windowRect.width - 10f));
        _windowRect.y = ClampFloat(_windowRect.y, 10, Math.Max(10f, screenHeight - _windowRect.height - 10f));
    }

    private static void DrawCell(float x, float y, float width, string text, bool header)
    {
        DrawRoundedRect(new Rect(x, y, width, 38), header ? _headerStyle : _cellStyleBg);
        GUI.Label(new Rect(x + 10, y + 10, width - 20, 22), text, header ? _headerCellStyle : _cellStyle);
    }

    private static void DrawRect(Rect rect, Texture2D texture)
    {
        GUI.DrawTexture(rect, texture);
    }

    private static void DrawRect(Rect rect, Color color)
    {
        var oldColor = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = oldColor;
    }

    private void DrawIconCentered(Rect bounds, string baseItemName, ItemIconService.IconResult icon, float y, float width, float height, int count, string weightText = "")
    {
        var x = bounds.x + (bounds.width - width) * 0.5f;
        DrawIconInRect(new Rect(x, y, width, height), baseItemName, icon, count, weightText);
    }

    private void DrawIconInRect(Rect iconRect, string baseItemName, ItemIconService.IconResult icon, int count, string weightText = "")
    {
        var pad = Mathf.Clamp(Mathf.Min(iconRect.width, iconRect.height) * 0.06f, 2f, 6f);
        var frameRect = new Rect(iconRect.x, iconRect.y, iconRect.width, iconRect.height);
        var imageRect = new Rect(frameRect.x + pad, frameRect.y + pad, frameRect.width - 2f * pad, frameRect.height - 2f * pad);
        DrawRoundedRect(frameRect, _headerStyle);
        
        try
        {
            if (icon?.Texture != null)
            {
                if (IsFullUv(icon.Uv))
                    GUI.DrawTexture(imageRect, icon.Texture, ScaleMode.ScaleToFit, true);
                else
                    GUI.DrawTextureWithTexCoords(imageRect, icon.Texture, icon.Uv, true);
            }
            else
            {
                GUI.Label(new Rect(imageRect.x, imageRect.y + (imageRect.height - 20) * 0.5f, imageRect.width, 20), FormatItemName(baseItemName), _mutedStyle);
            }

            DrawCountBadge(frameRect, count);

            if (!string.IsNullOrEmpty(weightText))
            {
                var weightBadgeWidth = Mathf.Max(28f, 12f + weightText.Length * 7f);
                var weightBadgeRect = new Rect(frameRect.x + 4f, frameRect.y + 4f, weightBadgeWidth, 18f);
                DrawRoundedRect(weightBadgeRect, _panelStyle);
                
                GUI.Label(new Rect(weightBadgeRect.x + 3f, weightBadgeRect.y + 1f, weightBadgeRect.width - 6f, weightBadgeRect.height - 2f), weightText, _valueStyle);
            }
        }
        catch
        {
            // ignored
        }
    }

    private static bool IsFullUv(Rect uv)
    {
        const float epsilon = 0.0001f;
        return Mathf.Abs(uv.x) < epsilon
            && Mathf.Abs(uv.y) < epsilon
            && Mathf.Abs(uv.width - 1f) < epsilon
            && Mathf.Abs(uv.height - 1f) < epsilon;
    }

    private void DrawCountBadge(Rect frameRect, int count)
    {
        if (count <= 1)
            return;

        var badgeText = $"x{count}";
        var badgeWidth = Mathf.Max(28f, 12f + badgeText.Length * 9f);
        var badgeHeight = 18f;
        var badgeRect = new Rect(frameRect.xMax - badgeWidth - 4f, frameRect.yMax - badgeHeight - 4f, badgeWidth, badgeHeight);
        DrawRoundedRect(badgeRect, _panelStyle);
        
        GUI.Label(new Rect(badgeRect.x + 3f, badgeRect.y + 1f, badgeRect.width - 6f, badgeRect.height - 2f), badgeText, _valueStyle);
    }

    private static void DrawBorder(Rect rect, Texture2D borderTexture)
    {
        if (borderTexture == null)
            return;

        var xMax = rect.x + rect.width;
        var yMax = rect.y + rect.height;
        DrawRect(new Rect(rect.x, rect.y, rect.width, 1), borderTexture);
        DrawRect(new Rect(rect.x, yMax - 1, rect.width, 1), borderTexture);
        DrawRect(new Rect(rect.x, rect.y, 1, rect.height), borderTexture);
        DrawRect(new Rect(xMax - 1, rect.y, 1, rect.height), borderTexture);
    }

    private static void EnsureVisuals()
    {
        if (_visualsReady) return; 

        var windowBg = new Color(0.078f, 0.086f, 0.102f, 1f);
        var borderCol = new Color(0.157f, 0.169f, 0.192f, 1f);
        var childBg = new Color(0.094f, 0.102f, 0.118f, 1f);
        var frameBg = new Color(0.114f, 0.125f, 0.153f, 1f);
        var popupBg = new Color(0.078f, 0.086f, 0.102f, 1f);
        
        var textNormalColor = new Color(1.0f, 1.0f, 1.0f, 1f);
        var textMutedColor = new Color(0.75f, 0.80f, 0.88f, 1f);
        var accentCol = new Color(0.031f, 0.949f, 0.843f, 1f);
        var titleBg = new Color(0.047f, 0.055f, 0.071f, 1f);

        _panelTexture = CreateTexture(windowBg);
        _panelBorderTexture = CreateTexture(borderCol);
        _cellTexture = CreateTexture(childBg);
        _headerTexture = CreateTexture(frameBg);
        _dropdownTexture = CreateTexture(popupBg);

        _panelStyle = CreateRoundedStyle(windowBg, borderCol, 12, 1);
        _cellStyleBg = CreateRoundedStyle(childBg, borderCol, 20, 1);
        _headerStyle = CreateRoundedStyle(frameBg, borderCol, 12, 1);
        _dropdownStyle = CreateRoundedStyle(popupBg, borderCol, 17, 1);
        _frameStyle = CreateRoundedStyle(frameBg, borderCol, 12, 1);

        _titleStyle = CreateLabelStyle(18, TextAnchor.MiddleLeft, FontStyle.Bold, textNormalColor);
        _sectionStyle = CreateLabelStyle(18, TextAnchor.MiddleLeft, FontStyle.Bold, textNormalColor);
        _cellStyle = CreateLabelStyle(14, TextAnchor.MiddleLeft, FontStyle.Normal, textNormalColor);
        _headerCellStyle = CreateLabelStyle(14, TextAnchor.MiddleLeft, FontStyle.Normal, textMutedColor);
        _valueStyle = CreateLabelStyle(14, TextAnchor.MiddleCenter, FontStyle.Normal, textNormalColor);
        _statusStyle = CreateLabelStyle(12, TextAnchor.MiddleLeft, FontStyle.Italic, textMutedColor);
        _previewItemStyle = CreateLabelStyle(16, TextAnchor.MiddleCenter, FontStyle.Normal, textNormalColor);
        _statusEnabledStyle = CreateLabelStyle(13, TextAnchor.MiddleLeft, FontStyle.Bold, new Color(0.13f, 0.77f, 0.36f, 1f));
        _statusDisabledStyle = CreateLabelStyle(13, TextAnchor.MiddleLeft, FontStyle.Bold, new Color(0.93f, 0.26f, 0.26f, 1f));
        _mutedStyle = CreateLabelStyle(13, TextAnchor.MiddleLeft, FontStyle.Normal, textMutedColor);

        var btnNormal = new Color(0.118f, 0.133f, 0.149f, 1f);
        var btnHover = new Color(0.180f, 0.188f, 0.196f, 1f);
        var btnActive = new Color(0.210f, 0.225f, 0.245f, 1f); // tık anında hafif açılır

        // Accent (teal/cyan): hover ve active teal tonlarında — yeşil değil
        _accentButtonStyle = CreateButtonStyle(
            accentCol,
            new Color(0.15f, 0.97f, 0.88f, 1f),  // hover: daha açık teal
            new Color(0.02f, 0.72f, 0.64f, 1f),  // active: daha koyu teal
            new Color(0.04f, 0.04f, 0.04f, 1f));

        _buttonStyle = CreateButtonStyle(btnNormal, btnHover, btnActive, textNormalColor);
        _dangerButtonStyle = CreateButtonStyle(
            new Color(0.86f, 0.14f, 0.14f, 1f),
            new Color(0.72f, 0.11f, 0.11f, 1f),
            new Color(0.60f, 0.10f, 0.10f, 1f),
            textNormalColor);

        _fieldButtonStyle = CreateButtonStyle(frameBg, btnHover, btnActive, textNormalColor);
        _fieldButtonStyle.alignment = TextAnchor.MiddleLeft;

        _checkboxStyle = new GUIStyle(GUI.skin.button)
        {
            normal = { background = CreateTexture(frameBg), textColor = textNormalColor },
            hover = { background = CreateTexture(btnHover), textColor = textNormalColor },
            active = { background = CreateTexture(btnActive), textColor = textNormalColor },
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };

        _closeButtonStyle = new GUIStyle(GUI.skin.button)
        {
            normal = { background = CreateTexture(new Color(0,0,0,0)), textColor = textNormalColor },
            hover = { background = CreateTexture(new Color(1,1,1,0.1f)), textColor = textNormalColor },
            active = { background = CreateTexture(new Color(1,1,1,0.2f)), textColor = textNormalColor },
            alignment = TextAnchor.MiddleCenter
        };

        GUI.color = Color.white;
        GUI.contentColor = Color.white;
        GUI.backgroundColor = Color.white;

        _visualsReady = true;
    }

    private static Texture2D CreateTexture(Color color)
    {
        var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }

    private static GUIStyle CreateLabelStyle(int fontSize, TextAnchor alignment, FontStyle fontStyle, Color textColor)
    {
        var style = new GUIStyle(GUI.skin.label)
        {
            fontSize = fontSize,
            alignment = alignment,
            fontStyle = fontStyle,
            clipping = TextClipping.Clip,
            wordWrap = false
        };

        style.normal.textColor = textColor;
        return style;
    }

    private static GUIStyle CreateButtonStyle(Color normal, Color hover, Color active, Color text)
    {
        var style = new GUIStyle(GUI.skin.button);
        int radius = 6;
        style.normal.background = GenerateRoundedTexture(normal, normal, radius, 0);
        style.normal.textColor = text;
        style.hover.background = GenerateRoundedTexture(hover, hover, radius, 0);
        style.hover.textColor = Color.white;
        style.active.background = GenerateRoundedTexture(active, active, radius, 0);
        style.active.textColor = Color.white;
        style.alignment = TextAnchor.MiddleCenter;
        style.fontSize = 13;
        style.fontStyle = FontStyle.Bold;
        var offset = new RectOffset();
        offset.left = radius; offset.right = radius; offset.top = radius; offset.bottom = radius;
        style.border = offset;
        return style;
    }

    private void HandleDragging()
    {
        var currentEvent = Event.current;
        if (currentEvent == null || currentEvent.button != 0 && currentEvent.type != EventType.MouseDrag && currentEvent.type != EventType.MouseUp)
            return;

        var mouse = currentEvent.mousePosition;
        var mainDragRect = new Rect(_windowRect.x, _windowRect.y, _windowRect.width - 120, 46);

        if (currentEvent.type == EventType.MouseDown)
        {
            if (mainDragRect.Contains(mouse))
            {
                _draggingMain = true;
                _dragOffset = new Vector2(mouse.x - _windowRect.x, mouse.y - _windowRect.y);
                currentEvent.Use();
                return;
            }
        }

        if (currentEvent.type == EventType.MouseDrag)
        {
            if (_draggingMain)
            {
                _windowRect.x = mouse.x - _dragOffset.x;
                _windowRect.y = mouse.y - _dragOffset.y;
                currentEvent.Use();
                return;
            }
        }

        if (currentEvent.type == EventType.MouseUp)
        {
            _draggingMain = false;
        }
    }

    private void ConsumeWindowInput()
    {
        var currentEvent = Event.current;
        if (currentEvent == null || currentEvent.type == EventType.Used)
            return;

        if (!IsConsumableEvent(currentEvent.type))
            return;

        var mousePosition = currentEvent.mousePosition;
        if (!_windowRect.Contains(mousePosition) && !GetTriggerDropdownRect().Contains(mousePosition) && !GetItemDropdownRect().Contains(mousePosition))
            return;

        if (currentEvent.type == EventType.MouseDown)
        {
            if (_activeNumericField == "editor_item_text") _itemText = FormatItemString(_activeNumericBuffer);
            _activeNumericField = null;
            _searchFieldActive = false;
            _itemSearchFieldActive = false;
        }

        currentEvent.Use();
    }

    private static bool IsConsumableEvent(EventType eventType)
    {
        return eventType == EventType.MouseDown
            || eventType == EventType.MouseUp
            || eventType == EventType.MouseDrag
            || eventType == EventType.ScrollWheel;
    }

    private void DrawCard(float x, float y, float width, float height, string title)
    {
        var rect = new Rect(x, y, width, height);
        DrawRoundedRect(rect, _panelStyle);
        

        GUI.Label(new Rect(x + 22, y + 18, width - 44, 24), title, _sectionStyle);
    }

    private void DrawBorder(Rect rect, float thickness, Color color)
    {
        Color oldColor = GUI.color;
        GUI.color = color;

        // Üst kenar
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), Texture2D.whiteTexture);
        // Alt kenar
        GUI.DrawTexture(new Rect(rect.x, rect.y + rect.height - thickness, rect.width, thickness), Texture2D.whiteTexture);
        // Sol kenar
        GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), Texture2D.whiteTexture);
        // Sağ kenar
        GUI.DrawTexture(new Rect(rect.x + rect.width - thickness, rect.y, thickness, rect.height), Texture2D.whiteTexture);

        GUI.color = oldColor;
    }

    private void DrawRulesListRow(EditableRule rule, int index, float x, float y, float tableWidth, float selectWidth, float triggerWidth, float conditionWidth, float itemWidth, float countWidth, float statusWidth, float actionsWidth, Rect bodyRect)
    {
        var isSelected = index == _selectedRuleIndex;
        var rowRect = new Rect(x + 1, y, tableWidth - 2, 44);
        DrawBorder(rowRect, 1f, new Color(0.153f, 0.153f, 0.165f, 1f));
        DrawRoundedRect(rowRect, isSelected ? _headerStyle : (index % 2 == 0 ? _cellStyleBg : _panelStyle));

        if (GUI.Button(new Rect(x + 8, y + 8, selectWidth - 16, 28), (index + 1).ToString(CultureInfo.InvariantCulture), _buttonStyle))
            _selectedRuleIndex = index;

        var triggerX = x + selectWidth;
        var conditionX = triggerX + triggerWidth;
        var itemX = conditionX + conditionWidth;
        var countX = itemX + itemWidth;
        var statusX = countX + countWidth;
        var actionX = statusX + statusWidth;

        GUI.Label(new Rect(triggerX + 10, y + 11, triggerWidth - 20, 22), GetTriggerSymbol(rule.Trigger) + " " + GetTriggerLabel(rule.Trigger), _cellStyle);
        GUI.Label(new Rect(conditionX + 10, y + 11, conditionWidth - 20, 22), GetCompactConditionLabel(rule), _cellStyle);
        
        var displayItemName = FormatItemName(rule.Item);
        if (rule.Item.Contains(","))
        {
            var parts = rule.Item.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 1)
                displayItemName = $"{FormatItemName(parts[0].Trim())} (+{parts.Length - 1})";
        }
        GUI.Label(new Rect(itemX + 10, y + 11, itemWidth - 20, 22), displayItemName, _cellStyle);
        GUI.Label(new Rect(countX + 10, y + 11, countWidth - 20, 22), rule.Count.ToString(CultureInfo.InvariantCulture), _valueStyle);
        
        var states = RuleScheduler.GetRuleStates();
        var badgeText = rule.Enabled ? "● ACTIVE" : "● DISABLED";
        var badgeWidth = 75f;
        
        var badgeStyle = rule.Enabled ? _statusEnabledStyle : _statusDisabledStyle;
        var detailStyle = _mutedStyle;

        if (isSelected)
        {
            // Seçili satırda badge ve detail renklerini okunabilir tut
            // Badge rengini koru (yeşil/kırmızı), sadece detail yazısını açık yap
            detailStyle = new GUIStyle(detailStyle)
            {
                normal = { textColor = new Color(0.92f, 0.94f, 0.97f, 1f) }
            };
        }

        GUI.Label(new Rect(statusX + 10, y + 11, badgeWidth, 22), badgeText, badgeStyle);

        if (rule.Enabled && index < states.Count)
        {
            var state = states[index];
            var detailText = state.DebugStatus ?? "";
            
            if (state.Rule.MaxGrants > 0)
                detailText += $" ({state.GrantsCount}/{state.Rule.MaxGrants})";
            
            if (state.LastTriggeredGameTime >= 0f)
                detailText += $" [L:{state.LastTriggeredGameTime:0}s]";

            GUI.Label(new Rect(statusX + 10 + badgeWidth + 6, y + 11, statusWidth - badgeWidth - 26, 22), detailText, detailStyle);
        }

        if (rule.Enabled && index < states.Count)
        {
            var state = states[index];
            float progressPct = 0f;
            if (rule.Trigger == RewardTrigger.Time || rule.Trigger == RewardTrigger.Kills || rule.Trigger == RewardTrigger.BossKill || rule.Trigger == RewardTrigger.Gold || rule.Trigger == RewardTrigger.Level || rule.Trigger == RewardTrigger.Combo)
            {
                progressPct = rule.Interval > 0 ? Math.Min(1f, state.Progress / rule.Interval) : 1f;
            }
            else if (rule.Trigger == RewardTrigger.Random && state.RandomTargetTrigger.HasValue)
            {
                var target = state.RandomTargetTrigger.Value;
                if (target == RewardTrigger.Time) progressPct = Math.Min(1f, state.Progress / rule.RandomTimeSeconds);
                else if (target == RewardTrigger.Kills) progressPct = Math.Min(1f, state.Progress / rule.RandomKillCount);
                else if (target == RewardTrigger.BossKill) progressPct = Math.Min(1f, state.Progress / 1f);
            }

            if (progressPct > 0)
            {
                DrawProgressBar(new Rect(statusX + 10, y + 34, statusWidth - 40, 4), progressPct);
            }
        }

        if (GUI.Button(new Rect(actionX + 10, y + 8, actionsWidth - 20, 28), "...", _buttonStyle))
        {
            _selectedRuleIndex = index;
            _rowMenuIndex = _rowMenuIndex == index ? -1 : index;
        }

        if (_rowMenuIndex == index)
        {
            _rowMenuDrawIndex = index;
            _rowMenuRect = new Rect(
                bodyRect.x + actionX - 26,
                bodyRect.y + y + 34 - _rulesListScrollPos.y,
                92,
                140);
        }
    }

    private void DuplicateSelectedRule()
    {
        if (_selectedRuleIndex < 0 || _selectedRuleIndex >= _rules.Count)
        {
            _status = "Select a rule to duplicate.";
            return;
        }

        var source = _rules[_selectedRuleIndex];
        _rules.Insert(_selectedRuleIndex + 1, source.Clone());
        SaveRules("Rule duplicated.");
        _selectedRuleIndex = ClampInt(_selectedRuleIndex + 1, 0, _rules.Count - 1);
        OpenEditEditor(_selectedRuleIndex);
    }

    private void DrawRowMenu(Rect rect, int index)
    {
        GUI.depth = -10001;
        DrawRect(rect, _dropdownTexture);
        
        var yPos = rect.y + 4;
        
        if (GUI.Button(new Rect(rect.x + 4, yPos, rect.width - 8, 28), "EDIT", _buttonStyle))
        {
            _rowMenuIndex = -1;
            OpenEditEditor(index);
        }
        yPos += 34;

        if (GUI.Button(new Rect(rect.x + 4, yPos, rect.width - 8, 28), "DELETE", _dangerButtonStyle))
        {
            _selectedRuleIndex = index;
            _rowMenuIndex = -1;
            DeleteSelectedRule();
        }
        yPos += 34;
        
        GUI.enabled = index > 0;
        if (GUI.Button(new Rect(rect.x + 4, yPos, rect.width - 8, 28), "UP", _buttonStyle))
        {
            _rowMenuIndex = -1;
            MoveRule(index, index - 1);
        }
        yPos += 34;
        GUI.enabled = index < _rules.Count - 1;
        
        if (GUI.Button(new Rect(rect.x + 4, yPos, rect.width - 8, 28), "DOWN", _buttonStyle))
        {
            _rowMenuIndex = -1;
            MoveRule(index, index + 1);
        }
        GUI.enabled = true;

        GUI.depth = -10000;
    }

    private void MoveRule(int sourceIndex, int targetIndex)
    {
        if (sourceIndex < 0 || sourceIndex >= _rules.Count || targetIndex < 0 || targetIndex >= _rules.Count) return;
        var rule = _rules[sourceIndex];
        _rules.RemoveAt(sourceIndex);
        _rules.Insert(targetIndex, rule);
        SaveRules("Rule moved.");
    }

    private void DeleteSelectedRule()
    {
        if (_selectedRuleIndex < 0 || _selectedRuleIndex >= _rules.Count)
        {
            _status = "Select a rule to delete.";
            return;
        }

        _rules.RemoveAt(_selectedRuleIndex);
        SaveRules("Rule deleted.");
        CloseEditor();
        _selectedRuleIndex = -1;
    }

    private void TestSelectedRule()
    {
        if (_selectedRuleIndex < 0 || _selectedRuleIndex >= _rules.Count)
        {
            _status = "Select a rule to test.";
            return;
        }

        if (ItemGrantService.GrantItem(_rules[_selectedRuleIndex].Item, _rules[_selectedRuleIndex].Count))
            _status = "Test grant succeeded.";
        else
            _status = "Test grant failed. Check the log.";
    }

    private void MoveSelectedRule(int direction)
    {
        if (_selectedRuleIndex < 0 || _selectedRuleIndex >= _rules.Count)
        {
            _status = "Select a rule to move.";
            return;
        }

        var targetIndex = ClampInt(_selectedRuleIndex + direction, 0, _rules.Count - 1);
        if (targetIndex == _selectedRuleIndex)
            return;

        var rule = _rules[_selectedRuleIndex];
        _rules.RemoveAt(_selectedRuleIndex);
        _rules.Insert(targetIndex, rule);
        _selectedRuleIndex = targetIndex;
        SaveRules("Rule order updated.");
        OpenEditEditor(_selectedRuleIndex);
    }

    private void ExportRules()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ExportPath));
            var payload = new RuleExportPayload
            {
                Version = 1,
                Rules = _rules.ConvertAll(ToConfigString)
            };

            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ExportPath, json);
            _status = $"Exported rules to {ExportPath}";
        }
        catch (Exception ex)
        {
            _status = $"Export failed: {ex.GetBaseException().Message}";
        }
    }

    private void ImportRules()
    {
        try
        {
            if (!File.Exists(ExportPath))
            {
                _status = $"No export file at {ExportPath}";
                return;
            }

            var json = File.ReadAllText(ExportPath);
            var payload = JsonSerializer.Deserialize<RuleExportPayload>(json);
            if (payload?.Rules == null || payload.Rules.Count == 0)
            {
                _status = "Import file has no rules.";
                return;
            }

            _rules.Clear();
            foreach (var rawRule in payload.Rules)
            {
                if (!RewardRule.TryParse(rawRule, out var rule, out _))
                    continue;

                _rules.Add(EditableRule.FromRule(rule));
            }

            SaveRules("Rules imported.");
            OpenAddEditor();
        }
        catch (Exception ex)
        {
            _status = $"Import failed: {ex.GetBaseException().Message}";
        }
    }

    private string GetPreviewTriggerText()
    {
        if (_editorTrigger == RewardTrigger.Time)
            return $"{_durationValue} Seconds";

        if (_editorTrigger == RewardTrigger.Kills)
            return _killsValue == 1 ? "1 Kill" : $"{_killsValue} Kills";

        if (_editorTrigger == RewardTrigger.NewStage)
            return "New Stage";

        if (_editorTrigger == RewardTrigger.BossKill)
            return "Boss Kill";

        if (_editorTrigger == RewardTrigger.Gold)
            return $"{_killsValue} Gold";

        if (_editorTrigger == RewardTrigger.Level)
            return $"{_killsValue} Level";

        if (_editorTrigger == RewardTrigger.Combo)
            return $"{_killsValue} Kills in {_comboTimeValue}s";
        if (_editorTrigger == RewardTrigger.Health)
            return $"Health < {_durationValue}%";
        return "Random Trigger";
    }

    private string GetCompactPreviewTriggerText()
    {
        if (_editorTrigger == RewardTrigger.Time)
            return $"{_durationValue}s";

        if (_editorTrigger == RewardTrigger.Kills)
            return _killsValue == 1 ? "1 Kill" : $"{_killsValue} Kills";

        if (_editorTrigger == RewardTrigger.NewStage)
            return "New Stage";

        if (_editorTrigger == RewardTrigger.BossKill)
            return "Boss Kill";

        if (_editorTrigger == RewardTrigger.Gold)
            return $"{_killsValue} Gold";

        if (_editorTrigger == RewardTrigger.Level)
            return _killsValue == 1 ? "1 Level" : $"{_killsValue} Levels";

        if (_editorTrigger == RewardTrigger.Combo)
            return $"{_killsValue} Kills / {_comboTimeValue}s";
        if (_editorTrigger == RewardTrigger.Health)
            return $"HP < {_durationValue}%";
        return "Random";
    }

    private static string GetConditionLabel(EditableRule rule)
    {
        if (rule.Trigger == RewardTrigger.Time)
            return $"{rule.Interval} Seconds";

        if (rule.Trigger == RewardTrigger.Kills)
            return rule.Interval == 1 ? "1 Kill" : $"{rule.Interval} Kills";

        if (rule.Trigger == RewardTrigger.NewStage)
            return "Every New Stage";

        if (rule.Trigger == RewardTrigger.BossKill)
            return "Every Boss Kill";

        if (rule.Trigger == RewardTrigger.Gold)
            return $"{rule.Interval} Gold";

        if (rule.Trigger == RewardTrigger.Level)
            return rule.Interval == 1 ? "1 Level" : $"{rule.Interval} Levels";

        if (rule.Trigger == RewardTrigger.Combo)
            return $"{rule.Interval} Kills in {rule.ComboTimeSeconds}s";
        if (rule.Trigger == RewardTrigger.Health)
            return $"Health < {rule.Interval}%";
        return GetRandomSummary(rule);
    }

    private static string GetCompactConditionLabel(EditableRule rule)
    {
        if (rule.Trigger == RewardTrigger.Time)
            return $"{rule.Interval}s";

        if (rule.Trigger == RewardTrigger.Kills)
            return rule.Interval == 1 ? "1 Kill" : $"{rule.Interval} Kills";

        if (rule.Trigger == RewardTrigger.NewStage)
            return "New Stage";

        if (rule.Trigger == RewardTrigger.BossKill)
            return "Boss Kill";

        if (rule.Trigger == RewardTrigger.Gold)
            return $"{rule.Interval} Gold";

        if (rule.Trigger == RewardTrigger.Level)
            return rule.Interval == 1 ? "1 Level" : $"{rule.Interval} Levels";

        if (rule.Trigger == RewardTrigger.Combo)
            return $"{rule.Interval} Kills in {rule.ComboTimeSeconds}s";
        if (rule.Trigger == RewardTrigger.Health)
            return $"Health < {rule.Interval}%";
        return GetRandomSummary(rule);
    }

    private static string GetTriggerLabel(RewardTrigger trigger)
    {
        return trigger switch
        {
            RewardTrigger.Time => "Time",
            RewardTrigger.Kills => "Kills",
            RewardTrigger.NewStage => "New Stage",
            RewardTrigger.BossKill => "Boss",
            RewardTrigger.Random => "Random",
            RewardTrigger.Combo => "Combo",
            RewardTrigger.Health => "Health Drop",
            RewardTrigger.Gold => "Gold",
            RewardTrigger.Level => "Level",
            _ => "Time"
        };
    }

    private static string GetRepeatModeLabel(RuleRepeatMode mode)
    {
        return mode switch
        {
            RuleRepeatMode.OneShot => "One Shot",
            RuleRepeatMode.Cooldown => "Cooldown",
            _ => "Repeat"
        };
    }

    private static RuleRepeatMode GetNextRepeatMode(RuleRepeatMode current)
    {
        return current switch
        {
            RuleRepeatMode.Repeat => RuleRepeatMode.OneShot,
            RuleRepeatMode.OneShot => RuleRepeatMode.Cooldown,
            _ => RuleRepeatMode.Repeat
        };
    }

    private static string GetRepeatModeConfigValue(RuleRepeatMode mode)
    {
        return mode switch
        {
            RuleRepeatMode.OneShot => "oneshot",
            RuleRepeatMode.Cooldown => "cooldown",
            _ => "repeat"
        };
    }

    private string GetTriggerFilterLabel()
    {
        return _ruleTriggerFilter.HasValue ? GetTriggerLabel(_ruleTriggerFilter.Value) : "All";
    }

    private static string GetFilterModeLabel(RuleFilterMode mode)
    {
        return mode switch
        {
            RuleFilterMode.All => "ALL",
            RuleFilterMode.Active => "ACTIVE",
            RuleFilterMode.Disabled => "DISABLED",
            _ => "ALL"
        };
    }

    private void CycleTriggerFilter()
    {
        var sequence = new RewardTrigger?[]
        {
            null,
            RewardTrigger.Time,
            RewardTrigger.Kills,
            RewardTrigger.Combo,
            RewardTrigger.Health,
            RewardTrigger.Gold,
            RewardTrigger.Level,
            RewardTrigger.NewStage,
            RewardTrigger.BossKill,
            RewardTrigger.Random
        };

        var currentIndex = Array.IndexOf(sequence, _ruleTriggerFilter);
        if (currentIndex < 0)
            currentIndex = 0;

        _ruleTriggerFilter = sequence[(currentIndex + 1) % sequence.Length];
    }

    private static string GetTriggerSymbol(RewardTrigger trigger)
    {
        return trigger switch
        {
            RewardTrigger.Time => "[T]",
            RewardTrigger.Kills => "[K]",
            RewardTrigger.NewStage => "[S]",
            RewardTrigger.BossKill => "[B]",
            RewardTrigger.Random => "[R]",
            RewardTrigger.Combo => "[C]",
            RewardTrigger.Health => "[H]",
            RewardTrigger.Gold => "[G]",
            RewardTrigger.Level => "[L]",
            _ => "[-]"
        };
    }

    private void DrawAdvancedStepper(ref int value, float x, float y, int min, int max, string fieldId)
    {
        if (GUI.Button(new Rect(x, y, 44, 36), "-100", _buttonStyle)) value = Math.Max(min, value - 100);
        if (GUI.Button(new Rect(x + 48, y, 40, 36), "-10", _buttonStyle)) value = Math.Max(min, value - 10);
        if (GUI.Button(new Rect(x + 92, y, 36, 36), "-1", _buttonStyle)) value = Math.Max(min, value - 1);

        DrawNumericValueBox(ref value, new Rect(x + 132, y, 60, 36), min, max, fieldId);

        if (GUI.Button(new Rect(x + 196, y, 36, 36), "+1", _buttonStyle)) value = Math.Min(max, value + 1);
        if (GUI.Button(new Rect(x + 236, y, 40, 36), "+10", _buttonStyle)) value = Math.Min(max, value + 10);
        if (GUI.Button(new Rect(x + 280, y, 44, 36), "+100", _buttonStyle)) value = Math.Min(max, value + 100);
    }

    private void DrawStepper(ref int value, float x, float y, int min, int max, int step, string fieldId)
    {
        if (GUI.Button(new Rect(x, y, 32, 32), "-", _buttonStyle))
            value = Math.Max(min, value - step);

        DrawNumericValueBox(ref value, new Rect(x + 40, y, 46, 32), min, max, fieldId);

        if (GUI.Button(new Rect(x + 94, y, 32, 32), "+", _buttonStyle))
            value = Math.Min(max, value + step);
    }

    private void DrawTextCursor(Rect rect, string text, int cursorPos, GUIStyle style)
    {
        if (UnityEngine.Time.time % 1f > 0.5f) return;
        
        int safeCursor = Math.Min(Math.Max(0, cursorPos), text.Length);
        
        float textWidth = style.CalcSize(new GUIContent(text)).x;
        float cursorOffset = style.CalcSize(new GUIContent(text.Substring(0, safeCursor))).x;
        
        float startX = rect.x;
        if (style.alignment == TextAnchor.MiddleCenter || style.alignment == TextAnchor.UpperCenter || style.alignment == TextAnchor.LowerCenter)
            startX += (rect.width - textWidth) / 2f;
        else if (style.alignment == TextAnchor.MiddleRight || style.alignment == TextAnchor.UpperRight || style.alignment == TextAnchor.LowerRight)
            startX += rect.width - textWidth;

        float cursorHeight = rect.height * 0.6f;
        float cursorY = rect.y + (rect.height - cursorHeight) / 2f;
        var cursorRect = new Rect(startX + cursorOffset, cursorY, 2, cursorHeight);
        GUI.DrawTexture(cursorRect, UnityEngine.Texture2D.whiteTexture);
    }

    private bool DrawSelectField(Rect rect, string text)
    {
        DrawRoundedRect(rect, _frameStyle);
        GUI.Label(new Rect(rect.x + 14, rect.y + 8, rect.width - 44, rect.height - 16), text, _cellStyle);
        GUI.Label(new Rect(rect.x + rect.width - 28, rect.y + 8, 16, rect.height - 16), "v", _mutedStyle);
        return GUI.Button(rect, new GUIContent(string.Empty), GUIStyle.none);
    }

    private void DrawNumericValueBox(ref int value, Rect rect, int min, int max, string fieldId)
    {
        var isActive = _activeNumericField == fieldId;
        DrawRoundedRect(rect, isActive ? _headerStyle : _frameStyle);
        

        if (GUI.Button(rect, string.Empty, _closeButtonStyle))
        {
            if (_activeNumericField == "editor_item_text") _itemText = FormatItemString(_activeNumericBuffer);
            _searchFieldActive = false;
            _itemSearchFieldActive = false;
            _activeNumericField = fieldId;
            _activeNumericBuffer = value.ToString(CultureInfo.InvariantCulture);
            _numericCursorPos = _activeNumericBuffer.Length;
        }

        if (isActive && int.TryParse(_activeNumericBuffer, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            value = ClampInt(parsed, min, max);

        var text = isActive ? _activeNumericBuffer : value.ToString(CultureInfo.InvariantCulture);
        var labelRect = new Rect(rect.x + 4, rect.y + 4, rect.width - 8, rect.height - 8);
        GUI.Label(labelRect, text.Length == 0 && !isActive ? "_" : text, _valueStyle);
        if (isActive) DrawTextCursor(labelRect, text, _numericCursorPos, _valueStyle);
    }

    private static void ParseItemString(string input, out string name, out int weight)
    {
        name = input.Trim();
        weight = 100;
        var match = System.Text.RegularExpressions.Regex.Match(name, @"(.*?)(?:%(\d+)|\((\d+)%\)|(\d+)%)$");
        if (match.Success)
        {
            name = match.Groups[1].Value.Trim();
            string wStr = match.Groups[2].Success ? match.Groups[2].Value :
                          match.Groups[3].Success ? match.Groups[3].Value :
                          match.Groups[4].Success ? match.Groups[4].Value : "";
            if (!string.IsNullOrEmpty(wStr) && int.TryParse(wStr, out var parsedWeight) && parsedWeight >= 0)
            {
                weight = parsedWeight;
            }
        }
        else
        {
            var parts = name.Split('%');
            name = parts[0].Trim();
            if (parts.Length > 1 && int.TryParse(parts[1], out var parsedWeight) && parsedWeight >= 0)
                weight = parsedWeight;
        }
    }

    private static string GetItemNameOnly(string input)
    {
        ParseItemString(input, out string name, out _);
        return name;
    }

    private string FormatItemString(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return input;
        var parts = input.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        var formatted = new System.Collections.Generic.List<string>();
        foreach (var part in parts)
        {
            var p = part.Trim();
            var match = System.Text.RegularExpressions.Regex.Match(p, @"(.*?)(?:%(\d+)|\((\d+)%\)|(\d+)%)$");
            if (match.Success)
            {
                string name = match.Groups[1].Value.Trim();
                if (name.Length > 0 && char.IsLower(name[0])) name = char.ToUpper(name[0]) + name.Substring(1);

                string wStr = match.Groups[2].Success ? match.Groups[2].Value :
                              match.Groups[3].Success ? match.Groups[3].Value :
                              match.Groups[4].Success ? match.Groups[4].Value : "";
                if (!string.IsNullOrEmpty(wStr))
                {
                    formatted.Add($"{name} ({wStr}%)");
                    continue;
                }
            }
            string partName = p;
            if (partName.Length > 0 && char.IsLower(partName[0])) partName = char.ToUpper(partName[0]) + partName.Substring(1);
            formatted.Add(partName);
        }
        return string.Join(", ", formatted);
    }

    private void DrawStringValueBox(ref string value, Rect rect, string fieldId)
    {
        var isActive = _activeNumericField == fieldId;
        DrawRoundedRect(rect, isActive ? _headerStyle : _frameStyle);

        if (GUI.Button(rect, new GUIContent(string.Empty), GUIStyle.none))
        {
            if (_activeNumericField == "editor_item_text" && fieldId != "editor_item_text") _itemText = FormatItemString(_activeNumericBuffer);
            _searchFieldActive = false;
            _itemSearchFieldActive = false;
            if (_activeNumericField == "editor_item_text" && fieldId == "editor_item_text")
                value = FormatItemString(_activeNumericBuffer);
            _activeNumericField = fieldId;
            _activeNumericBuffer = value;
            _numericCursorPos = _activeNumericBuffer.Length;
        }

        if (isActive)
        {
            value = _activeNumericBuffer;
        }
        else
        {
            if (fieldId == "editor_item_text")
                value = FormatItemString(value);
        }

        var text = isActive ? _activeNumericBuffer : value;
        var labelRect = new Rect(rect.x + 14, rect.y + 4, rect.width - 28, rect.height - 8);
        GUI.Label(labelRect, text.Length == 0 && !isActive ? "_" : text, _cellStyle);
        if (isActive) DrawTextCursor(labelRect, text, _numericCursorPos, _cellStyle);
    }

    private void DrawModeDropdown(float x, float y)
    {
        var modes = new[] { RuleRepeatMode.Repeat, RuleRepeatMode.OneShot, RuleRepeatMode.Cooldown };
        var height = 8f + modes.Length * 30f;
        var rect = new Rect(x, y, 200, height);
        
        GUI.depth = -10001;
        DrawRect(rect, _dropdownTexture);
        

        for (var i = 0; i < modes.Length; i++)
        {
            if (GUI.Button(new Rect(x + 4, y + 4 + i * 30, 192, 26), GetRepeatModeLabel(modes[i]), _fieldButtonStyle))
            {
                _editorRepeatMode = modes[i];
                _rowMenuIndex = -1;
            }
        }
        GUI.depth = -10000;
    }

    private void HandleNumericKeyboardInput()
    {
        if (string.IsNullOrEmpty(_activeNumericField))
            return;

        var currentEvent = Event.current;
        if (currentEvent == null || currentEvent.type != EventType.KeyDown)
            return;

        if (currentEvent.keyCode == KeyCode.Escape || currentEvent.keyCode == KeyCode.Return || currentEvent.keyCode == KeyCode.KeypadEnter)
        {
            _activeNumericField = null;
            _activeNumericBuffer = string.Empty;
            _numericCursorPos = -1;
            currentEvent.Use();
            return;
        }

        if (currentEvent.keyCode == KeyCode.LeftArrow)
        {
            if (_numericCursorPos > 0) _numericCursorPos--;
            currentEvent.Use();
            return;
        }

        if (currentEvent.keyCode == KeyCode.RightArrow)
        {
            if (_numericCursorPos < _activeNumericBuffer.Length) _numericCursorPos++;
            currentEvent.Use();
            return;
        }

        if (currentEvent.keyCode == KeyCode.Backspace)
        {
            if (_activeNumericBuffer.Length > 0 && _numericCursorPos > 0)
            {
                _activeNumericBuffer = _activeNumericBuffer.Remove(_numericCursorPos - 1, 1);
                _numericCursorPos--;
            }
            currentEvent.Use();
            return;
        }

        if (currentEvent.keyCode == KeyCode.Delete)
        {
            if (_numericCursorPos < _activeNumericBuffer.Length)
            {
                _activeNumericBuffer = _activeNumericBuffer.Remove(_numericCursorPos, 1);
            }
            currentEvent.Use();
            return;
        }

        var character = currentEvent.character;
        bool isStringField = _activeNumericField.Contains("text") || _activeNumericField.Contains("item");
        
        if (isStringField)
        {
            if (char.IsLetterOrDigit(character) || character == ' ' || character == ',' || character == '_' || character == '-' || character == '%' || character == '(' || character == ')')
            {
                int safeCursor = Math.Min(Math.Max(0, _numericCursorPos), _activeNumericBuffer.Length);
                _activeNumericBuffer = _activeNumericBuffer.Insert(safeCursor, character.ToString());
                _numericCursorPos = safeCursor + 1;
                currentEvent.Use();
            }
        }
        else if (character >= '0' && character <= '9')
        {
            int safeCursor = Math.Min(Math.Max(0, _numericCursorPos), _activeNumericBuffer.Length);
            if (_activeNumericBuffer == "0")
            {
                _activeNumericBuffer = character.ToString();
                _numericCursorPos = 1;
            }
            else
            {
                _activeNumericBuffer = _activeNumericBuffer.Insert(safeCursor, character.ToString());
                _numericCursorPos = safeCursor + 1;
            }
            currentEvent.Use();
        }
    }

    private void HandleSearchKeyboardInput()
    {
        if (!_searchFieldActive && !_itemSearchFieldActive && !_profileDialogOpen)
            return;

        var currentEvent = Event.current;
        if (currentEvent == null || currentEvent.type != EventType.KeyDown)
            return;

        if (currentEvent.keyCode == KeyCode.Escape || currentEvent.keyCode == KeyCode.Return || currentEvent.keyCode == KeyCode.KeypadEnter)
        {
            _searchFieldActive = false;
            _itemSearchFieldActive = false;
            _searchCursorPos = -1;
            _itemSearchCursorPos = -1;
            currentEvent.Use();
            return;
        }

        if (currentEvent.keyCode == KeyCode.LeftArrow)
        {
            if (_profileDialogOpen && _profileDialogCursorPos > 0) _profileDialogCursorPos--;
            else if (_searchFieldActive && _searchCursorPos > 0) _searchCursorPos--;
            else if (_itemSearchFieldActive && _itemSearchCursorPos > 0) _itemSearchCursorPos--;
            currentEvent.Use();
            return;
        }

        if (currentEvent.keyCode == KeyCode.RightArrow)
        {
            if (_profileDialogOpen && _profileDialogCursorPos < _profileDialogText.Length) _profileDialogCursorPos++;
            else if (_searchFieldActive && _searchCursorPos < _searchText.Length) _searchCursorPos++;
            else if (_itemSearchFieldActive && _itemSearchCursorPos < _itemSearchText.Length) _itemSearchCursorPos++;
            currentEvent.Use();
            return;
        }

        if (currentEvent.keyCode == KeyCode.Backspace)
        {
            if (_profileDialogOpen && _profileDialogText.Length > 0 && _profileDialogCursorPos > 0)
            {
                _profileDialogText = _profileDialogText.Remove(_profileDialogCursorPos - 1, 1);
                _profileDialogCursorPos--;
            }
            else if (_searchFieldActive && _searchText.Length > 0 && _searchCursorPos > 0)
            {
                _searchText = _searchText.Remove(_searchCursorPos - 1, 1);
                _searchCursorPos--;
            }
            else if (_itemSearchFieldActive && _itemSearchText.Length > 0 && _itemSearchCursorPos > 0)
            {
                _itemSearchText = _itemSearchText.Remove(_itemSearchCursorPos - 1, 1);
                _itemSearchCursorPos--;
            }
            currentEvent.Use();
            return;
        }

        if (currentEvent.keyCode == KeyCode.Delete)
        {
            if (_profileDialogOpen && _profileDialogCursorPos < _profileDialogText.Length)
            {
                _profileDialogText = _profileDialogText.Remove(_profileDialogCursorPos, 1);
            }
            else if (_searchFieldActive && _searchCursorPos < _searchText.Length)
            {
                _searchText = _searchText.Remove(_searchCursorPos, 1);
            }
            else if (_itemSearchFieldActive && _itemSearchCursorPos < _itemSearchText.Length)
            {
                _itemSearchText = _itemSearchText.Remove(_itemSearchCursorPos, 1);
            }
            currentEvent.Use();
            return;
        }

        var character = currentEvent.character;
        if (char.IsLetterOrDigit(character) || character == ' ' || character == '_' || character == '-')
        {
            if (_profileDialogOpen)
            {
                int safeCursor = Math.Min(Math.Max(0, _profileDialogCursorPos), _profileDialogText.Length);
                _profileDialogText = _profileDialogText.Insert(safeCursor, character.ToString());
                _profileDialogCursorPos = safeCursor + 1;
            }
            else if (_searchFieldActive)
            {
                int safeCursor = Math.Min(Math.Max(0, _searchCursorPos), _searchText.Length);
                _searchText = _searchText.Insert(safeCursor, character.ToString());
                _searchCursorPos = safeCursor + 1;
            }
            else if (_itemSearchFieldActive)
            {
                int safeCursor = Math.Min(Math.Max(0, _itemSearchCursorPos), _itemSearchText.Length);
                _itemSearchText = _itemSearchText.Insert(safeCursor, character.ToString());
                _itemSearchCursorPos = safeCursor + 1;
            }
            currentEvent.Use();
        }
    }

        // "scale" parametresi eklendi. Varsayılan değeri 1f (orijinal boyut).
    private void DrawToggleSwitch(ref bool value, float x, float y, float scale = 1f)
    {
        // Temel değerleri ölçek ile çarpıyoruz
        float trackWidth = 64f * scale;
        float trackHeight = 32f * scale;
        float padding = 3f * scale;
        float knobSize = 26f * scale;

        var trackRect = new Rect(x, y, trackWidth, trackHeight);
        
        // Topuzun açık ve kapalı konumlardaki X pozisyonunu matematiksel olarak hesaplıyoruz
        float knobX = value ? (x + trackWidth - knobSize - padding) : (x + padding);
        var knobRect = new Rect(knobX, y + padding, knobSize, knobSize);

        DrawRect(trackRect, value ? _accentButtonStyle.normal.background : _fieldButtonStyle.normal.background);
        
        if (GUI.Button(trackRect, string.Empty, _closeButtonStyle))
            value = !value;
            
        GUI.DrawTexture(knobRect, Texture2D.whiteTexture);
    }

    private void DrawKillStepper(ref int value, float x, float y, float width, int min, int max, string fieldId)
    {
        var deltas = new[] { -100, -10, -1, 1, 10, 100 };
        var gap = 6f;
        var valueWidth = 66f;
        var buttonWidths = new[] { 52f, 44f, 36f, 36f, 44f, 52f };

        var groupWidth = buttonWidths[0] + buttonWidths[1] + buttonWidths[2] + 2f * gap;
        var idealWidth = 2f * groupWidth + valueWidth;
        var scale = width < idealWidth ? Math.Max(0.78f, width / idealWidth) : 1f;
        for (var i = 0; i < buttonWidths.Length; i++)
            buttonWidths[i] *= scale;
        valueWidth *= scale;
        gap *= scale;

        groupWidth = buttonWidths[0] + buttonWidths[1] + buttonWidths[2] + 2f * gap;
        var remainingWidth = Math.Max(0f, width - (2f * groupWidth + valueWidth));
        var valueX = x + groupWidth + (remainingWidth * 0.5f);
        var plusX = x + width - groupWidth;

        for (var i = 0; i < deltas.Length; i++)
        {
            var delta = deltas[i];
            var label = delta > 0 ? $"+{delta}" : delta.ToString(CultureInfo.InvariantCulture);
            var buttonWidth = buttonWidths[i];
            var buttonX = i < 3
                ? x + (i == 0 ? 0 : buttonWidths[0] + gap + (i == 2 ? buttonWidths[1] + gap : 0))
                : plusX + (i == 3 ? 0 : buttonWidths[3] + gap + (i == 5 ? buttonWidths[4] + gap : 0));

            if (GUI.Button(new Rect(buttonX, y, buttonWidth, 32), label, _buttonStyle))
                value = ClampInt(value + delta, min, max);
        }

        DrawNumericValueBox(ref value, new Rect(valueX, y, valueWidth, 32), min, max, fieldId);
    }

    private void DrawRandomToggle(ref bool value, float x, float y, string label)
    {
        if (GUI.Button(new Rect(x, y, 92, 28), label, value ? _accentButtonStyle : _buttonStyle))
            value = !value;
    }

    private void DrawTriggerDropdown(float x, float y)
    {
        if (!_triggerDropdownVisible)
            return;

        var triggers = new[]
        {
            RewardTrigger.Time,
            RewardTrigger.Kills,
            RewardTrigger.Combo,
            RewardTrigger.Health,
            RewardTrigger.Gold,
            RewardTrigger.Level,
            RewardTrigger.NewStage,
            RewardTrigger.BossKill,
            RewardTrigger.Random
        };

        var dropdownHeight = 8f + triggers.Length * 30f;
        GUI.depth = -10001;
        DrawRoundedRect(new Rect(x, y, 200, dropdownHeight), _dropdownStyle);
        

        for (var i = 0; i < triggers.Length; i++)
        {
            if (!GUI.Button(new Rect(x + 4, y + 4 + i * 30, 192, 26), GetTriggerLabel(triggers[i]), _fieldButtonStyle))
                continue;

            _editorTrigger = triggers[i];
            _triggerDropdownVisible = false;
        }

        GUI.depth = -10000;
    }

    private void DrawOverlayDropdowns()
    {
        if (!_editorOpen)
            return;

        if (_triggerDropdownVisible)
            DrawTriggerDropdown(_lastTriggerRect.x, _lastTriggerRect.y + _lastTriggerRect.height + 2);

        if (_itemDropdownVisible)
            DrawItemDropdown(_lastItemRect.x, _lastItemRect.y + _lastItemRect.height + 2, _lastItemRect.width + 46f);

        if (_rowMenuIndex == -100)
            DrawModeDropdown(_lastModeRect.x, _lastModeRect.y + _lastModeRect.height + 2);
    }

    private Rect GetEditorDialogRect()
    {
        var width = Math.Min(920f, _windowRect.width - 80f);
        float baseHeight = 580f;
        
        if (_editorTrigger == RewardTrigger.Random) baseHeight += 124f;
        if (_editorTrigger == RewardTrigger.Combo) baseHeight += 48f;
        if (_editorRepeatMode == RuleRepeatMode.Cooldown) baseHeight += 48f;
        
        return new Rect(
            _windowRect.x + (_windowRect.width - width) * 0.5f,
            _windowRect.y + 74f,
            width,
            baseHeight);
    }

    private Rect GetTriggerDropdownRect()
    {
        const float dropdownWidth = 200f;
        const float rowHeight = 30f;
        const int triggerCount = 9;
        var dropdownHeight = 8f + triggerCount * rowHeight;
        return new Rect(_lastTriggerRect.x, _lastTriggerRect.y + _lastTriggerRect.height + 2f, dropdownWidth, dropdownHeight);
    }

    private Rect GetItemDropdownRect()
    {
        const float searchHeight = 36f;
        const float viewportHeight = 260f;
        var width = _lastItemRect.width + 46f;
        var height = viewportHeight + searchHeight + 8f;
        return new Rect(_lastItemRect.x, _lastItemRect.y + _lastItemRect.height + 2f, width, height);
    }

    private Rect GetModeDropdownRect()
    {
        const float rowHeight = 30f;
        var height = 8f + 3 * rowHeight;
        return new Rect(_lastModeRect.x, _lastModeRect.y + _lastModeRect.height + 2f, 200f, height);
    }

    private static string GetRandomSummary(EditableRule rule)
    {
        var parts = new List<string>();
        if (rule.RandomAllowTime)
            parts.Add($"{rule.RandomTimeSeconds}s");
        if (rule.RandomAllowKills)
            parts.Add($"{rule.RandomKillCount} " + (rule.RandomKillCount == 1 ? "Kill" : "Kills"));
        if (rule.RandomAllowNewStage)
            parts.Add("Stage");
        if (rule.RandomAllowBossKill)
            parts.Add("Boss");

        return "Random: " + string.Join(", ", parts);
    }

    private int FindItemIndex(string itemName)
    {
        for (var i = 0; i < KnownItems.Count; i++)
        {
            if (string.Equals(KnownItems[i], itemName, StringComparison.OrdinalIgnoreCase))
                return i;
        }

        return 0;
    }

    private static void EnsureKnownItemsLoaded()
    {
        if (_knownItemsLoaded)
            return;

        _knownItemsLoaded = true;

        try
        {
            var eItemType = GameReflection.FindType(
                "Il2CppAssets.Scripts.Inventory__Items__Pickups.Items.EItem",
                "Assets.Scripts.Inventory__Items__Pickups.Items.EItem",
                "EItem");

            if (eItemType == null || !eItemType.IsEnum)
            {
                Main.Warn("[UI] EItem enum not found. Using fallback known item list.");
                return;
            }

            var enumNames = Enum.GetNames(eItemType);
            if (enumNames == null || enumNames.Length == 0)
            {
                Main.Warn("[UI] EItem enum is empty. Using fallback known item list.");
                return;
            }

            KnownItems.Clear();
            for (var i = 0; i < enumNames.Length; i++)
            {
                var name = enumNames[i]?.Trim();
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                if (!KnownItems.Contains(name))
                    KnownItems.Add(name);
            }

            KnownItems.Add("Chaos");
            foreach (var effect in MegaChaos.Services.Chaos.ChaosEngine.Instance.AvailableEffects)
            {
                KnownItems.Add($"Chaos: {effect.Name}");
            }

            if (KnownItems.Count == 0)
            {
                KnownItems.AddRange(FallbackKnownItems);
                Main.Warn("[UI] EItem enum produced no valid names. Reverted to fallback known item list.");
                return;
            }

            Main.Warn($"[UI] Loaded {KnownItems.Count} known items from EItem enum.");
        }
        catch (Exception ex)
        {
            KnownItems.Clear();
            KnownItems.AddRange(FallbackKnownItems);
            Main.Warn($"[UI] Failed to load EItem enum for known items: {ex.GetBaseException().Message}. Using fallback list.");
        }
    }

    private static int ClampInt(int value, int min, int max)
    {
        if (value < min)
            return min;

        return value > max ? max : value;
    }

    private static float ClampFloat(float value, float min, float max)
    {
        if (value < min)
            return min;

        return value > max ? max : value;
    }

    private static string BoolToString(bool value)
    {
        return value ? "1" : "0";
    }

    private static string ToConfigString(EditableRule rule)
    {
        var trigger = rule.Trigger switch
        {
            RewardTrigger.Time => "time",
            RewardTrigger.Kills => "kills",
            RewardTrigger.Combo => "combo",
            RewardTrigger.Health => "health",
            RewardTrigger.Gold => "gold",
            RewardTrigger.Level => "level",
            RewardTrigger.NewStage => "newstage",
            RewardTrigger.BossKill => "bosskill",
            RewardTrigger.Random => "random",
            _ => "time"
        };

        var serialized = $"{trigger}:{rule.Interval}:{rule.Item}:{rule.Count}|enabled={BoolToString(rule.Enabled)}|mode={GetRepeatModeConfigValue(rule.RepeatMode)}";
        if (rule.CooldownSeconds > 0)
            serialized += $"|cd={rule.CooldownSeconds}";
        if (rule.MaxGrants > 0)
            serialized += $"|max={rule.MaxGrants}";
        if (rule.Trigger == RewardTrigger.Combo)
            serialized += $"|ctime={rule.ComboTimeSeconds}";

        if (rule.Trigger != RewardTrigger.Random)
            return serialized;

        return serialized
            + $"|rtime={rule.RandomTimeSeconds}"
            + $"|rkills={rule.RandomKillCount}"
            + $"|rallowtime={BoolToString(rule.RandomAllowTime)}"
            + $"|rallowkills={BoolToString(rule.RandomAllowKills)}"
            + $"|rstage={BoolToString(rule.RandomAllowNewStage)}"
            + $"|rboss={BoolToString(rule.RandomAllowBossKill)}";
    }

    private static string FormatTime(int seconds)
    {
        return $"{seconds} seconds";
    }

    private static string FormatItemName(string itemName)
    {
        if (string.IsNullOrWhiteSpace(itemName))
            return string.Empty;

        var builder = new StringBuilder(itemName.Length + 8);
        for (var i = 0; i < itemName.Length; i++)
        {
            var character = itemName[i];
            if (i > 0 && char.IsUpper(character) && char.IsLower(itemName[i - 1]))
                builder.Append(' ');

            builder.Append(character);
        }

        return builder.ToString();
    }

    private sealed class EditableRule
    {
        public EditableRule(
            bool enabled,
            RewardTrigger trigger,
            int interval,
            string item,
            int count,
            RuleRepeatMode repeatMode,
            int cooldownSeconds,
            int maxGrants,
            int comboTimeSeconds,
            int randomTimeSeconds,
            int randomKillCount,
            bool randomAllowTime,
            bool randomAllowKills,
            bool randomAllowNewStage,
            bool randomAllowBossKill)
        {
            Enabled = enabled;
            Trigger = trigger;
            Interval = interval;
            Item = item;
            Count = count;
            RepeatMode = repeatMode;
            CooldownSeconds = cooldownSeconds;
            MaxGrants = maxGrants;
            ComboTimeSeconds = comboTimeSeconds;
            RandomTimeSeconds = randomTimeSeconds;
            RandomKillCount = randomKillCount;
            RandomAllowTime = randomAllowTime;
            RandomAllowKills = randomAllowKills;
            RandomAllowNewStage = randomAllowNewStage;
            RandomAllowBossKill = randomAllowBossKill;
        }

        public bool Enabled { get; }

        public RewardTrigger Trigger { get; }

        public int Interval { get; }

        public string Item { get; }

        public int Count { get; }

        public RuleRepeatMode RepeatMode { get; }

        public int CooldownSeconds { get; }

        public int MaxGrants { get; }

        public int ComboTimeSeconds { get; }

        public int RandomTimeSeconds { get; }

        public int RandomKillCount { get; }

        public bool RandomAllowTime { get; }

        public bool RandomAllowKills { get; }

        public bool RandomAllowNewStage { get; }

        public bool RandomAllowBossKill { get; }

        public static EditableRule FromRule(RewardRule rule)
        {
            return new EditableRule(
                rule.Enabled,
                rule.Trigger,
                rule.Interval,
                rule.ItemName,
                rule.Count,
                rule.RepeatMode,
                rule.CooldownSeconds,
                rule.MaxGrants,
                rule.ComboTimeSeconds,
                rule.RandomTimeSeconds,
                rule.RandomKillCount,
                rule.RandomAllowTime,
                rule.RandomAllowKills,
                rule.RandomAllowNewStage,
                rule.RandomAllowBossKill);
        }

        public EditableRule Clone()
        {
            return new EditableRule(Enabled, Trigger, Interval, Item, Count, RepeatMode, CooldownSeconds, MaxGrants, ComboTimeSeconds, RandomTimeSeconds, RandomKillCount, RandomAllowTime, RandomAllowKills, RandomAllowNewStage, RandomAllowBossKill);
        }
    }

    private sealed class RuleExportPayload
    {
        public int Version { get; set; }

        public List<string> Rules { get; set; } = new();
    }
}
