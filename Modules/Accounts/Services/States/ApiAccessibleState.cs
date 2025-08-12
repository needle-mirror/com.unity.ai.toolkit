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

        /// <summary>
        /// Asynchronously waits for the API to become accessible. This method is safe to call
        /// even when the Unity Editor is not in focus. It uses an EditorApplication.update
        /// subscription to poll for the required state, which keeps the async context alive.
        /// </summary>
        /// <returns>A Task that completes when the API is accessible or a timeout is reached.</returns>
        public static Task<bool> WaitForCloudProjectSettings()
        {
            // If the API is already accessible, we can return immediately.
            if (IsAccessible)
            {
                return Task.FromResult(true);
            }

            // Use a TaskCompletionSource to convert the event-based check into an awaitable Task.
            var tcs = new TaskCompletionSource<bool>();
            var timeout = DateTime.Now + TimeSpan.FromSeconds(30);

            EditorApplication.CallbackFunction updateCallback = null;

            // This delegate will be called on every editor update frame.
            updateCallback = () =>
            {
                // Check for success condition
                if (IsAccessible)
                {
                    s_HasLoggedWarning = false;
                    tcs.TrySetResult(true); // Complete the task successfully
                    EditorApplication.update -= updateCallback; // Unsubscribe to stop polling

                    return;
                }

                // Check for timeout condition
                if (DateTime.Now > timeout)
                {
                    if (!s_HasLoggedWarning)
                    {
                        if (!Application.isBatchMode)
                            Debug.LogWarning("Account API did not become accessible within 30 seconds. This may be due to network issues or editor focus.");
                        s_HasLoggedWarning = true;
                    }
                    tcs.TrySetResult(false); // Complete the task to unblock the caller
                    EditorApplication.update -= updateCallback; // Unsubscribe to stop polling
                }
            };

            // Start polling by subscribing to the editor's update loop.
            EditorApplication.update += updateCallback;

            return tcs.Task;
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
