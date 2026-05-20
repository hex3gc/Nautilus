using RoR2;
using Nautilus.Configuration;
using System;
using R2API;
using UnityEngine;
using UnityEngine.AddressableAssets;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using BepInEx;

namespace Nautilus.Items
{
    public static partial class ItemInit
    {
        public static ApathyCore ApathyCore = new ApathyCore
        (
            "ApathyCore",
            [ItemTag.Damage, ItemTag.Healing, ItemTag.AIBlacklist, ItemTag.BrotherBlacklist, ItemTag.CannotSteal, ItemTag.ExtractorUnitBlacklist, ItemTag.CanBeTemporary],
            ItemTier.VoidBoss
        );
    }

    /// <summary>
    ///     // Ver.1
    ///     Apathy Core rewards the destruction of your drone allies with permanent buffs to defense, and transient buffs to reward consistent ally generation (squid turrets, etc.)
    ///     This prohibits a 'hundreds of drones' run but lets you play into the specific buffs you get
    /// </summary>
    public class ApathyCore : ItemBase
    {
        public override bool Enabled => ApathyCore_Enabled.Value;
        public override ItemDef ConversionItemDef => Addressables.LoadAssetAsync<ItemDef>("RoR2/Base/RoboBallBuddy/RoboBallBuddy.asset").WaitForCompletion();
        public override GameObject itemPrefab => OverwritePrefabMaterials();
        public override Sprite itemIcon => Main.Assets.LoadAsset<Sprite>("Assets/icons/apathyCore.png");
        public Material material0 => Addressables.LoadAssetAsync<Material>("RoR2/DLC3/ironalluvium/matIAHexMetalPlateDark.mat").WaitForCompletion();
        public Material material1 => Addressables.LoadAssetAsync<Material>("RoR2/DLC1/VoidSurvivor/matVoidSurvivorCorruptOverlay.mat").WaitForCompletion();
        public Material material2 => Addressables.LoadAssetAsync<Material>("RoR2/DLC1/VoidSurvivor/matVoidBlinkBodyOverlayCorrupted.mat").WaitForCompletion();
        public Material material3 => Addressables.LoadAssetAsync<Material>("RoR2/Base/Titan/matTitanPebble.mat").WaitForCompletion();
        public Material material4 => Addressables.LoadAssetAsync<Material>("RoR2/Base/Titan/matTitanProjectile.mat").WaitForCompletion();
        public Material material5 => Addressables.LoadAssetAsync<Material>("RoR2/DLC3/ironalluvium/matIAHexMetalPlateDark.mat").WaitForCompletion();
        public BuffDef ApathyBuff;
        public BuffDef ApathyBuffTemp;
        private GameObject _explodePrefab;
        public GameObject explodePrefab
        {
            get
            {
                if (_explodePrefab == null)
                {
                    _explodePrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/VoidSurvivor/VoidSurvivorMegaBlasterExplosionCorrupted.prefab").WaitForCompletion();
                }
                return _explodePrefab;
            }
            set;
        }
        private Material _ExplodeOverlay;
        public Material ExplodeOverlay
        {
            get
            {
                if (_ExplodeOverlay == null)
                {
                    _ExplodeOverlay = Addressables.LoadAssetAsync<Material>("RoR2/DLC1/VoidSurvivor/matVoidBlinkBodyOverlayCorrupted.mat").WaitForCompletion();
                }
                return _ExplodeOverlay;
            }
            set;
        }
        private GameObject _individualExplodePrefab;
        public GameObject individualExplodePrefab
        {
            get
            {
                if (_individualExplodePrefab == null)
                {
                    _individualExplodePrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/BleedOnHitVoid/FractureImpactEffect.prefab").WaitForCompletion();
                }
                return _individualExplodePrefab;
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
        // Permanent allies should only include drones, as allies from items aren't truly permanent
        public List<string> PermanentAllies = new List<string>
        {
            // Buyable drone allies in vanilla
            "drone1",
            "drone2",
            "junkdrone",
            "haulerdrone",
            "flamedrone",
            "missiledrone",
            "emergencydrone",
            "equipmentdrone",
            "cleanupdrone",
            "rechargedrone",
            "jailerdrone",
            "megadrone",
            "copycatdrone",
            "bombardmentdrone",
            "turret1",

            // Allies also count as permanent if they only spawn once per stage
            "dronecommander",
            "titangold",

            // Modded allies
            "infernodrone",
            "voltaicdrone"
        };

        public ApathyCore(string _name, ItemTag[] _tags, ItemTier _tier, bool _canRemove = true, bool _isConsumed = false, bool _hidden = false) :
        base(_name, _tags, _tier, _canRemove, _isConsumed, _hidden)
        { }

        // Config
        public static ConfigItem<bool> ApathyCore_Enabled = new ConfigItem<bool>
        (
            "Void boss: Apathy Core",
            "Item enabled",
            "Should this item appear in runs?",
            true
        );
        public static ConfigItem<float> ApathyCore_ConsumptionInterval = new ConfigItem<float>
        (
            "Void boss: Apathy Core",
            "Consumption interval",
            "How often, in seconds, do nearby allies get consumed by the Apathy Core?",
            20f,
            1f,
            40f,
            1f
        );
        public static ConfigItem<float> ApathyCore_ShieldGain = new ConfigItem<float>
        (
            "Void boss: Apathy Core",
            "Shield gain",
            "On consumption, how much shields should be gained per stack?",
            5f,
            1f,
            40f,
            1f
        );
        public static ConfigItem<float> ApathyCore_ArmorGain = new ConfigItem<float>
        (
            "Void boss: Apathy Core",
            "Armor gain",
            "On consumption, how much armor should be gained?",
            2f,
            1f,
            10f,
            1f
        );
        public static ConfigItem<float> ApathyCore_CriticalDamage = new ConfigItem<float>
        (
            "Void boss: Apathy Core",
            "Critical strike damage gain",
            "On consumption, what fraction of critical strike damage should be gained?",
            0.05f,
            0.05f,
            0.5f,
            0.01f
        );
        public static ConfigItem<bool> ApathyCore_SkillChargeGainPrimary = new ConfigItem<bool>
        (
            "Void boss: Apathy Core",
            "Skill charge gain (primary)",
            "On consumption, add a charge of your primary skill?",
            true
        );
        public static ConfigItem<bool> ApathyCore_SkillChargeGainSecondary = new ConfigItem<bool>
        (
            "Void boss: Apathy Core",
            "Skill charge gain (secondary)",
            "On consumption, add a charge of your secondary skill?",
            true
        );
        public static ConfigItem<bool> ApathyCore_SkillChargeGainUtility = new ConfigItem<bool>
        (
            "Void boss: Apathy Core",
            "Skill charge gain (utility)",
            "On consumption, add a charge of your utility skill?",
            true
        );
        public static ConfigItem<bool> ApathyCore_SkillChargeGainSpecial = new ConfigItem<bool>
        (
            "Void boss: Apathy Core",
            "Skill charge gain (special)",
            "On consumption, add a charge of your special skill?",
            true
        );
        public static ConfigItem<float> ApathyCore_TemporaryBuffLength = new ConfigItem<float>
        (
            "Void boss: Apathy Core",
            "Temporary buff length",
            "When consuming a temporary ally, how many seconds should the buff last per stack?",
            60f,
            10f,
            120f,
            10f
        );
        public static ConfigItem<string> ApathyCore_ExtraDrones = new ConfigItem<string>
        (
            "Void boss: Apathy Core",
            "Additional allied minions to consider as permanent",
            "(Separate by commas!) Vanilla and Sandswept drones already count as permanent allies for this item. Add the CharacterBody name of any minion to make it considered permanent as well",
            ""
        );
        public static ConfigItem<bool> ApathyCore_Recipe = new ConfigItem<bool>
        (
            "Void boss: Apathy Core",
            "Recipe enabled",
            "Should this item have a custom corruption recipe?",
            true
        );
        public static ConfigItem<string> ApathyCore_Ingredient1 = new ConfigItem<string>
        (
            "Void boss: Apathy Core",
            "Recipe ingredient 1",
            "First ingredient for corruption recipe",
            "RoboBallBuddy"
        );
        public static ConfigItem<string> ApathyCore_Ingredient2 = new ConfigItem<string>
        (
            "Void boss: Apathy Core",
            "Recipe ingredient 2",
            "Second ingredient for corruption recipe",
            "ViscousPot"
        );

        public GameObject OverwritePrefabMaterials()
        {
            GameObject ret = Main.Assets.LoadAsset<GameObject>("Assets/prefabs/apathyCore.prefab");

            Material[] materials =
            {
                material0,
                material1,
                material2,
                material3,
                material4,
                material5
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
                    ApathyCore_ShieldGain.Value,
                    ApathyCore_ArmorGain.Value,
                    ApathyCore_CriticalDamage.Value * 100f,
                    ApathyCore_SkillChargeGainSecondary.Value,
                    ApathyCore_TemporaryBuffLength.Value
                )
            );
        }

        // Hooks
        public override void RegisterHooks()
        {
            CreateApathyBuff();
            CreateApathyBuffTemp();

            // Add/remove behavior on inventory change
            On.RoR2.CharacterBody.OnInventoryChanged += (orig, self) =>
            {
                orig(self);

                ApathyCoreBehavior behavior = self.GetComponent<ApathyCoreBehavior>();
                int itemCount = GetItemCountEffective(self);

                if (GetItemCountEffective(self) > 0 && !behavior)
                {
                    behavior = self.AddItemBehavior<ApathyCoreBehavior>(itemCount);
                }

                if (behavior)
                {
                    behavior.stack = itemCount;
                }

                if (GetItemCountEffective(self) <= 0 && behavior)
                {
                    UnityEngine.Object.Destroy(self.GetComponent<ApathyCoreBehavior>());
                }
            };

            // Set buff count and get stats from buffs
            RecalculateStatsAPI.GetStatCoefficients += (orig, self) =>
            {
                if (orig.inventory)
                {
                    int itemCount = orig.inventory.GetItemCountEffective(ItemInit.ApathyCoreHidden.ItemDef);
                    if (itemCount > 0)
                    {
                        orig.SetBuffCount(ApathyBuff.buffIndex, itemCount);
                    }

                    int totalBuffs = orig.GetBuffCount(ApathyBuff) + orig.GetBuffCount(ApathyBuffTemp);
                    if (totalBuffs > 0)
                    {
                        self.baseShieldAdd += totalBuffs * ApathyCore_ShieldGain.Value;
                        self.armorAdd += totalBuffs * ApathyCore_ArmorGain.Value;
                        self.critDamageMultAdd += totalBuffs * ApathyCore_CriticalDamage.Value;
                    }
                }
            };
        }

        // Recipe
        public override void AddCorruptionRecipe()
        {
            if (ApathyCore_Recipe.Value == true)
            {
                ItemInit.MakeCorruptionRecipe
                (
                    ApathyCore_Ingredient1.Value,
                    ApathyCore_Ingredient2.Value,
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

        public void CreateApathyBuff()
        {
            BuffDef apathyBuff = ScriptableObject.CreateInstance<BuffDef>();
            apathyBuff.buffColor = new Color(1f, 1f, 1f);
            apathyBuff.canStack = true;
            apathyBuff.isDebuff = false;
            apathyBuff.ignoreGrowthNectar = false;
            apathyBuff.name = "Apathy Core";
            apathyBuff.isHidden = false;
            apathyBuff.isCooldown = false;
            apathyBuff.iconSprite = Main.Assets.LoadAsset<Sprite>("Assets/icons/apathyCoreBuffPerm.png");
            ContentAddition.AddBuffDef(apathyBuff);

            ApathyBuff = apathyBuff;
        }

        public void CreateApathyBuffTemp()
        {
            BuffDef apathyBuffTemp = ScriptableObject.CreateInstance<BuffDef>();
            apathyBuffTemp.buffColor = new Color(1f, 1f, 1f);
            apathyBuffTemp.canStack = true;
            apathyBuffTemp.isDebuff = false;
            apathyBuffTemp.ignoreGrowthNectar = false;
            apathyBuffTemp.name = "Apathy Core (Temporary)";
            apathyBuffTemp.isHidden = false;
            apathyBuffTemp.isCooldown = false;
            apathyBuffTemp.iconSprite = Main.Assets.LoadAsset<Sprite>("Assets/icons/apathyCoreBuffTemp.png");
            ContentAddition.AddBuffDef(apathyBuffTemp);

            ApathyBuffTemp = apathyBuffTemp;
        }

        public List<string> GetAllAllyNames()
        {
            List<string> ret = new();
            ret.AddRange(PermanentAllies);

            if (!ApathyCore_ExtraDrones.Value.IsNullOrWhiteSpace())
            {
                List<string> customDroneNames = ApathyCore_ExtraDrones.Value.Split(',').ToList();
                for (int i = 0; i < customDroneNames.Count; i++)
                {
                    customDroneNames[i] = customDroneNames[i].Trim();
                }

                ret.AddRange(customDroneNames);
            }

            return ret;
        }
    }

    public class ApathyCoreBehavior : CharacterBody.ItemBehavior
    {
        private float consumptionTimer = 0f;
        private float consumptionInterval = ApathyCore.ApathyCore_ConsumptionInterval.Value;
        private float effectTimer = 0f;
        private float effectInterval = 0.5f;
        private bool effectStarted = false;

        void FixedUpdate()
        {
            consumptionTimer += Time.fixedDeltaTime;
            if (consumptionTimer >= consumptionInterval)
            {
                TryConsumeAlly();
                consumptionTimer = 0f;
            }

            if (effectStarted)
            {
                effectTimer += Time.fixedDeltaTime;
                if (effectTimer >= effectInterval)
                {
                    EffectData effectData = new EffectData()
                    {
                        origin = body.corePosition
                    };
                    EffectManager.SpawnEffect(ItemInit.ApathyCore.individualExplodePrefab, effectData, true);

                    ModelLocator modelLocator = body.gameObject.GetComponent<ModelLocator>();
                    if (modelLocator && modelLocator.modelTransform && modelLocator.modelTransform.GetComponentInParent<CharacterModel>())
                    {
                        TemporaryOverlay temporaryOverlay = body.gameObject.AddComponent<TemporaryOverlay>();
                        temporaryOverlay.duration = 1f;
                        temporaryOverlay.animateShaderAlpha = true;
                        temporaryOverlay.alphaCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
                        temporaryOverlay.destroyComponentOnEnd = true;
                        temporaryOverlay.originalMaterial = ItemInit.ApathyCore.ExplodeOverlay;
                        temporaryOverlay.AddToCharacerModel(modelLocator.modelTransform.GetComponentInParent<CharacterModel>());
                    }

                    effectTimer = 0f;
                    effectStarted = false;
                }
            }
        }

        private void TryConsumeAlly()
        {
            CharacterBody[] minionBodies = body.GetMinionBodies();
            Util.ShuffleArray(minionBodies);
            foreach (CharacterBody minionBody in minionBodies)
            {
                if (minionBody.master)
                {
                    EffectData effectData = new EffectData()
                    {
                        origin = minionBody.corePosition
                    };
                    EffectManager.SpawnEffect(ItemInit.ApathyCore.explodePrefab, effectData, true);

                    CollapseInfectOrb.CreateInfectOrb(minionBody.corePosition, body.mainHurtBox);
                    Util.PlaySound(EntityStates.VoidJailer.Weapon.ChargeFire.attackSoundEffect, body.gameObject);
                    effectStarted = true;

                    bool permanent = false;
                    foreach (string name in ItemInit.ApathyCore.GetAllAllyNames())
                    {
                        if (minionBody.name.ToLower().Contains(name))
                        {
                            permanent = true;
                        }
                    }

                    if (permanent)
                    {
                        body.inventory.GiveItemPermanent(ItemInit.ApathyCoreHidden.ItemDef);
                    }
                    else
                    {
                        body.AddTimedBuff(ItemInit.ApathyCore.ApathyBuffTemp, ApathyCore.ApathyCore_TemporaryBuffLength.Value * stack);
                    }
                    body.RecalculateStats();

                    if (body.skillLocator)
                    {
                        if (body.skillLocator.primary.maxStock >= 1 && ApathyCore.ApathyCore_SkillChargeGainPrimary.Value)
                        {
                            body.skillLocator.primary.AddOneStock();
                        }
                        if (body.skillLocator.secondary.maxStock >= 1 && ApathyCore.ApathyCore_SkillChargeGainSecondary.Value)
                        {
                            body.skillLocator.secondary.AddOneStock();
                        }
                        if (body.skillLocator.utility.maxStock >= 1 && ApathyCore.ApathyCore_SkillChargeGainUtility.Value)
                        {
                            body.skillLocator.utility.AddOneStock();
                        }
                        if (body.skillLocator.special.maxStock >= 1 && ApathyCore.ApathyCore_SkillChargeGainSpecial.Value)
                        {
                            body.skillLocator.special.AddOneStock();
                        }
                    }

                    if (minionBody.inventory)
                    {
                        minionBody.inventory.GiveItemPermanent(RoR2Content.Items.Ghost);
                    }

                    minionBody.master.TrueKill();
                    break;
                }
            }
        }
    }
}