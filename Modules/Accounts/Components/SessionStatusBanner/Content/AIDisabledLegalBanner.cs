using System;
using UnityEngine.UIElements;

namespace Unity.AI.Toolkit.Accounts.Components
{
    [UxmlElement]
    public partial class AIDisabledLegalBanner : BasicBannerContent
    {
        public AIDisabledLegalBanner() : base("Legal agreement is missing.") { }
    }
}
