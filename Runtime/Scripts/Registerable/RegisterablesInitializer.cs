using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

using UnityEngine;

namespace Zlitz.General.Management
{
    internal static class RegisterablesInitializer
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            Type[] types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes()).Where(t => !t.IsAbstract).ToArray();

            HashSet<Type> loadedTypes = new HashSet<Type>();

            Type genericType = typeof(RegisterableScriptable<,>);
            foreach (Type type in types)
            {
                if (!DerivedFromGeneric(type, genericType, out Type baseType))
                {
                    continue;
                }

                if (loadedTypes.Add(baseType))
                {
                    MethodInfo loadMethod = baseType.GetMethod("Load", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                    loadMethod?.Invoke(null, null);
                }
            }

            foreach (Type loadedType in loadedTypes)
            {
                MethodInfo postLoadMethod = loadedType.GetMethod("PostLoad", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                postLoadMethod?.Invoke(null, null);
            }
        }

        private static bool DerivedFromGeneric(Type type, Type genericType, out Type baseType)
        {
            baseType = null;

            if (genericType == null)
            {
                return false;
            }

            while (type != null && type != typeof(object))
            {
                Type current = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
                if (genericType == current)
                {
                    baseType = type;
                    return true;
                }
                type = type.BaseType;
            }

            return false;
        }
    }
}
