using ModulesFramework.Data;
using UnityEngine;

namespace Modules.Extensions.Prototypes
{
    /// <summary>
    ///     Simple container for entity so we can get it or destroy with the GameObject
    /// </summary>
    public class EntityProvider : MonoBehaviour
    {
        public Entity entity;
        public bool destroyEntityWhenDestroyed;

        protected virtual void OnDestroy()
        {
            if (destroyEntityWhenDestroyed && entity.IsAlive())
                entity.Destroy();
        }
    }
}