#if !AI_ENABLED
using System;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.AI.Toolkit.Accounts.Services
{
    public class AIDropdownConfigArgs
    {
        public Action<Button> button;
        public Action<PopupWindowContent> defaultContent;
        public PopupWindowContent content;
    }

    /// <summary>
    /// Mock AIDropdownConfig class which is supposed to come from the editor.
    /// </summary>
    class AIDropdownConfig : ScriptableSingleton<AIDropdownConfig>
    {
        public void RegisterController(AIDropdownConfigArgs args)
        {
            var button = new Button();
            button.Add(new TextElement {text = "AI"});
            button.Add(new TextElement {text = "AI"});
            args?.button?.Invoke(button);
        }
    }
}
#endif
