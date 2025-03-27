using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;
#if OBJECT_SELECTOR_TOOLBAR_DECORATOR
using System.Linq;
using System.Reflection;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
#endif

namespace Unity.AI.Toolkit
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class GenerationObjectPicker
    {
        const bool k_MonitorObjectPickerTemplates = true;

        static readonly Dictionary<string, RegisteredTemplate> k_RegisteredTemplates = new();

        record RegisteredTemplate(string templatePath, Func<string, bool, string> createTemplate, string assetPath, Action<string> createAsset, Type assetType)
        {
            public Object templateObject;
        }

        /// <summary>
        /// Register an asset generation template to be used with the ObjectPicker
        /// </summary>
        /// <param name="templatePath">blank asset template path</param>
        /// <param name="createTemplate">blank asset template create function</param>
        /// <param name="assetPath">generate asset path on template pick</param>
        /// <param name="createAsset">generate asset action on template pick</param>
        public static void RegisterTemplate<T>(string templatePath, Func<string, bool, string> createTemplate, string assetPath, Action<string> createAsset) where T : Object
        {
            k_RegisteredTemplates.TryAdd(templatePath, new RegisteredTemplate(
                templatePath, createTemplate,
                assetPath, createAsset,
                typeof(T)));
        }

        [InitializeOnLoadMethod]
        static async void ObjectPickerBlankGenerationHook()
        {
            if (Application.isBatchMode)
                return;

            while (k_MonitorObjectPickerTemplates)
            {
                // poll every frame, the polling is pretty cheap, we will revisit this if it causes any issues. Using await NextFrameAsync() didn't behave properly on macOS.
                await Task.Yield();

                if (k_RegisteredTemplates.Count == 0)
                    continue;

                var templateObjects = new List<Object>();
                foreach (var template in k_RegisteredTemplates.Values)
                {
                    // create and assert the template asset if it doesn't exist
                    if (!File.Exists(template.templatePath))
                    {
                        // the template was moved
                        if (template.templateObject)
                            MakeOrphan(template.templateObject);

                        var createdPath = template.createTemplate(template.templatePath, false);
                        Debug.Assert(createdPath == template.templatePath, $"Failed to create template at {template.templatePath}");
                    }

                    if (!template.templateObject || AssetDatabase.GetAssetPath(template.templateObject) != template.templatePath)
                        template.templateObject = AssetDatabase.LoadAssetAtPath(template.templatePath, template.assetType);

                    templateObjects.Add(template.templateObject);
                }

                if (!ObjectSelectorUtilities.TryGetSelectedTemplate(templateObjects, out var selected) ||
                    !k_RegisteredTemplates.TryGetValue(AssetDatabase.GetAssetPath(selected), out var selectedTemplate))
                    continue;

                // generative template object was picked
                selectedTemplate.templateObject = null;
                var uniquePath = AssetDatabase.GenerateUniqueAssetPath(selectedTemplate.assetPath);
                AssetDatabase.MoveAsset(selectedTemplate.templatePath, uniquePath);
                AssetDatabase.Refresh();
                await Task.Yield(); // to prevent OnGUI repaint error logging
                selectedTemplate.createAsset(uniquePath);
            }
        }

        static void MakeOrphan(Object templateObject)
        {
            var assetPath = AssetDatabase.GetAssetPath(templateObject);
            if (string.IsNullOrEmpty(assetPath))
                return;

            var directory = Path.GetDirectoryName(assetPath);
            var extension = Path.GetExtension(assetPath);
            var uniqueAssetPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(directory, "Orphan Asset" + extension));

            AssetDatabase.MoveAsset(assetPath, uniqueAssetPath);
            AssetDatabase.Refresh();
        }

#if OBJECT_SELECTOR_TOOLBAR_DECORATOR

        [InitializeOnLoadMethod]
        static void SetupSelector()
        {
            try
            {
                ObjectSelectorUtils.SetupShownEventHandler(OnObjectSelectorShown);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to setup ObjectSelector toolbar decorator: {e.Message}");
            }
        }

        static void OnObjectSelectorShown(EditorWindow window)
        {
            var allowedTypes = ObjectSelectorUtils.GetAllowedTypes();
            if (allowedTypes is not { Length: > 0 })
                return;

            var templates = new List<RegisteredTemplate>();
            foreach (var template in k_RegisteredTemplates.Values)
            {
                foreach (var allowedType in allowedTypes)
                {
                    if (allowedType == null)
                        continue;

                    if (allowedType.IsAssignableFrom(template.assetType))
                    {
                        templates.Add(template);
                        break;
                    }
                }
            }

            if (templates.Count > 0)
            {
                // add a "Generate New" button next to the last button in the window's toolbar
                var toggle = ObjectSelectorUtils.GetTargetElement(window);
                if (toggle != null)
                {
                    var generateButton = new ToolbarButton { text = "Generate New" };
                    toggle.parent.Insert(toggle.parent.IndexOf(toggle), generateButton);
                    generateButton.clicked += () =>
                    {
                         if (templates.Count == 1)
                         {
                             SetSelectionFromTemplate(templates[0]);
                         }
                         else
                         {
                            var menu = new GenericMenu();
                            foreach (var template in templates)
                            {
                                menu.AddItem(new GUIContent(Path.GetFileNameWithoutExtension(template.assetPath)),
                                    false, () => SetSelectionFromTemplate(template));
                            }

                            menu.DropDown(generateButton.worldBound);
                         }
                    };
                }
            }

            return;

            void SetSelectionFromTemplate(RegisteredTemplate template)
            {
                var path = AssetDatabase.GenerateUniqueAssetPath(template.assetPath);
                path = template.createTemplate(path, true);
                var asset = AssetDatabase.LoadAssetAtPath(path, template.assetType);
                EditorApplication.delayCall += () => template.createAsset(path);
                ObjectSelectorUtils.SetSelection(asset.GetInstanceID());
            }
        }
#endif // OBJECT_SELECTOR_TOOLBAR_DECORATOR
    }
}
