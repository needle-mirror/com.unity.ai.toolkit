using System;
using Unity.AI.Toolkit.Accounts.Services;
using UnityEngine.UIElements;

namespace Unity.AI.Toolkit.Accounts.Manipulators
{
    /// <summary>
    /// Set the target UI Element enabled or disabled depending on the session status.
    ///
    /// The session status is essentially "if the user can actually do anything".
    ///
    /// More precisely:
    ///     * If there is a user signed in
    ///     * If the project is cloud connected to an organization
    ///     * If the internet is reachable
    ///     * If the user has agreed to required legal terms
    ///     * If the user has any points remaining
    /// </summary>
    public class SessionStatusTracker : Manipulator
    {
        readonly bool m_SetEnabled;
        readonly bool m_SetVisibility;
        readonly Action m_Callback;

        public SessionStatusTracker(bool setEnabled = true, bool setVisibility = false, Action callback = null)
        {
            m_SetEnabled = setEnabled;
            m_SetVisibility = setVisibility;
            m_Callback += callback;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            Account.sessionStatus.OnChange += Refresh;
            Refresh();
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            Account.sessionStatus.OnChange -= Refresh;
        }

        void Refresh()
        {
            if (m_SetEnabled)
                target.SetEnabled(Account.sessionStatus.IsUsable);
            else if (m_SetVisibility)
                target.style.display = Account.sessionStatus.IsUsable ? DisplayStyle.Flex : DisplayStyle.None;
            m_Callback?.Invoke();
        }
    }
}
