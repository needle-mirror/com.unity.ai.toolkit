using System;
using Unity.AI.Toolkit.Accounts.Services.Core;
using UnityEngine.UIElements;

namespace Unity.AI.Toolkit.Accounts.Components
{
    [UxmlElement]
    public partial class LowPointsBanner : BasicBannerContent
    {
        const string k_Tooltip = "Your points balance is running low. Request a top-up to continue generating content without interruption.";

        public LowPointsBanner()
            : base(
                "Points balance low. <link=get-points><color=#7BAEFA>Request a points top-up</color></link> to keep using Unity AI. Your points refresh automatically each week.",
                new[] { new LabelLink("get-points", AccountLinks.GetPoints) },
            true)
        {
            this.Query<VisualElement>().ForEach(ve => ve.tooltip = k_Tooltip);
        }
    }
}
