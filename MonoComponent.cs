using System;
using ModulesFramework.Data;

namespace Modules.Extensions.Prototypes
{
    /// <summary>
    ///     Wraps the component into the MonoBehaviour, so we can use Unity serialization system
    /// </summary>
    [Serializable]
    public class MonoComponent<T> : MonoComponent where T : struct
    {
        public T component;

        public override Type ComponentType => typeof(T);

        public override void Add(Entity entity)
        {
            entity.AddComponent(component);
        }

        public override void AddMultiple(Entity entity)
        {
            entity.AddNewComponent(component);
        }
    }

    [Serializable]
    public class MonoComponent
    {
        public virtual Type ComponentType => null;
        public virtual void Add(Entity entity){}
        public virtual void AddMultiple(Entity entity){}
    }
}