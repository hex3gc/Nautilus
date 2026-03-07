using RoR2;
using Nautilus.Configuration;
using System;
using R2API;
using UnityEngine;
using UnityEngine.AddressableAssets;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.PlayerLoop;

namespace Nautilus.Items
{
    public static partial class ItemInit
    {
        public static EffigyOfRot EffigyOfRot = new EffigyOfRot
        (
            "EffigyOfRot",
            [ItemTag.Healing, ItemTag.Damage, ItemTag.AIBlacklist, ItemTag.BrotherBlacklist, ItemTag.ExtractorUnitBlacklist],
            ItemTier.VoidBoss
        );
    }

    /// <summary>
    ///     // Ver.1
    ///     A poison option that requires a bit of risk to get going, but can be good for both AoE and single damage
    ///     Inspired by The Toxin from RoRR
    /// </summary>
    public class EffigyOfRot : ItemBase
    {
        public override bool Enabled => EffigyOfRot_Enabled.Value;
        public override ItemDef ConversionItemDef => Addressables.LoadAssetAsync<ItemDef>("RoR2/Base/SiphonOnLowHealth/SiphonOnLowHealth.asset").WaitForCompletion();
        public override GameObject itemPrefab => OverwritePrefabMaterials();
        public override Sprite itemIcon => Main.Assets.LoadAsset<Sprite>("Assets/icons/effigyOfRot.png");
        public Material material0 => Addressables.LoadAssetAsync<Material>("RoR2/Base/ShrineBlood/matShrineBlood.mat").WaitForCompletion();
        public Material material1 => Addressables.LoadAssetAsync<Material>("RoR2/Base/ShrineHealing/matShrineHealing.mat").WaitForCompletion();
        public Material material2 => Addressables.LoadAssetAsync<Material>("RoR2/Base/BeetleGland/matBeetleGland.mat").WaitForCompletion();
        public Material material3 => Addressables.LoadAssetAsync<Material>("RoR2/Base/ShrineBlood/matShrineBlood.mat").WaitForCompletion();
        private GameObject _poisonPrefab;
        public GameObject PoisonPrefab
        {
            get
            {
                if (_poisonPrefab == null)
                {
                    _poisonPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/MajorAndMinorConstruct/MajorConstructSpawnMinorConstructEffect.prefab").WaitForCompletion();
                }
                return _poisonPrefab;
            }
            set;
        }
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

        public EffigyOfRot(string _name, ItemTag[] _tags, ItemTier _tier, bool _canRemove = true, bool _isConsumed = false, bool _hidden = false) : 
        base(_name, _tags, _tier, _canRemove, _isConsumed, _hidden){}

        // Config
        public static ConfigItem<bool> EffigyOfRot_Enabled = new ConfigItem<bool>
        (
            "Void boss: Effigy Of Rot",
            "Item enabled",
            "Should this item appear in runs?",
            true
        );
        public static ConfigItem<float> EffigyOfRot_PoisonDuration = new ConfigItem<float>
        (
            "Void boss: Effigy Of Rot",
            "Poison duration",
            "Poison damage over time duration in seconds.",
            10f,
            1f,
            20f,
            1f
        );
        public static ConfigItem<float> EffigyOfRot_PoisonImmunityDuration = new ConfigItem<float>
        (
            "Void boss: Effigy Of Rot",
            "Poison spread immunity duration",
            "Seconds of hidden immunity to poison applied after it spreads to an enemy. Prevents infinite poison loops.",
            15f,
            1f,
            30f,
            1f
        );
        public static ConfigItem<float> EffigyOfRot_HealthPerSecond = new ConfigItem<float>
        (
            "Void boss: Effigy Of Rot",
            "Poison healing per second",
            "How much poison heals the inflictor per second.",
            3f,
            0.1f,
            6f,
            0.1f
        );
        public static ConfigItem<float> EffigyOfRot_HealthPerSecondStack = new ConfigItem<float>
        (
            "Void boss: Effigy Of Rot",
            "Poison healing per second (per stack)",
            "How much poison heals the inflictor per second, per additional stack.",
            3f,
            0.1f,
            6f,
            0.1f
        );
        public static ConfigItem<float> EffigyOfRot_SpreadChance = new ConfigItem<float>
        (
            "Void boss: Effigy Of Rot",
            "Poison spread chance per second",
            "Percent chance for poison to infect another enemy per second.",
            10f,
            0.1f,
            10f,
            0.1f
        );
        public static ConfigItem<float> EffigyOfRot_SpreadChanceStack = new ConfigItem<float>
        (
            "Void boss: Effigy Of Rot",
            "Poison spread chance per second (per stack)",
            "Percent chance for poison to infect another enemy per second, per additional stack.",
            5f,
            0.1f,
            5f,
            0.1f
        );
        public static ConfigItem<float> EffigyOfRot_SpreadRadius = new ConfigItem<float>
        (
            "Void boss: Effigy Of Rot",
            "Poison spread radius",
            "Poison can spread within this radius.",
            20f,
            1f,
            40f,
            1f
        );
        public static ConfigItem<float> EffigyOfRot_MeleeRange = new ConfigItem<float>
        (
            "Void boss: Effigy Of Rot",
            "Melee range",
            "Meters range within which damage is considered to be melee. Measured from the center of your character to the damage point of contact.",
            13f,
            1f,
            26f,
            1f
        );
        public static ConfigItem<bool> EffigyOfRot_Recipe = new ConfigItem<bool>
        (
            "Void boss: Effigy Of Rot",
            "Recipe enabled",
            "Should this item have a custom corruption recipe?",
            true
        );
        public static ConfigItem<string> EffigyOfRot_Ingredient1 = new ConfigItem<string>
        (
            "Void boss: Effigy Of Rot",
            "Recipe ingredient 1",
            "First ingredient for corruption recipe",
            "SiphonOnLowHealth"
        );
        public static ConfigItem<string> EffigyOfRot_Ingredient2 = new ConfigItem<string>
        (
            "Void boss: Effigy Of Rot",
            "Recipe ingredient 2",
            "Second ingredient for corruption recipe",
            "ViscousPot"
        );
        public static ConfigItem<bool> EffigyOfRot_CorruptsAllMiredUrns = new ConfigItem<bool>
        (
            "Void boss: Effigy Of Rot",
            "Corrupts all Mired Urns",
            "Corrupts all Mired Urns.",
            false
        );

        public GameObject OverwritePrefabMaterials()
        {
            GameObject ret = Main.Assets.LoadAsset<GameObject>("Assets/prefabs/effigyOfRot.prefab");

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
            string nameToken = ItemDef.nameToken;
            string descriptionToken = ItemDef.descriptionToken;

            if (EffigyOfRot_CorruptsAllMiredUrns.Value == true)
            {
                LanguageAPI.AddOverlay
                (
                    nameToken,
                    "<style=cIsVoid>Corrupts all Mired Urns.</style>"
                );
            }

            LanguageAPI.AddOverlay
            (
                descriptionToken,
                String.Format
                (
                    Language.currentLanguage.GetLocalizedStringByToken(descriptionToken),
                    EffigyOfRot_PoisonDuration.Value,
                    EffigyOfRot_HealthPerSecond.Value,
                    EffigyOfRot_HealthPerSecondStack.Value,
                    EffigyOfRot_SpreadChance.Value,
                    EffigyOfRot_SpreadChanceStack.Value,
                    EffigyOfRot_SpreadRadius.Value
                )
            );
        }

        // Hooks
        public override void RegisterHooks()
        {
            // Poison inflict, healing and spread
            On.RoR2.HealthComponent.TakeDamageProcess += (orig, self, damageInfo) =>
            {
                orig(self, damageInfo);

                if (damageInfo.attacker && self.body)
                {
                    CharacterBody attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();

                    if (!attackerBody || GetItemCountEffective(attackerBody) <= 0)
                    {
                        return;
                    }

                    int itemCount = GetItemCountEffective(attackerBody);

                    // On-hit
                    if (damageInfo.procCoefficient > 0f)
                    {
                        float distance = Vector3.Distance(damageInfo.position, attackerBody.corePosition);
                        if (distance <= EffigyOfRot_MeleeRange.Value)
                        {
                            InflictDotInfo newDot = new InflictDotInfo();
                            newDot.attackerObject = damageInfo.attacker;
                            newDot.damageMultiplier = 1f;
                            newDot.dotIndex = DotController.DotIndex.Poison;
                            newDot.duration = EffigyOfRot_PoisonDuration.Value;
                            newDot.hitHurtBox = self.body.mainHurtBox;
                            newDot.victimObject = self.body.gameObject;

                            DotController.InflictDot(ref newDot);
                        }
                    }

                    // On-poison
                    if ((damageInfo.dotIndex == DotController.DotIndex.Poison || damageInfo.dotIndex == DotController.DotIndex.Blight) && attackerBody.healthComponent && attackerBody.master)
                    {
                        float healTotal = (EffigyOfRot_HealthPerSecond.Value + (EffigyOfRot_HealthPerSecondStack.Value * (itemCount - 1))) / 3f;
                        float chanceTotal = (EffigyOfRot_SpreadChance.Value + (EffigyOfRot_SpreadChanceStack.Value * (itemCount - 1))) / 3f;
                        attackerBody.healthComponent.Heal(healTotal, new ProcChainMask());

                        if (Util.CheckRoll(chanceTotal, attackerBody.master.luck, attackerBody.master))
                        {
                            List<Collider> colliders = Physics.OverlapSphere(attackerBody.corePosition, EffigyOfRot_SpreadRadius.Value).ToList();
                            Util.ShuffleList(colliders);

                            foreach (Collider collider in colliders)
                            {
                                GameObject gameObject = collider.gameObject;
                                if (gameObject.GetComponentInChildren<CharacterBody>())
                                {
                                    CharacterBody colliderBody = gameObject.GetComponentInChildren<CharacterBody>();
                                    if (colliderBody != self.body && colliderBody.teamComponent && colliderBody.teamComponent.teamIndex != attackerBody.teamComponent.teamIndex && !colliderBody.gameObject.GetComponent<PoisonDoNotTransferBehavior>())
                                    {
                                        InflictDotInfo newDot = new InflictDotInfo();
                                        newDot.attackerObject = damageInfo.attacker;
                                        newDot.damageMultiplier = 1f;
                                        newDot.dotIndex = damageInfo.dotIndex;
                                        newDot.duration = EffigyOfRot_PoisonDuration.Value;
                                        newDot.hitHurtBox = colliderBody.mainHurtBox;
                                        newDot.victimObject = colliderBody.gameObject;

                                        colliderBody.gameObject.AddComponent<PoisonDoNotTransferBehavior>();
                                        PoisonInfectOrb.CreateInfectOrb(self.body.corePosition, colliderBody.mainHurtBox);

                                        DotController.InflictDot(ref newDot);

                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            };
        }

        public override void AddCorruptionRecipe()
        {
            if (EffigyOfRot_Recipe.Value == true && ItemInit.ViscousPot.Enabled)
            {
                ItemInit.MakeCorruptionRecipe
                (
                    EffigyOfRot_Ingredient1.Value,
                    EffigyOfRot_Ingredient2.Value,
                    ItemDef.name
                );
            }
        }

        public class PoisonDoNotTransferBehavior : MonoBehaviour
        {
            float timeLeft = EffigyOfRot_PoisonImmunityDuration.Value;

            void FixedUpdate()
            {
                timeLeft -= Time.fixedDeltaTime;

                if (timeLeft <= 0f)
                {
                    Destroy(this);
                }
            }
        }


    }
}