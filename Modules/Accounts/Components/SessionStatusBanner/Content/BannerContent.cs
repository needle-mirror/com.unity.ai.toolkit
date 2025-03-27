using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.AI.Toolkit.Accounts.Components
{
    public class BasicBannerContent : VisualElement
    {
        public VisualElement content = new();

        public BasicBannerContent(string message, LabelLink link) : this(message, new List<LabelLink> {link}) { }
        public BasicBannerContent(string message = "", IEnumerable<LabelLink> links = null)
        {
            styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>("Packages/com.unity.ai.toolkit/Modules/Accounts/Components/SessionStatusBanner/SessionStatusBanner.uss"));
            styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>("Packages/com.unity.ai.toolkit/Modules/Accounts/Components/AIDropdownRoot/AIDropdownRoot.uss"));
            AddToClassList("banner");

            content.AddToClassList("banner-content");

            var warningIcon = new Image
            {
                image = EditorGUIUtility.IconContent("console.warnicon").image as Texture2D
            };
            warningIcon.AddToClassList("warning-icon");
            content.Add(warningIcon);
            content.Add(new RichLabel(message, links));

            Add(content);
        }
    }
}
