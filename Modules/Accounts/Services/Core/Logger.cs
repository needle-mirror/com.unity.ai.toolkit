using System;
using UnityEngine;

namespace Unity.AI.Toolkit.Accounts.Services.Core
{
    class Logger : AiEditorToolsSdk.Domain.Abstractions.Services.ILogger
    {
        public void LogDebug(string message)
        {
            EditorTask.RunOnMainThread(() =>
            {
                Debug.Log(message);
            });
        }

        public void LogDebug(Exception exception, string message)
        {
            EditorTask.RunOnMainThread(() =>
            {
                Debug.Log(message);
                Debug.LogException(exception);
            });
        }

        public void LogDebug(Exception exception)
        {
            EditorTask.RunOnMainThread(() =>
            {
                Debug.LogException(exception);
            });
        }

        public void LogPublicInformation(string message)
        {
            EditorTask.RunOnMainThread(() =>
            {
                Debug.Log(message);
            });
        }

        public void LogPublicError(string message)
        {
            EditorTask.RunOnMainThread(() =>
            {
                Debug.LogError(message);
            });
        }
    }}
