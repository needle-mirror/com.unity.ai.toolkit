using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using Unity.AI.Toolkit.Accounts.Services;
using UnityEditor;
using UnityEngine;

namespace Unity.AI.Toolkit.GenerationContextMenu
{
    /// <summary>
    /// Use this attribute to register a method to be called when the user selects the "Generate" context menu item.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method), EditorBrowsable(EditorBrowsableState.Never)]
    public class GenerateContextMenuAttribute : Attribute
    {
        /// <summary>
        /// The name of the method that will be called to validate if the context menu item should be enabled.
        /// This is a required parameter.
        /// </summary>
        internal string ValidateFunctionName { get; }

        /// <summary>
        /// Use this attribute to register a method to be called when the user selects the "Generate" context menu item.
        /// </summary>
        /// <param name="validateFunction">
        /// The name of the method that will be called to validate if the context menu item should be enabled.
        /// This is a required parameter.
        /// </param>
        public GenerateContextMenuAttribute(string validateFunction)
        {
            ValidateFunctionName = validateFunction;
        }
    }

    [InitializeOnLoad, EditorBrowsable(EditorBrowsableState.Never)]
    static class GenerationContextMenu
    {
        static readonly List<(Action action, Func<bool> validateFunction)> k_GenerateContextMenuActions = new();

        static GenerationContextMenu()
        {
            foreach (var methodInfo in TypeCache.GetMethodsWithAttribute(typeof(GenerateContextMenuAttribute)))
            {
                var attribute = methodInfo.GetCustomAttribute<GenerateContextMenuAttribute>();
                if (attribute == null)
                    continue;

                var action = (Action)Delegate.CreateDelegate(typeof(Action), null, methodInfo);
                if (string.IsNullOrEmpty(attribute.ValidateFunctionName))
                    throw new InvalidOperationException($"Validate function name is not provided for {methodInfo.DeclaringType!.Name}.{methodInfo.Name}");
                var validateFunction = (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), methodInfo.DeclaringType!, attribute.ValidateFunctionName);
                k_GenerateContextMenuActions.Add((action, validateFunction));
            }
        }

        [MenuItem("Assets/Generate %g", false, 1)]
        static void Generate()
        {
            foreach (var (action, validateFunction) in k_GenerateContextMenuActions)
            {
                if (validateFunction())
                {
                    action();
                    return;
                }
            }
        }

        [MenuItem("Assets/Generate %g", true)]
        static bool ValidateGenerate()
        {
            if (!Account.settings.AiGeneratorsEnabled)
                return false;

            foreach (var (_, validateFunction) in k_GenerateContextMenuActions)
            {
                if (validateFunction())
                    return true;
            }

            return false;
        }
    }
}
