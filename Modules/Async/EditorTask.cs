using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Unity.AI.Toolkit
{
    /// <summary>
    /// Manages asynchronous operations in the Unity Editor. This utility provides a robust `Run` method
    /// for executing work on background threads with continuations on the main thread, even when the
    /// editor is paused or out of focus.
    ///
    /// The `Run` method is designed for maximum safety and responsiveness:
    /// - **Immediate Cancellation:** When a CancellationToken is triggered, the calling code that is `await`-ing
    ///   the task is unblocked immediately with a `TaskCanceledException`.
    /// - **Cooperative Cancellation:** A cancellation signal is simultaneously sent to the background work,
    ///   allowing it to terminate gracefully.
    /// - **Abandoned Task Detection:** If the background work does not stop within 5 seconds of cancellation,
    ///   a warning is logged to the console to help developers identify non-cooperative async code.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class EditorTask
    {
        // This constant can be adjusted as needed.
        const int k_AbandonmentTimeoutMilliseconds = 5000;

        /// <summary>
        /// Extension method for Task. Awaits the task ensuring its direct continuation
        /// does not capture the Unity synchronization context, then ensures the
        /// final continuation (after this awaitable) runs on the main Unity thread.
        /// Returns a standard Task that completes on the main thread.
        /// </summary>
        public static async Task ConfigureAwaitMainThread(this Task task)
        {
            if (task == null)
                throw new ArgumentNullException(nameof(task));

            await task.ConfigureAwait(false);
            await EditorThread.EnsureMainThreadAsync();
        }

        /// <summary>
        /// Extension method for Task(TResult). Awaits the task ensuring its direct continuation
        /// does not capture the Unity synchronization context, then ensures the
        /// final continuation (after this awaitable) runs on the main Unity thread.
        /// Returns a standard Task(TResult) whose result is available on the main thread.
        /// </summary>
        public static async Task<TResult> ConfigureAwaitMainThread<TResult>(this Task<TResult> task)
        {
            if (task == null)
                throw new ArgumentNullException(nameof(task));

            var result = await task.ConfigureAwait(false);
            await EditorThread.EnsureMainThreadAsync();
            return result;
        }

        /// <summary>
        /// Editor is playing and paused
        /// </summary>
        public static bool isPlayingPaused
        {
            get
            {
                try { return EditorApplication.isPlayingOrWillChangePlaymode && EditorApplication.isPaused; }
                catch { return false; }
            }
        }

        /// <summary>
        /// Yield and return to the main thread. Important in paused play mode.
        /// </summary>
        public static Task Yield()
        {
            if (!EditorThread.isMainThread || isPlayingPaused)
                return Delay(1);

            return YieldAsync();
        }

        static async Task YieldAsync() => await Task.Yield();

        /// <summary>
        /// Yield and return to the main thread. Important in paused play mode.
        /// </summary>
        public static Task Delay(int millisecondsDelay) => Delay(millisecondsDelay, CancellationToken.None);

        /// <summary>
        /// Yield and return to the main thread. Important in paused play mode.
        /// </summary>
        public static Task Delay(int millisecondsDelay, CancellationToken cancellationToken) => Delay(TimeSpan.FromMilliseconds(millisecondsDelay), cancellationToken);

        /// <summary>
        /// Pauses for a specified duration using the editor's update loop, making it reliable
        /// even when the editor is paused or out of focus.
        /// </summary>
        public static Task Delay(TimeSpan delay, CancellationToken cancellationToken)
        {
            if (delay <= TimeSpan.Zero)
            {
                return Task.CompletedTask;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }

            var tcs = new TaskCompletionSource<bool>();
            var endTime = EditorApplication.timeSinceStartup + delay.TotalSeconds;

            EditorApplication.CallbackFunction updateCallback = null;
            CancellationTokenRegistration cancellationRegistration = default;

            updateCallback = () =>
            {
                if (EditorApplication.timeSinceStartup >= endTime)
                {
                    tcs.TrySetResult(true);
                    EditorApplication.update -= updateCallback;
                    cancellationRegistration.Dispose();
                }
            };

            cancellationRegistration = cancellationToken.Register(() =>
            {
                tcs.TrySetCanceled(cancellationToken);
                EditorApplication.update -= updateCallback;
            });

            EditorApplication.update += updateCallback;
            return tcs.Task;
        }

        /// <summary>
        /// Runs an action on a background thread with its continuation on the main thread.
        /// Guarantees the caller is unblocked immediately on cancellation while making a best effort
        /// to cooperatively cancel the background work.
        /// </summary>
        public static Task Run(Action action) => Run(action, CancellationToken.None);

        /// <summary>
        /// Runs an action on a background thread with its continuation on the main thread.
        /// Guarantees the caller is unblocked immediately on cancellation while making a best effort
        /// to cooperatively cancel the background work. Logs a warning if the background work
        /// does not terminate soon after cancellation.
        /// </summary>
        public static Task Run(Action action, CancellationToken cancellationToken)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            // This is a convenience wrapper around the generic version.
            return Run<bool>(() =>
            {
                action();
                return Task.FromResult(true);
            }, cancellationToken);
        }

        /// <summary>
        /// Runs an async function on a background thread with its continuation on the main thread.
        /// Guarantees the caller is unblocked immediately on cancellation while making a best effort
        /// to cooperatively cancel the background work.
        /// </summary>
        public static Task<TResult> Run<TResult>(Func<Task<TResult>> function) => Run(function, CancellationToken.None);

        /// <summary>
        /// Runs an async function on a background thread with its continuation on the main thread.
        /// Guarantees the caller is unblocked immediately on cancellation while making a best effort
        /// to cooperatively cancel the background work. Logs a warning if the background work
        /// does not terminate soon after cancellation.
        /// </summary>
        public static Task<TResult> Run<TResult>(Func<Task<TResult>> function, CancellationToken cancellationToken)
        {
            if (function == null)
                throw new ArgumentNullException(nameof(function));

            var tcs = new TaskCompletionSource<TResult>();
            if (cancellationToken.IsCancellationRequested)
            {
                tcs.SetCanceled();
                return tcs.Task;
            }

            // This CTS is used to stop the abandonment logger once the primary work is complete.
            var abandonmentLogCts = new CancellationTokenSource();
            CancellationTokenRegistration callerTokenRegistration = default;

            // Start the primary work on a background thread.
            _ = Task.Run(async () =>
            {
                try
                {
                    // Pass the token to the inner Task.Run for cooperative cancellation.
                    var result = await Task.Run(function, cancellationToken).ConfigureAwait(false);
                    await EditorThread.EnsureMainThreadAsync();
                    tcs.TrySetResult(result);
                }
                catch (Exception ex)
                {
                    if (ex is OperationCanceledException) tcs.TrySetCanceled(cancellationToken);
                    else tcs.TrySetException(ex);
                }
                finally
                {
                    // Signal that the primary work is finished, so the abandonment logger should stop.
                    abandonmentLogCts.Cancel();
                    // ReSharper disable once AccessToModifiedClosure
                    callerTokenRegistration.Dispose();
                }
            });

            // Register the action to take when the caller requests cancellation.
            callerTokenRegistration = cancellationToken.Register(() =>
            {
                // 1. Unblock the caller immediately. This is the highest priority.
                tcs.TrySetCanceled(cancellationToken);

                // 2. Start a "race". If the abandonmentLogCts isn't cancelled within the timeout,
                // it means the main task's finally block hasn't run, so we log a warning.
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(k_AbandonmentTimeoutMilliseconds, abandonmentLogCts.Token);
                        // If the delay completes without being cancelled, the task is abandoned.
                        // Dispatch the log to the main thread as Debug.Log is a Unity API.
                        _ = RunOnMainThread(() => Debug.LogWarning("An EditorTask was cancelled, but the background work did not complete within 5 seconds. The task may be non-cooperative and has been abandoned."));
                    }
                    catch (OperationCanceledException)
                    {
                        // This is the success path: the main task finished and cancelled our timer.
                    }
                });
            });

            // Ensure the abandonment CTS is disposed when the task completes, preventing leaks.
            tcs.Task.ContinueWith(_ => abandonmentLogCts.Dispose(), TaskContinuationOptions.ExecuteSynchronously);

            return tcs.Task;
        }

        /// <summary>
        /// Dispatch an action on the main thread.
        /// </summary>
        public static Task RunOnMainThread(Action action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            if (EditorThread.isMainThread)
            {
                action();
                return Task.CompletedTask;
            }

            var tcs = new TaskCompletionSource<bool>();
            EditorApplication.delayCall += () =>
            {
                try
                {
                    action();
                    tcs.TrySetResult(true);
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            };
            return tcs.Task;
        }

        /// <summary>
        /// Dispatch an asynchronous action on the main thread.
        /// </summary>
        public static Task RunOnMainThread(Func<Task> asyncAction) => RunOnMainThread(asyncAction, CancellationToken.None);

        /// <summary>
        /// Dispatch an asynchronous action on the main thread.
        /// </summary>
        public static Task RunOnMainThread(Func<Task> asyncAction, CancellationToken cancellationToken)
        {
            if (asyncAction == null)
                throw new ArgumentNullException(nameof(asyncAction));

            if (EditorThread.isMainThread)
            {
                return asyncAction(); // Note: cancellation is only best-effort here.
            }

            var tcs = new TaskCompletionSource<bool>();
            var registration = cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));

            // ReSharper disable once AsyncVoidLambda
            EditorApplication.delayCall += async () =>
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await asyncAction();
                    tcs.TrySetResult(true);
                }
                catch (Exception ex)
                {
                    if (ex is OperationCanceledException) tcs.TrySetCanceled(cancellationToken);
                    else tcs.TrySetException(ex);
                }
                finally
                {
                    registration.Dispose();
                }
            };
            return tcs.Task;
        }

        /// <summary>
        /// Dispatch an asynchronous action on the main thread that returns a result.
        /// </summary>
        public static Task<TResult> RunOnMainThread<TResult>(Func<Task<TResult>> asyncAction) => RunOnMainThread(asyncAction, CancellationToken.None);

        /// <summary>
        /// Dispatch an asynchronous action on the main thread that returns a result.
        /// </summary>
        public static Task<TResult> RunOnMainThread<TResult>(Func<Task<TResult>> asyncAction, CancellationToken cancellationToken)
        {
            if (asyncAction == null)
                throw new ArgumentNullException(nameof(asyncAction));

            if (EditorThread.isMainThread)
            {
                return asyncAction(); // Note: cancellation is only best-effort here.
            }

            var tcs = new TaskCompletionSource<TResult>();
            var registration = cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));

            // ReSharper disable once AsyncVoidLambda
            EditorApplication.delayCall += async () =>
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var result = await asyncAction();
                    tcs.TrySetResult(result);
                }
                catch (Exception ex)
                {
                    if (ex is OperationCanceledException) tcs.TrySetCanceled(cancellationToken);
                    else tcs.TrySetException(ex);
                }
                finally
                {
                    registration.Dispose();
                }
            };
            return tcs.Task;
        }
    }
}
