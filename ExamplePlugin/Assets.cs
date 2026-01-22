using BepInEx;
using EntityStates;
using EntityStates.Railgunner.Weapon;
using IL.RoR2.UI;
using R2API;
using R2API.ContentManagement;
using RoR2;
using RoR2.Audio;
using RoR2.CharacterAI;
using RoR2.Skills;
using RoR2BepInExPack;
using RoR2BepInExPack.GameAssetPaths;
using System;
using System.Collections;
using System.Diagnostics.Tracing;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.Rendering;
using UnityEngine.TextCore.Text;
using Random = UnityEngine.Random;

namespace RailgunnerTurret
{
    public class Assets
    {
        public static GameObject railgunnerTurretPrefab;
        public static GameObject railgunnerTurretMasterPrefab;

        public static GameObject wristDisplay = Addressables.LoadAssetAsync<GameObject>(RoR2_Base_Engi.EngiTurretWristDisplay_prefab).WaitForCompletion();
        public static GameObject blueprints = Addressables.LoadAssetAsync<GameObject>(RoR2_Base_Engi.EngiWalkerTurretBlueprints_prefab).WaitForCompletion();

        public static GameObject hitEffectPrefab = Addressables.LoadAssetAsync<GameObject>(RoR2_DLC1_Railgunner.ImpactRailgun_prefab).WaitForCompletion();
        public static GameObject muzzleFlashPrefab = Addressables.LoadAssetAsync<GameObject>(RoR2_Base_Common_VFX.Muzzleflash1_prefab).WaitForCompletion();
        public static GameObject tracerEffectPrefab;

        public static void Init()
        {
            CreateRailgunnerTurretPrefab();
            CreateRailgunnerTurretSkill();
            CreateTracerEffectPrefab();
        }

        public static void CreateTracerEffectPrefab()
        {
            tracerEffectPrefab = PrefabAPI.InstantiateClone(Addressables.LoadAssetAsync<GameObject>(RoR2_DLC1_Railgunner.TracerRailgunSuper_prefab).WaitForCompletion(), "RailgunnerTurretTracerEffectPrefab",false);
            tracerEffectPrefab.transform.Find("StartTransform/PP").gameObject.SetActive(false);
            ContentPacks.effectDefs.Add(new EffectDef(tracerEffectPrefab));
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

            body.baseMoveSpeed = body.baseMoveSpeed * 0.6f; // movement speed tweak

            // replaces primary with railgun shot
            SkillDef railShotClone = ScriptableObject.Instantiate(Addressables.LoadAssetAsync<SkillDef>(RoR2_DLC1_Railgunner.RailgunnerBodyFireSnipeSuper_asset).WaitForCompletion());
            ContentPacks.skillDefs.Add(railShotClone);
            
            railShotClone.activationState = new EntityStates.SerializableEntityStateType(typeof(FireSnipeSuperTurret));
            ContentPacks.entityStateTypes.Add(typeof(FireSnipeSuperTurret));

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
                if (driver.moveTargetType == AISkillDriver.TargetType.CurrentLeader && driver.minDistance == 110) // this changes the sprinting behaviour
                {
                    driver.minDistance = 30;
                }
            }

            // sound setup
            AddRailgunnerSoundbank(railgunnerTurretPrefab);
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
                "Fires a high powered laser for <style=cIsDamage>3000% damage</style> with a cooldown of <style=cIsDamage>21 seconds</style>. Targets the <style=cIsUtility>highest health</style> enemy it can see. Can place up to 2.");

            // swap da turret in
            skill.activationState = new EntityStates.SerializableEntityStateType(typeof(PlaceRailgunnerTurret));
            ContentPacks.entityStateTypes.Add(typeof(PlaceRailgunnerTurret));

            // adds the skill to engi
            var skillFamily = Addressables.LoadAssetAsync<SkillFamily>(RoR2_Base_Engi.EngiBodySpecialFamily_asset).WaitForCompletion();

            var poop = new SkillFamily.Variant() { skillDef = skill };

            skillFamily.variants = skillFamily.variants.Append(poop).ToArray();

            ContentAddition.AddSkillDef(skill);
        }
    }
    public class PlaceRailgunnerTurret : EntityStates.Engi.EngiWeapon.PlaceWalkerTurret
    {
        public override void OnEnter()
        {
            wristDisplayPrefab = Assets.wristDisplay;
            blueprintPrefab = Assets.blueprints;
            placeSoundString = "";
            turretMasterPrefab = Assets.railgunnerTurretMasterPrefab;
            base.OnEnter();
            
        }
    }

    public class FireSnipeSuperTurret : EntityStates.Railgunner.Weapon.FireSnipeSuper
    {
        public override void OnEnter()
        {
            hitEffectPrefab = Assets.hitEffectPrefab;
            force = 4000f;
            headshotSoundString = "Play_railgunner_m2_headshot";
            fireSoundString = "Play_railgunner_R_fire";
            damageCoefficient = 30f; // DAMAGE COEFF ORIGINALLY 40
            critDamageMultiplier = 1.5f;
            bulletRadius = 1;
            bulletCount = 1;
            baseDuration = 1;
            animationStateName = "FireSuper";
            animationPlaybackRateParam = "Super.playbackRate";
            animationLayerName = "Gesture, Override";
            recoilAmplitudeX = 1f;
            recoilAmplitudeY = 6f;
            procCoefficient = 1.5f; // PROC COEFF ORIGINALLY 3
            piercingDamageCoefficientPerTarget = 1f;
            muzzleName = "Muzzle";
            muzzleFlashPrefab = Assets.muzzleFlashPrefab;
            minSpread = 0;
            maxSpread = 0;
            maxDistance = 9999;
            isPiercing = true;
            useSmartCollision = true;
            useSecondaryStocks = false;
            trajectoryAimAssistMultiplier = 0;
            tracerEffectPrefab = Assets.tracerEffectPrefab;
            spreadYawScale = 0;
            spreadPitchScale = 0;
            spreadBloomValue = 0;
            selfKnockbackForce = 10000; // ORIGINALLY 3000

            var stateMac = characterBody.GetComponent<EntityStateMachine>();
            if (stateMac.state is GenericCharacterMain a)
            {
                a.jumpInputReceived = true;
                a.ProcessJump(true);
            }

            base.OnEnter();
        }

        public override void ModifyBullet(BulletAttack bulletAttack)
        {
            base.ModifyBullet(bulletAttack);

            bulletAttack.sniper = false;
        }
    }
}
