using ModulesFramework.Data;
using UnityEngine;

namespace Modules.Extensions.Prototypes
{
    [DisallowMultipleComponent]
    public class EntityPrototypeComponent : MonoBehaviour
    {
        public bool createOnStart = true;
        public bool createEntityProvider = true;
        public bool destroyEntityWithGameObject = true;
        public EntityPrototype prototype;

        public virtual void Start()
        {
            if (createOnStart)
                Create();
        }

        public virtual Entity Create()
        {
            var entity = prototype.Create();
            if (createEntityProvider)
            {
                var entityProvider = gameObject.AddComponent<EntityProvider>();
                entityProvider.entity = entity;
                entityProvider.destroyEntityWhenDestroyed = destroyEntityWithGameObject;
            }
            else if (gameObject.TryGetComponent<EntityProvider>(out var entityProvider))
            {
                entityProvider.entity = entity;
                entityProvider.destroyEntityWhenDestroyed = destroyEntityWithGameObject;
            }

            Destroy(this);
            return entity;
        }
    }
}