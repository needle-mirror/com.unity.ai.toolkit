using System;
using Unity.AI.Toolkit.Accounts.Services.Core;
using Unity.AI.Toolkit.Accounts.Services.Data;
using Unity.AI.Toolkit.Connect;
using UnityEditor;
using UnityEngine;

namespace Unity.AI.Toolkit.Accounts.Services.States
{
    public class CloudConnectedState
    {
        internal readonly Signal<ProjectStatus> settings;

        public event Action OnChange;
        public ProjectStatus Value { get => settings.Value; internal set => settings.Value = value; }
        public void Refresh() => settings.Refresh();

        public bool IsConnected => Value == ProjectStatus.Connected;      // Is Api accessible with user rights.

        public CloudConnectedState()
        {
            settings = new(AccountPersistence.CloudConnectedProxy, RefreshInternal, () => OnChange?.Invoke());
            Refresh();
            AIDropdownBridge.ConnectProjectStateChanged(Refresh);
        }

        void RefreshInternal()
        {
            if (AIDropdownBridge.isProjectValid && Unsupported.IsDeveloperMode())
                Debug.Log($"Org id: {CloudProjectSettings.organizationKey}");

            if (!AIDropdownBridge.isProjectValid)
                Value = ProjectStatus.NotReady;
            else
                Value = CloudProjectSettings.projectBound ? ProjectStatus.Connected : ProjectStatus.NotConnected;
        }
    }
}
