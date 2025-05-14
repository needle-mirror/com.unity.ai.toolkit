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
        AIDisabledBanner m_AiDisabledBanner;
        ConnectToCloudBanner m_NotCloudConnected;
        SignInBanner m_SignIn;
        NoNetworkBanner m_NoNetwork;
        AIDisabledPackageBanner m_AIDisabledPackageBanner;
        AIDisabledLegalBanner m_AIDisabledLegalBanner;

        protected VisualElement m_Current;

        public SessionStatusBanner()
        {
            AddToClassList("session-status-banner");
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
            if (Account.signIn.Value == SignInStatus.NotReady)
                return new DropdownLoading("Loading user");
            if (Account.signIn.IsSignedOut)
                return m_SignIn ??= new();
            if (Account.cloudConnected.Value == ProjectStatus.NotReady)
                return new DropdownLoading("Checking cloud connection");
            if (Account.cloudConnected.Value == ProjectStatus.NotConnected)
                return m_NotCloudConnected ??= new();
            if (!Account.settings.AiAssistantEnabled && !Account.settings.AiGeneratorsEnabled)
                return m_AiDisabledBanner ??= new();
            if (!Account.legalAgreement.IsAgreed)
                return m_AIDisabledLegalBanner ??= new();
            if(!Account.settings.AiAssistantEnabled && this is AssistantSessionStatusBanner)
                return m_AIDisabledPackageBanner ??= new();
            if(!Account.settings.AiGeneratorsEnabled && this is GeneratorsSessionStatusBanner)
                return m_AIDisabledPackageBanner ??= new();
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
