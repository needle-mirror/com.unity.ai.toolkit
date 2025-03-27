using System;

namespace Unity.AI.Toolkit.Accounts.Services.States
{
    public class ApiAccessibleState
    {
        public static bool IsAccessible => Account.network.IsAvailable && Account.signIn.IsSignedIn && Account.cloudConnected.IsConnected;

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
