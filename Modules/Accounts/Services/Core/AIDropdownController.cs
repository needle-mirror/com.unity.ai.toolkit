using System;
using Unity.AI.Toolkit.Accounts.Components;
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.AI.Toolkit.Accounts.Services
{
    static class AIDropdownController
    {
        internal static AIDropdownContent dropdownContent;
        internal static Button aiButton;

        [InitializeOnLoadMethod]
        internal static void Init() => AIDropdownConfig.instance.RegisterController(new()
        {
            button = button =>
            {
                aiButton = button;
                AIToolbarButton.Init(button);
                button.style.display = DisplayStyle.Flex;
            },
            content = dropdownContent ??= new()
        });

        internal static void Reset()
        {
            dropdownContent = null;
            aiButton = null;
            AIDropdownConfig.instance.RegisterController(null);
        }
    }
}
