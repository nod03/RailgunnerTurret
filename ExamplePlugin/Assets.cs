using BepInEx;
using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using RoR2.Skills;
using System;
using RoR2.CharacterAI;
using System.Collections;
using UnityEngine.Rendering;
using RoR2BepInExPack;
using R2API.ContentManagement;
using RoR2BepInExPack.GameAssetPaths;
using System.Linq;
using System.Diagnostics.Tracing;

namespace RailgunnerTurret
{
    public class Assets
    {
        public static GameObject railgunnerTurretPrefab;
        public static GameObject railgunnerTurretMasterPrefab;

        public static GameObject w = Addressables.LoadAssetAsync<GameObject>(RoR2_Base_Engi.EngiTurretWristDisplay_prefab).WaitForCompletion();
        public static GameObject b = Addressables.LoadAssetAsync<GameObject>(RoR2_Base_Engi.EngiWalkerTurretBlueprints_prefab).WaitForCompletion();

        public static void Init()
        {
            CreateRailgunnerTurretPrefab();
            CreateRailgunnerTurretSkill();
        }

        public static void CreateRailgunnerTurretPrefab()
        {
            railgunnerTurretPrefab = PrefabAPI.InstantiateClone(Addressables.LoadAssetAsync<GameObject>(RoR2_Base_Engi.EngiWalkerTurretBody_prefab).WaitForCompletion(),"RailgunnerTurretBody");
            ContentPacks.bodyPrefabs.Add(railgunnerTurretPrefab);
            
            railgunnerTurretMasterPrefab = PrefabAPI.InstantiateClone(Addressables.LoadAssetAsync<GameObject>(RoR2_Base_Engi.EngiWalkerTurretMaster_prefab).WaitForCompletion(), "RailgunnerTurretMaster");
            ContentPacks.masterPrefabs.Add(railgunnerTurretMasterPrefab);

            var master = railgunnerTurretMasterPrefab.GetComponent<CharacterMaster>();
            master.bodyPrefab = railgunnerTurretPrefab;

            var body = railgunnerTurretPrefab.GetComponent<CharacterBody>();
            body.name = "RailgunnerTurretBody";

            var skillLocator = body.GetComponent<SkillLocator>();

            // replaces primary with railgun shot
            SkillDef railShotClone = ScriptableObject.Instantiate(Addressables.LoadAssetAsync<SkillDef>(RoR2_DLC1_Railgunner.RailgunnerBodyFireSnipeSuper_asset).WaitForCompletion());
            ContentPacks.skillDefs.Add(railShotClone);
            
            //railShotClone.activationState = new EntityStates.SerializableEntityStateType(typeof(FireSnipeSuperTurret));
            //ContentPacks.entityStateTypes.Add(typeof(FireSnipeSuperTurret));

            railShotClone.baseRechargeInterval = 21f;
            railShotClone.baseMaxStock = 1;

            var family = skillLocator.primary.skillFamily;
            family.variants =
            [
                new() {
                    skillDef = railShotClone,
                    unlockableDef = null,
                    viewableNode = new ViewablesCatalog.Node(railShotClone.skillNameToken, false)
                }
            ];

            // AI tweaks
            var drivers = master.GetComponents<AISkillDriver>();
            foreach (var driver in drivers)
            {
                if (driver.skillSlot == SkillSlot.Primary)
                {
                    driver.maxDistance = 9999f;
                    driver.activationRequiresTargetLoS = true;
                    driver.activationRequiresAimConfirmation = true;
                    driver.activationRequiresAimTargetLoS = true;
                }
            }

            // sound setup
            AddRailgunnerSoundbank(railgunnerTurretPrefab);

            Log.Debug("turret prefab created");
        }

        private static void AddRailgunnerSoundbank(GameObject gameObject)
        {
            AkBank a = Addressables.LoadAssetAsync<GameObject>(RoR2_DLC1_Railgunner.RailgunnerBody_prefab).WaitForCompletion().GetComponent<AkBank>();
            if (a != null)
            {
                AkBank b = gameObject.AddComponent<AkBank>();
                // the streets are saying. same ref is fine
                b.triggerList = a.triggerList;
                b.data.WwiseObjectReference = a.data.WwiseObjectReference;
                b.unloadTriggerList = a.unloadTriggerList;
            }
        }

        private static void CreateRailgunnerTurretSkill()
        {
            // clone carbonizer
            SkillDef carbonizerSkill = Addressables.LoadAssetAsync<SkillDef>(RoR2_Base_Engi.EngiBodyPlaceWalkerTurret_asset).WaitForCompletion();

            var skill = ScriptableObject.Instantiate(carbonizerSkill);

            // language stuff
            skill.skillName = "RailgunnerTurret";
            skill.skillNameToken = "ENGI_RAILGUNNER_TURRET_NAME";
            skill.skillDescriptionToken = "ENGI_RAILGUNNER_TURRET_DESC";

            LanguageAPI.Add("ENGI_RAILGUNNER_TURRET_NAME", "TRM99 \"Noisy Cricket\"");
            LanguageAPI.Add("ENGI_RAILGUNNER_TURRET_DESC", "Place a <style=cIsUtility>mobile</style> turret that <style=cIsUtility>inherits all your items.</style> " +
                "Fires a high powered laser for <style=cIsDamage>4000% damage</style> with a cooldown of <style=cIsDamage>21 seconds</style>. Targets the <style=cIsUtility>highest health</style> enemy it can see. Can place up to 2.");

            // swap da turret in
            skill.activationState = new EntityStates.SerializableEntityStateType(typeof(PlaceRailgunnerTurret));
            ContentPacks.entityStateTypes.Add(typeof(PlaceRailgunnerTurret));

            // adds the skill to engi
            var skillFamily = Addressables.LoadAssetAsync<SkillFamily>(RoR2_Base_Engi.EngiBodySpecialFamily_asset).WaitForCompletion();

            var poop = new SkillFamily.Variant() { skillDef = skill };

            skillFamily.variants = skillFamily.variants.Append(poop).ToArray();

            Log.Debug("skill created");
        }
    }
    public class PlaceRailgunnerTurret : EntityStates.Engi.EngiWeapon.PlaceWalkerTurret
    {
        public override void OnEnter()
        {
            wristDisplayPrefab = Assets.w;
            blueprintPrefab = Assets.b;
            placeSoundString = "";
            turretMasterPrefab = Assets.railgunnerTurretMasterPrefab;
            base.OnEnter();
            
        }
    }

    public class FireSnipeSuperTurret : EntityStates.Railgunner.Weapon.FireSnipeSuper
    {
        public override void OnEnter()
        {
            base.OnEnter();
            var body = this.characterBody;
            Vector3 recoilDir = body.characterDirection.forward;// + Vector3.down * 0.5f;
            recoilDir.Normalize();
            body.characterMotor.ApplyForce(-recoilDir * 10000f, true);
        }
    }
}
