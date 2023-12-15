using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;

namespace JetpackFallFix {
    class Patches {
        static float timeSinceRoundStarted = 0;

        [HarmonyPatch(typeof(StartOfRound), "Update")]
        [HarmonyPostfix]
        static void StartOfRound_Update_Postfix(ref StartOfRound __instance) {
            // I don't know a better way to get timeSinceRoundStarted
            timeSinceRoundStarted = __instance.timeSinceRoundStarted;
        }

        [HarmonyPatch(typeof(PlayerControllerB), "DamagePlayer")]
        [HarmonyPrefix]
        static bool PlayerControllerB_DamagePlayer_Prefix(ref PlayerControllerB __instance, ref CauseOfDeath __3) {
            var myLogSource = BepInEx.Logging.Logger.CreateLogSource("JetpackFallFix");
            if(__instance.jetpackControls && __3 == CauseOfDeath.Gravity) {
                // Fix check for collision damage in air by adding originally missing argument QueryTriggerInteraction.Ignore
                if(!Physics.CheckSphere(__instance.gameplayCamera.transform.position, 3f, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore)){
                    myLogSource.LogInfo($"Fix air damage");
                    return false;
                }
                // Fix bug where you take damage when starting the jetpack in the ship while it is landing
                if(timeSinceRoundStarted < 9){
                    Collider[] hitColliders = Physics.OverlapSphere(__instance.gameplayCamera.transform.position, 3f, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore);
                    foreach (var hitCollider in hitColliders)
                    {
                        switch(hitCollider.name){
                            case "ShipInside":
                            case "ShipRails":
                            case "WallInsulator":
                            case "HangarDoorRight":
                                myLogSource.LogInfo($"Fix start damage");
                                return false;
                        }
                    }
                }
            }
            return true;
        }

        [HarmonyPatch(typeof(PlayerControllerB), "PlayerHitGroundEffects")]
        [HarmonyPrefix]
        static void PlayerControllerB_PlayerHitGroundEffects_Prefix(ref PlayerControllerB __instance) {
            var myLogSource = BepInEx.Logging.Logger.CreateLogSource("JetpackFallFix");
            // New logic for jetpack falldamage that works more reliably. Without jetpack, this is essentially the same as original
            // This is somewhat required to fix a bug where previous fall speed caused player to take damage when landing
            if((__instance.jetpackControls || __instance.disablingJetpackControls) && __instance.fallValueUncapped >= -40){
                if(__instance.thisController.velocity.y < -15){
                    myLogSource.LogInfo($"Jetpack fall damage, velocity.y: {__instance.thisController.velocity.y}, fallValueUncapped: {__instance.fallValueUncapped}");
                    __instance.DamagePlayer(__instance.thisController.velocity.y < -50f ? 100 : 40, hasDamageSFX: true, callRPC: true, CauseOfDeath.Gravity);
                }
            }
            else if(__instance.takingFallDamage && !__instance.isSpeedCheating){
                myLogSource.LogInfo($"Basic fall damage, velocity.y: {__instance.thisController.velocity.y}, fallValueUncapped: {__instance.fallValueUncapped}");
                __instance.DamagePlayer(__instance.fallValueUncapped < -50f ? 100 : 40, hasDamageSFX: true, callRPC: true, CauseOfDeath.Gravity);
            }
            else{
                myLogSource.LogInfo($"No fall damage, velocity.y: {__instance.thisController.velocity.y}, fallValueUncapped: {__instance.fallValueUncapped}");
            }
            // Reset averageVelocity to fix immediately taking damage when launching from ground
            __instance.averageVelocity = 0f;
            // Prevent orignal fall damage logic from running
            __instance.takingFallDamage = false;
            return;
        }
    }
}