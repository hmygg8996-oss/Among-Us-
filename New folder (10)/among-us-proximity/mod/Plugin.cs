using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using UnityEngine;

namespace ProximityChat
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BasePlugin
    {
        public static ManualLogSource Log = null!;
        public static Plugin Instance = null!;
        internal static Harmony Harmony = new(MyPluginInfo.PLUGIN_GUID);

        public override void Load()
        {
            Instance = this;
            Log = base.Log;

            Log.LogInfo("ProximityChat mod loaded!");

            // הוסף את ה-component לסצנה
            AddComponent<ProximityChatManager>();

            Harmony.PatchAll();
        }
    }
}
