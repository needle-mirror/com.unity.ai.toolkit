using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Unity.AI.Toolkit.Accounts.Services.States
{
    public class ApiAccessibleState
    {
        static bool s_HasLoggedWarning = false;

        public static bool IsAccessible => Account.network.IsAvailable && Account.signIn.IsSignedIn && Account.cloudConnected.IsConnected;

        internal static async Task WaitForCloudProjectSettings()
        {
            var time = DateTime.Now;
            while (!IsAccessible)
            {
                if (DateTime.Now - time > TimeSpan.FromSeconds(1))
                {
                    if (!s_HasLoggedWarning)
                    {
                        if (!Application.isBatchMode)
                            Debug.Log("Account API is not accessible. Please bring the Editor into focus or check your network connection or environment settings.");
                        s_HasLoggedWarning = true;
                    }
                    return;
                }

                await EditorTask.Yield();
            }

            s_HasLoggedWarning = false;
        }

        public event Action OnChange
        {
            add
            {
                Account.network.OnChange += value;
                Account.signIn.OnChange += value;
                Account.cloudConnected.OnChange += value;
            }
            remove
            {
                Account.network.OnChange -= value;
                Account.signIn.OnChange -= value;
                Account.cloudConnected.OnChange -= value;
            }
        }
    }
}
