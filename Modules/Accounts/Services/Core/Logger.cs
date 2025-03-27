using System;
using UnityEngine;

namespace Unity.AI.Toolkit.Accounts.Services.Core
{
    class Logger : AiEditorToolsSdk.Domain.Abstractions.Services.ILogger
    {
        public void LogDebug(string message) => Debug.Log(message);

        public void LogDebug(Exception exception, string message)
        {
            Debug.Log(message);
            Debug.LogException(exception);
        }
        public void LogDebug(Exception exception) => Debug.LogException(exception);

        public void LogPublicInformation(string message) => Debug.Log(message);

        public void LogPublicError(string message) => Debug.LogError(message);
    }}
