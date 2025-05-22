using System;
using System.Threading.Tasks;
using Unity.AI.Toolkit.Accounts.Services.Core;
using Unity.AI.Toolkit.Accounts.Services.Data;
using Unity.AI.Toolkit;
using Unity.AI.Toolkit.Connect;
using UnityEngine;

namespace Unity.AI.Toolkit.Accounts.Services.States
{
    public class SettingsState
    {
        internal readonly Signal<SettingsRecord> settings;

        public event Action OnChange;
        public SettingsRecord Value { get => settings.Value; internal set => settings.Value = value; }
        public void Refresh() => settings.Refresh();

        public bool AiAssistantEnabled => Value?.IsAiAssistantEnabled ?? false;

        public bool AiGeneratorsEnabled => Value?.IsAiGeneratorsEnabled ?? false;

        public SettingsState()
        {
            settings = new(AccountPersistence.SettingsProxy, () => _ = RefreshInternal(), () => OnChange?.Invoke());
            Refresh();
            AIDropdownBridge.ConnectProjectStateChanged(Refresh);
            AIDropdownBridge.ConnectStateChanged(Refresh);
            AIDropdownBridge.UserStateChanged(Refresh);
        }

        async Task RefreshInternal() => Value = new(await AccountApi.GetSettings());
    }
}
