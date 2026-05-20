using RoR2;
using Nautilus.Configuration;
using System;
using R2API;
using UnityEngine;
using UnityEngine.AddressableAssets;
using HarmonyLib;
using UnityEngine.Networking;

namespace Nautilus.Items
{
    public static partial class ItemInit
    {
        public static EffigyOfPride EffigyOfPride = new EffigyOfPride
        (
            "EffigyOfPride",
            [ItemTag.Utility, ItemTag.AIBlacklist, ItemTag.ExtractorUnitBlacklist, ItemTag.BrotherBlacklist, ItemTag.CanBeTemporary],
            ItemTier.VoidTier2
        );
    }

    /// <summary>
    ///     // Ver.1
    ///     Has the same effect of 'upgrading item drops' as the Chance Doll, but in a different way
    ///     Also makes Void Boss items more common
    /// </summary>
    public class EffigyOfPride : ItemBase
    {
        public override bool Enabled => EffigyOfPride_Enabled.Value;
        public override ItemDef ConversionItemDef => Addressables.LoadAssetAsync<ItemDef>("RoR2/DLC2/Items/ExtraShrineItem/ExtraShrineItem.asset").WaitForCompletion();
        public override GameObject itemPrefab => OverwritePrefabMaterials();
        public Material material0 => Addressables.LoadAssetAsync<Material>("RoR2/Base/Squid/matSquidInkTrail.mat").WaitForCompletion();
        public Material material1 => Addressables.LoadAssetAsync<Material>("RoR2/Base/Treebot/matTreebotTreeBark.mat").WaitForCompletion();
        public Material material2 => Addressables.LoadAssetAsync<Material>("RoR2/DLC1/VoidSurvivor/matVoidBlinkBodyOverlay.mat").WaitForCompletion();
        public Material material3 => Addressables.LoadAssetAsync<Material>("RoR2/DLC2/meridian/Assets/matPMGlow.mat").WaitForCompletion();
        public override Sprite itemIcon => Main.Assets.LoadAsset<Sprite>("Assets/icons/effigyOfPride.png");
        private GameObject _glintPrefab;
        public GameObject GlintPrefab
        {
            get
            {
                if (_glintPrefab == null)
                {
                    _glintPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Clover/CloverEffect.prefab").WaitForCompletion();
                }
                return _glintPrefab;
            }
            set;
        }

        public EffigyOfPride(string _name, ItemTag[] _tags, ItemTier _tier, bool _canRemove = true, bool _isConsumed = false, bool _hidden = false) :
        base(_name, _tags, _tier, _canRemove, _isConsumed, _hidden)
        { }

        // Config
        public static ConfigItem<bool> EffigyOfPride_Enabled = new ConfigItem<bool>
        (
            "Void uncommon: Effigy of Pride",
            "Item enabled",
            "Should this item appear in runs?",
            true
        );
        public static ConfigItem<float> EffigyOfPride_BossPercent = new ConfigItem<float>
        (
            "Void uncommon: Effigy of Pride",
            "Added boss chance",
            "Fractional chance added to boss item drops after using a Mountain or Deep shrine.",
            0.1f,
            0.01f,
            0.20f,
            0.01f
        );
        public static ConfigItem<float> EffigyOfPride_BossPercentStack = new ConfigItem<float>
        (
            "Void uncommon: Effigy of Pride",
            "Added boss chance (per stack)",
            "Fractional chance added to boss item drops after using a Mountain or Deep shrine, per additional stack.",
            0.1f,
            0.01f,
            0.20f,
            0.01f
        );
        public static ConfigItem<bool> EffigyOfPride_Recipe = new ConfigItem<bool>
        (
            "Void uncommon: Effigy of Pride",
            "Recipe enabled",
            "Should this item have a custom corruption recipe?",
            true
        );
        public static ConfigItem<string> EffigyOfPride_Ingredient1 = new ConfigItem<string>
        (
            "Void uncommon: Effigy of Pride",
            "Recipe ingredient 1",
            "First ingredient for corruption recipe",
            "ExtraShrineItem"
        );
        public static ConfigItem<string> EffigyOfPride_Ingredient2 = new ConfigItem<string>
        (
            "Void uncommon: Effigy of Pride",
            "Recipe ingredient 2",
            "Second ingredient for corruption recipe",
            "CoralCrust"
        );

        public GameObject OverwritePrefabMaterials()
        {
            GameObject ret = Main.Assets.LoadAsset<GameObject>("Assets/prefabs/effigyOfPride.prefab");

            Material[] materials =
            {
                material0,
                material1,
                material2,
                material3
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
                    EffigyOfPride_BossPercent.Value * 100f,
                    EffigyOfPride_BossPercentStack.Value * 100f
                )
            );
        }

        // Hooks
        public override void RegisterHooks()
        {
            // On shrine interaction
            On.RoR2.GlobalEventManager.OnInteractionBegin += (orig, self, interactor, interactable, interactableObject) =>
            {
                orig(self, interactor, interactable, interactableObject);

                if (!interactor || interactable == null || !interactableObject)
                {
                    return;
                }
                
                CharacterBody interactorBody = interactor.GetComponent<CharacterBody>();
                if (interactorBody && GetItemCountEffective(interactorBody) > 0f && TeleporterInteraction.instance)
                {
                    if (interactableObject.name.ToLower().Contains("shrineboss") || interactableObject.name.ToLower().Contains("shrineofthedeep"))
                    {
                        EffectData effectData = new EffectData()
                        {
                            origin = interactableObject.transform.position,
                            scale = 2f
                        };
                        EffectManager.SpawnEffect(GlintPrefab, effectData, true);

                        if (!TeleporterInteraction.instance.gameObject.GetComponent<EffigyOfPrideBehavior>())
                        {
                            TeleporterInteraction.instance.gameObject.AddComponent<EffigyOfPrideBehavior>();
                        }

                        TeleporterInteraction.instance.gameObject.GetComponent<EffigyOfPrideBehavior>().TotalValue += EffigyOfPride_BossPercent.Value + (EffigyOfPride_BossPercentStack.Value * (GetItemCountEffective(interactorBody) - 1));
                    }
                }
            };

            // Adjust boss drop chance
            On.RoR2.BossGroup.DropRewards += (orig, self) =>
            {
                if ((self.bossDrops != null || self.bossDropTables != null) && TeleporterInteraction.instance != null && self.forceTier3Reward != true)
                {
                    EffigyOfPrideBehavior behavior = TeleporterInteraction.instance.gameObject.GetComponent<EffigyOfPrideBehavior>();
                    if (behavior)
                    {
                        self.bossDropChance += behavior.TotalValue;
                    }
                }

                orig(self);
            };
        }

        // Recipes
        public override void AddCorruptionRecipe()
        {
            if (EffigyOfPride_Recipe.Value == true)
            {
                ItemInit.MakeCorruptionRecipe
                (
                    EffigyOfPride_Ingredient1.Value,
                    EffigyOfPride_Ingredient2.Value,
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
                        childName = "Chest",
                        localPos = new Vector3(0.01177F, 0.01059F, -0.25409F),
                        localAngles = new Vector3(13.52315F, 359.1378F, 3.49795F),
                        localScale = new Vector3(0.66596F, 0.66596F, 0.66596F)
                    }
                }
            );
            rules.Add("mdlHuntress", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Chest",
                        localPos = new Vector3(0.14982F, -0.04776F, -0.12569F),
                        localAngles = new Vector3(355.7955F, 323.1131F, 1.43889F),
                        localScale = new Vector3(0.67472F, 0.67472F, 0.67472F)
                    }
                }
            );
            rules.Add("mdlBandit2", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Chest",
                        localPos = new Vector3(-0.01461F, -0.05179F, -0.25931F),
                        localAngles = new Vector3(2.21163F, 9.15637F, 358.2549F),
                        localScale = new Vector3(1F, 1F, 1F)
                    }
                }
            );
            rules.Add("mdlToolbot", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Chest",
                        localPos = new Vector3(0.08012F, 0.63228F, -2.24497F),
                        localAngles = new Vector3(0F, 0F, 0F),
                        localScale = new Vector3(5.24641F, 5.24641F, 5.24641F)
                    }
                }
            );
            rules.Add("mdlEngi", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Chest",
                        localPos = new Vector3(0.00262F, 0.07343F, -0.39939F),
                        localAngles = new Vector3(359.9664F, 3.1656F, 0.70336F),
                        localScale = new Vector3(1F, 1F, 1F)
                    }
                }
            );
            rules.Add("mdlMage", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Head",
                        localPos = new Vector3(-0.01953F, -0.00017F, 0.00848F),
                        localAngles = new Vector3(359.9509F, 8.74932F, 0.06613F),
                        localScale = new Vector3(0.76792F, 0.76792F, 0.76792F)
                    }
                }
            );
            rules.Add("mdlMerc", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Chest",
                        localPos = new Vector3(0.00092F, 0.14579F, -0.29947F),
                        localAngles = new Vector3(359.9892F, 4.47169F, 359.8691F),
                        localScale = new Vector3(1F, 1F, 1F)
                    }
                }
            );
            rules.Add("mdlTreebot", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "PlatformBase",
                        localPos = new Vector3(-1.09172F, 0.69543F, -0.31227F),
                        localAngles = new Vector3(0F, 0F, 0F),
                        localScale = new Vector3(1.56117F, 1.56117F, 1.56117F)
                    }
                }
            );
            rules.Add("mdlLoader", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Chest",
                        localPos = new Vector3(0.00341F, 0.19758F, -0.26222F),
                        localAngles = new Vector3(0F, 0F, 0F),
                        localScale = new Vector3(1.46871F, 1.46871F, 1.46871F)
                    }
                }
            );
            rules.Add("mdlCroco", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Head",
                        localPos = new Vector3(0.00813F, 2.00428F, 1.38981F),
                        localAngles = new Vector3(70.3292F, 0.09413F, 0.71868F),
                        localScale = new Vector3(8.0916F, 8.0916F, 8.0916F)
                    }
                }
            );
            rules.Add("mdlCaptain", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Head",
                        localPos = new Vector3(0F, 0F, 0F),
                        localAngles = new Vector3(359.4716F, 8.90825F, 3.00857F),
                        localScale = new Vector3(1F, 1F, 1F)
                    }
                }
            );
            rules.Add("mdlRailGunner", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Backpack",
                        localPos = new Vector3(-0.02791F, -0.04164F, -0.22107F),
                        localAngles = new Vector3(0F, 0F, 0F),
                        localScale = new Vector3(1F, 1F, 1F)
                    }
                }
            );
            rules.Add("mdlVoidSurvivor", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Chest",
                        localPos = new Vector3(0.03528F, 0.03321F, -0.35455F),
                        localAngles = new Vector3(359.7779F, 0.8718F, 3.58646F),
                        localScale = new Vector3(1.07758F, 1.07758F, 1.07758F)
                    }
                }
            );
            rules.Add("mdlSeeker", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Head",
                        localPos = new Vector3(0.00225F, 0.03499F, 0.04593F),
                        localAngles = new Vector3(0F, 178.0548F, 0F),
                        localScale = new Vector3(1.0717F, 1.0717F, 1.0717F)
                    }
                }
            );
            rules.Add("mdlFalseSon", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "UpperArmL",
                        localPos = new Vector3(0.09428F, 0.2281F, -0.07331F),
                        localAngles = new Vector3(326.2106F, 161.2051F, 190.7176F),
                        localScale = new Vector3(1.43028F, 1.43028F, 1.43028F)
                    }
                }
            );
            rules.Add("mdlChef", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Head",
                        localPos = new Vector3(-0.00919F, 0.02068F, 0.00917F),
                        localAngles = new Vector3(0F, 88.66284F, 0F),
                        localScale = new Vector3(1.07356F, 1.07356F, 1.07356F)
                    }
                }
            );
            rules.Add("mdlDroneTech", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Head",
                        localPos = new Vector3(-0.16372F, -0.12008F, 0.00049F),
                        localAngles = new Vector3(290.0664F, 84.478F, 4.6315F),
                        localScale = new Vector3(1F, 1F, 1F)
                    }
                }
            );
            rules.Add("mdlDrifter", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "UpperArmR",
                        localPos = new Vector3(0F, 0F, 0F),
                        localAngles = new Vector3(0F, 0F, 0F),
                        localScale = new Vector3(1F, 1F, 1F)
                    }
                }
            );
            #endregion

            return rules;
        }
    }

    public class EffigyOfPrideBehavior : NetworkBehaviour
    {
        public float TotalValue = 0f;
    }
}