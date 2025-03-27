using System;
using Unity.AI.Toolkit.Accounts.Services;
using Unity.AI.Toolkit.Accounts.Services.Data;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.AI.Toolkit.Accounts.Components
{
    [UxmlElement]
    partial class AIDropdownRoot : VisualElement
    {
        VisualElement m_Content;
        VisualElement m_Current;

        AIDropdown m_Dropdown;
        SessionStatusBanner m_Banner;
        LegalAgreement m_LegalAgreement;

        public AIDropdownRoot()
        {
            var tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/com.unity.ai.toolkit/Modules/Accounts/Components/AIDropdownRoot/AIDropdownRoot.uxml");
            tree.CloneTree(this);

            if (!EditorGUIUtility.isProSkin)
                AddToClassList("light");

            m_Content = this.Q<VisualElement>("content");

            RegisterCallback<AttachToPanelEvent>(_ =>
            {
                Refresh();
                Account.session.OnChange += Refresh;
            });
            RegisterCallback<DetachFromPanelEvent>(_ =>
            {
                Account.session.OnChange -= Refresh;
            });
        }


        void Refresh()
        {
            VisualElement current;
            if (Account.signIn.Value == SignInStatus.NotReady)
                current = new DropdownLoading("Loading user");
            else if (Account.cloudConnected.Value == ProjectStatus.NotReady)
                current = new DropdownLoading("Checking cloud connection");
            else if (Account.signIn.IsSignedOut)
                current = m_Banner ??= new();
            else if (Account.cloudConnected.Value == ProjectStatus.NotConnected)
                current = m_Banner ??= new();
            else if (!Account.legalAgreement.Value)
                current = m_LegalAgreement ??= new();
            else if (Account.settings.Value == null || Account.pointsBalance.Value == null)
                current = new DropdownLoading("Loading account information");
            else
                current = m_Dropdown ??= new();

            if (m_Current != current)
            {
                m_Current = current;
                m_Content.Clear();
                m_Content.Add(m_Current);
            }
        }
    }
}
