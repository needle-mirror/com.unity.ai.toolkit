using System;

namespace Unity.AI.Toolkit.Accounts.Services.Core
{
    public interface IProxy<T>
    {
        T Value { get; set; }
    }
}
