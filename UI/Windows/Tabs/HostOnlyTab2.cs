using System.Collections;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using SocietyMenu.features;
using InnerNet;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;


namespace SocietyMenu;

public class HostOnlyTab2 : ITab
{
    public string name => "Host-Only 2";

    private byte selectedMap = 0;

    public void Draw()
    {
        if (PlayerControl.LocalPlayer == null)
        {
            GUILayout.Label("You are not currently in a game, these options will not work.");
        }
        else if (!AmongUsClient.Instance.AmHost)
        {
            GUILayout.Label("You are not the host of the current lobby. Using these options will either do nothing or get you banned by the anticheat");
        }
        Host.BanMidGame.Enabled = UIHelpers.Toggle(Host.BanMidGame.Enabled, "Be able to ban players mid-game");

        Host.FlippedSkeld = UIHelpers.Toggle(Host.FlippedSkeld, "Use Flipped Skeld Map");

        Host.DisableMeetings.Enabled = UIHelpers.Toggle(Host.DisableMeetings.Enabled, "Disable Meetings");
        Host.DisableSabotages.Enabled = UIHelpers.Toggle(Host.DisableSabotages.Enabled, "Disable Sabotages");
        Host.DisableCloseDoors.Enabled = UIHelpers.Toggle(Host.DisableCloseDoors.Enabled, "Disable Close Doors");
        Host.DisableCameras.Enabled = UIHelpers.Toggle(Host.DisableCameras.Enabled, "Disable Security Cameras");
        Host.DisableGameEnd.Enabled = UIHelpers.Toggle(Host.DisableGameEnd.Enabled, "Disable Game End");
        Host.NoKillCooldown.Enabled = UIHelpers.Toggle(Host.NoKillCooldown.Enabled, "No Kill Cooldown");

        GUILayout.BeginHorizontal();
        Host.BlockLowLevels.Enabled = UIHelpers.Toggle(Host.BlockLowLevels.Enabled, $"Kick players with less than {Host.BlockLowLevels.MinLevel} levels");
        Host.BlockLowLevels.MinLevel = (uint)UIHelpers.HorizontalSlider(Host.BlockLowLevels.MinLevel, 0, 100);
        GUILayout.EndHorizontal();

        KEWLMENU.routines.reportBodySpam.Enabled = UIHelpers.Toggle(KEWLMENU.routines.reportBodySpam.Enabled, "Spam Report Bodies");

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Force Crewmate Victory"))
        {
            // Just incase the user has this enabled
            Host.DisableGameEnd.Enabled = false;

            GameManager.Instance.RpcEndGame(GameOverReason.CrewmatesByTask, false);
            KEWLMENU.notifications.Send("Game Finished", "You ended the game with a crewmate victory.", 5);
        }

        if (GUILayout.Button("Force Imposter Victory"))
        {
            // Just incase the user has this enabled
            Host.DisableGameEnd.Enabled = false;

            GameManager.Instance.RpcEndGame(GameOverReason.ImpostorsByKill, false);
            KEWLMENU.notifications.Send("Game Finished", "You ended the game with an imposter victory.", 5);
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(5);
        GUILayout.Label("Map Spawner/Despawner:");

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Despawn Lobby"))
        {
            if (LobbyBehaviour.Instance != null)
            {
                LobbyBehaviour.Instance.Despawn();
                KEWLMENU.notifications.Send("Lobby Map", "The lobby map has been despawned.", 5);
            }
            else
            {
                KEWLMENU.notifications.Send("Lobby Map", "The lobby map has already been despawned.", 5);
            }
        }

        if (GUILayout.Button("Spawn Lobby"))
        {
            // From GameStartManager::Start
            LobbyBehaviour.Instance = Object.Instantiate<LobbyBehaviour>(GameStartManager.Instance.LobbyPrefab);
            AmongUsClient.Instance.Spawn(LobbyBehaviour.Instance, -2, SpawnFlags.None);

            KEWLMENU.notifications.Send("Lobby Map", "A new instance of the lobby map has been spawned", 5);
        }
        GUILayout.EndHorizontal();

        GUILayout.Label($"Selected map: {(MapNames)selectedMap}");
        selectedMap = (byte)UIHelpers.HorizontalSlider(selectedMap, 0, 5);
        if (MenuUI.IsMaterialLayoutActive)
        {
            Texture2D mapPreview = RadarHandler.LoadMapPreview(selectedMap);
            if (mapPreview != null)
            {
                Rect previewRect = GUILayoutUtility.GetRect(260f, 135f, GUILayout.ExpandWidth(true));
                GUI.Box(previewRect, GUIContent.none, new GUIStyle
                {
                    normal = { background = mapPreview },
                    padding = new RectOffset(),
                    margin = new RectOffset(),
                    border = new RectOffset()
                });
            }
        }

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Despawn Map"))
        {
            if (ShipStatus.Instance != null)
            {
                ShipStatus.Instance.Despawn();
                KEWLMENU.notifications.Send("Game Map", "The current map has been despawned.", 5);
            }
            else
            {
                KEWLMENU.notifications.Send("Game Map", "The game map has already been despawned.", 5);
            }
        }

        if (GUILayout.Button("Spawn Map"))
        {
            AmongUsClient.Instance.StartCoroutine(SpawnMap(selectedMap).WrapToIl2Cpp());
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(5);

        GUILayout.Label("Disco Party:");
        KEWLMENU.routines.discoHost.Enabled = UIHelpers.Toggle(KEWLMENU.routines.discoHost.Enabled, "Enabled");
        GUILayout.Label($"Color randomization delay: {KEWLMENU.routines.discoHost.randomizationDelay:F2}s");
        KEWLMENU.routines.discoHost.randomizationDelay = UIHelpers.HorizontalSlider(KEWLMENU.routines.discoHost.randomizationDelay, 0.1f, 2.0f);
    }
    private static IEnumerator SpawnMap(byte mapId)
    {
        KEWLMENU.Log.LogInfo($"Attempting to spawn in map id {mapId}");

        AsyncOperationHandle<GameObject> asyncHandle = AmongUsClient.Instance.ShipPrefabs[mapId].InstantiateAsync(null, false);
        yield return asyncHandle;

        ShipStatus ship = asyncHandle.Result.GetComponent<ShipStatus>();
        AmongUsClient.Instance.Spawn(ship, -2, SpawnFlags.None);

        KEWLMENU.notifications.Send("Map Spawner", $"{(MapNames)mapId} has been spawned.", 5);
    }
}