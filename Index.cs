// Nothing fun here, go to Patches.cs for some great content
using BepInEx;
using HarmonyLib;

namespace JetpackFallFix {
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class JetpackFallFix : BaseUnityPlugin {
        public static Harmony _harmony;
        private void Awake() {
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");

            _harmony = new Harmony(PluginInfo.PLUGIN_GUID);
            _harmony.PatchAll(typeof(Patches));
        }
    }
}