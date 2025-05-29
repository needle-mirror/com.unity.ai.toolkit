using System;
using Unity.AI.Toolkit.Accounts.Services.Core;
using UnityEngine.UIElements;

namespace Unity.AI.Toolkit.Accounts.Components
{
    [UxmlElement]
    partial class AccountLoadDelayedBanner : BasicBannerContent
    {
        public AccountLoadDelayedBanner() : base(
            $"Unable to load account information from {AccountApi.selectedEnvironment}",
            null,
            "Loading account information", TimeSpan.FromSeconds(10)) { }
    }
}
