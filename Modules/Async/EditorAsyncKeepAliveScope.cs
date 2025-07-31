using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Unity.AI.Toolkit
{
    /// <summary>
    /// An IDisposable scope that ensures long-running editor async/await operations
    /// continue to progress even when the Unity Editor application is not in focus.
    /// It should be wrapped around the asynchronous logic in a `using` block.
    /// When the editor is unfocused, this scope initiates a background "heartbeat"
    /// that forces the main thread's task scheduler to update by repeatedly calling
    /// EditorUtility.DisplayProgressBar. This prevents main-thread `await` calls
    /// from stalling indefinitely while the editor is in the background.
    /// </summary>
    /// <example>
    /// <code>
    /// async Task MyLongEditorTask()
    /// {
    ///     using (new EditorAsyncKeepAliveScope())
    ///     {
    ///         Debug.Log("Starting operation...");
    ///         // This await will now complete even if you switch to another application
    ///         await Task.Delay(5000);
    ///         Debug.Log("Operation finished.");
    ///     }
    /// }
    /// </code>
    /// </example>
    public class EditorAsyncKeepAliveScope : IDisposable
    {
        static bool s_IsFocused = true;
        static int s_ActiveInstances = 0;
        static string s_ProgressTitle = null;
        static string s_ProgressMessage = null;
        static float s_ProgressValue = 1;

        static CancellationTokenSource s_BackgroundTaskCancellation;
        static Task s_BackgroundTask;

        const bool k_ShowProgressUntilFocused = false;
        readonly int m_ProgressID;

        [InitializeOnLoadMethod]
        static void RegisterFocusChange()
        {
            s_IsFocused = EditorApplication.isFocused;
            EditorApplication.focusChanged += OnFocusChanged;
        }

        static void OnFocusChanged(bool focus)
        {
            s_IsFocused = focus;

            if (s_IsFocused)
            {
                StopBackgroundTask();
                EditorUtility.ClearProgressBar();
            }
            else if (s_ActiveInstances > 0)
            {
                StartBackgroundTask();
            }
        }

        static void StartBackgroundTask()
        {
            if (s_BackgroundTask is { IsCompleted: false })
                return;

            s_BackgroundTaskCancellation?.Cancel();
            s_BackgroundTaskCancellation = new CancellationTokenSource();
            var token = s_BackgroundTaskCancellation.Token;

            // Display the progress bar once immediately for responsiveness.
            OnDelayCall();

            // Queue up two frames in case Task.Run takes a few frames to bootstrap.
            EditorApplication.delayCall += () => {
                OnDelayCall();
                EditorApplication.delayCall += OnDelayCall;
            };

            // This task runs entirely on a background thread.
            s_BackgroundTask = EditorTask.Run(async () =>
            {
                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        // Create a signal that the main thread will use to tell us it's done.
                        var tcs = new TaskCompletionSource<bool>();

                        // Schedule the work on the main thread.
                        // The lambda now does TWO things: pokes the UI and completes our task.
                        EditorApplication.delayCall += () =>
                        {
                            if (token.IsCancellationRequested)
                            {
                                tcs.TrySetCanceled();
                                return;
                            }
                            EditorUtility.DisplayProgressBar(s_ProgressTitle, s_ProgressMessage, s_ProgressValue);
                            tcs.TrySetResult(true); // Signal completion back to the background thread.
                        };

                        // 1. AWAIT CONFIRMATION: Wait for the main thread to run the delayCall.
                        // This will not hang because the DisplayProgressBar call inside the
                        // delegate will keep the editor alive long enough to complete this task.
                        await tcs.Task;

                        // 2. DELAY: Now that we KNOW the main thread is alive, we can safely
                        // wait for a short interval before scheduling the next poke.
                        await Task.Delay(25, token);
                    }
                }
                catch (OperationCanceledException)
                {
                    // This is expected when the task is stopped.
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }, token);

            return;

            void OnDelayCall()
            {
                if (token.IsCancellationRequested)
                    return;
                EditorUtility.DisplayProgressBar(s_ProgressTitle, s_ProgressMessage, s_ProgressValue);
            }
        }

        static void StopBackgroundTask()
        {
            s_BackgroundTaskCancellation?.Cancel();
            s_BackgroundTaskCancellation = null;
            s_BackgroundTask = null;
        }

        /// <summary>
        /// Creates a new editor focus scope that manages focus state and background processing.
        /// </summary>
        public EditorAsyncKeepAliveScope(string name = "")
        {
            if (Unsupported.IsDeveloperMode() && !string.IsNullOrEmpty(name))
            {
                m_ProgressID = Progress.Start("Internal: " + name);
                Progress.Report(m_ProgressID, 0.5f);
            }

            s_ActiveInstances++;

            if (!s_IsFocused)
                StartBackgroundTask();
        }

        /// <summary>
        /// Displays a progress bar when the editor is out of focus.
        /// Throws OperationCanceledException if the user cancels the operation.
        /// </summary>
        public static bool ShowProgressOrCancelIfUnfocused(string title, string message, float progress)
        {
            if (s_IsFocused)
                return false;

            s_ProgressTitle = title;
            s_ProgressMessage = message;
            s_ProgressValue = progress;

            EditorUtility.DisplayProgressBar(s_ProgressTitle, s_ProgressMessage, progress);
            return false;
        }

        /// <summary>
        /// Disposes of the scope, stopping the background task and clearing the progress bar
        /// if no other active instances remain.
        /// </summary>
        public void Dispose()
        {
            if (Unsupported.IsDeveloperMode() && Progress.Exists(m_ProgressID))
                Progress.Remove(m_ProgressID);

            s_ActiveInstances--;

            if (s_ActiveInstances > 0)
                return;

            s_ActiveInstances = 0;
#pragma warning disable CS0162 // Unreachable code detected
            if (k_ShowProgressUntilFocused && !s_IsFocused)
                return;
#pragma warning restore CS0162 // Unreachable code detected

            StopBackgroundTask();
            EditorUtility.ClearProgressBar();
        }
    }
}
