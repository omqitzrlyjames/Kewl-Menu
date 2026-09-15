using System.IO;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using UnityEngine.SceneManagement;
using System;
using UnityEngine;
using UnityEngine.Analytics;
using System.Collections.Generic;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using SocietyMenu.features;
using SocietyMenu.routines;
using SocietyMenu.ui;

namespace SocietyMenu;

[BepInAutoPlugin]
[BepInProcess("Among Us.exe")]
public partial class KEWLMENU : BasePlugin
{
    public Harmony Harmony { get; } = new(Id);
    public static KEWLMENU Plugin;
    public new static ManualLogSource Log;
    public static KEWLMENU Instance { get; private set; }
    public static readonly string ProfilePath = Path.Combine(Paths.ConfigPath, "KEWLMENUProfile.txt");

    public static MenuUI menuUI;
    public static ConsoleUI consoleUI;
    public static RolesUI rolesUI;
    public static OverloadUI overloadUI;
    public static DoorsUI doorsUI;
    public static TasksUI tasksUI;
    public static ProtectUI protectUI;
    public static StreamerUI streamerUI;
    public static KeybindListener keybindListener;

    public static string societyVersion = "1.3.0";
    public static List<string> supportedAU = new List<string> { "2026.8.18", "2026.6.5", "2026.3.31" };
    public static List<string> toleratedAU = new List<string> { "2026.2.24", "2026.3.17" };
    public static bool isPanicked = false;
    public static bool inStealthMode = false;
    public static bool overloadFixed = true;

    public static bool isDevRelease = false;

    public static ConfigEntry<string> menuKeybind;
    public static ConfigEntry<string> menuHtmlColor;
    public static ConfigEntry<string> menuChatColor;
    public static ConfigEntry<bool> menuOpenOnMouse;
    public static ConfigEntry<bool> menuKeepSubwindowsOpen;
    public static ConfigEntry<bool> menuAllowClickThrough;
    public static ConfigEntry<bool> menuMaterialLayout;
    public static ConfigEntry<string> menuUiTheme;
    public static ConfigEntry<bool> menuDarkerTheme;
    public static ConfigEntry<string> glassTintTheme;
    public static ConfigEntry<string> glassGradientStart;
    public static ConfigEntry<string> glassGradientEnd;
    public static ConfigEntry<float> glassOpacity;
    public static ConfigEntry<float> glassTintStrength;
    public static ConfigEntry<bool> menuMaterialSmoothScrolling;
    public static ConfigEntry<bool> menuMaterialPageAnimations;
    public static ConfigEntry<bool> menuMaterialLowPerformance;
    public static ConfigEntry<bool> menuMaterialVersionBToggles;
    public static ConfigEntry<bool> menuShowTestTab;
    public static ConfigEntry<bool> menuSettingBoundaries;
    public static ConfigEntry<string> spoofLevel;
    public static ConfigEntry<string> spoofPlatform;
    public static ConfigEntry<bool> spoofDeviceId;
    public static ConfigEntry<bool> noTelemetry;
    public static ConfigEntry<string> guestFriendCode;
    public static ConfigEntry<bool> guestMode;
    public static ConfigEntry<bool> autoLoadProfile;
    public static ConfigEntry<string> configEditor;
    public static ConfigEntry<int> adaptMaxStrength;
    public static ConfigEntry<float> adaptMaxCooldown;
    public static ConfigEntry<float> attackLogDelay;
    public static ConfigEntry<int> defaultStrength;
    public static ConfigEntry<float> defaultCooldown;
    public static ConfigEntry<int> killSwitchLvl;

    public static RoutineManager routines;
    public static NotificationManager notifications;

    public override void Load()
    {
        Instance = this;
        Log = base.Log;
        Log.LogInfo("KEWLMENU by omqItzrlyjames has loaded!");
        Plugin = this;
        notifications = AddComponent<NotificationManager>();
        routines = AddComponent<RoutineManager>();

        // Loads config settings
        menuKeybind = Config.Bind("KEWLMENU.GUI",
                                "Keybind",
                                "Delete",
                                "The keyboard key used to toggle the GUI on and off. List of supported keycodes: https://docs.unity3d.com/Packages/com.unity.tiny@0.16/api/Unity.Tiny.Input.KeyCode.html");

        if (menuKeybind.Value.Equals("K", StringComparison.OrdinalIgnoreCase))
            menuKeybind.Value = "Delete";

        menuHtmlColor = Config.Bind("KEWLMENU.GUI",
                                "Color",
                                "",
                                "A custom color for your KEWLMENU GUI. Supports html color codes");

        menuChatColor = Config.Bind("KEWLMENU.GUI",
                                "ChatColor",
                                "",
                                "A custom HTML color code for your in-game chat messages. Supports html color codes");

        menuOpenOnMouse = Config.Bind("KEWLMENU.GUI",
                                "OpenOnMouse",
                                false,
                                "When enabled, the KEWLMENU GUI will always be opened at the current mouse position");

        menuKeepSubwindowsOpen = Config.Bind("KEWLMENU.GUI",
                                "KeepSubwindowsOpen",
                                false,
                                "When enabled, closing the KEWLMENU GUI will not automatically close its subwindows");

        menuAllowClickThrough = Config.Bind("KEWLMENU.GUI",
                                "AllowClicksThrough",
                                false,
                                "Legacy click-through setting. KEWLMENU blocks clicks inside its windows.");
        menuAllowClickThrough.Value = false;

        menuMaterialLayout = Config.Bind("KEWLMENU.GUI",
                    "MaterialLayout",
                                true,
                    "When enabled, use the Material 3-inspired menu layout");

        menuUiTheme = Config.Bind("KEWLMENU.GUI",
                "UiTheme",
                    "Material",
                "Active menu UI theme. Only one theme can be active at a time.");

        menuDarkerTheme = Config.Bind("KEWLMENU.GUI",
                "DarkerTheme",
                    false,
                "When enabled, use the darker Material theme palette.");

        glassTintTheme = Config.Bind("KEWLMENU.GUI",
                "GlassTintTheme",
                    "Clear",
                "Glass tint or gradient preset.");
        glassGradientStart = Config.Bind("KEWLMENU.GUI",
                "GlassGradientStart",
                    "#8FD3FF",
                "First HTML color used by the Glass custom gradient.");
        glassGradientEnd = Config.Bind("KEWLMENU.GUI",
                "GlassGradientEnd",
                    "#D6F1FF",
                "Second HTML color used by the Glass custom gradient.");
        glassOpacity = Config.Bind("KEWLMENU.GUI",
                "GlassOpacity",
                    0.78f,
                "Transparency of the Glass theme window and panels.");
        glassTintStrength = Config.Bind("KEWLMENU.GUI",
                "GlassTintStrength",
                    0.35f,
                "Strength of the selected Glass tint color.");

        menuSettingBoundaries = Config.Bind("KEWLMENU.GUI",
                "SettingBoundaries",
                    true,
                "When enabled, keep lobby rules within the normal Among Us limits");

            menuMaterialSmoothScrolling = Config.Bind("KEWLMENU.GUI",
                    "MaterialSmoothScrolling",
                        true,
                    "When enabled, smooth scrolling is used in the new UI");

            menuMaterialPageAnimations = Config.Bind("KEWLMENU.GUI",
                    "MaterialPageAnimations",
                        true,
                    "When enabled, page transition animations are used in the new UI");

            menuMaterialLowPerformance = Config.Bind("KEWLMENU.GUI",
                    "MaterialLowPerformance",
                        false,
                    "When enabled, reduce Material UI animation and scrolling work for low-end laptops");

            menuMaterialVersionBToggles = Config.Bind("KEWLMENU.GUI",
                    "MaterialVersionBToggles",
                        true,
                    "When enabled, Material toggles use highlighted buttons instead of switches");

                menuShowTestTab = Config.Bind("KEWLMENU.GUI",
                        "ShowTestTab",
                            false,
                        "When enabled, show the diagnostic Test tab in the new UI");


        autoLoadProfile = Config.Bind("SocietyMenu.Profile",
                                "AutoLoadProfile",
                                false,
                                "When enabled, your saved keybind and toggle profile will be automatically loaded at game startup");

        configEditor = Config.Bind("SocietyMenu.Config",
                                "ConfigEditor",
                                "notepad.exe",
                                "The program used to open the config file when using the Open Config toggle. Can be any executable, but using a text editor is recommended");

        // GuestMode config settings are commented out as the cheats are broken in latest updates

        // guestMode = Config.Bind("SocietyMenu.GuestMode",
        //                         "GuestMode",
        //                         false,
        //                         "When enabled, a new guest account will generate every time you start the game, allowing you to bypass account bans and PUID detection");

        // guestFriendCode = Config.Bind("SocietyMenu.GuestMode",
        //                         "FriendName",
        //                         "",
        //                         "The username that will be used when setting a friend code for your guest account. IMPORTANT: Can only be used with GuestMode, needs to be ≤ 10 characters, and cannot include special characters/discriminator (#1234)");

        spoofLevel = Config.Bind("SocietyMenu.Spoofing",
                                "Level",
                                "",
                                "A custom player level to display to others in online games to hide your actual platform. IMPORTANT: Custom levels can only be within 1 and 100001. Decimal numbers will not work");

        spoofPlatform = Config.Bind("SocietyMenu.Spoofing",
                                "Platform",
                                "",
                                "A custom gaming platform to display to others in online lobbies to hide your actual platform. List of supported platforms: https://skeld.js.org/enums/_skeldjs_constant.Platform.html");

        spoofDeviceId = Config.Bind("SocietyMenu.Privacy",
                                "HideDeviceId",
                                true,
                                "When enabled, it will hide your unique deviceId from Among Us, which could potentially help bypass hardware bans in the future");

        noTelemetry = Config.Bind("SocietyMenu.Privacy",
                                "NoTelemetry",
                                true,
                                "When enabled, it will stop Among Us from collecting analytics of your games and sending them to Innersloth using Unity Analytics");

        // Enabled by default
        CheatToggles.antiOverload = false;
        CheatToggles.unlockFeatures = true;
        CheatToggles.freeCosmetics = true;
        CheatToggles.avoidPenalties = true;

        // Enabled by default
        CheatToggles.olAutoAdapt = true;
        CheatToggles.olKillSwitch = true;
        CheatToggles.olAutoStop = true;
        CheatToggles.olAutoClear = true;
        CheatToggles.olLogStartStop = true;
        CheatToggles.olLogAttack = true;
        CheatToggles.olLogAddRemove = true;
        CheatToggles.olLogDisconnect = true;

        try
        {
            Nocturne.NocturnePlugin.Initialize(this);
        }
        catch (Exception exception)
        {
            Log.LogError($"Nocturne integration failed to initialize: {exception}");
        }

        Harmony.PatchAll();

        // UI
        menuUI = AddComponent<MenuUI>();
        AddComponent<RadarHandler>();
        consoleUI = AddComponent<ConsoleUI>();
        doorsUI = AddComponent<DoorsUI>();
        tasksUI = AddComponent<TasksUI>();
        protectUI = AddComponent<ProtectUI>();
        streamerUI = AddComponent<StreamerUI>();
        // rolesUI = AddComponent<RolesUI>();

        // Components
        keybindListener = AddComponent<KeybindListener>();

        // Disables Telemetry (haven't fully tested if it works, but according to Unity docs it should)
        if (noTelemetry.Value)
        {
            Analytics.enabled = false;
            Analytics.deviceStatsEnabled = false;
            PerformanceReporting.enabled = false;
        }

        // Create profile file if it is missing
        if (!File.Exists(ProfilePath))
        {
            ProfileStore.EnsureLegacyProfile();
        }

        // Auto load profile on start if needed
        if (autoLoadProfile.Value)
        {
            ProfileStore.LoadLegacy(out _);
        }

        SceneManager.add_sceneLoaded((Action<Scene, LoadSceneMode>) ((scene, _) =>
        {
            if (scene.name == "MainMenu" && !(inStealthMode || isPanicked))
            {
                // Warns about unsupported AU versions
                if (!supportedAU.Contains(Application.version) && !toleratedAU.Contains(Application.version))
                {
                    Utils.ShowNewPopup("This version of KEWLMENU by omqItzrlyjames and this version of Among Us are incompatible\n\nInstall the right version to avoid problems");
                } 
                else if (!supportedAU.Contains(Application.version) && toleratedAU.Contains(Application.version))
                {
                    Utils.ShowNewPopup("This version of KEWLMENU by omqItzrlyjames and this version of Among Us are not fully compatible\n\nSome features may not work properly with this Among Us version.");
                }
            }
        }));
    }
}