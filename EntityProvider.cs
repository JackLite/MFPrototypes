using ModulesFramework.Data;
using UnityEngine;

namespace Modules.Extensions.Prototypes
{
    /// <summary>
    ///     Simple container for entity so we can get it
    ///     It useful when we use physics and do not want create chains
    /// </summary>
    public class EntityProvider : MonoBehaviour
    {
        public Entity entity;
        public bool destroyEntityWhenDestroyed;

        private void OnDestroy()
        {
            if (destroyEntityWhenDestroyed && entity.IsAlive())
                entity.Destroy();
        }
    }
}