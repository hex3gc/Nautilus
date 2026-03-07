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
        public static Wellies Wellies = new Wellies
        (
            "Wellies",
            [ItemTag.Utility],
            ItemTier.VoidTier1
        );
    }

    /// <summary>
    ///     // Ver.1
    ///     Wellies provide a utility alternative to ultra-crit builds, instead letting you invalidate annoying flying enemies
    ///     In exchange for the proc conditions and worse stacking, Weaken is a very powerful debuff (30 armor reduction goes a long way) and makes Death Mark far easier to activate
    /// </summary>
    public class Wellies : ItemBase
    {
        public override bool Enabled => Wellies_Enabled.Value;
        public override ItemDef ConversionItemDef => Addressables.LoadAssetAsync<ItemDef>("RoR2/DLC3/Items/CritAtLowerElevation/CritAtLowerElevation.asset").WaitForCompletion();
        public override GameObject itemPrefab => OverwritePrefabMaterials();
        public Material material0 => Addressables.LoadAssetAsync<Material>("RoR2/Base/Common/Props/matRescueshipDirtPiles.mat").WaitForCompletion();
        public Material material1 => Addressables.LoadAssetAsync<Material>("RoR2/DLC1/Railgunner/matRailGunnerBase.mat").WaitForCompletion();
        public Material material2 => Addressables.LoadAssetAsync<Material>("RoR2/DLC1/ScrapVoid/matScrapVoidMetal.mat").WaitForCompletion();
        public override Sprite itemIcon => Main.Assets.LoadAsset<Sprite>("Assets/icons/wellies.png");
        public BuffDef DebuffDef => Addressables.LoadAssetAsync<BuffDef>("RoR2/Base/Treebot/bdWeak.asset").WaitForCompletion();

        public Wellies(string _name, ItemTag[] _tags, ItemTier _tier, bool _canRemove = true, bool _isConsumed = false, bool _hidden = false) : 
        base(_name, _tags, _tier, _canRemove, _isConsumed, _hidden){}

        // Config
        public static ConfigItem<bool> Wellies_Enabled = new ConfigItem<bool>
        (
            "Void common: Waterlogged Wellies",
            "Item enabled",
            "Should this item appear in runs?",
            true
        );
        public static ConfigItem<float> Wellies_Force = new ConfigItem<float>
        (
            "Void common: Waterlogged Wellies",
            "Downward pull force",
            "How strong the on-hit pull down effect is in arbitrary wacky units.",
            150f,
            0f,
            240f,
            10f
        );
        public static ConfigItem<float> Wellies_ForceStack = new ConfigItem<float>
        (
            "Void common: Waterlogged Wellies",
            "Downward pull force (Per stack)",
            "How strong the on-hit pull down effect is per additional stack.",
            150f,
            0f,
            240f,
            10f
        );
        public static ConfigItem<float> Wellies_ForceUnmassed = new ConfigItem<float>
        (
            "Void common: Waterlogged Wellies",
            "Pure downward pull force percentage",
            "What percentage of downward pull force should treat the enemy as if it's massless? Equalizes large vs. small enemies, but high values can cause larger enemies to take too much fall damage.",
            1f,
            0.1f,
            2.5f,
            0.1f
        );
        public static ConfigItem<float> Wellies_DebuffSeconds = new ConfigItem<float>
        (
            "Void common: Waterlogged Wellies",
            "Debuff length",
            "How long the on-hit debuff should last in seconds.",
            2f,
            0f,
            12f,
            0.5f
        );
        public static ConfigItem<float> Wellies_DebuffSecondsStack = new ConfigItem<float>
        (
            "Void common: Waterlogged Wellies",
            "Debuff length (Per stack)",
            "How long the on-hit debuff should last in seconds per additional stack.",
            2f,
            0f,
            12f,
            0.5f
        );
        public static ConfigItem<bool> Wellies_Recipe = new ConfigItem<bool>
        (
            "Void common: Waterlogged Wellies",
            "Recipe enabled",
            "Should this item have a custom corruption recipe?",
            true
        );
        public static ConfigItem<string> Wellies_Ingredient1 = new ConfigItem<string>
        (
            "Void common: Waterlogged Wellies",
            "Recipe ingredient 1",
            "First ingredient for corruption recipe",
            "CritAtLowerElevation"
        );
        public static ConfigItem<string> Wellies_Ingredient2 = new ConfigItem<string>
        (
            "Void common: Waterlogged Wellies",
            "Recipe ingredient 2",
            "Second ingredient for corruption recipe",
            "CritGlassesVoid"
        );

        public GameObject OverwritePrefabMaterials()
        {
            GameObject ret = Main.Assets.LoadAsset<GameObject>("Assets/prefabs/wellies.prefab");

            Material[] materials =
            {
                material0,
                material1,
                material2,
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
                    Wellies_DebuffSeconds.Value,
                    Wellies_DebuffSecondsStack.Value
                )
            );
        }

        // Hooks
        public override void RegisterHooks()
        {
            // On-hit trigger
            On.RoR2.HealthComponent.TakeDamageProcess += (orig, self, damageInfo) =>
            {
                if (damageInfo.attacker)
                {
                    CharacterBody attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
                    CharacterBody victimBody = self.body;
                    
                    if (attackerBody && victimBody)
                    {
                        int itemCount = GetItemCountEffective(attackerBody);
                        if (itemCount > 0)
                        {
                            float buffLength = Wellies_DebuffSeconds.Value + (Wellies_DebuffSecondsStack.Value * (itemCount - 1));
                            float pullDownForce = (Wellies_Force.Value + (Wellies_ForceStack.Value * (itemCount - 1)));
                            float pullDownForceFlat = pullDownForce * Wellies_ForceUnmassed.Value / 100f;

                            if (pullDownForce > 0f && damageInfo.procCoefficient > 0f && !victimBody.name.ToLower().Contains("grandparent"))
                            {
                                PhysForceInfo physForceInfoNormal = new PhysForceInfo
                                {
                                    force = Vector3.down * pullDownForce
                                };
                                PhysForceInfo physForceInfoNormalFlat = new PhysForceInfo
                                {
                                    force = Vector3.down * pullDownForceFlat,
                                    massIsOne = true
                                };
                                
                                if (victimBody.TryGetComponent(out CharacterMotor victimMotor) && !victimMotor.isGrounded)
                                {
                                    victimMotor.ApplyForceImpulse(physForceInfoNormal);
                                    victimMotor.ApplyForceImpulse(physForceInfoNormalFlat);
                                    victimBody.AddTimedBuff(DebuffDef.buffIndex, buffLength);
                                    Util.PlaySound("Play_voidBarnacle_m1_chargeUp", victimBody.gameObject);
                                }
                                else if (victimBody.TryGetComponent(out RigidbodyMotor victimRigidMotor))
                                {
                                    victimRigidMotor.ApplyForceImpulse(physForceInfoNormal);
                                    victimRigidMotor.ApplyForceImpulse(physForceInfoNormalFlat);
                                    victimBody.AddTimedBuff(DebuffDef.buffIndex, buffLength);
                                    Util.PlaySound("Play_voidBarnacle_m1_chargeUp", victimBody.gameObject);
                                }
                            }
                        }
                    }
                }

                orig(self, damageInfo);   
            };
        }

        public override void AddCorruptionRecipe()
        {
            if (Wellies_Recipe.Value == true)
            {
                ItemInit.MakeCorruptionRecipe
                (
                    Wellies_Ingredient1.Value,
                    Wellies_Ingredient2.Value,
                    ItemDef.name
                );
            }
        }
    }
}