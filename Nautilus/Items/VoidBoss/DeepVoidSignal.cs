using RoR2;
using Nautilus.Configuration;
using System;
using R2API;
using UnityEngine;
using UnityEngine.AddressableAssets;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Networking;
using RoR2.Projectile;

namespace Nautilus.Items
{
    public static partial class ItemInit
    {
        public static DeepVoidSignal DeepVoidSignal = new DeepVoidSignal
        (
            "DeepVoidSignal",
            [ItemTag.Utility, ItemTag.AIBlacklist, ItemTag.BrotherBlacklist, ItemTag.CannotSteal, ItemTag.ExtractorUnitBlacklist, ItemTag.CanBeTemporary],
            ItemTier.VoidBoss
        );
    }

    /// <summary>
    ///     // Ver.1
    ///     A game-changing boss item that makes fights more lethal for both sides, and makes a ton of void explosions which we like
    /// </summary>
    public class DeepVoidSignal : ItemBase
    {
        public override bool Enabled => DeepVoidSignal_Enabled.Value;
        public override ItemDef ConversionItemDef => Addressables.LoadAssetAsync<ItemDef>("RoR2/DLC3/Items/ShockDamageAura/ShockDamageAura.asset").WaitForCompletion();
        public override GameObject itemPrefab => OverwritePrefabMaterials();
        public override Sprite itemIcon => Main.Assets.LoadAsset<Sprite>("Assets/icons/deepVoidSignal.png");
        public Material material0 => Addressables.LoadAssetAsync<Material>("RoR2/DLC1/CritGlassesVoid/matCritGlassesVoid.mat").WaitForCompletion();
        public Material material1 => Addressables.LoadAssetAsync<Material>("RoR2/DLC1/gauntlets/matGTVoidTerrain.mat").WaitForCompletion();
        public Material material2 => Addressables.LoadAssetAsync<Material>("RoR2/Base/Titan/matTitanPebble.mat").WaitForCompletion();
        public Material material3 => Addressables.LoadAssetAsync<Material>("RoR2/DLC1/VoidMegaCrab/matVoidCrabAntiMatterParticleStreak.mat").WaitForCompletion();
        public Material material4 => Addressables.LoadAssetAsync<Material>("RoR2/DLC1/voidstage/matVoidCoral.mat").WaitForCompletion();
        public Material material5 => Addressables.LoadAssetAsync<Material>("RoR2/Base/Common/matVoidDeathBombStreak.mat").WaitForCompletion();
        public BuffDef voidBuffDef => Addressables.LoadAssetAsync<BuffDef>("RoR2/DLC1/EliteVoid/bdEliteVoid.asset").WaitForCompletion();
        public GameObject deathBombProjectile => Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Nullifier/NullifierDeathBombProjectile.prefab").WaitForCompletion();
        public GameObject bigDeathBombProjectile => Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/VoidMegaCrab/VoidMegaCrabDeathBombProjectile.prefab").WaitForCompletion();
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

        public DeepVoidSignal(string _name, ItemTag[] _tags, ItemTier _tier, bool _canRemove = true, bool _isConsumed = false, bool _hidden = false) :
        base(_name, _tags, _tier, _canRemove, _isConsumed, _hidden)
        { }

        // Config
        public static ConfigItem<bool> DeepVoidSignal_Enabled = new ConfigItem<bool>
        (
            "Void boss: Deep Void Signal",
            "Item enabled",
            "Should this item appear in runs?",
            true
        );
        public static ConfigItem<int> DeepVoidSignal_InfestorSpawns = new ConfigItem<int>
        (
            "Void boss: Deep Void Signal",
            "Void Infestor spawns",
            "When using an interactable, how many Void Infestors should spawn?",
            2,
            1f,
            5f,
            1f
        );
        public static ConfigItem<int> DeepVoidSignal_InfestorSpawnsStack = new ConfigItem<int>
        (
            "Void boss: Deep Void Signal",
            "Void Infestor spawns (per stack)",
            "When using an interactable, how many Void Infestors should spawn per additional stack?",
            2,
            1f,
            5f,
            1f
        );
        public static ConfigItem<float> DeepVoidSignal_VoidPowerDuration = new ConfigItem<float>
        (
            "Void boss: Deep Void Signal",
            "Voidtouched power on interact duration",
            "How long the Voidtouched buff should last after using an interactable, in seconds",
            6f,
            1f,
            12f,
            1f
        );
        public static ConfigItem<float> DeepVoidSignal_VoidPowerDurationStack = new ConfigItem<float>
        (
            "Void boss: Deep Void Signal",
            "Voidtouched power on interact duration (per stack)",
            "How long the Voidtouched buff should last after using an interactable, in seconds per additional stack",
            2f,
            1f,
            12f,
            1f
        );
        public static ConfigItem<bool> DeepVoidSignal_Recipe = new ConfigItem<bool>
        (
            "Void boss: Deep Void Signal",
            "Recipe enabled",
            "Should this item have a custom corruption recipe?",
            true
        );
        public static ConfigItem<string> DeepVoidSignal_Ingredient1 = new ConfigItem<string>
        (
            "Void boss: Deep Void Signal",
            "Recipe ingredient 1",
            "First ingredient for corruption recipe",
            "ShockDamageAura"
        );
        public static ConfigItem<string> DeepVoidSignal_Ingredient2 = new ConfigItem<string>
        (
            "Void boss: Deep Void Signal",
            "Recipe ingredient 2",
            "Second ingredient for corruption recipe",
            "EffigyOfPride"
        );

        public GameObject OverwritePrefabMaterials()
        {
            GameObject ret = Main.Assets.LoadAsset<GameObject>("Assets/prefabs/deepVoidSignal.prefab");

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
                    DeepVoidSignal_InfestorSpawns.Value,
                    DeepVoidSignal_InfestorSpawnsStack.Value,
                    DeepVoidSignal_VoidPowerDuration.Value,
                    DeepVoidSignal_VoidPowerDurationStack.Value
                )
            );
        }

        // Hooks
        public override void RegisterHooks()
        {
            // Add/remove behavior on inventory change
            On.RoR2.CharacterBody.OnInventoryChanged += (orig, self) =>
            {
                orig(self);

                DeepVoidSignalBehavior behavior = self.GetComponent<DeepVoidSignalBehavior>();
                int itemCount = GetItemCountEffective(self);

                if (GetItemCountEffective(self) > 0 && !behavior)
                {
                    behavior = self.AddItemBehavior<DeepVoidSignalBehavior>(itemCount);
                }

                if (behavior)
                {
                    behavior.stack = itemCount;
                }

                if (GetItemCountEffective(self) <= 0 && behavior)
                {
                    UnityEngine.Object.Destroy(self.GetComponent<DeepVoidSignalBehavior>());
                }
            };

            // Interactions
            On.RoR2.GlobalEventManager.OnInteractionBegin += (orig, self, interactor, interactable, interactableObject) =>
            {
                orig(self, interactor, interactable, interactableObject);

                if (!interactor || interactable == null || !interactableObject)
                {
                    return;
                }
                  
                CharacterBody interactorBody = interactor.GetComponent<CharacterBody>();
                InteractionProcFilter interactionProcFilter = interactableObject.GetComponent<InteractionProcFilter>();
                if (interactorBody && interactorBody.inventory && GetItemCountEffective(interactorBody) > 0 && InteractableIsPermittedForSpawn((MonoBehaviour)interactable))
                {
                    int itemCount = GetItemCountEffective(interactorBody);

                    // Spawn void infestors
                    GameObject gameObject = new GameObject("infestorSpawner");
                    gameObject.transform.position = interactableObject.transform.position;
                    DeepVoidSignalInfestorSpawner deepVoidSignalInfestorSpawner = gameObject.AddComponent<DeepVoidSignalInfestorSpawner>();
                    deepVoidSignalInfestorSpawner.Initialize(DeepVoidSignal_InfestorSpawns.Value + (DeepVoidSignal_InfestorSpawnsStack.Value * (itemCount - 1)));

                    // Give Voidtouched buff
                    interactorBody.AddTimedBuff(voidBuffDef, DeepVoidSignal_VoidPowerDuration.Value + (DeepVoidSignal_VoidPowerDurationStack.Value * (itemCount - 1)));
                }
                
                // Filter for whether things should spawn on interact; copied from GlobalEventManager.OnInteractionBegin
                bool InteractableIsPermittedForSpawn(MonoBehaviour interactableAsMonoBehaviour)
                {
                    if (!interactableAsMonoBehaviour)
                    {
                        return false;
                    }
                    if ((bool)interactionProcFilter)
                    {
                        return interactionProcFilter.shouldAllowOnInteractionBeginProc;
                    }
                    if (interactable is PurchaseInteraction)
                    {
                        return !interactableObject.GetComponent<PurchaseInteraction>().disableSpawnOnInteraction;
                    }
                    if ((bool)interactableAsMonoBehaviour.GetComponent<DelusionChestController>())
                    {
                        if (interactableAsMonoBehaviour.GetComponent<PickupPickerController>().enabled)
                        {
                            return false;
                        }
                        return true;
                    }
                    if ((bool)interactableAsMonoBehaviour.GetComponent<GenericPickupController>())
                    {
                        return false;
                    }
                    if ((bool)interactableAsMonoBehaviour.GetComponent<VehicleSeat>())
                    {
                        return false;
                    }
                    if ((bool)interactableAsMonoBehaviour.GetComponent<NetworkUIPromptController>())
                    {
                        return false;
                    }
                    if ((bool)interactableAsMonoBehaviour.GetComponent<PowerPedestal>())
                    {
                        return interactableAsMonoBehaviour.GetComponent<PowerPedestal>().CanTriggerFireworks;
                    }
                    if ((bool)interactableAsMonoBehaviour.GetComponent<AccessCodesNodeController>())
                    {
                        return interactableAsMonoBehaviour.GetComponent<AccessCodesNodeController>().CheckInteractionOrder();
                    }
                    return true;
                }
            };
            
            // Implode on death
            On.RoR2.GlobalEventManager.OnCharacterDeath += (orig, self, damageReport) =>
            {
                orig(self, damageReport);

                if (damageReport.victimBody && damageReport.victimBody.HasBuff(voidBuffDef) && GameObject.FindAnyObjectByType<DeepVoidSignalBehavior>() != null)
                {
                    FireProjectileInfo fireProjectileInfo = new FireProjectileInfo
                    {
                        projectilePrefab = (damageReport.victimIsChampion || damageReport.victimIsBoss) ? bigDeathBombProjectile : deathBombProjectile,
                        position = damageReport.victimBody.corePosition,
                        rotation = Quaternion.identity,
                        owner = damageReport.victimBody.gameObject,
                        damage = damageReport.victimBody.damage,
                        crit = damageReport.victimBody.RollCrit()
                    };
                    ProjectileManager.instance.FireProjectile(fireProjectileInfo);
                }
            };
        }

        // Recipe
        public override void AddCorruptionRecipe()
        {
            if (DeepVoidSignal_Recipe.Value == true)
            {
                ItemInit.MakeCorruptionRecipe
                (
                    DeepVoidSignal_Ingredient1.Value,
                    DeepVoidSignal_Ingredient2.Value,
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
    }

    public class DeepVoidSignalBehavior : CharacterBody.ItemBehavior
    {

    }

    public class DeepVoidSignalInfestorSpawner : MonoBehaviour
    {
        private float spawnInterval = 0.5f;
        private float spawnTimer = 0.5f;
        private bool initialized = false;
        private int spawnAmount = 0;

        public void Initialize(int _spawnAmount)
        {
            spawnAmount = _spawnAmount;
            initialized = true;
        }

        void FixedUpdate()
        {
            if (initialized)
            {
                spawnTimer += Time.fixedDeltaTime;

                if (spawnTimer > spawnInterval)
                {
                    SpawnInfestor();
                    spawnAmount--;

                    if (spawnAmount <= 0)
                    {
                        Destroy(gameObject);
                    }
                }
            }
        }

        private void SpawnInfestor()
        {
            Vector3 upperPosition = new Vector3(gameObject.transform.position.x, gameObject.transform.position.y + 2f, gameObject.transform.position.z);
            GameObject infestor = UnityEngine.Object.Instantiate(Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/EliteVoid/VoidInfestorMaster.prefab").WaitForCompletion(), upperPosition, Quaternion.identity);
            CharacterMaster infestorMaster = infestor.GetComponent<CharacterMaster>();
            if (infestorMaster)
            {
                infestorMaster.teamIndex = TeamIndex.Void;
                NetworkServer.Spawn(infestor);
                infestorMaster.SpawnBodyHere();
            }
        }
    }
}