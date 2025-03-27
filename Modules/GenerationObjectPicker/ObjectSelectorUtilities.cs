using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Unity.AI.Toolkit
{
    static class ObjectSelectorUtilities
    {
        public static Object GetCurrentSelectedObject()
        {
            var objectSelectorType = typeof(Editor).Assembly.GetType("UnityEditor.ObjectSelector");
            if (objectSelectorType == null)
            {
                Debug.LogError("Could not find type UnityEditor.ObjectSelector");
                return null;
            }

            var getCurrentObjectMethod = objectSelectorType.GetMethod("GetCurrentObject", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            if (getCurrentObjectMethod == null)
            {
                Debug.LogError("Could not find method GetCurrentObject on UnityEditor.ObjectSelector");
                return null;
            }

            return getCurrentObjectMethod.Invoke(null, null) as Object;
        }

        public static bool TryGetSelectedTemplate(IEnumerable<Object> templates, out Object selected)
        {
            selected = null;
            if (templates == null)
                return false;

            var currentSelected = GetCurrentSelectedObject();
            if (!currentSelected)
                return false;

            selected = templates.FirstOrDefault(t => t == currentSelected);
            return selected != null;
        }
    }
}
