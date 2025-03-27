using System;
using System.Threading.Tasks;
using Unity.AI.Toolkit.Accounts.Services.Core;
using Unity.AI.Toolkit.Accounts.Services.Data;
using UnityEngine;

namespace Unity.AI.Toolkit.Accounts.Services.States
{
    public class SettingsState
    {
        internal readonly Signal<SettingsRecord> settings;

        public event Action OnChange;
        public SettingsRecord Value { get => settings.Value; internal set => settings.Value = value; }
        public void Refresh() => settings.Refresh();

        public bool AiEnabled => Value?.IsAiEnabled ?? false;

        public SettingsState() => settings = new(AccountPersistence.SettingsProxy, () => _ = RefreshInternal(), () => OnChange?.Invoke());
        async Task RefreshInternal() => Value = new(await AccountApi.GetSettings());
    }
}
