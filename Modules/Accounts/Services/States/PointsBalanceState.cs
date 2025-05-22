using System;
using System.Threading.Tasks;
using Unity.AI.Toolkit.Accounts.Services.Core;
using Unity.AI.Toolkit.Accounts.Services.Data;
using Unity.AI.Toolkit;

namespace Unity.AI.Toolkit.Accounts.Services.States
{
    public class PointsBalanceState
    {
        internal readonly Signal<PointsBalanceRecord> settings;

        public event Action OnChange;
        public PointsBalanceRecord Value { get => settings.Value; internal set => settings.Value = value; }
        public void Refresh() => settings.Refresh();

        public bool HasAny => Value?.PointsAvailable > 0;

        public PointsBalanceState() => settings = new(AccountPersistence.PointsBalanceProxy, () => _ = RefreshInternal(), () => OnChange?.Invoke());
        async Task RefreshInternal() => Value = new(await AccountApi.GetPointsBalance());
    }
}
