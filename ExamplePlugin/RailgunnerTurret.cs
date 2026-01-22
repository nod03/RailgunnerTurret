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

            Hooks();

            Assets.Init();

            new ContentPacks().Init(); // has to be last
        }

        private void Hooks()
        {
            On.RoR2.CharacterAI.BaseAI.UpdateTargets += TurretUpdateTargets;
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




