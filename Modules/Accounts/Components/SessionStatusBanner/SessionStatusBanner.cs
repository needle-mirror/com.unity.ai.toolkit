using System;
using Unity.AI.Toolkit.Accounts.Services;
using Unity.AI.Toolkit.Accounts.Services.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.AI.Toolkit.Accounts.Components
{
    [UxmlElement]
    public partial class SessionStatusBanner : VisualElement
    {
        AIDisabledBanner m_AiDisabled;
        ConnectToCloudBanner m_NotCloudConnected;
        SignInBanner m_SignIn;
        NoNetworkBanner m_NoNetwork;

        protected VisualElement m_Current;

        public SessionStatusBanner()
        {
            RegisterCallback<AttachToPanelEvent>(_ =>
            {
                Account.session.OnChange += Refresh;
                Refresh();
            });
            RegisterCallback<DetachFromPanelEvent>(_ =>
            {
                Account.session.OnChange -= Refresh;
            });
        }

        protected virtual VisualElement CurrentView()
        {
            if (!Account.network.IsAvailable)
                return m_NoNetwork ??= new();
            if (Account.signIn.IsSignedOut)
                return m_SignIn ??= new();
            if (Account.cloudConnected.Value == ProjectStatus.NotConnected)
                return m_NotCloudConnected ??= new();
            if (!Account.settings.AiEnabled)
                return m_AiDisabled ??= new();
            return null;
        }

        protected virtual void Refresh()
        {
            var current = CurrentView();
            if (m_Current != current)
            {
                Clear();
                m_Current = current;
                if (m_Current != null)
                    Add(m_Current);
            }
        }
    }
}
