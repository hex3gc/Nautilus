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
        public static ApathyCoreHidden ApathyCoreHidden = new ApathyCoreHidden
        (
            "ApathyCoreHidden",
            [ItemTag.CannotCopy, ItemTag.CannotDuplicate, ItemTag.CannotSteal, ItemTag.AIBlacklist],
            ItemTier.NoTier
        );
    }

    /// <summary>
    ///     // Ver.1
    /// </summary>
    public class ApathyCoreHidden : ItemBase
    {
        public override bool Enabled => ApathyCore.ApathyCore_Enabled.Value;
        public override ItemDef ConversionItemDef => Addressables.LoadAssetAsync<ItemDef>("RoR2/DLC2/Items/LowerPricedChests/LowerPricedChests.asset").WaitForCompletion();
        public ItemDef ConversionItemDefConsumed => Addressables.LoadAssetAsync<ItemDef>("RoR2/DLC2/Items/LowerPricedChests/LowerPricedChestsConsumed.asset").WaitForCompletion();
        public override GameObject itemPrefab => null;
        public override Sprite itemIcon => Main.Assets.LoadAsset<Sprite>("Assets/icons/paleStarConsumed.png");

        public ApathyCoreHidden(string _name, ItemTag[] _tags, ItemTier _tier, bool _canRemove = true, bool _isConsumed = true, bool _hidden = true) :
        base(_name, _tags, _tier, _canRemove, _isConsumed, _hidden)
        { }

        // Tokens
        public override void FormatDescriptionTokens()
        {

        }

        // Hooks
        public override void RegisterHooks()
        {

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