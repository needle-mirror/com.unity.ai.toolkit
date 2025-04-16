using System;
using Unity.AI.Toolkit.Accounts.Services;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.AI.Toolkit.Accounts.Services.Core;

namespace Unity.AI.Toolkit.Accounts.Components
{
    [UxmlElement]
    partial class AIDropdown : VisualElement
    {
        Points m_Points;
        Label m_DataSharing;

        public AIDropdown()
        {
            var tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/com.unity.ai.toolkit/Modules/Accounts/Components/AIDropdown/AIDropdown.uxml");
            tree.CloneTree(this);

            m_Points = this.Q<Points>("points");
            this.Q<Label>("manage-account").AddManipulator(new Clickable(AccountLinks.ManageAccount));
            m_DataSharing = this.Q<Label>("data-sharing");
            this.Q<Label>("check-updates").AddManipulator(new Clickable(AccountLinks.OpenInPackageManager));
            var manageAccountSeparator = this.Q<VisualElement>("manage-account-separator");
            var menuExtensions = this.Q<VisualElement>("menu-extensions");

            if (DropdownExtension.onExtend.Count > 0)
                manageAccountSeparator.RemoveFromClassList("hidden");

            Extensions.OnExtend(menuExtensions);

            RegisterCallback<AttachToPanelEvent>(_ =>
            {
                Account.settings.OnChange += Refresh;
                Account.network.OnChange += Refresh;
                Refresh();
            });
            RegisterCallback<DetachFromPanelEvent>(_ =>
            {
                Account.settings.OnChange -= Refresh;
                Account.network.OnChange -= Refresh;
            });
        }

        void Refresh()
        {
            m_Points.style.display = ShouldHidePoints ? DisplayStyle.None : DisplayStyle.Flex;
            m_DataSharing.text = Account.settings.Value.IsDataSharingEnabled ? "Data sharing on" : "";

            Extensions.OnShow(this);
        }

        static bool ShouldHidePoints =>
            !Account.network.IsAvailable ||
            (!Account.settings.AiAssistantEnabled && !Account.settings.AiGeneratorsEnabled);
    }
}
