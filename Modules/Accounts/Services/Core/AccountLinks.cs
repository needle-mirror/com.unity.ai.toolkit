using System;
using UnityEditor;
using UnityEngine;

namespace Unity.AI.Toolkit.Accounts.Services.Core
{
    static class AccountLinks
    {
        public static void ManageAccount()
        {
            var organizationId = CloudProjectSettings.organizationKey;
            if (string.IsNullOrEmpty(organizationId))
                Application.OpenURL("https://cloud.unity.com/home/organizations");
            else
                Application.OpenURL($"https://cloud.unity.com/home/organizations/{organizationId}/settings/general");
        }

        public static void ViewBundles() => Application.OpenURL("https://cloud.unity.com/home/ai");

        public static void OpenInPackageManager() =>
            UnityEditor.PackageManager.UI.Window.Open("com.unity.ai.toolkit");
    }
}
