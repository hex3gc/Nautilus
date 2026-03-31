using RoR2;
using Nautilus.Configuration;
using System;
using R2API;
using UnityEngine;
using UnityEngine.AddressableAssets;
using HarmonyLib;
using System.Collections.Generic;

namespace Nautilus.Items
{
    public static partial class ItemInit
    {
        public static Rebirth Rebirth = new Rebirth
        (
            "Rebirth",
            [ItemTag.Utility, ItemTag.Healing, ItemTag.AIBlacklist, ItemTag.BrotherBlacklist, ItemTag.CannotSteal, ItemTag.ExtractorUnitBlacklist],
            ItemTier.VoidBoss
        );
    }

    /// <summary>
    ///     // Ver.1
    ///     It was hard to think of a regen niche that hasn't been touched yet, so I decided to combine a regen boost with something that shakes up the run
    ///     Literally just a re-implementation of Corrupting Parasite, but I think this fits far better into the item pool
    ///     Big indirect buff to Crabsinthe as you're pretty likely to get one from a stage corruption
    ///     // Ver.2
    ///     Corrupting on pickup offsets the fact that Grandparents only spawn on Stage 5; if you're going straight to Mithrix, this gives you a guaranteed 5 void item corruptions
    /// </summary>
    public class Rebirth : ItemBase
    {
        public override bool Enabled => Rebirth_Enabled.Value;
        public override ItemDef ConversionItemDef => Addressables.LoadAssetAsync<ItemDef>("RoR2/Base/ParentEgg/ParentEgg.asset").WaitForCompletion();
        public override GameObject itemPrefab => OverwritePrefabMaterials();
        public override Sprite itemIcon => Main.Assets.LoadAsset<Sprite>("Assets/icons/rebirth.png");
        public Material material0 => Addressables.LoadAssetAsync<Material>("RoR2/DLC1/voidstage/matVoidCoral.mat").WaitForCompletion();
        public Material material1 => Addressables.LoadAssetAsync<Material>("RoR2/Base/Brother/matBrotherInfectionVoid.mat").WaitForCompletion();
        public Material material2 => Addressables.LoadAssetAsync<Material>("RoR2/Base/Brother/matBrotherInfectionVoid.mat").WaitForCompletion();
        public Material material3 => Addressables.LoadAssetAsync<Material>("RoR2/DLC1/BearVoid/matBearVoidShieldStars.mat").WaitForCompletion();
        private ExplicitPickupDropTable _explicitPickupDropTable;
        public ExplicitPickupDropTable explicitPickupDropTable
        {
            get
            {
                if (_explicitPickupDropTable == null)
                {
                    _explicitPickupDropTable = ScriptableObject.CreateInstance<ExplicitPickupDropTable>();
                    _explicitPickupDropTable.pickupEntries = new ExplicitPickupDropTable.PickupDefEntry[]
                    {
                        new ExplicitPickupDropTable.PickupDefEntry
                        {
                            pickupDef = ItemDef,
                            pickupWeight = 1
                        }
                    };
                }

                _explicitPickupDropTable.Regenerate(Run.instance);
                return _explicitPickupDropTable;
            }
            set;
        }

        public Rebirth(string _name, ItemTag[] _tags, ItemTier _tier, bool _canRemove = true, bool _isConsumed = false, bool _hidden = false) : 
        base(_name, _tags, _tier, _canRemove, _isConsumed, _hidden){}

        // Config
        public static ConfigItem<bool> Rebirth_Enabled = new ConfigItem<bool>
        (
            "Void boss: Rebirth",
            "Item enabled",
            "Should this item appear in runs?",
            true
        );
        public static ConfigItem<int> Rebirth_ItemsCorrupt = new ConfigItem<int>
        (
            "Void boss: Rebirth",
            "Items to corrupt",
            "Items corrupted at the start of each stage.",
            2,
            1f,
            5f,
            1f
        );
        /*
        public static ConfigItem<int> Rebirth_ItemsCorruptStack = new ConfigItem<int>
        (
            "Void boss: Rebirth",
            "Items to corrupt (per stack)",
            "Items corrupted at the start of each stage, per additional stack.",
            2,
            1f,
            5f,
            1f
        );
        */
        public static ConfigItem<float> Rebirth_RegenPerItem = new ConfigItem<float>
        (
            "Void boss: Rebirth",
            "Regen per void item",
            "Regen in hp/s per void item in your inventory.",
            1f,
            0.5f,
            3f,
            0.5f
        );
        public static ConfigItem<float> Rebirth_RegenPerItemStack = new ConfigItem<float>
        (
            "Void boss: Rebirth",
            "Regen per void item (per stack)",
            "Regen in hp/s per void item in your inventory, per additional stack.",
            0.5f,
            0f,
            3f,
            0.5f
        );
        public static ConfigItem<int> Rebirth_ItemsCorruptPickup = new ConfigItem<int>
        (
            "Void boss: Rebirth",
            "Items to corrupt on pickup",
            "Items corrupted on pickup.",
            3,
            1f,
            6f,
            1f
        );
        public static ConfigItem<bool> Rebirth_Recipe = new ConfigItem<bool>
        (
            "Void boss: Rebirth",
            "Recipe enabled",
            "Should this item have a custom corruption recipe?",
            true
        );
        public static ConfigItem<string> Rebirth_Ingredient1 = new ConfigItem<string>
        (
            "Void boss: Rebirth",
            "Recipe ingredient 1",
            "First ingredient for corruption recipe",
            "ParentEgg"
        );
        public static ConfigItem<string> Rebirth_Ingredient2 = new ConfigItem<string>
        (
            "Void boss: Rebirth",
            "Recipe ingredient 2",
            "Second ingredient for corruption recipe",
            "PaleStar"
        );

        public GameObject OverwritePrefabMaterials()
        {
            GameObject ret = Main.Assets.LoadAsset<GameObject>("Assets/prefabs/rebirth.prefab");

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
                    Rebirth_ItemsCorruptPickup.Value,
                    Rebirth_ItemsCorrupt.Value,
                    Rebirth_RegenPerItem.Value,
                    Rebirth_RegenPerItemStack.Value
                )
            );
        }

        // Hooks
        public override void RegisterHooks()
        {
            // Regen boost
            RecalculateStatsAPI.GetStatCoefficients += (orig, self) =>
            {
                int itemCount = GetItemCountEffective(orig);
                if (itemCount > 0)
                {
                    int voidItemCount = 0;
                    voidItemCount += orig.inventory.GetTotalItemCountOfTier(ItemTier.VoidTier1);
                    voidItemCount += orig.inventory.GetTotalItemCountOfTier(ItemTier.VoidTier2);
                    voidItemCount += orig.inventory.GetTotalItemCountOfTier(ItemTier.VoidTier3);
                    voidItemCount += orig.inventory.GetTotalItemCountOfTier(ItemTier.VoidBoss);

                    self.baseRegenAdd += (Rebirth_RegenPerItem.Value + (Rebirth_RegenPerItemStack.Value * (itemCount - 1))) * voidItemCount;
                }
            };

            // On getting the item, corrupt 3 items
            On.RoR2.Inventory.GiveItemPermanent_ItemIndex_int += (orig, self, itemIndex, count) =>
            {
                int rebirths = 0;
                int rebirthsAfter = 0;

                rebirths = self.GetItemCountPermanent(ItemIndex);

                orig(self, itemIndex, count);

                rebirthsAfter = self.GetItemCountPermanent(ItemIndex);

                if (rebirthsAfter > rebirths && self.gameObject.GetComponentInChildren<CharacterMaster>())
                {
                    CorruptItems(self.gameObject.GetComponentInChildren<CharacterMaster>(), Rebirth_ItemsCorruptPickup.Value);
                }
            };

            // Item transformation
            On.RoR2.CharacterMaster.OnServerStageBegin += (orig, self, stage) =>
            {
                orig(self, stage);
                if (!stage.sceneDef || stage.sceneDef.sceneType == SceneType.Intermission)
                {
                    return;
                }
                if (self.inventory && self.inventory.GetItemCountEffective(ItemDef) > 0)
                {
                    int itemsLeft = Rebirth_ItemsCorrupt.Value;
                    CorruptItems(self, itemsLeft);
                }
            };
        }

        // Recipe
        public override void AddCorruptionRecipe()
        {
            if (Rebirth_Recipe.Value == true && ItemInit.PaleStar.Enabled)
            {
                ItemInit.MakeCorruptionRecipe
                (
                    Rebirth_Ingredient1.Value,
                    Rebirth_Ingredient2.Value,
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

        public void CorruptItems(CharacterMaster characterMaster, int amount)
        {
            Xoroshiro128Plus rng = new Xoroshiro128Plus(Run.instance.stageRng.nextUlong);
            List<ItemIndex> itemList = new List<ItemIndex>(characterMaster.inventory.itemAcquisitionOrder);
            Util.ShuffleList(itemList, rng);
            rng.Next();

            foreach (ItemIndex item in itemList)
            {
                if (amount <= 0)
                {
                    break;
                }
                foreach (ItemDef.Pair pair in ItemCatalog.GetItemPairsForRelationship(DLC1Content.ItemRelationshipTypes.ContagiousItem))
                {
                    if (pair.itemDef1 == ItemCatalog.GetItemDef(item))
                    {
                        Inventory inventory = characterMaster.inventory;

                        Inventory.ItemTransformation itemTransformation = new Inventory.ItemTransformation // thanks prodz
                        {
                            originalItemIndex = ItemCatalog.GetItemDef(item).itemIndex,
                            newItemIndex = ItemCatalog.GetItemDef(pair.itemDef2.itemIndex).itemIndex
                        };

                        if (itemTransformation.TryTake(inventory, out Inventory.ItemTransformation.TakeResult takeResult))
                        {
                            takeResult.GiveTakenItem(inventory, itemTransformation.newItemIndex);
                        }

                        // CharacterMasterNotificationQueue.PushItemTransformNotification(characterMaster, item, pair.itemDef2.itemIndex, CharacterMasterNotificationQueue.TransformationType.ContagiousVoid);
                        amount--;
                        break;
                    }
                }
            }
        }
    }
}