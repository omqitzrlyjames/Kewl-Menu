using UnityEngine;
using System;
using System.Globalization;

namespace SocietyMenu;

public class SettingsTab : ITab
{
    public string name => "Settings";

    private bool _initialized = false;

    // Custom text fields
    private TextField _menuKeybindField;
    private TextField _menuColorField;
    private TextField _spoofLevelField;
    private TextField _spoofPlatformField;
    private TextField _glassGradientStartField;
    private TextField _glassGradientEndField;

    public void Draw()
    {
        if (!_initialized)
        {
            InitializeInputFields();
            _initialized = true;
        }

        GUILayout.BeginVertical(GUILayout.Width(MenuUI.windowWidth * 0.425f), GUILayout.ExpandWidth(true));

        DrawGUISettings();

        GUILayout.Space(15);

        DrawSpoofingSettings();

        GUILayout.Space(15);

        DrawPrivacySettings();

        GUILayout.EndVertical();
    }

    private void InitializeInputFields()
    {
        _menuKeybindField = new TextField(KEWLMENU.menuKeybind.Value);
        _menuColorField = new TextField(KEWLMENU.menuHtmlColor.Value);
        _spoofLevelField = new TextField(KEWLMENU.spoofLevel.Value);
        _spoofPlatformField = new TextField(KEWLMENU.spoofPlatform.Value);
        _glassGradientStartField = new TextField(KEWLMENU.glassGradientStart.Value);
        _glassGradientEndField = new TextField(KEWLMENU.glassGradientEnd.Value);

    }


    private void DrawGUISettings()
    {
        GUILayout.Label("GUI Settings", GUIStylePreset.TabSubtitle);

        GUILayout.BeginHorizontal();
        GUILayout.Label("Menu Keybind:", GUILayout.Width(150));
        _menuKeybindField.Draw(150);
        if (GUILayout.Button("Save", GUILayout.Width(100)))
        {
            KEWLMENU.menuKeybind.Value = _menuKeybindField.Content;
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(5);

        GUILayout.BeginHorizontal();
        GUILayout.Label("Menu Color (HTML):", GUILayout.Width(150));
        _menuColorField.Draw(150);
        if (GUILayout.Button("Save", GUILayout.Width(100)))
        {
            KEWLMENU.menuHtmlColor.Value = _menuColorField.Content;
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(5);

        KEWLMENU.menuOpenOnMouse.Value = UIHelpers.Toggle(KEWLMENU.menuOpenOnMouse.Value, " Open Menu on Mouse Position");

        GUILayout.Space(5);

        GUILayout.Label("Menu UI Theme", GUIStylePreset.TabSubtitle);
        for (int i = 0; i < MenuUI.ThemeNames.Length; i++)
        {
            bool selected = string.Equals(MenuUI.CurrentTheme, MenuUI.ThemeNames[i], StringComparison.Ordinal);
            if (GUILayout.Button((selected ? "[Active] " : string.Empty) + MenuUI.ThemeNames[i], GUILayout.Height(28)))
                MenuUI.SetTheme(i);
        }

        KEWLMENU.menuMaterialSmoothScrolling.Value = UIHelpers.Toggle(
            KEWLMENU.menuMaterialSmoothScrolling.Value, " Smooth scrolling (all themes)");
        KEWLMENU.menuMaterialPageAnimations.Value = UIHelpers.Toggle(
            KEWLMENU.menuMaterialPageAnimations.Value, " Page animations (all themes)");

        bool darkerTheme = UIHelpers.Toggle(MenuUI.DarkerTheme, " Darker theme");
        if (darkerTheme != MenuUI.DarkerTheme)
            MenuUI.SetDarkerTheme(darkerTheme);

        if (string.Equals(MenuUI.CurrentTheme, "Glass", StringComparison.Ordinal))
        {
            DrawGlassThemeSettings();
            GUILayout.Label($"Glass transparency: {MenuUI.GlassOpacity:P0}", GUIStylePreset.TabSubtitle);
            MenuUI.GlassOpacity = UIHelpers.HorizontalSlider(MenuUI.GlassOpacity, 0.20f, 1f);
            GUILayout.Label($"Glass tint strength: {MenuUI.GlassTintStrength:P0}", GUIStylePreset.TabSubtitle);
            MenuUI.GlassTintStrength = UIHelpers.HorizontalSlider(MenuUI.GlassTintStrength, 0f, 1f);
        }

        if (string.Equals(MenuUI.CurrentTheme, "Material", StringComparison.Ordinal))
        {
            KEWLMENU.menuMaterialLowPerformance.Value = UIHelpers.Toggle(
                KEWLMENU.menuMaterialLowPerformance.Value, " Low-end performance mode");
            KEWLMENU.menuMaterialVersionBToggles.Value = UIHelpers.Toggle(
                KEWLMENU.menuMaterialVersionBToggles.Value, " Version B Toggles Texture");
            KEWLMENU.menuShowTestTab.Value = UIHelpers.Toggle(
                KEWLMENU.menuShowTestTab.Value, " Show Test tab");

            GUILayout.Label("Material UI Color Presets", GUIStylePreset.TabSubtitle);
            for (int i = 0; i < MenuUI.MaterialColorPresets.Length; i++)
            {
                if (i % 4 == 0)
                    GUILayout.BeginHorizontal();
                if (GUILayout.Button(MenuUI.MaterialColorPresets[i], GUILayout.Height(28)))
                    MenuUI.SetMaterialColorPreset(i);
                if (i % 4 == 3 || i == MenuUI.MaterialColorPresets.Length - 1)
                    GUILayout.EndHorizontal();
            }

            GUILayout.Label($"Window Transparency: {1f - MenuUI.MaterialWindowOpacity:P0}", GUIStylePreset.TabSubtitle);
            MenuUI.MaterialWindowOpacity = UIHelpers.HorizontalSlider(MenuUI.MaterialWindowOpacity, 0.45f, 1f);
        }

        GUILayout.Space(5);

        KEWLMENU.autoLoadProfile.Value = UIHelpers.Toggle(KEWLMENU.autoLoadProfile.Value, " Auto-Load Profile on Startup");
    }

    private void DrawGlassThemeSettings()
    {
        GUILayout.Label("Glass Tint", GUIStylePreset.TabSubtitle);
        for (int i = 0; i < MenuUI.GlassTintThemes.Length; i++)
        {
            if (i % 3 == 0)
                GUILayout.BeginHorizontal();
            bool selected = string.Equals(KEWLMENU.glassTintTheme.Value, MenuUI.GlassTintThemes[i], StringComparison.Ordinal);
            if (GUILayout.Button((selected ? "[Active] " : string.Empty) + MenuUI.GlassTintThemes[i], GUILayout.Height(28)))
                MenuUI.SetGlassTintTheme(i);
            if (i % 3 == 2 || i == MenuUI.GlassTintThemes.Length - 1)
                GUILayout.EndHorizontal();
        }

        if (string.Equals(KEWLMENU.glassTintTheme.Value, "Custom Gradient", StringComparison.Ordinal))
        {
            GUILayout.Label("Custom Gradient Colors", GUIStylePreset.TabSubtitle);
            DrawGlassColorField("Start", _glassGradientStartField, true);
            DrawGlassColorField("End", _glassGradientEndField, false);
        }
    }

    private static void DrawGlassColorField(string label, TextField field, bool start)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label + ":", GUILayout.Width(70));
        field.Draw(130);
        if (GUILayout.Button("Save", GUILayout.Width(60)) &&
            ColorUtility.TryParseHtmlString(field.Content, out _))
        {
            if (start)
                KEWLMENU.glassGradientStart.Value = field.Content;
            else
                KEWLMENU.glassGradientEnd.Value = field.Content;
        }
        GUILayout.EndHorizontal();
    }

    private void DrawSpoofingSettings()
    {
        GUILayout.Label("Spoofing Settings", GUIStylePreset.TabSubtitle);

        GUILayout.BeginHorizontal();
        GUILayout.Label("Spoof Level (1-100001):", GUILayout.Width(150));
        _spoofLevelField.Draw(150);
        if (GUILayout.Button("Save", GUILayout.Width(100)))
        {
            // Validate that it's a number between 1 and 100001
            if (int.TryParse(_spoofLevelField.Content, NumberStyles.Integer, CultureInfo.InvariantCulture, out int level) &&
                level >= 1 && level <= 100001)
            {
                KEWLMENU.spoofLevel.Value = _spoofLevelField.Content;
            }
            else
            {
                _spoofLevelField.Content = KEWLMENU.spoofLevel.Value;
            }
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(5);

        GUILayout.BeginHorizontal();
        GUILayout.Label("Spoof Platform:", GUILayout.Width(150));
        _spoofPlatformField.Draw(150);
        if (GUILayout.Button("Save", GUILayout.Width(100)))
        {
            KEWLMENU.spoofPlatform.Value = _spoofPlatformField.Content;
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(5);

        GUILayout.Label("Supported Platforms: StandaloneEpicPC, StandaloneSteamPC, StandaloneMac, StandaloneWin10, etc.");
    }

    private void DrawPrivacySettings()
    {
        GUILayout.Label("Privacy Settings", GUIStylePreset.TabSubtitle);

        KEWLMENU.spoofDeviceId.Value = UIHelpers.Toggle(KEWLMENU.spoofDeviceId.Value, " Hide Device ID");

        GUILayout.Space(5);

        KEWLMENU.noTelemetry.Value = UIHelpers.Toggle(KEWLMENU.noTelemetry.Value, " Disable Telemetry");

        GUILayout.Space(10);

        if (GUILayout.Button("Open Config File", GUILayout.Width(200)))
        {
            Utils.OpenConfigFile();
        }

        GUILayout.Space(5);

        GUILayout.Label("For more advanced configuration options, click 'Open Config File'", GUIStylePreset.TabSubtitle);
    }
}
