using UnityEngine;

namespace Modules.Extensions.Prototypes
{
    [DisallowMultipleComponent]
    public class EntityPrototypeComponent : MonoBehaviour
    {
        public bool createOnStart;
        public bool createEntityProvider = true;
        public bool destroyEntityWithGameObject;
        public EntityPrototype prototype;

        public void Start()
        {
            if (createOnStart)
                Create();
        }

        public void Create()
        {
            var ent = prototype.Create();
            if (createEntityProvider)
            {
                var entityProvider = gameObject.AddComponent<EntityProvider>();
                entityProvider.entity = ent;
                entityProvider.destroyEntityWhenDestroyed = destroyEntityWithGameObject;
            }
            else if (gameObject.TryGetComponent<EntityProvider>(out var entityProvider))
            {
                entityProvider.entity = ent;
                entityProvider.destroyEntityWhenDestroyed = destroyEntityWithGameObject;
            }

            Destroy(this);
        }
    }
}