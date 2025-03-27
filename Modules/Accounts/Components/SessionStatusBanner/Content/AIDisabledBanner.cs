using System;
using Unity.AI.Toolkit.Accounts.Services.Core;
using UnityEngine.UIElements;

namespace Unity.AI.Toolkit.Accounts.Components
{
    [UxmlElement]
    public partial class AIDisabledBanner : BasicBannerContent
    {
        public AIDisabledBanner() : base(
            "AI features are not enabled. Your administrator has disabled AI features for your organization. <link=manageaccount><color=#7BAEFA>Manage account</color></link>",
            new LabelLink("manageaccount", AccountLinks.ManageAccount)
        ) { }
    }
}
