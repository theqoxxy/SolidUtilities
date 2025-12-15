namespace SolidUtilities.Editor
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using JetBrains.Annotations;
    using SolidUtilities;
    using UnityEditor;
    using UnityEditorInternals;
    using UnityEngine.Assertions;

    /// <summary>Different useful extensions for <see cref="SerializedProperty"/>.</summary>
    [PublicAPI]
    public static class SerializedPropertyExtensions
    {
        /// <summary>
        /// Checks whether the serialized property is built-in. <see cref="SerializedObject"/> has a lot of built-in
        /// properties and we are often interested only in the custom ones.
        /// </summary>
        /// <param name="property">The property to check.</param>
        /// <returns>Whether the property is built-in.</returns>
        public static bool IsBuiltIn(this SerializedProperty property)
        {
            if (property.name == "size" || property.name == "Array")
                return true;

            string firstTwoChars = property.name.Substring(0, 2);
            return firstTwoChars == "m_";
        }

        public static SerializedProperty GetParent(this SerializedProperty property)
        {
            if (!property.propertyPath.Contains('.'))
                return null;

            if (string.IsNullOrEmpty(property.propertyPath))
                return null;

            var parentPropertyPath = property.propertyPath.GetSubstringBeforeLast('.');
            if (parentPropertyPath.EndsWith(".Array"))
            {
                parentPropertyPath = parentPropertyPath.Substring(0, parentPropertyPath.Length - 6);
            }

            if (string.IsNullOrEmpty(parentPropertyPath))
                return null;

            return property.serializedObject.FindProperty(parentPropertyPath);
        }

        /// <summary>Gets type of the object serialized by the <paramref name="property"/>.</summary>
        /// <param name="property">The property whose type to find.</param>
        /// <returns>Type of the object serialized by <paramref name="property"/>.</returns>
        [NotNull, PublicAPI]
        public static Type GetObjectType(this SerializedProperty property) => property.GetFieldInfoAndType().Type;

        [PublicAPI]
        public static FieldInfo GetFieldInfo(this SerializedProperty property) => property.GetFieldInfoAndType().FieldInfo;

        public static T GetObject<T>(this SerializedProperty property) => (T) property.GetObject();

        public static object GetObject(this SerializedProperty property)
{
    var propertyPaths = property.propertyPath.Split('.');
    
    // Get the root property
    var rootProperty = property.serializedObject.FindProperty(propertyPaths[0]);
    if (rootProperty == null)
        return null;
        
    var fieldInfo = rootProperty.GetFieldInfo();
    if (fieldInfo == null)
        return null;
        
    // Get the root object from the serialized object's target
    object target = fieldInfo.GetValue(property.serializedObject.targetObject);
    if (target == null)
        return null;

    var currentProperty = rootProperty;
    
    // Traverse through the property path
    foreach (string path in propertyPaths.Skip(1))
    {
        if (path == "Array")
            continue;

        if (path.StartsWith("data["))
        {
            // Handle array elements
            int startIndex = path.IndexOf('[') + 1;
            int endIndex = path.IndexOf(']');
            if (startIndex > 0 && endIndex > startIndex)
            {
                string indexStr = path.Substring(startIndex, endIndex - startIndex);
                if (int.TryParse(indexStr, out int index))
                {
                    currentProperty = currentProperty.GetArrayElementAtIndex(index);
                    
                    // Safely handle IList access
                    if (target is IList list && index >= 0 && index < list.Count)
                    {
                        target = list[index];
                    }
                    else
                    {
                        return null;
                    }
                }
            }
        }
        else
        {
            // Handle nested properties
            currentProperty = currentProperty.FindPropertyRelative(path);
            if (currentProperty == null || target == null)
                return null;
                
            fieldInfo = currentProperty.GetFieldInfo();
            if (fieldInfo == null)
                return null;
                
            target = fieldInfo.GetValue(target);
        }
    }

    return target;
}
    }
}
