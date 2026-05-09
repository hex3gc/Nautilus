using UnityEngine;
using RoR2;
using RoR2.Orbs;

namespace Nautilus.Items
{
    public class CollapseInfectOrb : Orb
    {
        public override void Begin()
        {
            EffectData effectData = new EffectData
            {
                origin = origin,
                genericFloat = base.duration,
                scale = 2f
            };
            effectData.SetHurtBoxReference(target);
            EffectManager.SpawnEffect(OrbStorageUtility.Get("Prefabs/Effects/OrbEffects/InfusionOrbEffect"), effectData, transmit: true);
        }

        public static void CreateInfectOrb(Vector3 origin, HurtBox target)
        {
            CollapseInfectOrb orb = new CollapseInfectOrb();

            orb.duration = 0.5f;
            orb.origin = origin;
            orb.target = target;

            OrbManager.instance.AddOrb(orb);
        }
    }
}