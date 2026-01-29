using BepInEx;
using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using RoR2.Skills;
using System;
using IL.RoR2.CharacterAI;
using System.Collections;
using UnityEngine.Rendering;
using RoR2BepInExPack;
using R2API.ContentManagement;
using UnityEngine.Bindings;
using BepInEx.Configuration;
using RoR2BepInExPack.GameAssetPaths;

namespace RailgunnerTurret
{
    [BepInDependency(LanguageAPI.PluginGUID)]
    [BepInDependency(PrefabAPI.PluginGUID)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class RailgunnerTurret : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "bouncyshield";
        public const string PluginName = "RailgunnerTurret";
        public const string PluginVersion = "1.0.0";

        public void Awake()
        {
            Log.Init(Logger);

            Configs();

            Hooks();

            Assets.Init();

            new ContentPacks().Init(); // has to be last
        }

        private void Hooks()
        {
            On.RoR2.CharacterAI.BaseAI.UpdateTargets += TurretUpdateTargets;
        }

        internal static ConfigEntry<float> damageCoefficient;
        internal static ConfigEntry<float> critDamageMultiplier;
        internal static ConfigEntry<float> procCoefficient;
        internal static ConfigEntry<float> selfKnockbackForce;
        internal static ConfigEntry<bool> postProcessingEffects;
        internal static ConfigEntry<float> baseRechargeInterval;
        internal static ConfigEntry<float> maxDistance;
        internal static ConfigEntry<float> moveSpeedMulitplier;
        internal static ConfigEntry<float> distanceToStartSprinting;

        private void Configs()
        {
            damageCoefficient = Config.Bind("Turret", "damageCoefficient", 30f,"e.g. 30 gives 3000% damage");
            critDamageMultiplier = Config.Bind("Turret", "critDamageMultiplier", 1.5f);
            procCoefficient = Config.Bind("Turret", "procCoefficient", 1.5f);
            selfKnockbackForce = Config.Bind("Turret", "selfKnockbackForce", 10000f,"the recoil");
            postProcessingEffects = Config.Bind("Turret", "postProcessingEffects", false,"the red glow on each shot, can be annoying");
            baseRechargeInterval = Config.Bind("Turret", "baseRechargeInterval", 21f,"the cooldown");
            maxDistance = Config.Bind("Turret", "maxDistance", 9999f,"the farthest distance the turret will lock onto a target from");
            moveSpeedMulitplier = Config.Bind("Turret", "moveSpeedMulitplier", 0.6f, "this multiplies carbonizer's base speed");
            distanceToStartSprinting = Config.Bind("Turret", "distanceToStartSprinting", 30f,"the distance from engi that the turret will start sprinting");
        }

        private void TurretUpdateTargets(On.RoR2.CharacterAI.BaseAI.orig_UpdateTargets orig, RoR2.CharacterAI.BaseAI self)
        {
            orig(self);

            var body = self.body;
            if (self.name.Contains("RailgunnerTurret"))
            {
                // null checks
                var inputBank = body.inputBank;
                var teamComponent = body.teamComponent;
                if (body == null|| inputBank == null || teamComponent == null)
                {
                    return;
                }

                BullseyeSearch search = new BullseyeSearch()
                {
                    searchOrigin = body.corePosition,
                    searchDirection = inputBank.aimDirection,
                    teamMaskFilter = TeamMask.GetEnemyTeams(teamComponent.teamIndex),
                    maxDistanceFilter = 9999f,
                    filterByLoS = true,
                    sortMode = BullseyeSearch.SortMode.None
                };
                search.RefreshCandidates();

                HurtBox best = null;
                float highestScore = float.NegativeInfinity;
                foreach (HurtBox guy in search.GetResults())
                {
                    float score = guy.healthComponent.combinedHealth;
                    if (score > highestScore)
                    {
                        highestScore = score;
                        best = guy;
                    }
                }

                if (best) // sometimes. it's not looking at any enemies at all. account for this!
                {
                    self.currentEnemy.bestHurtBox = best;
                }
            }
        }
    }
}




