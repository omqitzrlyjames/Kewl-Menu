using HarmonyLib;
using InnerNet;

namespace SocietyMenu.features
{
	internal class PlayerLogger
	{
		[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Start))]
		class OnJoin
		{
			static void Postfix(PlayerControl __instance)
			{
				if(__instance == PlayerControl.LocalPlayer || AmongUsClient.Instance.NetworkMode == NetworkModes.FreePlay) return;

				ClientData clientData = AmongUsClient.Instance.GetClientFromCharacter(__instance);
				if(clientData == null) return;

				PlatformSpecificData platformData = clientData.PlatformData;

				KEWLMENU.Log.LogMessage($"[PlayerLogger] {clientData.PlayerName} ({__instance.NetId}) joined on {platformData.Platform}. friendcode {clientData.FriendCode}, puid {clientData.ProductUserId}");

			}
		}
	}
}