using System;
using System.Collections.Generic;
using System.Linq;
using Nautilus.Configuration;
using R2API;
using RoR2;
using UnityEngine;

namespace Nautilus.Items
{
    /// <summary>
    ///     Item setup
    /// </summary>
    public static partial class ItemInit
    {
        # region CONFIG
        // Weeping Fungus
        public static ConfigItem<bool> WeepingFungus_Recipe = new ConfigItem<bool>
        (
            "Vanilla: Weeping Fungus",
            "Recipe enabled",
            "Should this item have a custom corruption recipe?",
            true
        );
        public static ConfigItem<string> WeepingFungus_Ingredient1 = new ConfigItem<string>
        (
            "Vanilla: Weeping Fungus",
            "Recipe ingredient 1",
            "First ingredient for corruption recipe",
            "Mushroom"
        );
        public static ConfigItem<string> WeepingFungus_Ingredient2 = new ConfigItem<string>
        (
            "Vanilla: Weeping Fungus",
            "Recipe ingredient 2",
            "Second ingredient for corruption recipe",
            "Crabsinthe"
        );
        
        // Safer Spaces
        public static ConfigItem<bool> SaferSpaces_Recipe = new ConfigItem<bool>
        (
            "Vanilla: Safer Spaces",
            "Recipe enabled",
            "Should this item have a custom corruption recipe?",
            true
        );
        public static ConfigItem<string> SaferSpaces_Ingredient1 = new ConfigItem<string>
        (
            "Vanilla: Safer Spaces",
            "Recipe ingredient 1",
            "First ingredient for corruption recipe",
            "Bear"
        );
        public static ConfigItem<string> SaferSpaces_Ingredient2 = new ConfigItem<string>
        (
            "Vanilla: Safer Spaces",
            "Recipe ingredient 2",
            "Second ingredient for corruption recipe",
            "BleedOnHitVoid"
        );

        // Encrusted Key
        public static ConfigItem<bool> EncrustedKey_Recipe = new ConfigItem<bool>
        (
            "Vanilla: Encrusted Key",
            "Recipe enabled",
            "Should this item have a custom corruption recipe?",
            true
        );
        public static ConfigItem<string> EncrustedKey_Ingredient1 = new ConfigItem<string>
        (
            "Vanilla: Encrusted Key",
            "Recipe ingredient 1",
            "First ingredient for corruption recipe",
            "TreasureCache"
        );
        public static ConfigItem<string> EncrustedKey_Ingredient2 = new ConfigItem<string>
        (
            "Vanilla: Encrusted Key",
            "Recipe ingredient 2",
            "Second ingredient for corruption recipe",
            "VoidWatch"
        );

        // Lost Seer's Lenses
        public static ConfigItem<bool> Lenses_Recipe = new ConfigItem<bool>
        (
            "Vanilla: Lost Seers Lenses",
            "Recipe enabled",
            "Should this item have a custom corruption recipe?",
            true
        );
        public static ConfigItem<string> Lenses_Ingredient1 = new ConfigItem<string>
        (
            "Vanilla: Lost Seers Lenses",
            "Recipe ingredient 1",
            "First ingredient for corruption recipe",
            "CritGlasses"
        );
        public static ConfigItem<string> Lenses_Ingredient2 = new ConfigItem<string>
        (
            "Vanilla: Lost Seers Lenses",
            "Recipe ingredient 2",
            "Second ingredient for corruption recipe",
            "BleedOnHitVoid"
        );

        // Needletick
        public static ConfigItem<bool> NeedleTick_Recipe = new ConfigItem<bool>
        (
            "Vanilla: Needletick",
            "Recipe enabled",
            "Should this item have a custom corruption recipe?",
            true
        );
        public static ConfigItem<string> NeedleTick_Ingredient1 = new ConfigItem<string>
        (
            "Vanilla: Needletick",
            "Recipe ingredient 1",
            "First ingredient for corruption recipe",
            "BleedOnHit"
        );
        public static ConfigItem<string> NeedleTick_Ingredient2 = new ConfigItem<string>
        (
            "Vanilla: Needletick",
            "Recipe ingredient 2",
            "Second ingredient for corruption recipe",
            "Crabsinthe"
        );

        // Lysate Cell
        public static ConfigItem<bool> LysateCell_Recipe = new ConfigItem<bool>
        (
            "Vanilla: Lysate Cell",
            "Recipe enabled",
            "Should this item have a custom corruption recipe?",
            true
        );
        public static ConfigItem<string> LysateCell_Ingredient1 = new ConfigItem<string>
        (
            "Vanilla: Lysate Cell",
            "Recipe ingredient 1",
            "First ingredient for corruption recipe",
            "EquipmentMagazine"
        );
        public static ConfigItem<string> LysateCell_Ingredient2 = new ConfigItem<string>
        (
            "Vanilla: Lysate Cell",
            "Recipe ingredient 2",
            "Second ingredient for corruption recipe",
            "MushroomVoid"
        );

        // Polylute
        public static ConfigItem<bool> Polylute_Recipe = new ConfigItem<bool>
        (
            "Vanilla: Polylute",
            "Recipe enabled",
            "Should this item have a custom corruption recipe?",
            true
        );
        public static ConfigItem<string> Polylute_Ingredient1 = new ConfigItem<string>
        (
            "Vanilla: Polylute",
            "Recipe ingredient 1",
            "First ingredient for corruption recipe",
            "ChainLightning"
        );
        public static ConfigItem<string> Polylute_Ingredient2 = new ConfigItem<string>
        (
            "Vanilla: Polylute",
            "Recipe ingredient 2",
            "Second ingredient for corruption recipe",
            "CritGlassesVoid"
        );

        // Tentabauble
        public static ConfigItem<bool> Tentabauble_Recipe = new ConfigItem<bool>
        (
            "Vanilla: Tentabauble",
            "Recipe enabled",
            "Should this item have a custom corruption recipe?",
            true
        );
        public static ConfigItem<string> Tentabauble_Ingredient1 = new ConfigItem<string>
        (
            "Vanilla: Tentabauble",
            "Recipe ingredient 1",
            "First ingredient for corruption recipe",
            "SlowOnHit"
        );
        public static ConfigItem<string> Tentabauble_Ingredient2 = new ConfigItem<string>
        (
            "Vanilla: Tentabauble",
            "Recipe ingredient 2",
            "Second ingredient for corruption recipe",
            "Crabsinthe"
        );

        // Voidsent Flame
        public static ConfigItem<bool> Voidsent_Recipe = new ConfigItem<bool>
        (
            "Vanilla: Voidsent Flame",
            "Recipe enabled",
            "Should this item have a custom corruption recipe?",
            true
        );
        public static ConfigItem<string> Voidsent_Ingredient1 = new ConfigItem<string>
        (
            "Vanilla: Voidsent Flame",
            "Recipe ingredient 1",
            "First ingredient for corruption recipe",
            "ExplodeOnDeath"
        );
        public static ConfigItem<string> Voidsent_Ingredient2 = new ConfigItem<string>
        (
            "Vanilla: Voidsent Flame",
            "Recipe ingredient 2",
            "Second ingredient for corruption recipe",
            "VoidWatch"
        );

        // Singularity Band
        public static ConfigItem<bool> Band_Recipe = new ConfigItem<bool>
        (
            "Vanilla: Singularity Band",
            "Recipe enabled",
            "Should this item have a custom corruption recipe?",
            true
        );
        public static ConfigItem<string> Band_Ingredient1 = new ConfigItem<string>
        (
            "Vanilla: Singularity Band",
            "Recipe ingredient 1",
            "First ingredient for corruption recipe",
            "FireRing"
        );
        public static ConfigItem<string> Band_Ingredient1Alt = new ConfigItem<string>
        (
            "Vanilla: Singularity Band",
            "Recipe ingredient 1 (alt)",
            "First ingredient for corruption recipe (alt)",
            "IceRing"
        );
        public static ConfigItem<string> Band_Ingredient2 = new ConfigItem<string>
        (
            "Vanilla: Singularity Band",
            "Recipe ingredient 2",
            "Second ingredient for corruption recipe",
            "BearVoid"
        );

        // Plasma Shrimp
        public static ConfigItem<bool> PlasmaShrimp_Recipe = new ConfigItem<bool>
        (
            "Vanilla: Plasma Shrimp",
            "Recipe enabled",
            "Should this item have a custom corruption recipe?",
            true
        );
        public static ConfigItem<string> PlasmaShrimp_Ingredient1 = new ConfigItem<string>
        (
            "Vanilla: Plasma Shrimp",
            "Recipe ingredient 1",
            "First ingredient for corruption recipe",
            "Missile"
        );
        public static ConfigItem<string> PlasmaShrimp_Ingredient2 = new ConfigItem<string>
        (
            "Vanilla: Plasma Shrimp",
            "Recipe ingredient 2",
            "Second ingredient for corruption recipe",
            "BearVoid"
        );

        // Benthic Bloom
        public static ConfigItem<bool> Benthic_Recipe = new ConfigItem<bool>
        (
            "Vanilla: Benthic Bloom",
            "Recipe enabled",
            "Should this item have a custom corruption recipe?",
            true
        );
        public static ConfigItem<string> Benthic_Ingredient1 = new ConfigItem<string>
        (
            "Vanilla: Benthic Bloom",
            "Recipe ingredient 1",
            "First ingredient for corruption recipe",
            "Clover"
        );
        public static ConfigItem<string> Benthic_Ingredient2 = new ConfigItem<string>
        (
            "Vanilla: Benthic Bloom",
            "Recipe ingredient 2",
            "Second ingredient for corruption recipe",
            "ChainLightningVoid"
        );

        // Pluripotent Larva
        public static ConfigItem<bool> Larva_Recipe = new ConfigItem<bool>
        (
            "Vanilla: Pluripotent Larva",
            "Recipe enabled",
            "Should this item have a custom corruption recipe?",
            true
        );
        public static ConfigItem<string> Larva_Ingredient1 = new ConfigItem<string>
        (
            "Vanilla: Pluripotent Larva",
            "Recipe ingredient 1",
            "First ingredient for corruption recipe",
            "ExtraLife"
        );
        public static ConfigItem<string> Larva_Ingredient2 = new ConfigItem<string>
        (
            "Vanilla: Pluripotent Larva",
            "Recipe ingredient 2",
            "Second ingredient for corruption recipe",
            "EquipmentMagazineVoid"
        );

        // Newly Hatched Zoea
        public static ConfigItem<bool> Zoea_Recipe = new ConfigItem<bool>
        (
            "Vanilla: Newly Hatched Zoea",
            "Recipe enabled",
            "Should this item have a custom corruption recipe?",
            true
        );
        public static ConfigItem<string> Zoea_Ingredient1 = new ConfigItem<string>
        (
            "Vanilla: Newly Hatched Zoea",
            "Recipe ingredient 1",
            "First ingredient for corruption recipe",
            "BeetleGland"
        );
        public static ConfigItem<string> Zoea_Ingredient2 = new ConfigItem<string>
        (
            "Vanilla: Newly Hatched Zoea",
            "Recipe ingredient 2",
            "Second ingredient for corruption recipe",
            "SlowOnHitVoid"
        );
        #endregion

        private static List<ItemBase> _itemList;
        public static List<ItemBase> ItemList
        {
            get
            {
                if (_itemList == null)
                {
                    _itemList = new List<ItemBase>();
                }
                return _itemList;
            }
            set
            {
                _itemList = value;
            }
        }
        private static List<VoidRecipe> _recipeList;
        public static List<VoidRecipe> RecipeList
        {
            get
            {
                if (_recipeList == null)
                {
                    _recipeList = new List<VoidRecipe>();
                }
                return _recipeList;
            }
            set
            {
                _recipeList = value;
            }
        }

        public static void Init()
        {
            foreach (ItemBase ib in ItemList)
            {
                if (ib.RegisterItem())
                {
                    Log.Info("Added definition for item " + ib.Name);
                    ib.RegisterHooks();
                    ib.AddCorruptionRecipe();
                }
            }
        }

        public static void FormatDescriptions()
        {
            foreach(ItemBase ib in ItemList)
            {
                if (ib.Enabled)
                {
                    ib.FormatDescriptionTokens();
                }
            }
        }

        public static void MakeCorruptionRecipe(string ingredient1, string ingredient2, string result)
        {
            if (String.IsNullOrEmpty(ingredient1) || String.IsNullOrEmpty(ingredient2) || String.IsNullOrEmpty(result))
            {
                Log.Warning("Failed adding recipe for " + result + " due to missing item names!");
                return;
            }

            VoidRecipe ret = new VoidRecipe
            {
                ingredient1 = ingredient1,
                ingredient2 = ingredient2,
                result = result
            };

            RecipeList.Add(ret);
        }
    }

    public struct VoidRecipe
    {
        public string ingredient1;
        public string ingredient2;
        public string result;
    }
}