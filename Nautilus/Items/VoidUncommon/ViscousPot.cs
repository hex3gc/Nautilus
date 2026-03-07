using RoR2;
using Nautilus.Configuration;
using System;
using R2API;
using UnityEngine;
using UnityEngine.AddressableAssets;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using RoR2.Orbs;

namespace Nautilus.Items
{
    public static partial class ItemInit
    {
        public static ViscousPot ViscousPot = new ViscousPot
        (
            "ViscousPot",
            [ItemTag.Healing, ItemTag.Damage],
            ItemTier.VoidTier2
        );
    }

    /// <summary>
    ///     // Ver.1
    ///     Defensive alternative to Luminous Shot. I wanted an option for reducing barrier decay, and it gives you somewhat of a reason to take it over luminous shot. Boosts void watch as well
    ///     // Ver.2
    ///     While still keeping the 'barrier green item' niche, I decided to make this one require a more aggressive approach to get the benefit, and have some damage utility
    /// </summary>
    public class ViscousPot : ItemBase
    {
        public override bool Enabled => ViscousPot_Enabled.Value;
        public override ItemDef ConversionItemDef => Addressables.LoadAssetAsync<ItemDef>("RoR2/DLC2/Items/IncreasePrimaryDamage/IncreasePrimaryDamage.asset").WaitForCompletion();
        public override GameObject itemPrefab => OverwritePrefabMaterials();
        public Material material0 => Addressables.LoadAssetAsync<Material>("RoR2/DLC1/EliteVoid/matVoidInfestorMetal.mat").WaitForCompletion();
        public Material material1 => Addressables.LoadAssetAsync<Material>("RoR2/Base/Clay/matClayBubble.mat").WaitForCompletion();
        public Material material2 => Addressables.LoadAssetAsync<Material>("RoR2/DLC1/voidstage/matVoidAsteroid.mat").WaitForCompletion();
        public override Sprite itemIcon => Main.Assets.LoadAsset<Sprite>("Assets/icons/viscousPot.png");
        private GameObject _gooExplodePrefab;
        public GameObject gooExplodePrefab
        {
            get
            {
                if (_gooExplodePrefab == null)
                {
                    _gooExplodePrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/goolake/ClayGooOrbImpact.prefab").WaitForCompletion();
                }
                return _gooExplodePrefab;
            }
            set;
        }

        public ViscousPot(string _name, ItemTag[] _tags, ItemTier _tier, bool _canRemove = true, bool _isConsumed = false, bool _hidden = false) : 
        base(_name, _tags, _tier, _canRemove, _isConsumed, _hidden){}

        // Config
        public static ConfigItem<bool> ViscousPot_Enabled = new ConfigItem<bool>
        (
            "Void uncommon: Viscous Pot",
            "Item enabled",
            "Should this item appear in runs?",
            true
        );
        public static ConfigItem<float> ViscousPot_DecayReduction = new ConfigItem<float>
        (
            "Void uncommon: Viscous Pot",
            "Barrier decay reduction",
            "Fraction for barrier decay reduction.",
            0.2f,
            0f,
            1f,
            0.05f
        );
        /*
        public static ConfigItem<float> ViscousPot_BarrierAddv2 = new ConfigItem<float>
        (
            "Void uncommon: Viscous Pot",
            "Barrier on hit",
            "Fraction of barrier added when a viscous orb hits an enemy.",
            0.05f,
            0f,
            1f,
            0.05f
        );
        */
        public static ConfigItem<float> ViscousPot_BarrierAddFlatv2 = new ConfigItem<float>
        (
            "Void uncommon: Viscous Pot",
            "Barrier on hit",
            "Flat amount of barrier added when a viscous orb hits an enemy.",
            15f,
            0f,
            50f,
            1f
        );
        public static ConfigItem<int> ViscousPot_OrbAmountv2 = new ConfigItem<int>
        (
            "Void uncommon: Viscous Pot",
            "Viscous orb amount",
            "Amount of orbs launched per secondary skill.",
            3,
            1f,
            6f,
            1f
        );
        public static ConfigItem<int> ViscousPot_OrbAmountStackv2 = new ConfigItem<int>
        (
            "Void uncommon: Viscous Pot",
            "Viscous orb amount (per stack)",
            "Amount of orbs launched per secondary skill, per additional stack.",
            1,
            1f,
            6f,
            1f
        );
        public static ConfigItem<float> ViscousPot_OrbRadiusv2 = new ConfigItem<float>
        (
            "Void uncommon: Viscous Pot",
            "Viscous orb radius",
            "Meters radius where enemies can be targeted by orbs.",
            20f,
            1f,
            60f,
            1f
        );
        public static ConfigItem<float> ViscousPot_OrbRadiusStackv2 = new ConfigItem<float>
        (
            "Void uncommon: Viscous Pot",
            "Viscous orb radius (per stack)",
            "Meters radius where enemies can be targeted by orbs, per additional stack.",
            8f,
            1f,
            60f,
            1f
        );
        public static ConfigItem<float> ViscousPot_OrbDamagev2 = new ConfigItem<float>
        (
            "Void uncommon: Viscous Pot",
            "Viscous orb damage",
            "Fractional damage from viscous orbs.",
            2f,
            0f,
            6f,
            0.1f
        );
        public static ConfigItem<float> ViscousPot_OrbProcCoefficient = new ConfigItem<float>
        (
            "Void uncommon: Viscous Pot",
            "Viscous orb proc coefficient",
            "Proc coefficient of viscous orbs.",
            0.2f,
            0f,
            1f,
            0.05f
        );
        public static ConfigItem<bool> ViscousPot_Recipe = new ConfigItem<bool>
        (
            "Void uncommon: Viscous Pot",
            "Recipe enabled",
            "Should this item have a custom corruption recipe?",
            true
        );
        public static ConfigItem<string> ViscousPot_Ingredient1 = new ConfigItem<string>
        (
            "Void uncommon: Viscous Pot",
            "Recipe ingredient 1",
            "First ingredient for corruption recipe",
            "IncreasePrimaryDamage"
        );
        public static ConfigItem<string> ViscousPot_Ingredient2 = new ConfigItem<string>
        (
            "Void uncommon: Viscous Pot",
            "Recipe ingredient 2",
            "Second ingredient for corruption recipe",
            "Wellies"
        );

        public GameObject OverwritePrefabMaterials()
        {
            GameObject ret = Main.Assets.LoadAsset<GameObject>("Assets/prefabs/viscousPot.prefab");

            Material[] materials =
            {
                material0,
                material1,
                material2
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
                    ViscousPot_DecayReduction.Value * 100f,
                    ViscousPot_OrbAmountv2.Value,
                    ViscousPot_OrbAmountStackv2.Value,
                    ViscousPot_OrbRadiusv2.Value,
                    ViscousPot_OrbRadiusStackv2.Value,
                    ViscousPot_OrbDamagev2.Value * 100f,
                    ViscousPot_BarrierAddFlatv2.Value
                )
            );
        }

        // Hooks
        public override void RegisterHooks()
        {
            // Barrier decay reduction
            RecalculateStatsAPI.GetStatCoefficients += (orig, self) =>
            {
                int itemCount = GetItemCountEffective(orig);
                if (itemCount > 0)
                {
                    self.barrierDecayMult *= 1f - ViscousPot_DecayReduction.Value;
                }
            };

            // Orbs on skill
            On.RoR2.CharacterBody.OnSkillActivated += (orig, self, genericSkill) =>
            {
                orig(self, genericSkill);
                
                if (GetItemCountEffective(self) <= 0 || !self.healthComponent)
                {
                    return;
                }

                int itemCount = GetItemCountEffective(self);

                if (self.bodyIndex == BodyCatalog.SpecialCases.RailGunner())
                {
                    if ((object)self.skillLocator.primary == genericSkill && self.canAddIncrasePrimaryDamage)
                    {
                        FireOrbs(self, itemCount);
                    }
                }
                else if ((genericSkill.skillDef.autoHandleLuminousShot || self.canAddIncrasePrimaryDamage) && (object)self.skillLocator.secondary == genericSkill)
                {
                    FireOrbs(self, itemCount);
                }
            };
        }
        public override void AddCorruptionRecipe()
        {
            if (ViscousPot_Recipe.Value == true && ItemInit.Wellies.Enabled)
            {
                ItemInit.MakeCorruptionRecipe
                (
                    ViscousPot_Ingredient1.Value,
                    ViscousPot_Ingredient2.Value,
                    ItemDef.name
                );
            }
        }

        public void FireOrbs(CharacterBody body, int itemCount)
        {
            List<Collider> colliders = Physics.OverlapSphere(body.corePosition, ViscousPot_OrbRadiusv2.Value + (ViscousPot_OrbRadiusStackv2.Value * (itemCount - 1))).ToList();
            Util.ShuffleList(colliders);
            
            int orbCount = ViscousPot_OrbAmountv2.Value + (ViscousPot_OrbAmountStackv2.Value * (itemCount - 1));
            bool exploded = false;

            foreach(Collider collider in colliders)
            {
                if (orbCount <= 0)
                {
                    break;
                }

                GameObject gameObject = collider.gameObject;
                if (gameObject.GetComponentInChildren<CharacterBody>())
                {
                    CharacterBody colliderBody = gameObject.GetComponentInChildren<CharacterBody>();
                    if (colliderBody.healthComponent && colliderBody.healthComponent.health > 0f && colliderBody.teamComponent && colliderBody.teamComponent.teamIndex != body.teamComponent.teamIndex)
                    {
                        if (!exploded)
                        {
                            EffectData effectData = new EffectData()
                            {
                                origin = body.corePosition
                            };
                            EffectManager.SpawnEffect(gooExplodePrefab, effectData, true);
                            
                            exploded = true;
                        }

                        ViscousPotOrb viscousPotOrb = new ViscousPotOrb();
                        viscousPotOrb.attacker = body.gameObject;
                        viscousPotOrb.target = colliderBody.mainHurtBox;
                        viscousPotOrb.teamIndex = body.teamComponent.teamIndex;
                        viscousPotOrb.origin = body.corePosition;

                        OrbManager.instance.AddOrb(viscousPotOrb);

                        orbCount--;
                    }
                }
            }
        }

        public class ViscousPotOrb : RoR2.Orbs.SquidOrb
        {
            public override void OnArrival()
            {
                if (!target)
                {
                    return;
                }

                CharacterBody attackerBody = attacker.GetComponent<CharacterBody>();

                if (target.healthComponent && attackerBody.healthComponent && attackerBody.teamComponent && target.teamIndex != attackerBody.teamComponent.teamIndex)
                {
                    DamageInfo damageInfo = new DamageInfo();
                    damageInfo.damage = attackerBody.damage * ViscousPot_OrbDamagev2.Value;
                    damageInfo.attacker = attacker;
                    damageInfo.inflictor = null;
                    damageInfo.force = Vector3.zero;
                    damageInfo.crit = false;
                    damageInfo.procChainMask = procChainMask;
                    damageInfo.procCoefficient = ViscousPot_OrbProcCoefficient.Value;
                    damageInfo.position = target.transform.position;
                    damageInfo.damageColorIndex = DamageColorIndex.Void;
                    damageInfo.damageType = damageType;
                    damageInfo.inflictedHurtbox = target;
                    target.healthComponent.TakeDamage(damageInfo);
                    GlobalEventManager.instance.OnHitEnemy(damageInfo, target.healthComponent.gameObject);
                    GlobalEventManager.instance.OnHitAll(damageInfo, target.healthComponent.gameObject);

                    attackerBody.healthComponent.AddBarrier(ViscousPot_BarrierAddFlatv2.Value);
                }
            }
        }
    }
}