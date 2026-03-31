using RoR2;
using Nautilus.Configuration;
using System;
using R2API;
using UnityEngine;
using UnityEngine.AddressableAssets;
using HarmonyLib;
using System.Linq;
using System.Collections.Generic;

namespace Nautilus.Items
{
    public static partial class ItemInit
    {
        public static PaleStar PaleStar = new PaleStar
        (
            "PaleStar",
            [ItemTag.Utility, ItemTag.ExtractorUnitBlacklist, ItemTag.AIBlacklist],
            ItemTier.VoidTier2
        );
    }

    /// <summary>
    ///     // Ver.1
    ///     Sale Star gives you a free item per stage (and a free legendary on stage 4), but this void version instead gives you much more choice in what you get
    ///     Pale Star's potentials will contain an increasingly large list of the available void items, letting you choose your void build much more precisely
    ///     Recharging via void cradle slightly nerfs E8 by encouraging you to take the 50% damage early, and makes void seeds a little more exciting
    /// </summary>
    public class PaleStar : ItemBase
    {
        public override bool Enabled => PaleStar_Enabled.Value;
        public override ItemDef ConversionItemDef => Addressables.LoadAssetAsync<ItemDef>("RoR2/DLC2/Items/LowerPricedChests/LowerPricedChests.asset").WaitForCompletion();
        public ItemDef ConversionItemDefConsumed => Addressables.LoadAssetAsync<ItemDef>("RoR2/DLC2/Items/LowerPricedChests/LowerPricedChestsConsumed.asset").WaitForCompletion();
        public override GameObject itemPrefab => OverwritePrefabMaterials();
        public Material material0 => Addressables.LoadAssetAsync<Material>("RoR2/DLC1/TreasureCacheVoid/matKeyVoid.mat").WaitForCompletion();
        public Material material1 => Addressables.LoadAssetAsync<Material>("RoR2/DLC1/voidstage/matVoidCoral.mat").WaitForCompletion();
        public override Sprite itemIcon => Main.Assets.LoadAsset<Sprite>("Assets/icons/paleStar.png");
        public GameObject potentialPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/OptionPickup/OptionPickup.prefab").WaitForCompletion();
        public GameObject chestKillPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/TreasureCacheVoid/VoidCacheOpenExplosion.prefab").WaitForCompletion();
        private ItemDef _consumedItemDef;
        public ItemDef ConsumedItemDef
        {
            get
            {
                if (!_consumedItemDef)
                {
                    _consumedItemDef = ItemCatalog.GetItemDef(ItemCatalog.FindItemIndex("PaleStarConsumed"));
                }

                return _consumedItemDef;
            }
        }

        public PaleStar(string _name, ItemTag[] _tags, ItemTier _tier, bool _canRemove = true, bool _isConsumed = false, bool _hidden = false) : 
        base(_name, _tags, _tier, _canRemove, _isConsumed, _hidden){}

        // Config
        public static ConfigItem<bool> PaleStar_Enabled = new ConfigItem<bool>
        (
            "Void uncommon: Pale Star",
            "Item enabled",
            "Should this item appear in runs?",
            true
        );
        public static ConfigItem<int> PaleStar_Choices = new ConfigItem<int>
        (
            "Void uncommon: Pale Star",
            "Void potential choices",
            "Amount of choices offered by Pale Star's void potential.",
            2,
            1f,
            6f,
            1f
        );
        public static ConfigItem<int> PaleStar_ChoicesStack = new ConfigItem<int>
        (
            "Void uncommon: Pale Star",
            "Void potential choices (per stack)",
            "Amount of choices offered by Pale Star's void potential, per additional stack.",
            1,
            1f,
            3f,
            1f
        );
        public static ConfigItem<bool> PaleStar_Recipe = new ConfigItem<bool>
        (
            "Void uncommon: Pale Star",
            "Recipe enabled",
            "Should this item have a custom corruption recipe?",
            true
        );
        public static ConfigItem<string> PaleStar_Ingredient1 = new ConfigItem<string>
        (
            "Void uncommon: Pale Star",
            "Recipe ingredient 1",
            "First ingredient for corruption recipe",
            "LowerPricedChests"
        );
        public static ConfigItem<string> PaleStar_Ingredient2 = new ConfigItem<string>
        (
            "Void uncommon: Pale Star",
            "Recipe ingredient 2",
            "Second ingredient for corruption recipe",
            "TreasureCacheVoid"
        );

        public GameObject OverwritePrefabMaterials()
        {
            GameObject ret = Main.Assets.LoadAsset<GameObject>("Assets/prefabs/paleStar.prefab");

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
                    PaleStar_Choices.Value,
                    PaleStar_ChoicesStack.Value
                )
            );
        }

        // Hooks
        public override void RegisterHooks()
        {
            // Additional void conversion
            ItemDef.Pair transformation = new()
            {
                itemDef1 = ConversionItemDefConsumed,
                itemDef2 = ItemDef
            };
            Main.ItemConversionList.Add(transformation);

            Log.Info(String.Format("Added void conversion from {0} to {1}", ConversionItemDefConsumed.name, ItemDef.name));

            // Behavior
            On.RoR2.GlobalEventManager.OnInteractionBegin += (orig, self, interactor, interactable, interactableObject) =>
            {
                ChestBehavior chestBehavior = interactableObject.GetComponent<ChestBehavior>();
                CharacterBody characterBody = interactor.GetComponent<CharacterBody>();
                PurchaseInteraction purchaseInteraction = interactableObject.GetComponent<PurchaseInteraction>();

                // Regenerate on void cradle usage
                if (interactableObject.name.ToLower().Contains("voidchest") && characterBody && characterBody.inventory && characterBody.master)
                {
                    int permCount = characterBody.inventory.GetItemCountPermanent(ConsumedItemDef);
                    int tempCount = characterBody.inventory.GetItemCountEffective(ConsumedItemDef) - permCount;

                    if (tempCount > 0)
                    {
                        characterBody.inventory.RemoveItemTemp(ConsumedItemDef.itemIndex, tempCount);
                        characterBody.inventory.GiveItemTemp(ItemIndex, tempCount);
                    }
                    if (permCount > 0)
                    {
                        characterBody.inventory.RemoveItemPermanent(ConsumedItemDef.itemIndex, permCount);
                        characterBody.inventory.GiveItemPermanent(ItemIndex, permCount);

                        CharacterMasterNotificationQueue.SendTransformNotification(characterBody.master, ConsumedItemDef.itemIndex, ItemIndex, CharacterMasterNotificationQueue.TransformationType.Default);
                    }
                }
                
                // Create void potential
                if 
                (
                    chestBehavior 
                    && purchaseInteraction
                    && purchaseInteraction.costType == CostTypeIndex.Money
                    && purchaseInteraction.saleStarCompatible
                    && !interactableObject.name.ToLower().Contains("equip")
                    && characterBody
                    && characterBody.master
                    && GetItemCountEffective(characterBody) > 0
                )
                {
                    int itemCount = GetItemCountEffective(characterBody);
                    List<PickupIndex> voidedDrops = new();
                    List<UniquePickup> voidedDropsUnique = new();
                    PickupDef currentPickupDef = null;

                    List<UniquePickup> generatedDropsList = new List<UniquePickup>(); // why
                    chestBehavior.dropTable.GenerateDistinctPickups(generatedDropsList, PaleStar_Choices.Value + (PaleStar_ChoicesStack.Value * (itemCount - 1)), chestBehavior.rng);
                    int dropAmount = 0;

                    List<PickupIndex> voidTier1Indices = Run.instance.availableVoidTier1DropList;
                    Util.ShuffleList(voidTier1Indices);
                    List<PickupIndex> voidTier2Indices = Run.instance.availableVoidTier2DropList;
                    Util.ShuffleList(voidTier2Indices);
                    List<PickupIndex> voidTier3Indices = Run.instance.availableVoidTier3DropList;
                    Util.ShuffleList(voidTier3Indices);

                    foreach (UniquePickup uniquePickup in generatedDropsList)
                    {
                        PickupDef innerPickupDef = PickupCatalog.GetPickupDef(uniquePickup.pickupIndex);
                        if (innerPickupDef != null && innerPickupDef.itemIndex != ItemIndex.None && ItemCatalog.GetItemDef(innerPickupDef.itemIndex))
                        {
                            ItemDef innerItemDef = ItemCatalog.GetItemDef(PickupCatalog.GetPickupDef(uniquePickup.pickupIndex).itemIndex);
                            if (!innerItemDef.tags.Contains(ItemTag.WorldUnique))
                            {
                                switch(innerItemDef.tier)
                                {
                                    case ItemTier.Tier2:
                                        if (voidTier2Indices.Count > 0)
                                        {
                                            currentPickupDef = voidTier2Indices.First().pickupDef;
                                            voidTier2Indices.RemoveAt(0);
                                        }
                                        break;
                                    case ItemTier.Tier3:
                                        if (voidTier3Indices.Count > 0)
                                        {
                                            currentPickupDef = voidTier3Indices.First().pickupDef;
                                            voidTier3Indices.RemoveAt(0);
                                        }
                                        break;
                                    case ItemTier.Tier1:
                                    default: 
                                        if (voidTier1Indices.Count > 0)
                                        {
                                            currentPickupDef = voidTier1Indices.First().pickupDef;
                                            voidTier1Indices.RemoveAt(0);
                                        }
                                        break;
                                }

                                if (currentPickupDef != null)
                                {
                                    voidedDrops.Add(currentPickupDef.pickupIndex);
                                    voidedDropsUnique.Add(new UniquePickup(currentPickupDef.pickupIndex));
                                    dropAmount++;
                                }
                            }
                        }
                    }

                    if (dropAmount != 0)
                    {
                        PickupDropletController.CreatePickupDroplet
                        (
                            new GenericPickupController.CreatePickupInfo
                            {
                                pickerOptions = PickupPickerController.GenerateOptionsFromList(voidedDropsUnique), // violence
                                prefabOverride = potentialPrefab,
                                position = chestBehavior.dropTransform.position,
                                rotation = Quaternion.identity,
                                pickupIndex = PickupCatalog.FindPickupIndex(ItemTier.VoidTier1)
                            }, 
                            chestBehavior.dropTransform.position, 
                            Vector3.up * chestBehavior.dropUpVelocityStrength + chestBehavior.dropTransform.forward * chestBehavior.dropForwardVelocityStrength
                        );

                        EffectData effectData = new EffectData
                        {
                            origin = chestBehavior.transform.position
                        };
                        EffectManager.SpawnEffect(chestKillPrefab, effectData, false);

                        int permCount = characterBody.inventory.GetItemCountPermanent(ItemDef);
                        int tempCount = characterBody.inventory.GetItemCountEffective(ItemDef) - permCount;

                        if (tempCount > 0)
                        {
                            characterBody.inventory.RemoveItemTemp(ItemIndex, tempCount);
                            characterBody.inventory.GiveItemTemp(ConsumedItemDef.itemIndex, tempCount);
                        }
                        if (permCount > 0)
                        {
                            characterBody.inventory.RemoveItemPermanent(ItemIndex, permCount);
                            characterBody.inventory.GiveItemPermanent(ConsumedItemDef.itemIndex, permCount);

                            CharacterMasterNotificationQueue.SendTransformNotification(characterBody.master, ItemIndex, ConsumedItemDef.itemIndex, CharacterMasterNotificationQueue.TransformationType.Default);
                        }

                        UnityEngine.Object.Destroy(interactableObject);
                    }
                }
                else
                {
                    orig(self, interactor, interactable, interactableObject);
                }
            };
        }
        
        // Recipe
        public override void AddCorruptionRecipe()
        {
            if (PaleStar_Recipe.Value == true)
            {
                ItemInit.MakeCorruptionRecipe
                (
                    PaleStar_Ingredient1.Value,
                    PaleStar_Ingredient2.Value,
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
    }
}