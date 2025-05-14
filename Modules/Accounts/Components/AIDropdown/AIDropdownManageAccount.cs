using Unity.AI.Toolkit.Accounts.Services;
using Unity.AI.Toolkit.Accounts.Services.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.AI.Toolkit.Accounts
{
    class AIDropdownManageAccount : VisualElement
    {
        // Disabling for now as the manage account link is not yet live and data sharing display messaging is not yet finalized.
        //[InitializeOnLoadMethod]
        //static void Init() => DropdownExtension.RegisterMainMenuExtension(container => container.Add(new AIDropdownManageAccount()), 4);

        Label m_DataSharing;

        public AIDropdownManageAccount()
        {
            var tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/com.unity.ai.toolkit/Modules/Accounts/Components/AIDropdown/AIDropdownManageAccount.uxml");
            tree.CloneTree(this);

            this.Q<Label>("manage-account").AddManipulator(new Clickable(AccountLinks.ManageAccount));
            m_DataSharing = this.Q<Label>("data-sharing");

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
            m_DataSharing.text = Account.settings.Value.IsDataSharingEnabled ? "Content data sharing on" : "Content data sharing off";
        }
    }
}
