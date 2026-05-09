using RoR2;
using Nautilus.Configuration;
using System;
using R2API;
using UnityEngine;
using UnityEngine.AddressableAssets;
using HarmonyLib;
using UnityEngine.Networking;
using RoR2.Projectile;
using UnityEngine.TextCore.Text;
using HG;
using EntityStates.JunkCube;

namespace Nautilus.Items
{
    public static partial class ItemInit
    {
        public static AlteredSoul AlteredSoul = new AlteredSoul
        (
            "AlteredSoul",
            [ItemTag.Utility, ItemTag.AIBlacklist],
            ItemTier.VoidTier3
        );
    }

    /// <summary>
    ///     // Ver.1
    ///     On-kill items are powerful enough to make this worth replacing a great legendary (daggers) for
    ///     Tons of synergies
    /// </summary>
    public class AlteredSoul : ItemBase
    {
        public override bool Enabled => AlteredSoul_Enabled.Value;
        public override ItemDef ConversionItemDef => Addressables.LoadAssetAsync<ItemDef>("RoR2/Base/Dagger/Dagger.asset").WaitForCompletion();
        public override GameObject itemPrefab => OverwritePrefabMaterials();
        public Material material0 => Addressables.LoadAssetAsync<Material>("RoR2/Base/crystalworld/matTimeCrystalSolid.mat").WaitForCompletion();
        public Material material1 => Addressables.LoadAssetAsync<Material>("RoR2/DLC1/VoidRaidCrab/matVoidRaidCrabParticleBlue.mat").WaitForCompletion();
        public override Sprite itemIcon => Main.Assets.LoadAsset<Sprite>("Assets/icons/alteredSoul.png");
        public GameObject itemDropPrefab
        {
            get
            {
                if (_itemDropPrefab == null)
                {
                    _itemDropPrefab = CreateDropPrefab();
                }
                return _itemDropPrefab;
            }
            set;
        }
        private GameObject _itemDropPrefab;
        public LayerMask dropLayerMask => LayerIndex.world.mask;
        public static GameObject itemKillEffect => Addressables.LoadAssetAsync<GameObject>("RoR2/Base/DeathProjectile/DeathProjectileTickEffect.prefab").WaitForCompletion();
        public static GameObject itemBlinkEffect => Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/VoidBarnacle/VoidBarnacleSpawnEffect.prefab").WaitForCompletion();
        public static GameObject itemImpactEffect => Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/MissileVoid/VoidImpactEffect.prefab").WaitForCompletion();

        public AlteredSoul(string _name, ItemTag[] _tags, ItemTier _tier, bool _canRemove = true, bool _isConsumed = false, bool _hidden = false) : 
        base(_name, _tags, _tier, _canRemove, _isConsumed, _hidden){}

        // Config
        public static ConfigItem<bool> AlteredSoul_Enabled = new ConfigItem<bool>
        (
            "Void legendary: Altered Soul",
            "Item enabled",
            "Should this item appear in runs?",
            true
        );
        public static ConfigItem<int> AlteredSoul_Kills = new ConfigItem<int>
        (
            "Void legendary: Altered Soul",
            "Additional on-kills",
            "How many on-kill effect duplications occur.",
            1,
            1f,
            5f,
            1f
        );
        public static ConfigItem<int> AlteredSoul_KillsStack = new ConfigItem<int>
        (
            "Void legendary: Altered Soul",
            "Additional on-kills (per stack)",
            "How many on-kill effect duplications occur, per additional stack.",
            1,
            1f,
            5f,
            1f
        );
        public static ConfigItem<float> AlteredSoul_KillInterval = new ConfigItem<float>
        (
            "Void legendary: Altered Soul",
            "On-kill interval",
            "Time (in seconds) between on-kill effect triggers from crystals.",
            1f,
            0.1f,
            5f,
            0.1f
        );
        public static ConfigItem<bool> AlteredSoul_Recipe = new ConfigItem<bool>
        (
            "Void legendary: Altered Soul",
            "Recipe enabled",
            "Should this item have a custom corruption recipe?",
            true
        );
        public static ConfigItem<string> AlteredSoul_Ingredient1 = new ConfigItem<string>
        (
            "Void legendary: Altered Soul",
            "Recipe ingredient 1",
            "First ingredient for corruption recipe",
            "Dagger"
        );
        public static ConfigItem<string> AlteredSoul_Ingredient2 = new ConfigItem<string>
        (
            "Void legendary: Altered Soul",
            "Recipe ingredient 2",
            "Second ingredient for corruption recipe",
            "ElementalRingVoid"
        );

        public GameObject OverwritePrefabMaterials()
        {
            GameObject ret = Main.Assets.LoadAsset<GameObject>("Assets/prefabs/alteredSoul.prefab");

            Material[] materials =
            {
                material0,
                material1
            };
            ret.GetComponentInChildren<MeshRenderer>().SetMaterialArray(materials);

            return ret;
        }

        public GameObject CreateDropPrefab()
        {
            GameObject ret = Main.Assets.LoadAsset<GameObject>("Assets/prefabs/alteredSoul.prefab");

            Material[] materials =
            {
                material0,
                material1
            };
            ret.GetComponentInChildren<MeshRenderer>().SetMaterialArray(materials);

            ret.transform.localScale = new Vector3(4f, 4f, 4f);

            AlteredSoulBehavior droppedSoulBehavior = ret.AddComponent<AlteredSoulBehavior>();

            CharacterBody characterBody = ret.AddComponent<CharacterBody>();

            HealthComponent healthComponent = ret.AddComponent<HealthComponent>();
            healthComponent.health = 100f;
            healthComponent.body = characterBody;
            characterBody.healthComponent = healthComponent;

            SphereCollider sphereCollider = ret.AddComponent<SphereCollider>();
            sphereCollider.radius = 0.01f;

            return ret;
        }

        // Tokens
        public override void FormatDescriptionTokens()
        {
            string descriptionToken = ItemDef.descriptionToken;

            LanguageAPI.AddOverlay
            (
                descriptionToken,
                String.Format
                (
                    Language.currentLanguage.GetLocalizedStringByToken(descriptionToken),
                    AlteredSoul_Kills.Value,
                    AlteredSoul_KillsStack.Value
                )
            );
        }

        // Hooks
        public override void RegisterHooks()
        {
            On.RoR2.GlobalEventManager.OnCharacterDeath += (orig, self, damageReport) =>
            {
                if (damageReport.attackerBody && damageReport.victimBody && damageReport.victimBody.teamComponent && GetItemCountEffective(damageReport.attackerBody) > 0 && !damageReport.victim.gameObject.GetComponentInChildren<AlteredSoulBehavior>())
                {
                    int itemCount = GetItemCountEffective(damageReport.attackerBody);

                    GameObject droppedSoul = UnityEngine.Object.Instantiate(itemDropPrefab, damageReport.victimBody.corePosition, Quaternion.identity);

                    if (Physics.Raycast(damageReport.victimBody.corePosition, Vector3.down, out RaycastHit hit, 1000f, dropLayerMask))
                    {
                        droppedSoul.transform.SetPositionAndRotation(hit.point, Quaternion.identity);

                        EffectData effectData = new EffectData()
                        {
                            origin = hit.point
                        };
                        EffectManager.SpawnEffect(itemBlinkEffect, effectData, true);

                        EffectData effectData2 = new EffectData()
                        {
                            origin = hit.point
                        };
                        EffectManager.SpawnEffect(itemImpactEffect, effectData2, true);

                        AlteredSoulBehavior droppedSoulBehavior = droppedSoul.GetComponent<AlteredSoulBehavior>();
                        HealthComponent droppedSoulHealthComponent = droppedSoul.GetComponent<HealthComponent>();
                        CharacterBody droppedSoulCharacterBody = droppedSoul.GetComponent<CharacterBody>();

                        DamageReport copyReport = new DamageReport(damageReport.damageInfo, droppedSoulHealthComponent, damageReport.damageDealt, damageReport.combinedHealthBeforeDamage);
                        if (damageReport.victimIsElite)
                        {
                            copyReport.victimIsElite = true;
                        }

                        droppedSoulBehavior.DamageReport = copyReport;
                        droppedSoulBehavior.onKillInterval = AlteredSoul_KillInterval.Value;
                        droppedSoulBehavior.remainingKills = AlteredSoul_Kills.Value + (AlteredSoul_KillsStack.Value * (itemCount - 1));

                        droppedSoulCharacterBody.teamComponent.teamIndex = damageReport.victimBody.teamComponent.teamIndex;

                        // TODO: Add original character's HealthComponent and CharacterBody to the soul for more useful effects
                    }
                    else
                    {
                        UnityEngine.Object.Destroy(droppedSoul);
                    }
                }

                orig(self, damageReport);
            };
        }

        // Recipe
        public override void AddCorruptionRecipe()
        {
            if (AlteredSoul_Recipe.Value == true)
            {
                ItemInit.MakeCorruptionRecipe
                (
                    AlteredSoul_Ingredient1.Value,
                    AlteredSoul_Ingredient2.Value,
                    ItemDef.name
                );
            }
        }

        // IDR
        public override ItemDisplayRuleDict AddItemDisplays()
        {
            GameObject ItemDisplayPrefab = Helpers.PrepareItemDisplayModel(PrefabAPI.InstantiateClone(itemPrefab, ItemDef.name + "Display", false));
            ItemDisplayRuleDict rules = new ItemDisplayRuleDict();

            /*
            #region IDR
            rules.Add("mdlCommandoDualies", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Pelvis",
                        localPos = new Vector3(0F, 0F, 0F),
                        localAngles = new Vector3(0F, 0F, 0F),
                        localScale = new Vector3(1F, 1F, 1F)
                    }
                }
            );
            rules.Add("mdlHuntress", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Pelvis",
                        localPos = new Vector3(0F, 0F, 0F),
                        localAngles = new Vector3(0F, 0F, 0F),
                        localScale = new Vector3(1F, 1F, 1F)
                    }
                }
            );
            rules.Add("mdlBandit2", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Pelvis",
                        localPos = new Vector3(0F, 0F, 0F),
                        localAngles = new Vector3(0F, 0F, 0F),
                        localScale = new Vector3(1F, 1F, 1F)
                    }
                }
            );
            rules.Add("mdlToolbot", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Pelvis",
                        localPos = new Vector3(0F, 0F, 0F),
                        localAngles = new Vector3(0F, 0F, 0F),
                        localScale = new Vector3(1F, 1F, 1F)
                    }
                }
            );
            rules.Add("mdlEngi", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Pelvis",
                        localPos = new Vector3(0F, 0F, 0F),
                        localAngles = new Vector3(0F, 0F, 0F),
                        localScale = new Vector3(1F, 1F, 1F)
                    }
                }
            );
            rules.Add("mdlMage", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Pelvis",
                        localPos = new Vector3(0F, 0F, 0F),
                        localAngles = new Vector3(0F, 0F, 0F),
                        localScale = new Vector3(1F, 1F, 1F)
                    }
                }
            );
            rules.Add("mdlMerc", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Pelvis",
                        localPos = new Vector3(0F, 0F, 0F),
                        localAngles = new Vector3(0F, 0F, 0F),
                        localScale = new Vector3(1F, 1F, 1F)
                    }
                }
            );
            rules.Add("mdlTreebot", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Pelvis",
                        localPos = new Vector3(0F, 0F, 0F),
                        localAngles = new Vector3(0F, 0F, 0F),
                        localScale = new Vector3(1F, 1F, 1F)
                    }
                }
            );
            rules.Add("mdlLoader", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Pelvis",
                        localPos = new Vector3(0F, 0F, 0F),
                        localAngles = new Vector3(0F, 0F, 0F),
                        localScale = new Vector3(1F, 1F, 1F)
                    }
                }
            );
            rules.Add("mdlCroco", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Pelvis",
                        localPos = new Vector3(0F, 0F, 0F),
                        localAngles = new Vector3(0F, 0F, 0F),
                        localScale = new Vector3(1F, 1F, 1F)
                    }
                }
            );
            rules.Add("mdlCaptain", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Pelvis",
                        localPos = new Vector3(0F, 0F, 0F),
                        localAngles = new Vector3(0F, 0F, 0F),
                        localScale = new Vector3(1F, 1F, 1F)
                    }
                }
            );
            rules.Add("mdlRailGunner", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Pelvis",
                        localPos = new Vector3(0F, 0F, 0F),
                        localAngles = new Vector3(0F, 0F, 0F),
                        localScale = new Vector3(1F, 1F, 1F)
                    }
                }
            );
            rules.Add("mdlVoidSurvivor", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Pelvis",
                        localPos = new Vector3(0F, 0F, 0F),
                        localAngles = new Vector3(0F, 0F, 0F),
                        localScale = new Vector3(1F, 1F, 1F)
                    }
                }
            );
            rules.Add("mdlSeeker", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Pelvis",
                        localPos = new Vector3(0F, 0F, 0F),
                        localAngles = new Vector3(0F, 0F, 0F),
                        localScale = new Vector3(1F, 1F, 1F)
                    }
                }
            );
            rules.Add("mdlFalseSon", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Pelvis",
                        localPos = new Vector3(0F, 0F, 0F),
                        localAngles = new Vector3(0F, 0F, 0F),
                        localScale = new Vector3(1F, 1F, 1F)
                    }
                }
            );
            rules.Add("mdlChef", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Pelvis",
                        localPos = new Vector3(0F, 0F, 0F),
                        localAngles = new Vector3(0F, 0F, 0F),
                        localScale = new Vector3(1F, 1F, 1F)
                    }
                }
            );
            rules.Add("mdlDroneTech", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Pelvis",
                        localPos = new Vector3(0F, 0F, 0F),
                        localAngles = new Vector3(0F, 0F, 0F),
                        localScale = new Vector3(1F, 1F, 1F)
                    }
                }
            );
            rules.Add("mdlDrifter", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Pelvis",
                        localPos = new Vector3(0F, 0F, 0F),
                        localAngles = new Vector3(0F, 0F, 0F),
                        localScale = new Vector3(1F, 1F, 1F)
                    }
                }
            );
            #endregion
            */

            return rules;
        }

        public class AlteredSoulBehavior : MonoBehaviour
        {
            public DamageReport DamageReport;
            public float onKillInterval = 1f;
            public float onKillTimer = 0f;
            public float remainingKills = 2;

            void FixedUpdate()
            {
                onKillTimer += Time.fixedDeltaTime;

                if (onKillTimer >= onKillInterval)
                {
                    onKillTimer = 0f;

                    EffectData effectData = new EffectData()
                    {
                        origin = gameObject.transform.position
                    };
                    EffectManager.SpawnEffect(itemKillEffect, effectData, true);

                    GlobalEventManager.instance.OnCharacterDeath(DamageReport);
                    
                    remainingKills--;
                    if (remainingKills <= 0)
                    {
                        EffectData effectData2 = new EffectData()
                        {
                            origin = gameObject.transform.position
                        };
                        EffectManager.SpawnEffect(itemImpactEffect, effectData2, true);

                        Destroy(gameObject);
                    }
                }
            }
        }
    }
}