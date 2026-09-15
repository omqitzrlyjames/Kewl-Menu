using HarmonyLib;

namespace SocietyMenu;

[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.FixedUpdate))]
public static class ShipStatus_FixedUpdate
{
    public static void Postfix(ShipStatus __instance)
    {
        SocietyMenuSabotageCheats.Process(__instance);
        SocietyMenuCheats.OpenSabotageMapCheat();

        SocietyMenuCheats.CloseMeetingCheat();
        SocietyMenuCheats.SkipMeetingCheat();
        SocietyMenuCheats.CallMeetingCheat();
        SocietyMenuCheats.WalkInVentCheat();
        SocietyMenuCheats.KickVentsCheat();

        SocietyMenuCheats.DoAnyTaskCheat();

        SocietyMenuPPMCheats.ReportBodyPPM();
    }
}

[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.FixedUpdate))]
public static class FungleShipStatus_FixedUpdate
{
    public static void Postfix(FungleShipStatus __instance)
    {
        SocietyMenuSabotageCheats.ProcessFungle(__instance);
    }
}
