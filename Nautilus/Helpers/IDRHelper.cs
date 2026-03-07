using System.Collections.Generic;
using RoR2;
using UnityEngine;

namespace Nautilus
{
    public static class Helpers
    {
        public static GameObject PrepareItemDisplayModel(GameObject itemDisplayModel)
        {
            ItemDisplay itemDisplay = itemDisplayModel.AddComponent<ItemDisplay>();
            List<CharacterModel.RendererInfo> rendererInfos = new List<CharacterModel.RendererInfo>();
            foreach (Renderer renderer in itemDisplayModel.GetComponentsInChildren<Renderer>())
            {
                CharacterModel.RendererInfo rendererInfo = new CharacterModel.RendererInfo
                {
                    renderer = renderer,
                    defaultMaterial = renderer.material
                };
                rendererInfos.Add(rendererInfo);
            }
            itemDisplay.rendererInfos = rendererInfos.ToArray();

            return itemDisplayModel;
        }
    }
}