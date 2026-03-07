using System;
using HarmonyLib;
using IL.RoR2.UI;
using R2API;
using RoR2;
using RoR2.ContentManagement;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Nautilus.Items
{
    /// <summary>
    ///     Abstract item used for initializing new items.
    /// </summary>
    public abstract class ItemBase
    {
        public ItemDef ItemDef;
        public ItemIndex ItemIndex
        {
            get
            {
                return ItemCatalog.FindItemIndex(ItemDef.name);
            }
        }
        public abstract bool Enabled
        {
            get;
        }
        public abstract ItemDef ConversionItemDef
        {
            get;
        }
        public abstract GameObject itemPrefab
        {
            get;
        }
        public abstract Sprite itemIcon
        {
            get;
        }
        public string Name;
        public ItemTag[] Tags;
        public ItemTier Tier;
        public bool CanRemove;
        public bool IsConsumed;
        public bool Hidden;

        public ItemBase(string _name, ItemTag[] _tags, ItemTier _tier, bool _canRemove, bool _isConsumed, bool _hidden)
        {
            Name = _name;
            Tags = _tags;
            Tier = _tier;
            CanRemove = _canRemove;
            IsConsumed = _isConsumed;
            Hidden = _hidden;

            ItemInit.ItemList.Add(this);
        }

        public bool RegisterItem()
        {
            if (!Enabled)
            {
                return Enabled;
            }

            ItemDef = ScriptableObject.CreateInstance<ItemDef>();

            ItemDef.name = Name;
            ItemDef.nameToken = "NT_ITEM_" + Name.ToUpper() + "_NAME";
            ItemDef.pickupToken = "NT_ITEM_"+ Name.ToUpper() + "_PICKUP";
            ItemDef.descriptionToken = "NT_ITEM_" + Name.ToUpper() + "_DESC";
            ItemDef.loreToken = "NT_ITEM_" + Name.ToUpper() + "_LORE";

            ItemDef.tags = Tags;
            ItemDef.tier = Tier;
            ItemDef.deprecatedTier = Tier;
            ItemDef.canRemove = CanRemove;
            ItemDef.isConsumed = IsConsumed;
            ItemDef.hidden = Hidden;
            ItemDef.requiredExpansion = Main.Expansion;

            ItemDef.pickupModelPrefab = itemPrefab;
            ItemDef.pickupIconSprite = itemIcon;

            if (itemPrefab)
            {
                Transform child = itemPrefab.transform.GetChild(0);
                ModelPanelParameters modelPanelParameters = itemPrefab.AddComponent<ModelPanelParameters>();
                modelPanelParameters.minDistance = 1f;
                modelPanelParameters.maxDistance = 2f;
                modelPanelParameters.focusPointTransform = child;
                modelPanelParameters.cameraPositionTransform = child;
            }

            ItemAPI.Add(new CustomItem(ItemDef, AddItemDisplays()));

            if (ConversionItemDef)
            {
                ItemDef.Pair transformation = new()
                {
                    itemDef1 = ConversionItemDef,
                    itemDef2 = ItemDef
                };
                Main.ItemConversionList.Add(transformation);

                Log.Info(String.Format("Added void conversion from {0} to {1}", ConversionItemDef.name, ItemDef.name));
            }
            
            return Enabled;
        }

        public int GetItemCountEffective(CharacterBody body)
        {
            int ret = 0;

            if (body && body.inventory)
            {
                ret = body.inventory.GetItemCountEffective(ItemDef);
            }

            return ret;
        }
        public int GetItemCountPermanent(CharacterBody body)
        {
            int ret = 0;

            if (body && body.inventory)
            {
                ret = body.inventory.GetItemCountPermanent(ItemDef);
            }

            return ret;
        }
        public abstract void FormatDescriptionTokens();
        public abstract void RegisterHooks();
        public abstract void AddCorruptionRecipe();
        public abstract ItemDisplayRuleDict AddItemDisplays();
    }
}