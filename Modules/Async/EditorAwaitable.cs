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

        // This determines if the continuation runs synchronously
        public bool IsCompleted => false;  // Always use the async path for consistency

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
