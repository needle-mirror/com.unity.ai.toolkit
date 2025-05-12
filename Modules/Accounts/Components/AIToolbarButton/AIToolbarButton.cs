using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.AI.Toolkit.Accounts
{
    public static class AIToolbarButton
    {
        const string k_UssClassName = "ai-toolbar-button";

        const string k_NotificationVariantUssClassName = "ai-toolbar-button--with-points-notifications";

        const string k_StyleSheetPath =
            "Packages/com.unity.ai.toolkit/Modules/Accounts/Components/AIToolbarButton/AIToolbarButton.uss";

        const int k_NotificationDurationMs = 2000;

        static Button s_Button;
        static TextElement s_TextElement;
        static string s_OriginalContent;
        static IVisualElementScheduledItem s_AIToolbarButtonSchedule;

        public static void ShowPointsCostNotification(int amount)
        {
            if (s_Button == null)
                return;

            s_AIToolbarButtonSchedule?.Pause();
            s_TextElement.text = $"-{amount}";
            s_AIToolbarButtonSchedule = s_Button.schedule.Execute(() =>
            {
                s_TextElement.text = s_OriginalContent;
                s_Button.RemoveFromClassList(k_NotificationVariantUssClassName);
            }).StartingIn(k_NotificationDurationMs);
            s_Button.AddToClassList(k_NotificationVariantUssClassName);
        }

        internal static void Init(Button btn)
        {
            if (s_Button != null)
                return;

            s_Button = btn;
            s_TextElement = (TextElement)btn[1];
            s_OriginalContent = s_TextElement.text;

            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(k_StyleSheetPath);
            if (!styleSheet)
                return;

            s_Button.styleSheets.Add(styleSheet);
            s_Button.AddToClassList(k_UssClassName);
        }
    }
}
