using System;
using System.Collections.Generic;
using EntityStates;
using RoR2;
using RoR2.ContentManagement;
using RoR2.Skills;
using UnityEngine;
using UnityEngine.UIElements.UIR;

namespace RailgunnerTurret {
    internal class ContentPacks : IContentPackProvider {
        internal ContentPack contentPack = new ContentPack();
        public string identifier => RailgunnerTurret.PluginGUID;

        public static List<GameObject> bodyPrefabs = new List<GameObject>();
        public static List<GameObject> masterPrefabs = new List<GameObject>();

        public static List<SkillDef> skillDefs = new List<SkillDef>();

        public static List<Type> entityStateTypes = new List<Type>();

        public void Init() {
            ContentManager.collectContentPackProviders += ContentManager_collectContentPackProviders;
        }

        private void ContentManager_collectContentPackProviders(ContentManager.AddContentPackProviderDelegate addContentPackProvider) {
            addContentPackProvider(this);
        }

        public System.Collections.IEnumerator LoadStaticContentAsync(LoadStaticContentAsyncArgs args) {
            this.contentPack.identifier = this.identifier;

            contentPack.bodyPrefabs.Add(bodyPrefabs.ToArray());
            contentPack.masterPrefabs.Add(masterPrefabs.ToArray());
            contentPack.entityStateTypes.Add(entityStateTypes.ToArray());
            contentPack.skillDefs.Add(skillDefs.ToArray());

            args.ReportProgress(1f);
            yield break;
        }

        public System.Collections.IEnumerator GenerateContentPackAsync(GetContentPackAsyncArgs args) {
            ContentPack.Copy(this.contentPack, args.output);
            args.ReportProgress(1f);
            yield break;
        }

        public System.Collections.IEnumerator FinalizeAsync(FinalizeAsyncArgs args) {
            args.ReportProgress(1f);
            yield break;
        }
    }
}
