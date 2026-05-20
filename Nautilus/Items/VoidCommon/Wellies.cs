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
            [ItemTag.Utility, ItemTag.CanBeTemporary],
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

                            if (pullDownForce > 0f && damageInfo.procCoefficient > 0f && !victimBody.name.ToLower().Contains("grandparent") && !victimBody.name.ToLower().Contains("voidraid"))
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

        // Recipes
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
        
        // IDR
        public override ItemDisplayRuleDict AddItemDisplays()
        {
            GameObject ItemDisplayPrefab = Helpers.PrepareItemDisplayModel(PrefabAPI.InstantiateClone(itemPrefab, ItemDef.name + "Display", false));
            ItemDisplayRuleDict rules = new ItemDisplayRuleDict();

            #region IDR
            rules.Add("mdlCommandoDualies", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "CalfL",
                        localPos = new Vector3(0.05459F, 0.35279F, -0.02096F),
                        localAngles = new Vector3(24.00437F, 241.7346F, 175.7336F),
                        localScale = new Vector3(1.04054F, 1.04054F, 1.04054F)
                    }
                }
            );
            rules.Add("mdlHuntress", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "CalfL",
                        localPos = new Vector3(0.0339F, 0.46121F, -0.02344F),
                        localAngles = new Vector3(18.96789F, 238.3667F, 167.2122F),
                        localScale = new Vector3(1.04054F, 1.04054F, 1.04054F)
                    }
                }
            );
            rules.Add("mdlBandit2", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "CalfL",
                        localPos = new Vector3(0.06766F, 0.42942F, -0.02797F),
                        localAngles = new Vector3(18.01975F, 236.5676F, 166.5165F),
                        localScale = new Vector3(1.04054F, 1.04054F, 1.04054F)
                    }
                }
            );
            rules.Add("mdlToolbot", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "CalfL",
                        localPos = new Vector3(0.0906F, 4.26193F, -1.60567F),
                        localAngles = new Vector3(33.83314F, 231.597F, 176.2843F),
                        localScale = new Vector3(4.6442F, 4.6442F, 4.6442F)
                    }
                }
            );
            rules.Add("mdlEngi", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "CalfL",
                        localPos = new Vector3(0.04855F, 0.29898F, -0.02227F),
                        localAngles = new Vector3(29.31547F, 235.8502F, 181.9146F),
                        localScale = new Vector3(1.04054F, 1.04054F, 1.04054F)
                    }
                }
            );
            rules.Add("mdlMage", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "CalfL",
                        localPos = new Vector3(0.00463F, 0.44786F, -0.10045F),
                        localAngles = new Vector3(27.64817F, 242.7562F, 174.6417F),
                        localScale = new Vector3(1.04054F, 1.04054F, 1.04054F)
                    }
                }
            );
            rules.Add("mdlMerc", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "CalfL",
                        localPos = new Vector3(0.05201F, 0.32554F, -0.03236F),
                        localAngles = new Vector3(27.87446F, 246.3371F, 184.4269F),
                        localScale = new Vector3(1.04054F, 1.04054F, 1.04054F)
                    }
                }
            );
            rules.Add("mdlTreebot", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "FootFrontL",
                        localPos = new Vector3(0.0376F, 1.37253F, -0.04084F),
                        localAngles = new Vector3(23.72903F, 223.4445F, 161.818F),
                        localScale = new Vector3(1.04054F, 1.04054F, 1.04054F)
                    }
                }
            );
            rules.Add("mdlLoader", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "CalfL",
                        localPos = new Vector3(0.04056F, 0.49894F, -0.00329F),
                        localAngles = new Vector3(27.55028F, 214.5608F, 169.1206F),
                        localScale = new Vector3(1.04054F, 1.04054F, 1.04054F)
                    }
                }
            );
            rules.Add("mdlCroco", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "HeadCenter",
                        localPos = new Vector3(-0.12383F, 2.23713F, 1.70597F),
                        localAngles = new Vector3(23.80973F, 50.96521F, 168.1773F),
                        localScale = new Vector3(5.01094F, 5.01094F, 5.01094F)
                    }
                }
            );
            rules.Add("mdlCaptain", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "CalfL",
                        localPos = new Vector3(0.03605F, 0.43529F, 0.02278F),
                        localAngles = new Vector3(20.46794F, 224.6553F, 160.8648F),
                        localScale = new Vector3(1.04054F, 1.04054F, 1.04054F)
                    }
                }
            );
            rules.Add("mdlRailGunner", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "CalfL",
                        localPos = new Vector3(0.00597F, 0.49747F, 0.07436F),
                        localAngles = new Vector3(16.03705F, 129.8903F, 163.5802F),
                        localScale = new Vector3(1.04054F, 1.04054F, 1.04054F)
                    }
                }
            );
            rules.Add("mdlVoidSurvivor", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "CalfR",
                        localPos = new Vector3(-0.06144F, 0.38091F, -0.01169F),
                        localAngles = new Vector3(31.09837F, 291.495F, 160.6339F),
                        localScale = new Vector3(1.04054F, 1.04054F, 1.04054F)
                    }
                }
            );
            rules.Add("mdlSeeker", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "CalfL",
                        localPos = new Vector3(0.07177F, 0.42745F, 0.0448F),
                        localAngles = new Vector3(31.23956F, 136.0638F, 181.1546F),
                        localScale = new Vector3(1.04054F, 1.04054F, 1.04054F)
                    }
                }
            );
            rules.Add("mdlFalseSon", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "ClubExplosionPoint",
                        localPos = new Vector3(0.04072F, -0.03077F, -2.62772F),
                        localAngles = new Vector3(35.57562F, 297.9604F, 273.7678F),
                        localScale = new Vector3(1.04054F, 1.04054F, 1.04054F)
                    }
                }
            );
            rules.Add("mdlChef", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "PizzaCutter",
                        localPos = new Vector3(0.00503F, 0.44955F, 0.05011F),
                        localAngles = new Vector3(17.83064F, 135.3451F, 161.9421F),
                        localScale = new Vector3(1.20654F, 1.20654F, 1.20654F)
                    }
                }
            );
            rules.Add("mdlDroneTech", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "CalfL",
                        localPos = new Vector3(-0.66415F, 0.04357F, 0.03509F),
                        localAngles = new Vector3(44.04141F, 136.1547F, 51.91541F),
                        localScale = new Vector3(1.04054F, 1.04054F, 1.04054F)
                    }
                }
            );
            rules.Add("mdlDrifter", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "BagPocketL",
                        localPos = new Vector3(-0.02132F, 0.18006F, 0.23867F),
                        localAngles = new Vector3(355.0318F, 229.4706F, 92.83186F),
                        localScale = new Vector3(0.64488F, 0.64258F, 0.64258F)
                    }
                }
            );
            #endregion

            return rules;
        }
    }
}