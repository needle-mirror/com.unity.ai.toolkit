using System;
using AiEditorToolsSdk.Components.Organization.Responses;

namespace Unity.AI.Toolkit.Accounts.Services.Data
{
    [Serializable]
    public record SettingsRecord
    {
        public string OrgId;
        public bool IsAiAssistantEnabled;
        public bool IsAiGeneratorsEnabled;
        public bool IsDataSharingEnabled;
        public bool IsTermsOfServiceAccepted;

        public SettingsRecord(SettingsResult result)
        {
            OrgId = result.OrgId;
            IsAiAssistantEnabled = result.IsAiAssistantEnabled;
            IsAiGeneratorsEnabled = result.IsAiGeneratorsEnabled;
            IsDataSharingEnabled = result.IsDataSharingEnabled;
            IsTermsOfServiceAccepted = result.IsTermsOfServiceAccepted;
        }
    }

    [Serializable]
    public record PointsBalanceRecord
    {
        public string OrgId;
        public long PointsAllocated;
        public long PointsAvailable;

        public PointsBalanceRecord(PointsBalanceResult result)
        {
            OrgId = result.OrgId;
            PointsAllocated = result.PointsAllocated;
            PointsAvailable = result.PointsAvailable;
        }
    }

    [Serializable]
    public enum SignInStatus
    {
        NotReady,
        SignedIn,
        SignedOut,
    }

    [Serializable]
    public enum ProjectStatus
    {
        NotReady,
        Connected,
        NotConnected,
    }
}
