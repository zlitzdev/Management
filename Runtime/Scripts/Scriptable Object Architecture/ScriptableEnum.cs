using System;
using System.Collections.Generic;

using UnityEngine;

namespace Zlitz.General.Management
{
    public abstract class ScriptableEnum<T, TId> : ScriptableObject where T : ScriptableEnum<T, TId> where TId : IComparable<TId>
    {
        [SerializeField]
        private TId m_id;

        public TId id => m_id;

        public virtual bool includeInRegistry => true;

        public static T Get(TId id)
        {
            if (ScriptableEnumManager.instance != null)
            {
                return ScriptableEnumManager.instance.Get<T, TId>(id);
            }
            return null;
        }

        public static IEnumerable<T> GetAll()
        {
            if (ScriptableEnumManager.instance != null)
            {
                return ScriptableEnumManager.instance.GetAll<T, TId>();
            }
            return null;
        }

        internal static bool CompareId(ScriptableObject obj1, ScriptableObject obj2)
        {
            if (obj1 is ScriptableEnum<T, TId> enum1 && obj2 is ScriptableEnum<T, TId> emum2)
            {
                return Comparer<TId>.Default.Compare(enum1.m_id, emum2.m_id) == 0;
            }
            return false;
        }

        internal static bool ShouldInclude(ScriptableObject obj)
        {
            if (obj is ScriptableEnum<T, TId> enumObject)
            {
                return enumObject.includeInRegistry;
            }
            return false;
        }
    }
}
