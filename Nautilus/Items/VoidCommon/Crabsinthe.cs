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
        public static Crabsinthe Crabsinthe = new Crabsinthe
        (
            "Crabsinthe",
            [ItemTag.Healing],
            ItemTier.VoidTier1
        );
    }

    /// <summary>
    ///     // Ver.1
    ///     Crabsinthe is another iteration of the 'simple regen item' from Many Other Mods, but hey I wanted to make one too
    ///     The catch is that it corrupts both active and consumed elixirs, making the empty bottles useful and letting you stack up a lot of bonus regen
    ///     Helps counteract Monsoon regen reduction and synergizes with items that aren't often interactable like slug & knurl.
    /// </summary>
    public class Crabsinthe : ItemBase
    {
        public override bool Enabled => Crabsinthe_Enabled.Value;
        public override ItemDef ConversionItemDef => Addressables.LoadAssetAsync<ItemDef>("RoR2/DLC1/HealingPotion/HealingPotion.asset").WaitForCompletion();
        public override GameObject itemPrefab => OverwritePrefabMaterials();
        public override Sprite itemIcon => Main.Assets.LoadAsset<Sprite>("Assets/icons/crabsinthe.png");
        public ItemDef ConversionItemDefConsumed => Addressables.LoadAssetAsync<ItemDef>("RoR2/DLC1/HealingPotion/HealingPotionConsumed.asset").WaitForCompletion();
        public Material material0 => Addressables.LoadAssetAsync<Material>("RoR2/DLC1/HealingPotion/matHealingPotionGlass.mat").WaitForCompletion();
        public Material material1 => Addressables.LoadAssetAsync<Material>("RoR2/DLC1/VoidSurvivor/matVoidSurvivorBlasterSphereAreaIndicator.mat").WaitForCompletion();
        public Material material2 => Addressables.LoadAssetAsync<Material>("RoR2/DLC1/TreasureCacheVoid/matLockboxVoidEgg.mat").WaitForCompletion();
        public Material material4 => Addressables.LoadAssetAsync<Material>("RoR2/DLC1/VoidRaidCrab/matVoidRaidCrabEyeOverlay2.mat").WaitForCompletion();

        public Crabsinthe(string _name, ItemTag[] _tags, ItemTier _tier, bool _canRemove = true, bool _isConsumed = false, bool _hidden = false) : 
        base(_name, _tags, _tier, _canRemove, _isConsumed, _hidden){}

        // Config
        public static ConfigItem<bool> Crabsinthe_Enabled = new ConfigItem<bool>
        (
            "Void common: Crabsinthe",
            "Item enabled",
            "Should this item appear in runs?",
            true
        );
        public static ConfigItem<float> Crabsinthe_Regen = new ConfigItem<float>
        (
            "Void common: Crabsinthe",
            "Regen boost",
            "Grants a regen boost with this multiplier.",
            0.5f,
            0f,
            6f,
            0.1f
        );
        public static ConfigItem<float> Crabsinthe_RegenStack = new ConfigItem<float>
        (
            "Void common: Crabsinthe",
            "Regen boost (Per stack)",
            "Grants a regen boost with this multiplier per additional stack.",
            0.5f,
            0f,
            6f,
            0.1f
        );
        public static ConfigItem<bool> Crabsinthe_CorruptBottles = new ConfigItem<bool>
        (
            "Void common: Crabsinthe",
            "Corrupt empty bottles",
            "Should this item corrupt consumed elixirs?",
            true
        );
        public static ConfigItem<bool> Crabsinthe_Recipe = new ConfigItem<bool>
        (
            "Void common: Crabsinthe",
            "Recipe enabled",
            "Should this item have a custom corruption recipe?",
            true
        );
        public static ConfigItem<string> Crabsinthe_Ingredient1 = new ConfigItem<string>
        (
            "Void common: Crabsinthe",
            "Recipe ingredient 1",
            "First ingredient for corruption recipe",
            "HealingPotion"
        );
        public static ConfigItem<string> Crabsinthe_Ingredient2 = new ConfigItem<string>
        (
            "Void common: Crabsinthe",
            "Recipe ingredient 2",
            "Second ingredient for corruption recipe",
            "MushroomVoid"
        );

        public GameObject OverwritePrefabMaterials()
        {
            GameObject ret = Main.Assets.LoadAsset<GameObject>("Assets/prefabs/crabsinthe.prefab");

            Material[] materials =
            {
                material0,
                material1,
                material2,
                ret.GetComponentInChildren<MeshRenderer>().GetMaterialArray()[3],
                material4
            };
            ret.GetComponentInChildren<MeshRenderer>().SetMaterialArray(materials);

            return ret;
        }

        // Tokens
        public override void FormatDescriptionTokens()
        {
            string descriptionToken = ItemDef.descriptionToken;
            string extraConversionDesc = Crabsinthe_CorruptBottles.Value == true? " and Empty Bottles" : "";

            LanguageAPI.AddOverlay
            (
                descriptionToken,
                String.Format
                (
                    Language.currentLanguage.GetLocalizedStringByToken(descriptionToken),
                    Crabsinthe_Regen.Value * 100f,
                    Crabsinthe_RegenStack.Value * 100f,
                    extraConversionDesc
                )
            );
        }

        // Hooks
        public override void RegisterHooks()
        {
            // Additional void conversion
            if (Crabsinthe_CorruptBottles.Value)
            {
                ItemDef.Pair transformation = new()
                {
                    itemDef1 = ConversionItemDefConsumed,
                    itemDef2 = ItemDef
                };
                Main.ItemConversionList.Add(transformation);

                Log.Info(String.Format("Added void conversion from {0} to {1}", ConversionItemDefConsumed.name, ItemDef.name));
            }

            // Regen boost
            RecalculateStatsAPI.GetStatCoefficients += (orig, self) =>
            {
                int itemCount = GetItemCountEffective(orig);
                if (itemCount > 0)
                {
                    self.regenMultAdd += Crabsinthe_Regen.Value + (Crabsinthe_RegenStack.Value * (itemCount - 1));
                }
            };
        }

        // Recipes
        public override void AddCorruptionRecipe()
        {
            if (Crabsinthe_Recipe.Value == true)
            {
                ItemInit.MakeCorruptionRecipe
                (
                    Crabsinthe_Ingredient1.Value,
                    Crabsinthe_Ingredient2.Value,
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
                        localPos = new Vector3(0.22006F, 0.01029F, -0.03222F),
                        localAngles = new Vector3(342.1314F, 263.4621F, 143.145F),
                        localScale = new Vector3(0.29222F, 0.29222F, 0.29222F)
                    }
                }
            );
            rules.Add("mdlHuntress", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Pelvis",
                        localPos = new Vector3(0.22006F, 0.01029F, -0.03222F),
                        localAngles = new Vector3(342.1314F, 263.4621F, 143.145F),
                        localScale = new Vector3(0.29222F, 0.29222F, 0.29222F)
                    }
                }
            );
            rules.Add("mdlBandit2", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Pelvis",
                        localPos = new Vector3(0.22006F, 0.01029F, -0.03222F),
                        localAngles = new Vector3(342.1314F, 263.4621F, 143.145F),
                        localScale = new Vector3(0.29222F, 0.29222F, 0.29222F)
                    }
                }
            );
            rules.Add("mdlToolbot", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "LowerArmL",
                        localPos = new Vector3(-3.13168F, 0.39244F, -0.15114F),
                        localAngles = new Vector3(297.6138F, 184.4109F, 212.2324F),
                        localScale = new Vector3(1.98902F, 1.98902F, 1.98902F)
                    }
                }
            );
            rules.Add("mdlEngi", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Pelvis",
                        localPos = new Vector3(0.2742F, 0.05734F, 0.01088F),
                        localAngles = new Vector3(338.9652F, 266.8782F, 132.2899F),
                        localScale = new Vector3(0.29222F, 0.29222F, 0.29222F)
                    }
                }
            );
            rules.Add("mdlMage", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Pelvis",
                        localPos = new Vector3(0.17946F, 0.01028F, -0.03222F),
                        localAngles = new Vector3(342.1314F, 263.4621F, 143.145F),
                        localScale = new Vector3(0.29222F, 0.29222F, 0.29222F)
                    }
                }
            );
            rules.Add("mdlMerc", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "UpperArmR",
                        localPos = new Vector3(-0.19241F, 0.25936F, -0.00967F),
                        localAngles = new Vector3(314.5362F, 254.9864F, 127.1974F),
                        localScale = new Vector3(0.29222F, 0.29222F, 0.29222F)
                    }
                }
            );
            rules.Add("mdlTreebot", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "PlatformBase",
                        localPos = new Vector3(-0.28089F, 0.49833F, -0.78794F),
                        localAngles = new Vector3(18.34931F, 274.9786F, 318.1312F),
                        localScale = new Vector3(0.46942F, 0.46942F, 0.46942F)
                    }
                }
            );
            rules.Add("mdlLoader", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Pelvis",
                        localPos = new Vector3(0.22006F, 0.01029F, -0.03222F),
                        localAngles = new Vector3(342.1314F, 263.4621F, 143.145F),
                        localScale = new Vector3(0.29222F, 0.29222F, 0.29222F)
                    }
                }
            );
            rules.Add("mdlCroco", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Pelvis",
                        localPos = new Vector3(1.96845F, 0.58616F, 0.07431F),
                        localAngles = new Vector3(336.642F, 239.765F, 187.3825F),
                        localScale = new Vector3(2.2141F, 2.2141F, 2.2141F)
                    }
                }
            );
            rules.Add("mdlCaptain", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Pelvis",
                        localPos = new Vector3(-0.41247F, -0.05317F, 0.15905F),
                        localAngles = new Vector3(351.2866F, 264.4579F, 120.5038F),
                        localScale = new Vector3(0.33326F, 0.33326F, 0.33326F)
                    }
                }
            );
            rules.Add("mdlRailGunner", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Pelvis",
                        localPos = new Vector3(0.1722F, 0.10773F, -0.00286F),
                        localAngles = new Vector3(324.0815F, 269.0406F, 128.8251F),
                        localScale = new Vector3(0.29222F, 0.29222F, 0.29222F)
                    }
                }
            );
            rules.Add("mdlVoidSurvivor", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "ForeArmR",
                        localPos = new Vector3(0.03669F, -0.17884F, -0.04173F),
                        localAngles = new Vector3(342.1314F, 263.4621F, 143.145F),
                        localScale = new Vector3(0.29222F, 0.29222F, 0.29222F)
                    }
                }
            );
            rules.Add("mdlSeeker", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Pelvis",
                        localPos = new Vector3(0.29005F, 0.00948F, -0.03139F),
                        localAngles = new Vector3(316.0573F, 78.27985F, 129.4233F),
                        localScale = new Vector3(0.29222F, 0.29222F, 0.29222F)
                    }
                }
            );
            rules.Add("mdlFalseSon", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Chest",
                        localPos = new Vector3(0.13337F, 0.60202F, -1.07796F),
                        localAngles = new Vector3(354.3503F, 245.6798F, 188.1472F),
                        localScale = new Vector3(0.43405F, 0.43405F, 0.43405F)
                    }
                }
            );
            rules.Add("mdlChef", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Chest",
                        localPos = new Vector3(0.12288F, -0.49027F, 0.21384F),
                        localAngles = new Vector3(293.7258F, 146.1052F, 269.1249F),
                        localScale = new Vector3(0.38783F, 0.38783F, 0.38783F)
                    }
                }
            );
            rules.Add("mdlDroneTech", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "ThighL",
                        localPos = new Vector3(-0.10692F, -0.17225F, -0.09653F),
                        localAngles = new Vector3(46.51857F, 187.6929F, 45.30641F),
                        localScale = new Vector3(0.29222F, 0.29222F, 0.29222F)
                    }
                }
            );
            rules.Add("mdlDrifter", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Pelvis",
                        localPos = new Vector3(0.02914F, 0.13215F, -0.43569F),
                        localAngles = new Vector3(288.0392F, 100.3947F, 318.3475F),
                        localScale = new Vector3(0.29222F, 0.29222F, 0.29222F)
                    }
                }
            );
            #endregion

            return rules;
        }
    }
}