using HarmonyLib;

namespace ProximityChat
{
    /// <summary>
    /// Harmony patches - מיירטים אירועים מהמשחק
    /// </summary>

    // כשהלובי נטען - הצטרף לחדר
    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameJoined))]
    public static class Patch_OnGameJoined
    {
        public static void Postfix(AmongUsClient __instance)
        {
            Plugin.Log.LogInfo("Game joined - connecting to proximity server...");

            var roomCode = __instance.GameId.ToString();
            var playerName = PlayerControl.LocalPlayer?.Data?.PlayerName ?? "Unknown";

            ProximityChatManager.Instance?.JoinRoom(roomCode, playerName);
        }
    }

    // כשמתחילה פגישה (meeting) - כולם שומעים את כולם
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
    public static class Patch_MeetingStart
    {
        public static void Postfix()
        {
            Plugin.Log.LogInfo("Meeting started");
            ProximityChatManager.Instance?.SendJson(new { type = "meeting_start" });
        }
    }

    // כשהפגישה נגמרת
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Close))]
    public static class Patch_MeetingEnd
    {
        public static void Postfix()
        {
            Plugin.Log.LogInfo("Meeting ended");
            ProximityChatManager.Instance?.SendJson(new { type = "meeting_end" });
        }
    }

    // כשהשחקן מת
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Die))]
    public static class Patch_PlayerDie
    {
        public static void Postfix(PlayerControl __instance)
        {
            if (__instance.AmOwner)
            {
                Plugin.Log.LogInfo("Local player died");
                ProximityChatManager.Instance?.NotifyDead();
            }
        }
    }
}
