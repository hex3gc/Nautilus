using RoR2;
using Nautilus.Configuration;
using System;
using R2API;
using UnityEngine;
using UnityEngine.AddressableAssets;
using HarmonyLib;

namespace Nautilus.Items
{
    public static partial class ItemInit
    {
        public static Dreadleech Dreadleech = new Dreadleech
        (
            "Dreadleech",
            [ItemTag.Healing, ItemTag.CanBeTemporary],
            ItemTier.VoidTier2
        );
    }

    /// <summary>
    ///     // Ver.1
    ///     Has more healing potential than Leeching Seed, but is less consistent
    ///     Meant to synergize with Collapse effects and the new Xenobacteria
    /// </summary>
    public class Dreadleech : ItemBase
    {
        public override bool Enabled => Dreadleech_Enabled.Value;
        public override ItemDef ConversionItemDef => Addressables.LoadAssetAsync<ItemDef>("RoR2/Base/Seed/Seed.asset").WaitForCompletion();
        public override GameObject itemPrefab => OverwritePrefabMaterials();
        public Material material0 => Addressables.LoadAssetAsync<Material>("RoR2/Base/artifactworld/matArtifactGem.mat").WaitForCompletion();
        public Material material1 => Addressables.LoadAssetAsync<Material>("RoR2/Base/Scav/matScavEyes.mat").WaitForCompletion();
        public Material material2 => Addressables.LoadAssetAsync<Material>("RoR2/DLC1/VoidSurvivor/matVoidSurvivorAltMetal.mat").WaitForCompletion();
        public override Sprite itemIcon => Main.Assets.LoadAsset<Sprite>("Assets/icons/dreadleech.png");

        public Dreadleech(string _name, ItemTag[] _tags, ItemTier _tier, bool _canRemove = true, bool _isConsumed = false, bool _hidden = false) :
        base(_name, _tags, _tier, _canRemove, _isConsumed, _hidden)
        { }

        // Config
        public static ConfigItem<bool> Dreadleech_Enabled = new ConfigItem<bool>
        (
            "Void uncommon: Dreadleech",
            "Item enabled",
            "Should this item appear in runs?",
            true
        );
        public static ConfigItem<float> Dreadleech_BaseHealing = new ConfigItem<float>
        (
            "Void uncommon: Dreadleech",
            "Healing on kill",
            "Base healing on kill.",
            8f,
            1f,
            16f,
            1f
        );
        public static ConfigItem<float> Dreadleech_BaseHealingStack = new ConfigItem<float>
        (
            "Void uncommon: Dreadleech",
            "Healing on kill (per stack)",
            "Base healing on kill, per additional stack.",
            8f,
            1f,
            16f,
            1f
        );
        public static ConfigItem<float> Dreadleech_BaseHealingDebuff = new ConfigItem<float>
        (
            "Void uncommon: Dreadleech",
            "Healing on kill per debuff",
            "Base healing on kill per debuff.",
            8f,
            1f,
            16f,
            1f
        );
        public static ConfigItem<float> Dreadleech_BaseHealingStackDebuff = new ConfigItem<float>
        (
            "Void uncommon: Dreadleech",
            "Healing on kill per debuff (per stack)",
            "Base healing on kill, per additional stack per debuff.",
            8f,
            1f,
            16f,
            1f
        );
        public static ConfigItem<bool> Dreadleech_Recipe = new ConfigItem<bool>
        (
            "Void uncommon: Dreadleech",
            "Recipe enabled",
            "Should this item have a custom corruption recipe?",
            true
        );
        public static ConfigItem<string> Dreadleech_Ingredient1 = new ConfigItem<string>
        (
            "Void uncommon: Dreadleech",
            "Recipe ingredient 1",
            "First ingredient for corruption recipe",
            "Seed"
        );
        public static ConfigItem<string> Dreadleech_Ingredient2 = new ConfigItem<string>
        (
            "Void uncommon: Dreadleech",
            "Recipe ingredient 2",
            "Second ingredient for corruption recipe",
            "ProtonPop"
        );

        public GameObject OverwritePrefabMaterials()
        {
            GameObject ret = Main.Assets.LoadAsset<GameObject>("Assets/prefabs/dreadleech.prefab");

            Material[] materials =
            {
                material0,
                material1,
                material2
            };
            ret.GetComponentInChildren<MeshRenderer>().SetMaterialArray(materials);

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
                    Dreadleech_BaseHealing.Value,
                    Dreadleech_BaseHealingStack.Value,
                    Dreadleech_BaseHealingDebuff.Value,
                    Dreadleech_BaseHealingStackDebuff.Value
                )
            );
        }

        // Hooks
        public override void RegisterHooks()
        {
            // Heal on kill
            On.RoR2.GlobalEventManager.OnCharacterDeath += (orig, self, damageReport) =>
            {
                orig(self, damageReport);

                if (damageReport.attackerBody && damageReport.attackerBody.healthComponent && damageReport.victimBody && GetItemCountEffective(damageReport.attackerBody) > 0)
                {
                    int itemCount = GetItemCountEffective(damageReport.attackerBody);
                    float totalHealing = Dreadleech_BaseHealing.Value + (Dreadleech_BaseHealingStack.Value * (itemCount - 1));

                    foreach (BuffIndex buff in damageReport.victimBody.activeBuffsList)
                    {
                        BuffDef buffDef = BuffCatalog.GetBuffDef(buff);
                        if (buffDef && (buffDef.isDebuff || buffDef.isDOT))
                        {
                            totalHealing += Dreadleech_BaseHealingDebuff.Value + (Dreadleech_BaseHealingStackDebuff.Value * (itemCount - 1));
                        }
                    }
                    
                    damageReport.attackerBody.healthComponent.Heal(totalHealing, new ProcChainMask());
                }
            };
        }

        // Recipes
        public override void AddCorruptionRecipe()
        {
            if (Dreadleech_Recipe.Value == true)
            {
                ItemInit.MakeCorruptionRecipe
                (
                    Dreadleech_Ingredient1.Value,
                    Dreadleech_Ingredient2.Value,
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
                        localPos = new Vector3(0.19812F, 0.36372F, -0.13752F),
                        localAngles = new Vector3(289.9234F, 180F, 180F),
                        localScale = new Vector3(0.46626F, 0.46626F, 0.46626F)
                    }
                }
            );
            rules.Add("mdlHuntress", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Head",
                        localPos = new Vector3(0.19853F, 0.28441F, -0.11405F),
                        localAngles = new Vector3(295.8632F, 138.3061F, 218.4923F),
                        localScale = new Vector3(0.46626F, 0.46626F, 0.46626F)
                    }
                }
            );
            rules.Add("mdlBandit2", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Head",
                        localPos = new Vector3(0.15007F, 0.27518F, -0.02566F),
                        localAngles = new Vector3(289.9234F, 180F, 180F),
                        localScale = new Vector3(0.36402F, 0.36402F, 0.36402F)
                    }
                }
            );
            rules.Add("mdlToolbot", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Head",
                        localPos = new Vector3(-2.05582F, 1.2834F, 1.22138F),
                        localAngles = new Vector3(328.0736F, 301.3525F, 267.9135F),
                        localScale = new Vector3(3.13677F, 3.13677F, 3.13677F)
                    }
                }
            );
            rules.Add("mdlEngi", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Chest",
                        localPos = new Vector3(0.12818F, 0.75955F, -0.17616F),
                        localAngles = new Vector3(292.1144F, 230.635F, 167.2383F),
                        localScale = new Vector3(0.46626F, 0.46626F, 0.46626F)
                    }
                }
            );
            rules.Add("mdlMage", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Head",
                        localPos = new Vector3(0.12317F, 0.20884F, -0.16131F),
                        localAngles = new Vector3(289.9234F, 180F, 180F),
                        localScale = new Vector3(0.28452F, 0.28452F, 0.28452F)
                    }
                }
            );
            rules.Add("mdlMerc", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Head",
                        localPos = new Vector3(0.19832F, 0.31948F, -0.02115F),
                        localAngles = new Vector3(289.9234F, 180F, 180F),
                        localScale = new Vector3(0.46626F, 0.46626F, 0.46626F)
                    }
                }
            );
            rules.Add("mdlTreebot", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Base",
                        localPos = new Vector3(-0.58653F, 0.19312F, -0.13745F),
                        localAngles = new Vector3(309.4855F, 237.5957F, 116.0989F),
                        localScale = new Vector3(1.04363F, 1.04363F, 1.04363F)
                    }
                }
            );
            rules.Add("mdlLoader", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Head",
                        localPos = new Vector3(0.19857F, 0.28031F, -0.0353F),
                        localAngles = new Vector3(289.7959F, 158.5163F, 179.8091F),
                        localScale = new Vector3(0.46626F, 0.46626F, 0.46626F)
                    }
                }
            );
            rules.Add("mdlCroco", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Head",
                        localPos = new Vector3(-2.3432F, 0.85443F, 0.9616F),
                        localAngles = new Vector3(357.4428F, 325.9451F, 153.941F),
                        localScale = new Vector3(3.88346F, 3.88346F, 3.88346F)
                    }
                }
            );
            rules.Add("mdlCaptain", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Head",
                        localPos = new Vector3(0.2004F, 0.28001F, -0.08015F),
                        localAngles = new Vector3(289.9234F, 180F, 180F),
                        localScale = new Vector3(0.46626F, 0.46626F, 0.46626F)
                    }
                }
            );
            rules.Add("mdlRailGunner", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Head",
                        localPos = new Vector3(0.14334F, 0.19473F, -0.09565F),
                        localAngles = new Vector3(297.5251F, 134.5541F, 205.9134F),
                        localScale = new Vector3(0.35216F, 0.35216F, 0.35216F)
                    }
                }
            );
            rules.Add("mdlVoidSurvivor", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Head",
                        localPos = new Vector3(0.24202F, 0.15027F, -0.05395F),
                        localAngles = new Vector3(295.5653F, 134.7148F, 209.7682F),
                        localScale = new Vector3(0.46626F, 0.46626F, 0.46626F)
                    }
                }
            );
            rules.Add("mdlSeeker", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Head",
                        localPos = new Vector3(0.22839F, 0.21796F, -0.03813F),
                        localAngles = new Vector3(300.3624F, 121.4059F, 225.6018F),
                        localScale = new Vector3(0.46626F, 0.46626F, 0.46626F)
                    }
                }
            );
            rules.Add("mdlFalseSon", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Head",
                        localPos = new Vector3(0.19466F, 0.33716F, -0.20051F),
                        localAngles = new Vector3(308.1665F, 181.9651F, 180.0541F),
                        localScale = new Vector3(0.62297F, 0.62297F, 0.62297F)
                    }
                }
            );
            rules.Add("mdlChef", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Head",
                        localPos = new Vector3(-0.09428F, 0.18345F, -0.21267F),
                        localAngles = new Vector3(323.6022F, 137.6627F, 335.7572F),
                        localScale = new Vector3(0.46626F, 0.46626F, 0.46626F)
                    }
                }
            );
            rules.Add("mdlDroneTech", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Head",
                        localPos = new Vector3(-0.20355F, 0.23569F, -0.181F),
                        localAngles = new Vector3(316.8329F, 284.5176F, 137.8123F),
                        localScale = new Vector3(0.46626F, 0.46626F, 0.46626F)
                    }
                }
            );
            rules.Add("mdlDrifter", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Head",
                        localPos = new Vector3(-0.24492F, -0.03672F, -0.32254F),
                        localAngles = new Vector3(325.3324F, 231.1708F, 213.2711F),
                        localScale = new Vector3(0.46626F, 0.46626F, 0.46626F)
                    }
                }
            );
            #endregion

            return rules;
        }
    }
}