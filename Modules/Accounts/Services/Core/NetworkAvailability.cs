using System;
using System.Threading.Tasks;
using Unity.AI.Toolkit;
using UnityEditor;
using UnityEngine;

namespace Unity.AI.Toolkit.Accounts.Services.Core
{
    static class NetworkAvailability
    {
        static Action s_OnChange;
        public static event Action OnChanged
        {
            add
            {
                if (s_OnChange == null) Start();
                s_OnChange += value;
            }
            remove
            {
                s_OnChange -= value;
                if (s_OnChange == null) Stop();
            }
        }
        public static bool IsAvailable => Application.internetReachability != NetworkReachability.NotReachable;
        public static int delay = 2000;

        static bool s_PreviousAvailability;
        static bool s_Cancel;

        public static void Start()
        {
            s_PreviousAvailability = IsAvailable;
            EditorApplication.quitting += Stop;
            AsyncUtils.SafeExecute(CheckNetworkAction);
        }

        public static void Stop()
        {
            s_Cancel = true;
            EditorApplication.delayCall -= OnDelayCall;
        }

        // Keep checking availability.
        // Tried using NetworkChange.NetworkAvailabilityChanged += OnNetworkAvailabilityChanged instead but Unity kept crashing with it and it throws on windows (not supported on this platform).
        static async Task CheckNetworkAction()
        {
            await EditorTask.Delay(delay);

            // Make sure to stop this method when quitting otherwise builds won't complete.
            if (!s_Cancel)
            {
                CheckNetworkAvailability();
                EditorApplication.delayCall += OnDelayCall;
            }
        }

        static void OnDelayCall() => AsyncUtils.SafeExecute(CheckNetworkAction);

        static void CheckNetworkAvailability()
        {
            if (s_PreviousAvailability != IsAvailable)
            {
                s_PreviousAvailability = IsAvailable;
                s_OnChange?.Invoke();
            }
        }
    }
}
