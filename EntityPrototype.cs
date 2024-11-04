using System;
using System.Collections.Generic;
using System.Linq;
using ModulesFramework.Data;
using UnityEngine;

namespace Modules.Extensions.Prototypes
{
    [Serializable]
    public class EntityPrototype : ISerializationCallbackReceiver
    {
        public string customId = string.Empty;

        [SerializeReference]
        public List<MonoComponent> components = new();

        private HashSet<Type> _multipleTypes = new();

        public virtual Entity Create()
        {
            return Create(ModulesFramework.MF.World);
        }

        public virtual Entity Create(DataWorld world)
        {
            var ent = world.NewEntity();
            FillEntity(ent);
            if (!string.IsNullOrWhiteSpace(customId))
                ent.SetCustomId(customId);

            return ent;
        }

        public virtual void FillCustomId(Entity ent)
        {
            if (!string.IsNullOrWhiteSpace(customId))
                ent.SetCustomId(customId);
        }

        public virtual void FillEntity(Entity ent)
        {
            foreach (var monoComponent in components)
            {
                if (monoComponent == null)
                    continue;
                var isMultiple = _multipleTypes.Contains(monoComponent.ComponentType);
                if (isMultiple)
                    monoComponent.AddMultiple(ent);
                else
                    monoComponent.Add(ent);
            }
        }

        public virtual bool HasPrototypeFor<T>()
        {
            return components.Any(c => c.ComponentType == typeof(T));
        }

        public virtual T GetPrototypeComponent<T>() where T : struct
        {
            if (_multipleTypes.Contains(typeof(T)))
                throw new Exception($"Type {typeof(T).Name} is multiple. Use ");
            foreach (var wrapper in components)
            {
                if (wrapper.ComponentType == typeof(T))
                {
                    return ((MonoComponent<T>)wrapper).component;
                }
            }

            return default;
        }

        public virtual IEnumerable<T> GetPrototypeMultipleComponents<T>() where T : struct
        {
            if (!_multipleTypes.Contains(typeof(T)))
                throw new Exception($"Type {typeof(T).Name} is multiple. Use ");
            foreach (var wrapper in components)
            {
                if (wrapper.ComponentType == typeof(T))
                {
                    yield return ((MonoComponent<T>)wrapper).component;
                }
            }
        }

        public virtual void OnBeforeSerialize()
        {
            components.RemoveAll(c => c == null);
        }

        public virtual void OnAfterDeserialize()
        {
            _multipleTypes.Clear();
            components.RemoveAll(c => c == null);
            var counter = new Dictionary<Type, int>();
            foreach (var component in components)
            {
                if (component == null) // it's null in EditMode
                    continue;
                if (!counter.TryAdd(component.ComponentType, 1))
                    counter[component.ComponentType]++;
            }

            foreach (var (type, _) in counter.Where(kvp => kvp.Value > 1))
            {
                _multipleTypes.Add(type);
            }
        }
    }
}