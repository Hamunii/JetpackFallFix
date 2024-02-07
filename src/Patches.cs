using System;
using System.Linq;
using UnityEngine;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using GameNetcodeStuff;

namespace JetpackFallFix {
    class Patches {
        static void LogIfDebugBuild(string text){
            #if DEBUG
            Plugin.Logger.LogInfo(text);
            #endif
        }

        internal static void Init(){
            On.GameNetcodeStuff.PlayerControllerB.Awake += PlayerControllerB_Awake;
            IL.GameNetcodeStuff.PlayerControllerB.Update += PlayerControllerB_Update;
            IL.GameNetcodeStuff.PlayerControllerB.PlayerHitGroundEffects += PlayerControllerB_PlayerHitGroundEffects;
            On.GameNetcodeStuff.PlayerControllerB.PlayerHitGroundEffects += On_PlayerControllerB_PlayerHitGroundEffects;
        }

        private static void PlayerControllerB_Awake(On.GameNetcodeStuff.PlayerControllerB.orig_Awake orig, PlayerControllerB self)
        {   
            orig(self);
            // velocityAverageCount never gets reset, it only goes up when jetpack is used.
            // If this variable is greater than velocityMovingAverageLength, which is always 20,
            // averageVelocity will be higher when the jetpack is first used, which in turn means
            // you will take immediate fall damage when moving fast enough and activating the jetpack.
            self.velocityAverageCount = 21;
        }

        private static void PlayerControllerB_Update(ILContext il)
        {
            /*
            // Find:

            if (Physics.CheckSphere(this.gameplayCamera.transform.position, 3f, StartOfRound.Instance.collidersAndRoomMaskAndDefault))
            
            // And add QueryTriggerInteraction.Ignore at the end of CheckSphere().
            // This prevents us from colliding with trigger colliders when flying with the jetpack.
            */

            ILCursor c = new(il);
            c.GotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<PlayerControllerB>("gameplayCamera"),
                x => x.MatchCallvirt<Component>("get_transform"),
                x => x.MatchCallvirt<Transform>("get_position"),
                x => x.MatchLdcR4(3),
                x => x.MatchCall<StartOfRound>("get_Instance"),
                x => x.MatchLdfld<StartOfRound>("collidersAndRoomMaskAndDefault"),
                x => x.MatchCall<Physics>("CheckSphere")
            );
            c.Index += 7;
            c.Remove();
            c.Emit(OpCodes.Ldc_I4_1);   // push QueryTriggerInteraction.Ignore to stack
            c.Emit(                     // And call CheckSphere with QueryTriggerInteraction
                OpCodes.Call, typeof(Physics)
                .GetMethods()
                .Where(x => x.Name == "CheckSphere")
                .FirstOrDefault()
            );
            LogIfDebugBuild(il.ToString());
        }

        private static void On_PlayerControllerB_PlayerHitGroundEffects(On.GameNetcodeStuff.PlayerControllerB.orig_PlayerHitGroundEffects orig, PlayerControllerB self)
        {
            orig(self);
            // We should reset averageVelocity when hitting ground.
            self.averageVelocity = 0;
        }

        private static void PlayerControllerB_PlayerHitGroundEffects(ILContext il)
        {
            /*
            // Find:

            if (this.takingFallDamage
                && !this.jetpackControls
                && !this.disablingJetpackControls
                && !this.isSpeedCheating
            )

            // And transform it to:

            if (this.takingFallDamage
                && [our Delegate method]
                && !this.isSpeedCheating
            )
            */

            ILCursor c = new(il);

            c.GotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<PlayerControllerB>("takingFallDamage"),
                x => x.MatchBrfalse(out _),
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<PlayerControllerB>("jetpackControls"),
                x => x.MatchBrtrue(out _),
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<PlayerControllerB>("disablingJetpackControls"),
                x => x.MatchBrtrue(out _),
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<PlayerControllerB>("isSpeedCheating"),
                x => x.MatchBrtrue(out _)
            );
            c.Index += 4;

            // remove check for !jetpackControls && !disablingJetpackControls
            c.RemoveRange(4); 
            // above checks get replaced with this method
            c.EmitDelegate<Func<PlayerControllerB, bool>>((self) =>
            {
                if(!(self.jetpackControls || self.disablingJetpackControls) || self.thisController.velocity.y < -15f){
                    LogIfDebugBuild("Take fall damage!");
                    // fallValueUncapped gets set to -8f when jetpack is used, so we do this.
                    self.fallValueUncapped = self.thisController.velocity.y;
                    // Also, fallValueUncapped gets reset back to -7 when we are on ground, in Update().
                    return false;
                }
                LogIfDebugBuild("Prevented fall damage bug!");
                // Not calling DamagePlayer() does not run this, so we do it here.
                self.takingFallDamage = false;
                return true;
            });
        }
    }
}