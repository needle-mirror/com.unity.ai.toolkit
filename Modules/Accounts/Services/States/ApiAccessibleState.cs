using System;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Unity.AI.Toolkit.Accounts.Services.States
{
    public class ApiAccessibleState
    {
        static bool s_HasLoggedWarning = false;

        public static bool IsAccessible => Account.network.IsAvailable && Account.signIn.IsSignedIn && Account.cloudConnected.IsConnected;

        internal static async Task WaitForCloudProjectSettings()
        {
            var showedProgressBar = false;

            var time = DateTime.Now;
            try
            {
                // This loop will now continue even if the editor is unfocused
                // because of the DisplayProgressBar call inside.
                while (!IsAccessible)
                {
                    // Check for timeout
                    if (DateTime.Now - time > TimeSpan.FromSeconds(30)) // Increased timeout for robustness
                    {
                        if (!s_HasLoggedWarning)
                        {
                            if (!Application.isBatchMode)
                                Debug.LogWarning("Account API did not become accessible within 30 seconds. This may be due to network issues or editor focus.");
                            s_HasLoggedWarning = true;
                        }
                        return; // Exit after timeout
                    }

                    // If the editor is not in focus, we must manually tickle the main thread's
                    // update loop. Calling DisplayProgressBar is a known way to do this.
                    if (!EditorApplication.isFocused)
                    {
                        showedProgressBar = true;
                        EditorUtility.DisplayProgressBar("Initializing Services", "Waiting for API access...", 0.5f);
                    }

                    // Yield to the next frame to prevent a synchronous infinite loop.
                    // The progress bar call above ensures this await will complete.
                    await EditorTask.Delay(100); // Using a small delay is slightly better than Yield() in a tight poll
                }

                s_HasLoggedWarning = false;
            }
            finally
            {
                if (showedProgressBar)
                {
                    EditorUtility.ClearProgressBar();
                }
            }
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
