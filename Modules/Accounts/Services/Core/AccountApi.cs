#define UNITY_AI_OPEN_BETA
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AiEditorToolsSdk;
using AiEditorToolsSdk.Components.Common.Responses.Wrappers;
using AiEditorToolsSdk.Components.Organization;
using AiEditorToolsSdk.Components.Organization.Responses;
using Unity.AI.Toolkit;
using UnityEditor;
using UnityEngine;

namespace Unity.AI.Toolkit.Accounts.Services.Core
{
    static class AccountApi
    {
        const string k_SelectedEnvironmentKey = "AI_Toolkit_Account_Environment";

#if UNITY_AI_OPEN_BETA
        public const string prodEnvironment = "https://generators-beta.ai.unity.com";
        public const string stagingEnvironment = "https://generators-stg-beta.ai.unity.com";
        public const string testEnvironment = "https://generators-test-beta.ai.unity.com";
#else
        public const string prodEnvironment = "https://musetools.unity.com";
        public const string stagingEnvironment = "https://musetools-stg.unity.com";
        public const string testEnvironment = "https://musetools-test.unity.com";
#endif
        public const string localEnvironment = "https://localhost:5050";

        static string selectedEnvironment
        {
            get => EditorPrefs.GetString(k_SelectedEnvironmentKey, prodEnvironment);
            set => EditorPrefs.SetString(k_SelectedEnvironmentKey, value);
        }

        static string s_LastLoggedError = string.Empty;
        static string s_LastLoggedException = string.Empty;

        static async Task<TResponse> Request<TResponse>(Func<IOrganizationComponent, Task<OperationResult<TResponse>>> callback) where TResponse : class
        {
            try
            {
                using var client = new HttpClient();
                var builder = Builder.Build(CloudProjectSettings.organizationKey, CloudProjectSettings.userId, CloudProjectSettings.projectId, client, selectedEnvironment, new Logger(), new Auth());
                var component = builder.OrganizationComponent();

                var result = await EditorTask.Run(() => callback(component));
                if (result.Result.IsSuccessful)
                {
                    return result.Result.Value;
                }

                var errorMessage = $"Error: {result.Result.Error.AiResponseError} - {result.Result.Error.Errors.FirstOrDefault()} -- Result type: {typeof(TResponse).Name}";
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

#if AI_ENABLED
        internal static Task<SettingsResult> GetSettings() => Request(component => component.GetSettings());
        internal static Task<PointsBalanceResult> GetPointsBalance() => Request(component => component.GetPointsBalance());
        internal static Task<SettingsResult> SetTermsOfServiceAcceptance(bool value) =>
            Request(component => component.SetTermsOfServiceAcceptance(value));
#else

        // Unless actual button is present, fake response to always be available.
        internal static Task<SettingsResult> GetSettings() => Task.FromResult(new SettingsResult("", "", true, true, true, true));
        internal static Task<PointsBalanceResult> GetPointsBalance() => Task.FromResult(new PointsBalanceResult("", 5000, 4000));
        internal static Task<SettingsResult> SetTermsOfServiceAcceptance(bool value) => Task.FromResult(new SettingsResult("", "", true, true, true, true));
#endif
    }
}
