using System;
using HarmonyLib;
using UnityEngine;

namespace SocietyMenu;

[HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.LateUpdate))]
public static class PlayerPhysics_LateUpdate
{
    public static void Postfix(PlayerPhysics __instance)
    {
        SocietyMenuESP.PlayerNametags(__instance);
        SocietyMenuESP.SeeGhostsCheat(__instance);

        SocietyMenuCheats.NoClipCheat();
        SocietyMenuCheats.ProtectCheat();
        SocietyMenuCheats.KillAllCheat();
        SocietyMenuCheats.KillAllCrewCheat();
        SocietyMenuCheats.KillAllImpsCheat();
        SocietyMenuCheats.ForceStartGameCheat();
        SocietyMenuCheats.FakeStartCounterCheat();
        SocietyMenuCheats.TeleportCursorCheat();
        SocietyMenuCheats.CompleteMyTasksCheat();
        SocietyMenuCheats.CompleteAllTasksCheat();
        SocietyMenuCheats.PlayAnimationCheat();
        SocietyMenuCheats.PlayScannerCheat();

        SocietyMenuPPMCheats.EjectPlayerPPM();
        SocietyMenuPPMCheats.SpectatePPM();
        SocietyMenuPPMCheats.KillPlayerPPM();
        SocietyMenuPPMCheats.TelekillPlayerPPM();
        SocietyMenuPPMCheats.TeleportPlayerPPM();
        SocietyMenuPPMCheats.SetFakeRolePPM();
        SocietyMenuPPMCheats.SetFakeAlivePPM();
        // SocietyMenuPPMCheats.ForceRolePPM();

        // This check ensures there is only one run per frame
        // so that OverloadHandler._timer progression remains accurate
        // if (__instance.AmOwner)
        // {
        //     OverloadHandler.Run();
        // }

        TracersHandler.DrawPlayerTracer(__instance);

        GameObject[] bodyObjects = GameObject.FindGameObjectsWithTag("DeadBody");
        foreach (GameObject bodyObject in bodyObjects) // Finds and loops through all dead bodies
        {
            DeadBody deadBody = bodyObject.GetComponent<DeadBody>();
            if (!deadBody) continue;

            TracersHandler.DrawBodyTracer(deadBody);

            if (CheatToggles.autoReportBodies)
            {
                if (deadBody.Reported) continue;

                deadBody.Reported = true;

                PlayerControl.LocalPlayer.CmdReportDeadBody(GameData.Instance.GetPlayerById(deadBody.ParentId));
            }
        }

        try
        {
            if (CheatToggles.invertControls)
            {
                PlayerControl.LocalPlayer.MyPhysics.Speed = -Mathf.Abs(PlayerControl.LocalPlayer.MyPhysics.Speed);
                PlayerControl.LocalPlayer.MyPhysics.GhostSpeed = -Mathf.Abs(PlayerControl.LocalPlayer.MyPhysics.GhostSpeed);
            }
            else
            {
                PlayerControl.LocalPlayer.MyPhysics.Speed = Mathf.Abs(PlayerControl.LocalPlayer.MyPhysics.Speed);
                PlayerControl.LocalPlayer.MyPhysics.GhostSpeed = Mathf.Abs(PlayerControl.LocalPlayer.MyPhysics.GhostSpeed);
            }
        } catch (NullReferenceException) { }
    }
}

[HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.HandleAnimation))]
public static class PlayerPhysics_HandleAnimation
{
    // Prefix patch of PlayerPhysics.HandleAnimation to disable walking animation
    public static bool Prefix(PlayerPhysics __instance)
    {
        if (CheatToggles.moonWalk && __instance.AmOwner)
        {
            __instance.ResetAnimState();

            return false;
        }

        return true;
    }
}
