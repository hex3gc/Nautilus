using RoR2;
using Nautilus.Configuration;
using System;
using R2API;
using UnityEngine;
using UnityEngine.AddressableAssets;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;

namespace Nautilus.Items
{
    public static partial class ItemInit
    {
        public static RebelSoul RebelSoul = new RebelSoul
        (
            "RebelSoul",
            [ItemTag.Utility, ItemTag.Healing, ItemTag.AIBlacklist, ItemTag.BrotherBlacklist, ItemTag.ExtractorUnitBlacklist],
            ItemTier.VoidBoss
        );
    }

    /// <summary>
    ///     // Ver.1
    /// </summary>
    public class RebelSoul : ItemBase
    {
        public override bool Enabled => RebelSoul_Enabled.Value;
        public override ItemDef ConversionItemDef => Addressables.LoadAssetAsync<ItemDef>("RoR2/Base/SprintWisp/SprintWisp.asset").WaitForCompletion();
        public override GameObject itemPrefab => OverwritePrefabMaterials();
        public override Sprite itemIcon => Main.Assets.LoadAsset<Sprite>("Assets/icons/rebelSoul.png");
        public Material material0 => Addressables.LoadAssetAsync<Material>("RoR2/Base/Treebot/matTreebotTreeBark.mat").WaitForCompletion();
        public Material material1 => Addressables.LoadAssetAsync<Material>("RoR2/Base/Common/matDebugBlack.mat").WaitForCompletion();
        public Material material2 => Addressables.LoadAssetAsync<Material>("RoR2/DLC1/VoidMegaCrab/matVoidCrabAntiMatterParticleStreak.mat").WaitForCompletion();
        public Material material3 => Addressables.LoadAssetAsync<Material>("RoR2/DLC1/VoidSurvivor/matVoidSplatAreanIndicator.mat").WaitForCompletion();
        private GameObject _stealPrefab;
        public GameObject stealPrefab
        {
            get
            {
                if (_stealPrefab == null)
                {
                    _stealPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/VoidJailer/VoidJailerDartImpact.prefab").WaitForCompletion();
                }
                return _stealPrefab;
            }
            set;
        }
        private GameObject _blinkPrefab;
        public GameObject blinkPrefab
        {
            get
            {
                if (_blinkPrefab == null)
                {
                    _blinkPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/VoidRaidCrab/VoidRaidCrabImpact1.prefab").WaitForCompletion();
                }
                return _blinkPrefab;
            }
            set;
        }
        private GameObject _radiusPrefab;
        public GameObject radiusPrefab
        {
            get
            {
                if (_radiusPrefab == null)
                {
                    _radiusPrefab = UnityEngine.Object.Instantiate(Addressables.LoadAssetAsync<GameObject>("RoR2/DLC2/Items/AttackSpeedPerNearbyAllyOrEnemy/BolsteringLanternBonusIndicator.prefab").WaitForCompletion());
                    UnityEngine.Object.Destroy(_radiusPrefab.GetComponent<SphereCollider>());
                    UnityEngine.Object.Destroy(_radiusPrefab.GetComponent<AttackSpeedPerNearbyCollider>());

                    MeshRenderer radiusRenderer = _radiusPrefab.GetComponentInChildren<MeshRenderer>();
                    if (radiusRenderer)
                    {
                        radiusRenderer.material = material3;
                    }
                }
                return _radiusPrefab;
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

        public RebelSoul(string _name, ItemTag[] _tags, ItemTier _tier, bool _canRemove = true, bool _isConsumed = false, bool _hidden = false) : 
        base(_name, _tags, _tier, _canRemove, _isConsumed, _hidden){}

        // Config
        public static ConfigItem<bool> RebelSoul_Enabled = new ConfigItem<bool>
        (
            "Void boss: Rebel Soul",
            "Item enabled",
            "Should this item appear in runs?",
            true
        );
        /*
        public static ConfigItem<float> RebelSoul_MoveSpeedBoost = new ConfigItem<float>
        (
            "Void boss: Rebel Soul",
            "Movement speed boost",
            "Fractional movement speed boost while cloaked.",
            0.5f,
            0.05f,
            1f,
            0.05f
        );
        */
        public static ConfigItem<float> RebelSoul_BoostDuration = new ConfigItem<float>
        (
            "Void boss: Rebel Soul",
            "Cloak duration",
            "Length of cloaking in seconds.",
            4f,
            0.5f,
            6f,
            0.5f
        );
        public static ConfigItem<float> RebelSoul_BoostDurationStack = new ConfigItem<float>
        (
            "Void boss: Rebel Soul",
            "Cloak duration (per stack)",
            "Length of cloaking in seconds, per additional stack.",
            2f,
            0.5f,
            6f,
            0.5f
        );
        public static ConfigItem<float> RebelSoul_LifeSteal = new ConfigItem<float>
        (
            "Void boss: Rebel Soul",
            "Lifesteal fraction",
            "Fraction of enemy health to steal, and own health to heal per second.",
            0.02f,
            0.01f,
            0.1f,
            0.01f
        );
        public static ConfigItem<float> RebelSoul_LifeStealStack = new ConfigItem<float>
        (
            "Void boss: Rebel Soul",
            "Lifesteal fraction (per stack)",
            "Fraction of enemy health to steal, and own health to heal per second, per additional stack.",
            0.01f,
            0.01f,
            0.1f,
            0.01f
        );
        public static ConfigItem<float> RebelSoul_Radius = new ConfigItem<float>
        (
            "Void boss: Rebel Soul",
            "Lifesteal radius",
            "Meters radius of lifesteal around a cloaked player.",
            20f,
            1f,
            40f,
            1f
        );
        /*
        public static ConfigItem<float> RebelSoul_EquipmentCooldownReductionFraction = new ConfigItem<float>
        (
            "Void boss: Rebel Soul",
            "Cooldown reduction",
            "Fractional cooldown reduction from the first stack of this item.",
            0.1f,
            0f,
            0.5f,
            0.01f
        );
        */
        public static ConfigItem<bool> RebelSoul_Recipe = new ConfigItem<bool>
        (
            "Void boss: Rebel Soul",
            "Recipe enabled",
            "Should this item have a custom corruption recipe?",
            true
        );
        public static ConfigItem<string> RebelSoul_Ingredient1 = new ConfigItem<string>
        (
            "Void boss: Rebel Soul",
            "Recipe ingredient 1",
            "First ingredient for corruption recipe",
            "SprintWisp"
        );
        public static ConfigItem<string> RebelSoul_Ingredient2 = new ConfigItem<string>
        (
            "Void boss: Rebel Soul",
            "Recipe ingredient 2",
            "Second ingredient for corruption recipe",
            "ExplodeOnDeathVoid"
        );

        public GameObject OverwritePrefabMaterials()
        {
            GameObject ret = Main.Assets.LoadAsset<GameObject>("Assets/prefabs/rebelSoul.prefab");

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
                    RebelSoul_BoostDuration.Value,
                    RebelSoul_BoostDurationStack.Value,
                    RebelSoul_LifeSteal.Value * 100f,
                    RebelSoul_LifeStealStack.Value * 100f,
                    RebelSoul_Radius.Value
                )
            );
        }

        // Hooks
        public override void RegisterHooks()
        {
            // Equipment trigger
            On.RoR2.EquipmentSlot.OnEquipmentExecuted_byte_byte_EquipmentIndex += (orig, self, slot, set, index) =>
            {
                orig(self, slot, set, index);

                if (self.characterBody && self.characterBody.healthComponent && GetItemCountEffective(self.characterBody) > 0)
                {
                    if (!self.characterBody.gameObject.GetComponent<RebelSoulBehavior>())
                    {
                        self.characterBody.gameObject.AddComponent<RebelSoulBehavior>();
                    }
                    else
                    {
                        RebelSoulBehavior rebelSoulBehavior = self.characterBody.gameObject.GetComponent<RebelSoulBehavior>();
                        rebelSoulBehavior.ResetTimer();
                    }
                }
            };

            // Remove cloak on entering combat
            On.RoR2.CharacterBody.UpdateOutOfCombatAndDanger += (orig, self) =>
            {
                orig(self);

                if (!self.outOfCombat && self.gameObject.GetComponent<RebelSoulBehavior>())
                {
                    UnityEngine.Object.Destroy(self.gameObject.GetComponent<RebelSoulBehavior>());
                }
            };
        }

        // Recipe
        public override void AddCorruptionRecipe()
        {
            if (RebelSoul_Recipe.Value == true)
            {
                ItemInit.MakeCorruptionRecipe
                (
                    RebelSoul_Ingredient1.Value,
                    RebelSoul_Ingredient2.Value,
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
            /*
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
            */
            #endregion

            return rules;
        }
    }

    public class RebelSoulBehavior : MonoBehaviour
    {
        GameObject radius;
        CharacterBody characterBody;
        float scale = 20f;
        float stealInterval = 1f;
        float stealTimer = 0f;
        float timeToExpiration = 3f;
        float expirationTimer = 0f;
        float lifeStealFraction = 0.03f;
        int itemCount = 1;
        bool firstHit = true;

        void Awake()
        {
            radius = Instantiate(ItemInit.RebelSoul.radiusPrefab);
            NetworkedBodyAttachment networkedBodyAttachment = radius.GetComponent<NetworkedBodyAttachment>();
            characterBody = gameObject.GetComponent<CharacterBody>();
            
            networkedBodyAttachment.AttachToGameObjectAndSpawn(gameObject);
            UpdateScale();

            itemCount = characterBody.inventory.GetItemCountEffective(ItemInit.RebelSoul.ItemIndex);

            timeToExpiration = RebelSoul.RebelSoul_BoostDuration.Value;
            timeToExpiration += RebelSoul.RebelSoul_BoostDurationStack.Value * (itemCount - 1);

            lifeStealFraction = RebelSoul.RebelSoul_LifeSteal.Value;
            lifeStealFraction += RebelSoul.RebelSoul_LifeStealStack.Value * (itemCount - 1);

            ResetTimer();
            firstHit = true;

            EffectData effectData = new EffectData()
            {
                origin = characterBody.corePosition
            };
            EffectManager.SpawnEffect(ItemInit.RebelSoul.blinkPrefab, effectData, true);
        }

        void FixedUpdate()
        {
            stealTimer += Time.fixedDeltaTime;
            expirationTimer += Time.fixedDeltaTime;

            if (stealTimer >= stealInterval || firstHit)
            {
                List<Collider> colliders = Physics.OverlapSphere(characterBody.corePosition, scale).ToList();
                foreach(Collider collider in colliders)
                {
                    GameObject gameObject = collider.gameObject;
                    if (gameObject.GetComponentInChildren<CharacterBody>())
                    {
                        CharacterBody colliderBody = gameObject.GetComponentInChildren<CharacterBody>();
                        if (!colliderBody.isBoss && colliderBody.healthComponent && colliderBody.mainHurtBox && colliderBody.teamComponent && colliderBody.teamComponent.teamIndex != characterBody.teamComponent.teamIndex)
                        {
                            DamageInfo damageInfo = new DamageInfo
                            {
                                damage = colliderBody.maxHealth * lifeStealFraction,
                                crit = false,
                                inflictor = gameObject,
                                attacker = gameObject,
                                position = colliderBody.corePosition,
                                inflictedHurtbox = colliderBody.mainHurtBox,
                                procChainMask = new ProcChainMask(),
                                procCoefficient = 0f,
                                damageColorIndex = DamageColorIndex.Void
                            };

                            EffectData effectData = new EffectData()
                            {
                                origin = colliderBody.corePosition
                            };
                            EffectManager.SpawnEffect(ItemInit.RebelSoul.stealPrefab, effectData, true);

                            colliderBody.healthComponent.TakeDamage(damageInfo);
                        }

                        characterBody.healthComponent.HealFraction(lifeStealFraction, new ProcChainMask());
                    }
                }

                stealTimer = 0f;
                firstHit = false;
            }

            if (expirationTimer >= timeToExpiration + 0.1f)
            {
                Destroy(this);
            }
        }
        
        private void OnDestroy()
        {
            characterBody.SetBuffCount(RoR2Content.Buffs.Cloak.buffIndex, 0);
            characterBody.SetBuffCount(RoR2Content.Buffs.CloakSpeed.buffIndex, 0);
            characterBody.RecalculateStats();

            EffectData effectData = new EffectData()
            {
                origin = characterBody.corePosition
            };
            EffectManager.SpawnEffect(ItemInit.RebelSoul.blinkPrefab, effectData, true);

            Destroy(radius);
        }

        public void UpdateScale()
        {
            scale = RebelSoul.RebelSoul_Radius.Value;
            float scaleNum = scale / 10f;
            radius.transform.localScale = new UnityEngine.Vector3(scaleNum, scaleNum, scaleNum);
        }

        public void ResetTimer()
        {
            expirationTimer = 0f;

            characterBody.SetBuffCount(RoR2Content.Buffs.Cloak.buffIndex, 1);
            characterBody.SetBuffCount(RoR2Content.Buffs.CloakSpeed.buffIndex, 1);
            characterBody.outOfDangerStopwatch = 10f;
            characterBody.outOfCombatStopwatch = 10f;
            characterBody.UpdateOutOfCombatAndDanger();
        }
    }
}