using System;
using UnityEngine;

namespace Unity.AI.Toolkit.Accounts.Services.Core
{
    class Logger : AiEditorToolsSdk.Domain.Abstractions.Services.ILogger
    {
        readonly int m_MainThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;

        public void LogDebug(string message)
        {
            if (System.Threading.Thread.CurrentThread.ManagedThreadId != m_MainThreadId)
                return;
            Debug.Log(message);
        }

        public void LogDebug(Exception exception, string message)
        {
            if (System.Threading.Thread.CurrentThread.ManagedThreadId != m_MainThreadId)
                return;
            Debug.Log(message);
            Debug.LogException(exception);
        }
        public void LogDebug(Exception exception)
        {
            if (System.Threading.Thread.CurrentThread.ManagedThreadId != m_MainThreadId)
                return;
            Debug.LogException(exception);
        }

        public void LogPublicInformation(string message)
        {
            if (System.Threading.Thread.CurrentThread.ManagedThreadId != m_MainThreadId)
                return;
            Debug.Log(message);
        }

        public void LogPublicError(string message)
        {
            if (System.Threading.Thread.CurrentThread.ManagedThreadId != m_MainThreadId)
                return;
            Debug.LogError(message);
        }
    }
}
