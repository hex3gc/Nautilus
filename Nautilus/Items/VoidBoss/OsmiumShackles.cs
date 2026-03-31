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
        public static OsmiumShackles OsmiumShackles = new OsmiumShackles
        (
            "OsmiumShackles",
            [ItemTag.Damage],
            ItemTier.VoidBoss
        );
    }

    /// <summary>
    ///     // Ver.1
    /// </summary>
    public class OsmiumShackles : ItemBase
    {
        public override bool Enabled => OsmiumShackles_Enabled.Value;
        public override ItemDef ConversionItemDef => Addressables.LoadAssetAsync<ItemDef>("RoR2/Base/Knurl/Knurl.asset").WaitForCompletion();
        public override GameObject itemPrefab => OverwritePrefabMaterials();
        public override Sprite itemIcon => Main.Assets.LoadAsset<Sprite>("Assets/icons/osmiumShackles.png");
        public ItemDef ConversionItemDefExtra => Addressables.LoadAssetAsync<ItemDef>("RoR2/Base/LightningStrikeOnHit/LightningStrikeOnHit.asset").WaitForCompletion();
        public Material material0 => Addressables.LoadAssetAsync<Material>("RoR2/GlobalContent/matArtifact.mat").WaitForCompletion();
        public Material material1 => Addressables.LoadAssetAsync<Material>("RoR2/Base/Nullifier/matNullifierZoneAreaIndicator.mat").WaitForCompletion();
        public BuffDef OsmiumBuff;
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
                        radiusRenderer.material = material1;
                    }
                }
                return _radiusPrefab;
            }
            set;
        }
        private Material _OsmiumOverlay;
        public Material OsmiumOverlay
        {
            get
            {
                if (_OsmiumOverlay == null)
                {
                    _OsmiumOverlay = Addressables.LoadAssetAsync<Material>("RoR2/DLC1/VoidSurvivor/matVoidBlinkSwimOverlay.mat").WaitForCompletion();
                }
                return _OsmiumOverlay;
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

        public OsmiumShackles(string _name, ItemTag[] _tags, ItemTier _tier, bool _canRemove = true, bool _isConsumed = false, bool _hidden = false) : 
        base(_name, _tags, _tier, _canRemove, _isConsumed, _hidden){}

        // Config
        public static ConfigItem<bool> OsmiumShackles_Enabled = new ConfigItem<bool>
        (
            "Void boss: Osmium Shackles",
            "Item enabled",
            "Should this item appear in runs?",
            true
        );
        public static ConfigItem<float> OsmiumShackles_DebuffRadius = new ConfigItem<float>
        (
            "Void boss: Osmium Shackles",
            "Weigh down radius",
            "Radius where enemies are debuffed by this item.",
            20f,
            1f,
            40f,
            1f
        );
        public static ConfigItem<float> OsmiumShackles_DebuffRadiusStack = new ConfigItem<float>
        (
            "Void boss: Osmium Shackles",
            "Weigh down radius (per stack)",
            "Radius where enemies are debuffed by this item, per additional stack.",
            8f,
            1f,
            40f,
            1f
        );
        public static ConfigItem<float> OsmiumShackles_DebuffPercent = new ConfigItem<float>
        (
            "Void boss: Osmium Shackles",
            "Debuff encumbrance percentage",
            "Slows enemy speed, attack speed and cooldowns by this fraction.",
            0.3f,
            0.1f,
            1f,
            0.1f
        );
        public static ConfigItem<float> OsmiumShackles_CritAdd = new ConfigItem<float>
        (
            "Void boss: Osmium Shackles",
            "Crit addition",
            "Fractional crit chance increase against encumbered enemies",
            0.25f,
            0.05f,
            1f,
            0.05f
        );
        public static ConfigItem<bool> OsmiumShackles_Recipe = new ConfigItem<bool>
        (
            "Void boss: Osmium Shackles",
            "Recipe enabled",
            "Should this item have a custom corruption recipe?",
            true
        );
        public static ConfigItem<string> OsmiumShackles_Ingredient1 = new ConfigItem<string>
        (
            "Void boss: Osmium Shackles",
            "Recipe ingredient 1",
            "First ingredient for corruption recipe",
            "Knurl"
        );
        public static ConfigItem<string> OsmiumShackles_Ingredient2 = new ConfigItem<string>
        (
            "Void boss: Osmium Shackles",
            "Recipe ingredient 2",
            "Second ingredient for corruption recipe",
            "ElementalRingVoid"
        );

        public GameObject OverwritePrefabMaterials()
        {
            GameObject ret = Main.Assets.LoadAsset<GameObject>("Assets/prefabs/osmiumShackles.prefab");

            Material[] materials =
            {
                material0
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
                    OsmiumShackles_DebuffRadius.Value,
                    OsmiumShackles_DebuffRadiusStack.Value,
                    OsmiumShackles_DebuffPercent.Value * 100f,
                    OsmiumShackles_CritAdd.Value * 100f
                )
            );
        }

        // Hooks
        public override void RegisterHooks()
        {
            CreateOsmiumBuff();

            // Add/remove behavior on inventory change
            On.RoR2.CharacterBody.OnInventoryChanged += (orig, self) =>
            {
                orig(self);

                OsmiumShacklesBehavior behavior = self.gameObject.GetComponent<OsmiumShacklesBehavior>();
                int itemCount = GetItemCountEffective(self);

                if (GetItemCountEffective(self) > 0 && !behavior)
                {
                    behavior = self.gameObject.AddComponent<OsmiumShacklesBehavior>();
                }

                if (behavior)
                {
                    behavior.UpdateScale();
                }

                if(GetItemCountEffective(self) <= 0 && behavior)
                {
                    UnityEngine.Object.Destroy(self.gameObject.GetComponent<OsmiumShacklesBehavior>());
                }
            };

            // Debuff stats
            RecalculateStatsAPI.GetStatCoefficients += (orig, self) =>
            {
                int buffCount = orig.GetBuffCount(OsmiumBuff);
                if (buffCount > 0)
                {
                    self.attackSpeedTotalMult -= OsmiumShackles_DebuffPercent.Value;
                    self.moveSpeedTotalMult -= OsmiumShackles_DebuffPercent.Value;
                    self.allSkills.cooldownMultiplier *= -OsmiumShackles_DebuffPercent.Value;
                }
            };

            // Unconditional crit debuff
            On.RoR2.HealthComponent.TakeDamageProcess += (orig, self, damageInfo) =>
            {
                if (!damageInfo.crit && self.body && self.body.GetBuffCount(OsmiumBuff) > 0 && damageInfo.attacker != self.body.gameObject)
                {
                    if (Util.CheckRoll(OsmiumShackles_CritAdd.Value * 100f))
                    {
                        damageInfo.crit = true;
                    }
                }

                orig(self, damageInfo);
            };

            // Remove debuff instantly after leaving range
            On.RoR2.CharacterBody.OnBuffFinalStackLost += (orig, self, buffDef) =>
            {
                orig(self, buffDef);

                if (buffDef.buffIndex == OsmiumBuff.buffIndex)
                {
                    self.RecalculateStats();
                }
            };
        }

        // Recipe
        public override void AddCorruptionRecipe()
        {
            if (OsmiumShackles_Recipe.Value == true)
            {
                ItemInit.MakeCorruptionRecipe
                (
                    OsmiumShackles_Ingredient1.Value,
                    OsmiumShackles_Ingredient2.Value,
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

        public void CreateOsmiumBuff()
        {
            BuffDef osmiumBuff = ScriptableObject.CreateInstance<BuffDef>();
            osmiumBuff.buffColor = new Color(1f, 1f, 1f);
            osmiumBuff.canStack = false;
            osmiumBuff.isDebuff = true;
            osmiumBuff.name = "Osmium weight";
            osmiumBuff.isHidden = false;
            osmiumBuff.isCooldown = false;
            osmiumBuff.iconSprite = Main.Assets.LoadAsset<Sprite>("Assets/icons/osmiumBuff.png");
            ContentAddition.AddBuffDef(osmiumBuff);

            OsmiumBuff = osmiumBuff;
        }
    }

    public class OsmiumShacklesBehavior : MonoBehaviour
    {
        GameObject radius;
        CharacterBody characterBody;
        float scale = 20f;
        float debuffInterval = 0.5f;
        float debuffTimer = 0f;

        void Awake()
        {
            radius = Instantiate(ItemInit.OsmiumShackles.radiusPrefab);
            NetworkedBodyAttachment networkedBodyAttachment = radius.GetComponent<NetworkedBodyAttachment>();
            characterBody = gameObject.GetComponent<CharacterBody>();

            if (characterBody && networkedBodyAttachment)
            {
                networkedBodyAttachment.AttachToGameObjectAndSpawn(gameObject);
                UpdateScale();
            }
            else
            {
                Destroy(this);
            }
        }

        void FixedUpdate()
        {
            debuffTimer += Time.fixedDeltaTime;

            if (debuffTimer >= debuffInterval)
            {
                Collider[] colliders = Physics.OverlapSphere(characterBody.corePosition, scale);
                foreach(Collider collider in colliders)
                {
                    if (collider.gameObject)
                    {
                        GameObject colliderGameObject = collider.gameObject;
                        CharacterBody colliderBody = colliderGameObject.GetComponentInChildren<CharacterBody>();
                        if (colliderBody && colliderBody.teamComponent && colliderBody.teamComponent.teamIndex != characterBody.teamComponent.teamIndex)
                        {
                            colliderBody.AddTimedBuff(ItemInit.OsmiumShackles.OsmiumBuff, 1f);

                            ModelLocator modelLocator = colliderBody.gameObject.GetComponent<ModelLocator>();
                            if (modelLocator && modelLocator.modelTransform && modelLocator.modelTransform.GetComponentInParent<CharacterModel>())
                            {
                                TemporaryOverlay temporaryOverlay = colliderBody.gameObject.AddComponent<TemporaryOverlay>();
                                temporaryOverlay.duration = 1f;
                                temporaryOverlay.animateShaderAlpha = true;
                                temporaryOverlay.alphaCurve = AnimationCurve.Constant(0f, 1f, 1f);
                                temporaryOverlay.destroyComponentOnEnd = true;
                                temporaryOverlay.originalMaterial = ItemInit.OsmiumShackles.OsmiumOverlay;
                                temporaryOverlay.AddToCharacerModel(modelLocator.modelTransform.GetComponentInParent<CharacterModel>());
                            }
                        }
                    }
                }

                debuffTimer = 0f;
            }
        }
        
        private void OnDestroy()
        {
            Destroy(radius);
        }

        public void UpdateScale()
        {
            int itemCount = characterBody.inventory.GetItemCountEffective(ItemInit.OsmiumShackles.ItemDef);
            scale = OsmiumShackles.OsmiumShackles_DebuffRadius.Value + (OsmiumShackles.OsmiumShackles_DebuffRadiusStack.Value * (itemCount - 1));
            float scaleNum = scale / 10f; // Lantern prefab has a 10m radius instead of 20m???
            radius.transform.localScale = new UnityEngine.Vector3(scaleNum, scaleNum, scaleNum);
        }
    }
}