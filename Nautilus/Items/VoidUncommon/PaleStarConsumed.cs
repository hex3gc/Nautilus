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
        public static PaleStarConsumed PaleStarConsumed = new PaleStarConsumed
        (
            "PaleStarConsumed",
            [ItemTag.CannotCopy, ItemTag.CannotDuplicate, ItemTag.CannotSteal, ItemTag.AIBlacklist],
            ItemTier.NoTier
        );
    }

    /// <summary>
    ///     // Ver.1
    /// </summary>
    public class PaleStarConsumed : ItemBase
    {
        public override bool Enabled => PaleStar.PaleStar_Enabled.Value;
        public override ItemDef ConversionItemDef => Addressables.LoadAssetAsync<ItemDef>("RoR2/DLC2/Items/LowerPricedChests/LowerPricedChests.asset").WaitForCompletion();
        public ItemDef ConversionItemDefConsumed => Addressables.LoadAssetAsync<ItemDef>("RoR2/DLC2/Items/LowerPricedChests/LowerPricedChestsConsumed.asset").WaitForCompletion();
        public override GameObject itemPrefab => null;
        public override Sprite itemIcon => Main.Assets.LoadAsset<Sprite>("Assets/icons/paleStarConsumed.png");

        public PaleStarConsumed(string _name, ItemTag[] _tags, ItemTier _tier, bool _canRemove = true, bool _isConsumed = true, bool _hidden = false) : 
        base(_name, _tags, _tier, _canRemove, _isConsumed, _hidden){}
        
        // Tokens
        public override void FormatDescriptionTokens()
        {
            
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
        }

        // Recipe
        public override void AddCorruptionRecipe()
        {
            
        }

        // IDR
        public override ItemDisplayRuleDict AddItemDisplays()
        {
            ItemDisplayRuleDict rules = new ItemDisplayRuleDict();
            return rules;
        }
    }
}