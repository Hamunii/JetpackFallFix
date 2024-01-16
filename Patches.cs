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

        [HarmonyPatch(typeof(PlayerControllerB), "Awake")]
        [HarmonyPostfix]
        static void PlayerControllerB_Awake_Postfix(ref PlayerControllerB __instance) {
            // velocityAverageCount never gets reset, it only goes up when jetpack is used.
            // If this variable is greater than velocityMovingAverageLength, which is always 20,
            // averageVelocity will be higher when the jetpack is first used, which in turn means
            // you will take immediate fall damage when moving fast enough and activating the jetpack.
            __instance.velocityAverageCount = 21;
        }

        [HarmonyPatch(typeof(PlayerControllerB), "DamagePlayer")]
        [HarmonyPrefix]
        static bool PlayerControllerB_DamagePlayer_Prefix(ref PlayerControllerB __instance, ref CauseOfDeath __3) {
            //var myLogSource = BepInEx.Logging.Logger.CreateLogSource("JetpackFallFix");
            if(__instance.jetpackControls && __3 == CauseOfDeath.Gravity) {
                // Fix check for collision damage in air by adding originally missing argument QueryTriggerInteraction.Ignore
                if(!Physics.CheckSphere(__instance.gameplayCamera.transform.position, 3f, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore)){
                    //myLogSource.LogInfo($"Fix air damage");
                    return false;
                }
                // Fix bug where you take damage when starting the jetpack in the ship while it is landing.
                // Might already be fixed with setting velocityAverageCount to over 20, but  I'm keeping this just in case.
                if(timeSinceRoundStarted < 10){
                    Collider[] hitColliders = Physics.OverlapSphere(__instance.gameplayCamera.transform.position, 3f, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore);
                    foreach (var hitCollider in hitColliders)
                    {
                        switch(hitCollider.name){
                            case "ShipInside":
                            case "ShipRails":
                            case "WallInsulator":
                            case "HangarDoorRight":
                                //myLogSource.LogInfo($"Fix start damage");
                                return false;
                        }
                    }
                }
            }
            //myLogSource.LogInfo($"Took Damage, (vel): {__instance.thisController.velocity.magnitude}");
            return true;
        }

        [HarmonyPatch(typeof(PlayerControllerB), "PlayerHitGroundEffects")]
        [HarmonyPrefix]
        static void PlayerControllerB_PlayerHitGroundEffects_Prefix(ref PlayerControllerB __instance) {
            //var myLogSource = BepInEx.Logging.Logger.CreateLogSource("JetpackFallFix");
            // New logic for jetpack falldamage that works more reliably. Without jetpack, this is essentially the same as original
            // This is somewhat required to fix a bug where previous fall speed caused player to take damage when landing
            if((__instance.jetpackControls || __instance.disablingJetpackControls) && __instance.fallValueUncapped >= -40){
                if(__instance.thisController.velocity.y < -15){
                    //myLogSource.LogInfo($"Jetpack fall damage, velocity.y: {__instance.thisController.velocity.y}, fallValueUncapped: {__instance.fallValueUncapped}");
                    if (__instance.thisController.velocity.y < -45f) {
                        __instance.DamagePlayer(__instance.thisController.velocity.y < -48.5f ? 100 : 80, hasDamageSFX: true, callRPC: true, CauseOfDeath.Gravity);
                    }
                    else {
                        __instance.DamagePlayer(__instance.thisController.velocity.y < -40f ? 50 : 30, hasDamageSFX: true, callRPC: true, CauseOfDeath.Gravity);
                    }
                }
            }
            else if(__instance.takingFallDamage && !__instance.isSpeedCheating){
                //myLogSource.LogInfo($"Basic fall damage, velocity.y: {__instance.thisController.velocity.y}, fallValueUncapped: {__instance.fallValueUncapped}");
                if (__instance.fallValueUncapped < -45f) {
                    __instance.DamagePlayer(__instance.fallValueUncapped < -48.5f ? 100 : 80, hasDamageSFX: true, callRPC: true, CauseOfDeath.Gravity);
                }
                else {
                    __instance.DamagePlayer(__instance.fallValueUncapped < -40f ? 50 : 30, hasDamageSFX: true, callRPC: true, CauseOfDeath.Gravity);
                }
            }
            else{
                //myLogSource.LogInfo($"Touch ground, velocity.y: {__instance.thisController.velocity.y}, fallValueUncapped: {__instance.fallValueUncapped}");
            }
            // Reset averageVelocity to fix immediately taking damage when launching from ground
            __instance.averageVelocity = 0f;
            // Prevent orignal fall damage logic from running
            __instance.takingFallDamage = false;
            return;
        }
    }
}