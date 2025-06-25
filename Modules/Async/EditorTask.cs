using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Unity.AI.Toolkit
{
    /// <summary>
    /// Manages asynchronous operations in the Unity Editor. Facilitates main thread continuations
    /// that can progress even when play mode is paused or the editor is transitioning states.
    ///
    /// - `task.ConfigureAwaitMainThread()`: Use on an *existing Task*. Ensures the code after `await`
    ///   executes on the main Unity thread. It handles context switching, using `EditorApplication.delayCall`
    ///   (via `EditorAwaitable`) for the final step to the main thread. This makes it robust,
    ///   especially when play mode is paused.
    ///
    /// - `EditorTask.Run(...)`: Use to *initiate new work* (an Action or `Func Task `). It decides whether to
    ///   run work on a background thread (`Task.Run`) or synchronously, based on context. Awaiting
    ///   its returned Task also ensures the continuation is on the main thread, leveraging the same
    ///   `ConfigureAwaitMainThread` logic (and thus `EditorApplication.delayCall`) for resilience.
    ///
    /// The `isPlayingOrWillChangePlaymode` check critically influences behavior:
    /// If true (editor is playing, paused in play mode, or transitioning):
    ///   - `EditorTask.Run` prioritizes executing the core work on a background thread via `Task.Run`.
    ///   - Both methods ensure their main thread continuations are scheduled via `EditorApplication.delayCall`.
    /// This strategy allows asynchronous operations to "step" and complete even if Unity's standard game
    /// object updates are paused.
    /// If `isPlayingOrWillChangePlaymode` is false and the call is already on the main thread, operations
    /// may execute more directly for efficiency.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class EditorTask
    {
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
        public static Task Delay(int millisecondsDelay, CancellationToken cancellationToken) => Task.Delay(millisecondsDelay, cancellationToken).ConfigureAwaitMainThread();

        /// <summary>
        /// Run a task
        /// </summary>
        public static Task Run(Action action) => Run(action, CancellationToken.None);

        /// <summary>
        /// Run a task
        /// </summary>
        public static Task Run(Action action, CancellationToken cancellationToken)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            return Task.Run(action, cancellationToken).ConfigureAwaitMainThread();
        }

        /// <summary>
        /// Run a task
        /// </summary>
        public static Task<TResult> Run<TResult>(Func<Task<TResult>> function) => Run(function, CancellationToken.None);

        /// <summary>
        /// Run a task
        /// </summary>
        public static Task<TResult> Run<TResult>(Func<Task<TResult>> function, CancellationToken cancellationToken)
        {
            if (function == null)
                throw new ArgumentNullException(nameof(function));

            return Task.Run(function, cancellationToken).ConfigureAwaitMainThread();
        }

        /// <summary>
        /// Dispatch an action on the main thread.
        /// </summary>
        /// <param name="action">The action to dispatch.</param>
        /// <returns> A Task that completes when the action has been executed on the main thread.</returns>
        /// <exception cref="ArgumentNullException"> Thrown when the action is null.</exception>
        public static Task RunOnMainThread(Action action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            if (EditorThread.isMainThread)
            {
                action();
                return Task.CompletedTask;
            }

            return Task.Run(async () =>
            {
                await EditorThread.EnsureMainThreadAsync();
                action();
            });
        }

        /// <summary>
        /// Dispatch an asynchronous action on the main thread.
        /// </summary>
        /// <param name="asyncAction"> The asynchronous action to dispatch.</param>
        /// <returns> A Task that completes when the action has been executed on the main thread.</returns>
        /// <exception cref="ArgumentNullException"> Thrown when the action is null.</exception>
        public static Task RunOnMainThread(Func<Task> asyncAction) => RunOnMainThread(asyncAction, CancellationToken.None);

        /// <summary>
        /// Dispatch an asynchronous action on the main thread.
        /// </summary>
        /// <param name="asyncAction"> The asynchronous action to dispatch.</param>
        /// <param name="cancellationToken"></param>
        /// <returns> A Task that completes when the action has been executed on the main thread.</returns>
        /// <exception cref="ArgumentNullException"> Thrown when the action is null.</exception>
        public static Task RunOnMainThread(Func<Task> asyncAction, CancellationToken cancellationToken)
        {
            if (asyncAction == null)
                throw new ArgumentNullException(nameof(asyncAction));

            if (EditorThread.isMainThread)
            {
                return asyncAction();
            }

            return Task.Run(async () =>
            {
                await EditorThread.EnsureMainThreadAsync();
                await asyncAction();
            }, cancellationToken);
        }

        /// <summary>
        /// Dispatch an asynchronous action on the main thread that returns a result.
        /// </summary>
        /// <param name="asyncAction"> The asynchronous action to dispatch that returns a result.</param>
        /// <typeparam name="TResult"> The type of the result returned by the asynchronous action.</typeparam>
        /// <returns> A Task that completes with the result of the action executed on the main thread.</returns>
        /// <exception cref="ArgumentNullException"> Thrown when the action is null.</exception>
        public static Task<TResult> RunOnMainThread<TResult>(Func<Task<TResult>> asyncAction) => RunOnMainThread(asyncAction, CancellationToken.None);

        /// <summary>
        /// Dispatch an asynchronous action on the main thread that returns a result.
        /// </summary>
        /// <param name="asyncAction"> The asynchronous action to dispatch that returns a result.</param>
        /// <param name="cancellationToken"></param>
        /// <typeparam name="TResult"> The type of the result returned by the asynchronous action.</typeparam>
        /// <returns> A Task that completes with the result of the action executed on the main thread.</returns>
        /// <exception cref="ArgumentNullException"> Thrown when the action is null.</exception>
        public static Task<TResult> RunOnMainThread<TResult>(Func<Task<TResult>> asyncAction, CancellationToken cancellationToken)
        {
            if (asyncAction == null)
                throw new ArgumentNullException(nameof(asyncAction));

            if (EditorThread.isMainThread)
            {
                return asyncAction();
            }

            return Task.Run(async () =>
            {
                await EditorThread.EnsureMainThreadAsync();
                return await asyncAction();
            }, cancellationToken);
        }
    }
}
