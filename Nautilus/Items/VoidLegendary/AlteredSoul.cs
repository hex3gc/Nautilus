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
            [ItemTag.Utility, ItemTag.AIBlacklist, ItemTag.CanBeTemporary],
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

            #region IDR
            rules.Add("mdlCommandoDualies", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Head",
                        localPos = new Vector3(-0.00128F, 0.22994F, -0.20636F),
                        localAngles = new Vector3(275.3801F, 356.6017F, 3.30798F),
                        localScale = new Vector3(0.62757F, 0.62757F, 0.62757F)
                    }
                }
            );
            rules.Add("mdlHuntress", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Head",
                        localPos = new Vector3(-0.00104F, 0.22706F, -0.19517F),
                        localAngles = new Vector3(281.0011F, 0F, 0F),
                        localScale = new Vector3(0.70585F, 0.70585F, 0.70585F)
                    }
                }
            );
            rules.Add("mdlBandit2", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Head",
                        localPos = new Vector3(-0.00001F, -0.04432F, 0.11259F),
                        localAngles = new Vector3(54.48512F, 180F, 180F),
                        localScale = new Vector3(0.65642F, 0.65642F, 0.65642F)
                    }
                }
            );
            rules.Add("mdlToolbot", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "ThighR",
                        localPos = new Vector3(-0.00002F, -0.00001F, 0.76063F),
                        localAngles = new Vector3(87.88058F, 0F, 0F),
                        localScale = new Vector3(5.74914F, 5.74914F, 5.74914F)
                    }
                }
            );
            rules.Add("mdlEngi", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "UpperArmR",
                        localPos = new Vector3(-0.14194F, -0.0007F, 0F),
                        localAngles = new Vector3(0F, 0F, 81.67231F),
                        localScale = new Vector3(1F, 1F, 1F)
                    }
                }
            );
            rules.Add("mdlMage", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Chest",
                        localPos = new Vector3(0F, 0.14945F, 0.17846F),
                        localAngles = new Vector3(71.03831F, 0F, 0F),
                        localScale = new Vector3(0.62252F, 0.62252F, 0.62252F)
                    }
                }
            );
            rules.Add("mdlMerc", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Chest",
                        localPos = new Vector3(-0.1924F, 0.27873F, -0.00001F),
                        localAngles = new Vector3(0F, 0F, 30.07195F),
                        localScale = new Vector3(0.85493F, 0.85493F, 0.85493F)
                    }
                }
            );
            rules.Add("mdlTreebot", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "PlatformBase",
                        localPos = new Vector3(0F, 1.4849F, 0F),
                        localAngles = new Vector3(0F, 33.34313F, 0F),
                        localScale = new Vector3(3.13878F, 3.13878F, 3.13878F)
                    }
                }
            );
            rules.Add("mdlLoader", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Head",
                        localPos = new Vector3(0F, 0.22693F, 0.13046F),
                        localAngles = new Vector3(42.87527F, 0F, 0F),
                        localScale = new Vector3(0.57249F, 0.57249F, 0.57249F)
                    }
                }
            );
            rules.Add("mdlCroco", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Chest",
                        localPos = new Vector3(0F, 1.38303F, -2.72033F),
                        localAngles = new Vector3(279.489F, 0F, 0F),
                        localScale = new Vector3(6.89953F, 6.89953F, 6.89953F)
                    }
                }
            );
            rules.Add("mdlCaptain", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "LowerArmL",
                        localPos = new Vector3(-0.00003F, 0.33383F, 0.00003F),
                        localAngles = new Vector3(0F, 0F, 0F),
                        localScale = new Vector3(1F, 1F, 1F)
                    }
                }
            );
            rules.Add("mdlRailGunner", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "GunScope",
                        localPos = new Vector3(-0.07261F, -0.1454F, 0.32971F),
                        localAngles = new Vector3(270.2167F, -0.00296F, 180.9618F),
                        localScale = new Vector3(0.50734F, 0.50734F, 0.50734F)
                    }
                }
            );
            rules.Add("mdlVoidSurvivor", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Head",
                        localPos = new Vector3(0.11898F, 0.15649F, 0.15299F),
                        localAngles = new Vector3(40.65683F, 34.01722F, 355.9835F),
                        localScale = new Vector3(1F, 1F, 1F)
                    }
                }
            );
            rules.Add("mdlSeeker", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Chest",
                        localPos = new Vector3(0F, 0.1742F, 0.08795F),
                        localAngles = new Vector3(66.07602F, 0F, 0F),
                        localScale = new Vector3(0.95988F, 0.95988F, 0.95988F)
                    }
                }
            );
            rules.Add("mdlFalseSon", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Chest",
                        localPos = new Vector3(-0.22021F, -0.39672F, 0F),
                        localAngles = new Vector3(0F, 0F, 150.8223F),
                        localScale = new Vector3(1.51775F, 1.51775F, 1.51775F)
                    }
                }
            );
            rules.Add("mdlChef", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Head",
                        localPos = new Vector3(-0.46869F, 0.17221F, -0.00001F),
                        localAngles = new Vector3(0F, 0F, 36.08236F),
                        localScale = new Vector3(0.7726F, 0.7726F, 0.7726F)
                    }
                }
            );
            rules.Add("mdlDroneTech", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "CalfR",
                        localPos = new Vector3(-0.06542F, -0.00015F, 0.06721F),
                        localAngles = new Vector3(89.8718F, 180.0015F, 212.0169F),
                        localScale = new Vector3(0.776F, 0.776F, 0.776F)
                    }
                }
            );
            rules.Add("mdlDrifter", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Head",
                        localPos = new Vector3(-0.09273F, 0.28798F, -0.00001F),
                        localAngles = new Vector3(0F, 0F, 0F),
                        localScale = new Vector3(1F, 1F, 1F)
                    }
                }
            );
            #endregion

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