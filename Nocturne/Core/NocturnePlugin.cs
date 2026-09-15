using System.Collections.Generic;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.CrashReportHandler;

namespace Nocturne;

internal static class NocturnePlugin
{
    public const string PluginId = "nocturne.mod";
    public const string PluginName = "Nocturne";
    public const string PluginVersion = "1.1.9";

    internal static ManualLogSource Logger { get; private set; }
    internal static NocturneMenu Menu { get; private set; }
    private static readonly List<Behaviour> _components = new List<Behaviour>();

    internal static void Initialize(SocietyMenu.KEWLMENU plugin)
    {
        NocturneDependencies.Setup();
        Logger = SocietyMenu.KEWLMENU.Log;
        NocturneDependencies.FlushLog();
        NocturneConfig.Bind(plugin.Config);

        ApplyTelemetryPreference();
        Patches.NocturneBanWords.Init();

        Menu = AddTracked(plugin, typeof(NocturneMenu)) as NocturneMenu;
        AddTracked(plugin, typeof(NocturneHud));
        AddTracked(plugin, typeof(NocturneToast));
        AddTracked(plugin, typeof(NocturneMenuButton));
        AddTracked(plugin, typeof(NocturneAutoLobbyReturn));
        AddTracked(plugin, typeof(NocturneJoinDetector));
        AddTracked(plugin, typeof(NocturneJoinLogger));
        AddTracked(plugin, typeof(NocturneHistoryTracker));
        AddTracked(plugin, typeof(NocturneTracers));
        AddTracked(plugin, typeof(NocturneOverheadChat));
        AddTracked(plugin, typeof(NocturneRadar));
        AddTracked(plugin, typeof(NocturneMouseTools));
        AddTracked(plugin, typeof(NocturneAutoVent));
        AddTracked(plugin, typeof(NocturneColoredName));
        AddTracked(plugin, typeof(NocturneColorSnipe));
        AddTracked(plugin, typeof(NocturneOutfitApplier));
        AddTracked(plugin, typeof(Patches.NocturneMainArtDriver));
        AddTracked(plugin, typeof(Patches.NocturneSpoofDriver));
        AddTracked(plugin, typeof(Patches.NocturneAutoHost));
        AddTracked(plugin, typeof(NocturneLobbySettings));
        AddTracked(plugin, typeof(NocturneDummies));
        AddTracked(plugin, typeof(Patches.NocturneAccessGuard));
        AddTracked(plugin, typeof(NocturneLobbyPranks));
        AddTracked(plugin, typeof(NocturneGlichRooms));
        AddTracked(plugin, typeof(NocturneVotekick));
        AddTracked(plugin, typeof(NocturneAntiVotekick));
        AddTracked(plugin, typeof(NocturneSabotage));
        AddTracked(plugin, typeof(NocturneGodMode));
        AddTracked(plugin, typeof(NocturneRoleBuffs));
        AddTracked(plugin, typeof(NocturneSpeed));
        AddTracked(plugin, typeof(NocturneChatSender));
        AddTracked(plugin, typeof(NocturneRadial));
        AddTracked(plugin, typeof(NocturneEventNotify));
        AddTracked(plugin, typeof(NocturneEventLog));
        AddTracked(plugin, typeof(NocturneReplay));
        AddTracked(plugin, typeof(NocturneChatWindow));
        AddTracked(plugin, typeof(NocturneFakeTasks));
        AddTracked(plugin, typeof(NocturneAutoTasks));
        AddTracked(plugin, typeof(NocturneTwins));
        AddTracked(plugin, typeof(NocturneUpdateCheck));

        NocturneGate.Init();
        Logger.LogInfo($"{PluginName} {PluginVersion} loaded.");
    }

    internal static void Disable()
    {
        foreach (Behaviour b in _components)
        {
            try
            {
                if (b != null)
                    b.enabled = false;
            }
            catch { }
        }
    }

    private static Behaviour AddTracked(SocietyMenu.KEWLMENU plugin, System.Type type)
    {
        var addComponent = typeof(SocietyMenu.KEWLMENU).GetMethod("AddComponent", System.Type.EmptyTypes);
        if (addComponent == null)
            return null;

        var generic = addComponent.MakeGenericMethod(type);
        var component = generic.Invoke(plugin, null) as Behaviour;
        if (component != null)
            _components.Add(component);
        return component;
    }

    private static void ApplyTelemetryPreference()
    {
        if (!NocturneConfig.BlockTelemetry.Value || Application.platform == RuntimePlatform.Android)
        {
            return;
        }

        Analytics.enabled = false;
        Analytics.deviceStatsEnabled = false;
        Analytics.initializeOnStartup = false;
        Analytics.limitUserTracking = true;
        PerformanceReporting.enabled = false;
        CrashReportHandler.enableCaptureExceptions = false;
    }
}
