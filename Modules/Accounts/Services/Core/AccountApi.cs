using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AiEditorToolsSdk;
using AiEditorToolsSdk.Components.Common.Responses.Wrappers;
using AiEditorToolsSdk.Components.Organization;
using AiEditorToolsSdk.Components.Organization.Responses;
using UnityEditor;
using UnityEngine;

namespace Unity.AI.Toolkit.Accounts.Services.Core
{
    static class AccountApi
    {
        const string k_InternalMenu = "internal:";
        const string k_SetEnvironmentMenu = "AI Toolkit/Internals/AI.Account/Set Environment";
        const string k_SelectedEnvironmentKey = "AI_Toolkit_Account_Environment";

        const string k_ProdEnvironment = "https://musetools.unity.com";
        const string k_StagingEnvironment = "https://musetools-stg.unity.com";
        const string k_TestEnvironment = "https://musetools-test.unity.com";
        const string k_LocalEnvironment = "https://localhost:5050";

        static string selectedEnvironment
        {
            get => EditorPrefs.GetString(k_SelectedEnvironmentKey, k_StagingEnvironment);
            set => EditorPrefs.SetString(k_SelectedEnvironmentKey, value);
        }

        [MenuItem(k_InternalMenu + k_SetEnvironmentMenu + "/Production", false, 100)]
        static void SetProductionEnvironment() => selectedEnvironment = k_ProdEnvironment;

        [MenuItem(k_InternalMenu + k_SetEnvironmentMenu + "/Production", true, 100)]
        static bool ValidateSetProductionEnvironment()
        {
            Menu.SetChecked(k_SetEnvironmentMenu + "/Production", selectedEnvironment == k_ProdEnvironment);
            return true;
        }

        [MenuItem(k_InternalMenu + k_SetEnvironmentMenu + "/Staging", false, 100)]
        static void SetStagingEnvironment() => selectedEnvironment = k_StagingEnvironment;

        [MenuItem(k_InternalMenu + k_SetEnvironmentMenu + "/Staging", true, 100)]
        static bool ValidateSetStagingEnvironment()
        {
            Menu.SetChecked(k_SetEnvironmentMenu + "/Staging", selectedEnvironment == k_StagingEnvironment);
            return true;
        }

        [MenuItem(k_InternalMenu + k_SetEnvironmentMenu + "/Test", false, 100)]
        static void SetTestEnvironment() => selectedEnvironment = k_TestEnvironment;

        [MenuItem(k_InternalMenu + k_SetEnvironmentMenu + "/Test", true, 100)]
        static bool ValidateSetTestEnvironment()
        {
            Menu.SetChecked(k_SetEnvironmentMenu + "/Test", selectedEnvironment == k_TestEnvironment);
            return true;
        }

        [MenuItem(k_InternalMenu + k_SetEnvironmentMenu + "/Local :5050", false, 101)]
        static void SetLocalEnvironment() => selectedEnvironment = k_LocalEnvironment;

        [MenuItem(k_InternalMenu + k_SetEnvironmentMenu + "/Local :5050", true, 101)]
        static bool ValidateSetLocalEnvironment()
        {
            Menu.SetChecked(k_SetEnvironmentMenu + "/Local :5050", selectedEnvironment == k_LocalEnvironment);
            return true;
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

                var result = await callback(component);
                if (result.Result.IsSuccessful)
                {
                    return result.Result.Value;
                }
                else
                {
                    string errorMessage = $"Error: {result.Result.Error.AiResponseError} - {result.Result.Error.Errors.FirstOrDefault()} -- Result type: {typeof(TResponse).Name}";
                    if (!string.IsNullOrEmpty(CloudProjectSettings.organizationKey) && errorMessage != s_LastLoggedError)
                    {
                        Debug.Log(errorMessage);
                        s_LastLoggedError = errorMessage;
                    }
                }
            }
            catch (Exception exception)
            {
                string exceptionMessage = exception.ToString();
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
