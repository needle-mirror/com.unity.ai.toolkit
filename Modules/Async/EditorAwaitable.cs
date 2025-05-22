using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using UnityEditor;

namespace Unity.AI.Toolkit
{
    /// <summary>
    /// Custom awaitable that ensures the continuation runs on the Unity main thread.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public struct EditorAwaitable : INotifyCompletion
    {
        public EditorAwaitable GetAwaiter() => this;
        public bool IsCompleted => EditorThread.isMainThread;

        public void OnCompleted(Action continuation)
        {
            if (continuation == null)
                throw new ArgumentNullException(nameof(continuation));

            if (EditorThread.isMainThread)
                continuation();
            else
                EditorApplication.delayCall += () => continuation();
        }

        public void GetResult() {}
    }
}
