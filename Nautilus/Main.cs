using BepInEx;
using R2API.Utils;
using RiskOfOptions;
using RoR2;
using RoR2.ExpansionManagement;
using UnityEngine;
using UnityEngine.AddressableAssets;
using R2API;
using Nautilus.Items;
using Nautilus.Interactables;
using System.Collections.Generic;
using System.Reflection;
using RiskOfOptions.Options;
using BepInEx.Configuration;
using System;
using RoR2.ContentManagement;
using System.Linq;
using Nautilus.Configuration;
// using ShaderSwapper;

namespace Nautilus
{
    [BepInPlugin(NAUTILUS_GUID, NAUTILUS_NAME, NAUTILUS_VER)]
    [BepInDependency("com.bepis.r2api", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.bepis.r2api.items", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.bepis.r2api.language", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.bepis.r2api.recalculatestats", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.bepis.r2api.prefab", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.bepis.r2api.director", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.bepis.r2api.proctype", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.rune580.riskofoptions", BepInDependency.DependencyFlags.HardDependency)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]
    public class Main : BaseUnityPlugin
    {
        public const string NAUTILUS_GUID = "com.Hex3.Nautilus";
        public const string NAUTILUS_NAME = "Nautilus";
        public const string NAUTILUS_VER = "1.4.1";
        public static Main Instance;
        public static ExpansionDef Expansion;
        public static AssetBundle Assets;
        public static ItemRelationshipProvider ItemRelationshipProvider = ScriptableObject.CreateInstance<ItemRelationshipProvider>();
        public static List<ItemDef.Pair> ItemConversionList = new();
        public static ConfigEntry<bool> Config_Enabled;

        public void Awake()
        {
            Log.Init(Logger);
            Log.Info($"Init {NAUTILUS_NAME} {NAUTILUS_VER}");

            Instance = this;

            Log.Info("Creating assets...");
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Nautilus.nautilusvfx"))
            {
                Assets = AssetBundle.LoadFromStream(stream);
            }
            // base.StartCoroutine(Assets.UpgradeStubbedShadersAsync());

            InteractableInit.shrineOfTheDeep = InteractableInit.shrineOfTheDeep;

            Log.Info($"Creating config...");
            Config_Enabled = Instance.Config.Bind(new ConfigDefinition("CONFIG - IMPORTANT", "Enable custom config"), false, new ConfigDescription("Set to 'true' to enable custom configuration for this mod. False by default to allow balance changes to take effect.", null, Array.Empty<object>()));
            ModSettingsManager.SetModDescription("Adds new void counterparts for vanilla items.");
            ModSettingsManager.SetModIcon(Assets.LoadAsset<Sprite>("Assets/icons/expansion.png"));
            ModSettingsManager.AddOption
            (
                new CheckBoxOption
                (
                    Config_Enabled,
                    true
                )
            );
            ConfigItem.Init();

            Log.Info($"Creating expansion...");
            Expansion = ScriptableObject.CreateInstance<ExpansionDef>();
            Expansion.name = NAUTILUS_NAME;
            Expansion.nameToken = "NT_EXPANSION_NAME";
            Expansion.descriptionToken = "NT_EXPANSION_DESC";
            Expansion.iconSprite = Assets.LoadAsset<Sprite>("Assets/icons/expansion.png");
            Expansion.disabledIconSprite = Assets.LoadAsset<Sprite>("Assets/icons/expansion-inactive.png");
            Expansion.requiredEntitlement = null;
            ContentAddition.AddExpansionDef(Expansion);

            Log.Info($"Creating items...");
            ItemInit.Init();

            Log.Info($"Creating interactables...");
            InteractableInit.Init();

            Log.Info($"Creating void conversions...");
            ItemRelationshipProvider.name = "NT_ITEMRELATIONSHIPPROVIDER";
            ItemRelationshipProvider.relationshipType = Addressables.LoadAssetAsync<ItemRelationshipType>("RoR2/DLC1/Common/ContagiousItem.asset").WaitForCompletion();
            ItemRelationshipProvider.relationships = ItemConversionList.ToArray();
            ContentAddition.AddItemRelationshipProvider(ItemRelationshipProvider);

            Log.Info($"Creating crafting recipes...");
            if (ItemInit.WeepingFungus_Recipe.Value == true) {ItemInit.MakeCorruptionRecipe(ItemInit.WeepingFungus_Ingredient1.Value, ItemInit.WeepingFungus_Ingredient2.Value, "MushroomVoid");}
            if (ItemInit.SaferSpaces_Recipe.Value == true) { ItemInit.MakeCorruptionRecipe(ItemInit.SaferSpaces_Ingredient1.Value, ItemInit.SaferSpaces_Ingredient2.Value, "BearVoid"); }
            if (ItemInit.EncrustedKey_Recipe.Value == true) { ItemInit.MakeCorruptionRecipe(ItemInit.EncrustedKey_Ingredient1.Value, ItemInit.EncrustedKey_Ingredient2.Value, "TreasureCacheVoid"); }
            if (ItemInit.Lenses_Recipe.Value == true) { ItemInit.MakeCorruptionRecipe(ItemInit.Lenses_Ingredient1.Value, ItemInit.Lenses_Ingredient2.Value, "CritGlassesVoid"); }
            if (ItemInit.NeedleTick_Recipe.Value == true) { ItemInit.MakeCorruptionRecipe(ItemInit.NeedleTick_Ingredient1.Value, ItemInit.NeedleTick_Ingredient2.Value, "BleedOnHitVoid"); }
            if (ItemInit.LysateCell_Recipe.Value == true) { ItemInit.MakeCorruptionRecipe(ItemInit.LysateCell_Ingredient1.Value, ItemInit.LysateCell_Ingredient2.Value, "EquipmentMagazineVoid"); }
            if (ItemInit.Polylute_Recipe.Value == true) { ItemInit.MakeCorruptionRecipe(ItemInit.Polylute_Ingredient1.Value, ItemInit.Polylute_Ingredient2.Value, "ChainLightningVoid"); }
            if (ItemInit.Tentabauble_Recipe.Value == true) { ItemInit.MakeCorruptionRecipe(ItemInit.Tentabauble_Ingredient1.Value, ItemInit.Tentabauble_Ingredient2.Value, "SlowOnHitVoid"); }
            if (ItemInit.Voidsent_Recipe.Value == true) { ItemInit.MakeCorruptionRecipe(ItemInit.Voidsent_Ingredient1.Value, ItemInit.Voidsent_Ingredient2.Value, "ExplodeOnDeathVoid"); }
            if (ItemInit.Band_Recipe.Value == true) { ItemInit.MakeCorruptionRecipe(ItemInit.Band_Ingredient1.Value, ItemInit.Band_Ingredient2.Value, "ElementalRingVoid"); }
            if (ItemInit.Band_Recipe.Value == true) { ItemInit.MakeCorruptionRecipe(ItemInit.Band_Ingredient1Alt.Value, ItemInit.Band_Ingredient2.Value, "ElementalRingVoid"); }
            if (ItemInit.PlasmaShrimp_Recipe.Value == true) { ItemInit.MakeCorruptionRecipe(ItemInit.PlasmaShrimp_Ingredient1.Value, ItemInit.PlasmaShrimp_Ingredient2.Value, "MissileVoid"); }
            if (ItemInit.Benthic_Recipe.Value == true) { ItemInit.MakeCorruptionRecipe(ItemInit.Benthic_Ingredient1.Value, ItemInit.Benthic_Ingredient2.Value, "CloverVoid"); }
            if (ItemInit.Larva_Recipe.Value == true) { ItemInit.MakeCorruptionRecipe(ItemInit.Larva_Ingredient1.Value, ItemInit.Larva_Ingredient2.Value, "ExtraLifeVoid"); }
            if (ItemInit.Zoea_Recipe.Value == true) { ItemInit.MakeCorruptionRecipe(ItemInit.Zoea_Ingredient1.Value, ItemInit.Zoea_Ingredient2.Value, "VoidMegaCrabItem"); }
            for (int i = 0; i < ItemInit.RecipeList.Count; i++)
            {
                CraftableDef c = ScriptableObject.CreateInstance<CraftableDef>();
                c.name = "NautilusRecipe" + i;
                RecipeContentPackProvider.Craftables.Add(c);
            }
            new RecipeContentPackProvider().Initialise();
            PickupCatalog.availability.CallWhenAvailable(InitRecipes);

            Log.Info($"Adding language hooks...");
            On.RoR2.RoR2Application.OnMainMenuControllerInitialized += (orig, self) =>
            {
                ItemInit.FormatDescriptions();
                orig(self);
            };

            Log.Info($"Done");
        }

        public void InitRecipes()
        {
            int i = 0;
            foreach (VoidRecipe vr in ItemInit.RecipeList)
            {
                CraftableDef craftableDef = RecipeContentPackProvider.Craftables[i];

                Recipe recipe = new Recipe();
                recipe.amountToDrop = 1;
                recipe.ingredients = new[]
                {
                    new RecipeIngredient
                    {
                        pickup = ItemCatalog.GetItemDef(ItemCatalog.FindItemIndex(vr.ingredient1))
                    },
                    new RecipeIngredient
                    {
                        pickup = ItemCatalog.GetItemDef(ItemCatalog.FindItemIndex(vr.ingredient2))
                    }
                };

                craftableDef.pickup = ItemCatalog.GetItemDef(ItemCatalog.FindItemIndex(vr.result));
                craftableDef.recipes = new[]
                {
                    recipe
                };

                RecipeContentPackProvider.Craftables.Add(craftableDef);
                Log.Info($"Added recipe for " + vr.result + ": " + vr.ingredient1 + " + " + vr.ingredient2);

                i++;
            }
        }

        public class RecipeContentPackProvider : IContentPackProvider
        {
            internal ContentPack contentPack = new ContentPack();
            public static List<CraftableDef> Craftables = new List<CraftableDef>();
            public string identifier => "com.hex3.NautilusRecipes";

            public void Initialise()
            {
                ContentManager.collectContentPackProviders += AddSelf;
            }

            private void AddSelf(ContentManager.AddContentPackProviderDelegate add)
            {
                add(this);
            }

            public System.Collections.IEnumerator LoadStaticContentAsync(LoadStaticContentAsyncArgs args)
            {
                Log.Info($"Craftable content provider called (Load)");
                contentPack.identifier = identifier;
                contentPack.craftableDefs.Add(Craftables.ToArray());
                args.ReportProgress(1f);
                yield break;
            }

            public System.Collections.IEnumerator GenerateContentPackAsync(GetContentPackAsyncArgs args)
            {
                Log.Info($"Craftable content provider called (Generate)");
                ContentPack.Copy(contentPack, args.output);
                args.ReportProgress(1f);
                yield break;
            }

            public System.Collections.IEnumerator FinalizeAsync(FinalizeAsyncArgs args)
            {
                Log.Info($"Craftable content provider called (Finalize)");
                args.ReportProgress(1f);
                yield break;
            }
        }
    }
}
