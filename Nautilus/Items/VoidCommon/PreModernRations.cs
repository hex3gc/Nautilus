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
        public static PreModernRations PreModernRations = new PreModernRations
        (
            "PreModernRations",
            [ItemTag.Healing, ItemTag.Utility, ItemTag.AIBlacklist, ItemTag.ExtractorUnitBlacklist, ItemTag.BrotherBlacklist, ItemTag.CanBeTemporary],
            ItemTier.VoidTier1
        );
    }

    /// <summary>
    ///     // Ver.1
    ///     Scaleable equipment cooldown reduction in a common item is rare, so this will provide it
    ///     Eclipse Lite can still be preferable if you want to keep up more consistent barrier, or if you don't care about your equipment's uptime
    /// </summary>
    public class PreModernRations : ItemBase
    {
        public override bool Enabled => PreModernRations_Enabled.Value;
        public override ItemDef ConversionItemDef => Addressables.LoadAssetAsync<ItemDef>("RoR2/DLC3/Items/BarrierOnCooldown/BarrierOnCooldown.asset").WaitForCompletion();
        public override GameObject itemPrefab => OverwritePrefabMaterials();
        public Material material0 => Addressables.LoadAssetAsync<Material>("RoR2/Base/artifactworld/matArtifactBloody.mat").WaitForCompletion();
        public Material material1 => Addressables.LoadAssetAsync<Material>("RoR2/Base/artifactworld/matArtifactBloody.mat").WaitForCompletion();
        public override Sprite itemIcon => Main.Assets.LoadAsset<Sprite>("Assets/icons/preModernRations.png");

        public PreModernRations(string _name, ItemTag[] _tags, ItemTier _tier, bool _canRemove = true, bool _isConsumed = false, bool _hidden = false) :
        base(_name, _tags, _tier, _canRemove, _isConsumed, _hidden)
        { }

        // Config
        public static ConfigItem<bool> PreModernRations_Enabled = new ConfigItem<bool>
        (
            "Void common: Pre Modern Rations",
            "Item enabled",
            "Should this item appear in runs?",
            true
        );
        public static ConfigItem<float> PreModernRations_CooldownMultiplier = new ConfigItem<float>
        (
            "Void common: Pre Modern Rations",
            "Equipment cooldown multiplier",
            "Multiply equipment cooldown by this much for each stack.",
            0.93f,
            0.5f,
            1f,
            0.01f
        );
        public static ConfigItem<float> PreModernRations_Barrier = new ConfigItem<float>
        (
            "Void common: Pre Modern Rations",
            "Barrier on equipment activation",
            "Flat barrier gained on equipment activation.",
            120f,
            1f,
            240f,
            1f
        );
        public static ConfigItem<bool> PreModernRations_Recipe = new ConfigItem<bool>
        (
            "Void common: Pre Modern Rations",
            "Recipe enabled",
            "Should this item have a custom corruption recipe?",
            true
        );
        public static ConfigItem<string> PreModernRations_Ingredient1 = new ConfigItem<string>
        (
            "Void common: Pre Modern Rations",
            "Recipe ingredient 1",
            "First ingredient for corruption recipe",
            "BarrierOnCooldown"
        );
        public static ConfigItem<string> PreModernRations_Ingredient2 = new ConfigItem<string>
        (
            "Void common: Pre Modern Rations",
            "Recipe ingredient 2",
            "Second ingredient for corruption recipe",
            "Wellies"
        );

        public GameObject OverwritePrefabMaterials()
        {
            GameObject ret = Main.Assets.LoadAsset<GameObject>("Assets/prefabs/preModernRations.prefab");

            Material[] materials =
            {
                material0,
                material1
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
                    PreModernRations_Barrier.Value,
                    Mathf.CeilToInt((1f - PreModernRations_CooldownMultiplier.Value) * 100f)
                )
            );
        }

        // Hooks
        public override void RegisterHooks()
        {
            // Equipment barrier trigger
            On.RoR2.EquipmentSlot.PerformEquipmentAction += (orig, self, equipmentDef) =>
            {
                if (self.equipmentDisabled)
                {
                    return false;
                }

                if (self.characterBody && self.characterBody.healthComponent && GetItemCountEffective(self.characterBody) > 0)
                {
                    self.characterBody.healthComponent.AddBarrier(PreModernRations_Barrier.Value);
                }

                return orig(self, equipmentDef);
            };

            // Cooldown decrease
            On.RoR2.Inventory.CalculateEquipmentCooldownScale += (orig, self) =>
            {
                float result = orig(self);

                if (self.GetItemCountEffective(ItemIndex) > 0)
                {
                    result *= Mathf.Pow(PreModernRations_CooldownMultiplier.Value, self.GetItemCountEffective(ItemIndex));
                }

                return result;
            };
        }

        // Recipes
        public override void AddCorruptionRecipe()
        {
            if (PreModernRations_Recipe.Value == true)
            {
                ItemInit.MakeCorruptionRecipe
                (
                    PreModernRations_Ingredient1.Value,
                    PreModernRations_Ingredient2.Value,
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
                        childName = "Pelvis",
                        localPos = new Vector3(-0.22966F, -0.00001F, 0F),
                        localAngles = new Vector3(0F, 0F, 0F),
                        localScale = new Vector3(0.3793F, 0.3793F, 0.3793F)
                    }
                }
            );
            rules.Add("mdlHuntress", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "ThighR",
                        localPos = new Vector3(-0.1571F, 0.12805F, 0.0688F),
                        localAngles = new Vector3(0F, 0F, 0F),
                        localScale = new Vector3(0.35579F, 0.35579F, 0.35579F)
                    }
                }
            );
            rules.Add("mdlBandit2", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "MainWeapon",
                        localPos = new Vector3(0.09775F, 0.38544F, -0.00005F),
                        localAngles = new Vector3(0F, 0F, 0F),
                        localScale = new Vector3(0.66131F, 0.43897F, 0.43897F)
                    }
                }
            );
            rules.Add("mdlToolbot", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Chest",
                        localPos = new Vector3(1.17309F, -0.52145F, -0.00003F),
                        localAngles = new Vector3(0F, 0F, 0F),
                        localScale = new Vector3(3.71708F, 3.71708F, 3.71708F)
                    }
                }
            );
            rules.Add("mdlEngi", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Pelvis",
                        localPos = new Vector3(-0.2807F, 0.06793F, 0F),
                        localAngles = new Vector3(0F, 0F, 0F),
                        localScale = new Vector3(0.4663F, 0.4663F, 0.4663F)
                    }
                }
            );
            rules.Add("mdlMage", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "UpperArmL",
                        localPos = new Vector3(0.07707F, 0.07311F, -0.00001F),
                        localAngles = new Vector3(0F, 0F, 0F),
                        localScale = new Vector3(0.33687F, 0.33687F, 0.33687F)
                    }
                }
            );
            rules.Add("mdlMerc", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Chest",
                        localPos = new Vector3(0F, -0.00831F, 0.01484F),
                        localAngles = new Vector3(0F, 0F, 0F),
                        localScale = new Vector3(1.56962F, 0.95237F, 1.18011F)
                    }
                }
            );
            rules.Add("mdlTreebot", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "PlatformBase",
                        localPos = new Vector3(0.69552F, -0.00002F, 0F),
                        localAngles = new Vector3(0F, 0F, 0F),
                        localScale = new Vector3(1F, 1F, 1F)
                    }
                }
            );
            rules.Add("mdlLoader", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "MechLowerArmR",
                        localPos = new Vector3(0.00001F, 0.23015F, -0.00001F),
                        localAngles = new Vector3(0F, 270.0261F, 0F),
                        localScale = new Vector3(0.38812F, 0.38812F, 0.38812F)
                    }
                }
            );
            rules.Add("mdlCroco", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "LowerArmR",
                        localPos = new Vector3(-0.00004F, 3.45236F, 0.00038F),
                        localAngles = new Vector3(0F, 0F, 0F),
                        localScale = new Vector3(8.48733F, 4.63253F, 5.40147F)
                    }
                }
            );
            rules.Add("mdlCaptain", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "LowerArmL",
                        localPos = new Vector3(-0.0762F, 0.14571F, -0.00528F),
                        localAngles = new Vector3(0F, 0F, 6.89104F),
                        localScale = new Vector3(0.38232F, 0.38232F, 0.38232F)
                    }
                }
            );
            rules.Add("mdlRailGunner", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "ThighR",
                        localPos = new Vector3(-0.11381F, -0.08035F, 0.00001F),
                        localAngles = new Vector3(0F, 0F, 0F),
                        localScale = new Vector3(0.42495F, 0.42495F, 0.42495F)
                    }
                }
            );
            rules.Add("mdlVoidSurvivor", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "ThighL",
                        localPos = new Vector3(-0.00653F, 0.12465F, -0.13107F),
                        localAngles = new Vector3(0F, 288.0034F, 0F),
                        localScale = new Vector3(0.39145F, 0.39145F, 0.39145F)
                    }
                }
            );
            rules.Add("mdlSeeker", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "LowerArmR",
                        localPos = new Vector3(-0.00001F, 0.15629F, 0F),
                        localAngles = new Vector3(0F, 0F, 0F),
                        localScale = new Vector3(0.47622F, 0.40637F, 0.40637F)
                    }
                }
            );
            rules.Add("mdlFalseSon", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "ClavR",
                        localPos = new Vector3(-0.00001F, 0.88057F, 0.32105F),
                        localAngles = new Vector3(1.9716F, 275.1638F, 34.93844F),
                        localScale = new Vector3(0.65992F, 0.65992F, 0.65992F)
                    }
                }
            );
            rules.Add("mdlChef", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Chest",
                        localPos = new Vector3(-0.01962F, -0.1967F, -0.42322F),
                        localAngles = new Vector3(66.35584F, 9.1197F, 99.93943F),
                        localScale = new Vector3(0.74881F, 0.74881F, 0.74881F)
                    }
                }
            );
            rules.Add("mdlDroneTech", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Gun",
                        localPos = new Vector3(0.00001F, -0.18407F, 0.11887F),
                        localAngles = new Vector3(0F, 0F, 0F),
                        localScale = new Vector3(0.43347F, 0.43347F, 0.43347F)
                    }
                }
            );
            rules.Add("mdlDrifter", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "ThighL",
                        localPos = new Vector3(-0.24819F, -0.04988F, 0.01246F),
                        localAngles = new Vector3(0F, 0F, 0F),
                        localScale = new Vector3(0.47239F, 0.47239F, 0.47239F)
                    }
                }
            );
            #endregion

            return rules;
        }
    }
}