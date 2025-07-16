using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AiEditorToolsSdk;
using AiEditorToolsSdk.Components.Common.Enums;
using AiEditorToolsSdk.Components.Common.Responses.Wrappers;
using AiEditorToolsSdk.Components.Organization;
using AiEditorToolsSdk.Components.Organization.Responses;
using AiEditorToolsSdk.Domain.Abstractions.Services;
using Unity.AI.Toolkit.Accounts.Services.States;
using UnityEditor;
using UnityEngine;

namespace Unity.AI.Toolkit.Accounts.Services.Core
{
    static class AccountApi
    {
        [InitializeOnLoadMethod]
        static void InitializeEnvironmentKeys() => Environment.RegisterEnvironmentKey(k_AccountEnvironmentKey, "Account Environment", _ => {
            Account.settings.Refresh();
            Account.pointsBalance.Refresh();
        });

        const string k_AccountEnvironmentKey = "AI_Toolkit_Account_Environment";

        public static string selectedEnvironment => Environment.GetSelectedEnvironment(k_AccountEnvironmentKey);

        static string s_LastLoggedError = string.Empty;
        static string s_LastLoggedException = string.Empty;

        static string s_SessionTraceId = Guid.NewGuid().ToString();

        class TraceIdProvider : ITraceIdProvider
        {
            readonly string m_SessionId;

            public TraceIdProvider(string sessionId) => m_SessionId = sessionId;

            public Task<string> GetTraceId() => Task.FromResult(m_SessionId);
        }

        static async Task<TResponse> Request<TResponse>(Func<IOrganizationComponent, Task<OperationResult<TResponse>>> callback) where TResponse : class
        {
            try
            {
                await ApiAccessibleState.WaitForCloudProjectSettings();

                using var client = new HttpClient();
                var builder = Builder.Build(CloudProjectSettings.organizationKey, CloudProjectSettings.userId, CloudProjectSettings.projectId, client, selectedEnvironment, new Logger(), new Auth(), new TraceIdProvider(s_SessionTraceId));
                var component = builder.OrganizationComponent();

                var result = await EditorTask.Run(() => callback(component));
                if (result.Result.IsSuccessful)
                {
                    return result.Result.Value;
                }

                Debug.Log($"Trace Id {result.SdkTraceId} => {result.W3CTraceId}");

                if (result.Result.Error.AiResponseError == AiResultErrorEnum.UnavailableForLegalReasons)
                    Account.settings.RegionAvailable = false;

                var errorMessage = $"Error: {result.Result.Error.AiResponseError} - {result.Result.Error.Errors.FirstOrDefault()} -- Result type: {typeof(TResponse).Name} -- Url: {selectedEnvironment}";
                if (!string.IsNullOrEmpty(CloudProjectSettings.organizationKey) && errorMessage != s_LastLoggedError)
                {
                    Debug.Log(errorMessage);
                    s_LastLoggedError = errorMessage;
                }
            }
            catch (Exception exception)
            {
                var exceptionMessage = exception.ToString();
                if (!string.IsNullOrEmpty(CloudProjectSettings.organizationKey) && exceptionMessage != s_LastLoggedException)
                {
                    Debug.Log($"Exception: {exceptionMessage}");
                    s_LastLoggedException = exceptionMessage;
                }
            }

            return null;
        }

        internal static Func<Task<SettingsResult>> GetSettingsDelegate = () => Request(component => component.GetSettings());
        internal static Func<Task<PointsBalanceResult>> GetPointsDelegate = () => Request(component => component.GetPointsBalance());

        internal static Task<SettingsResult> GetSettings() => GetSettingsDelegate();
        internal static Task<PointsBalanceResult> GetPointsBalance() => GetPointsDelegate();
        internal static Task<SettingsResult> SetTermsOfServiceAcceptance(bool value) =>
            Request(component => component.SetTermsOfServiceAcceptance(value));
    }
}
