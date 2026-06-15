// IPurchaseService.cs
using System;

namespace AIAirHockey
{
    // Future IAP. Interface only.
    public interface IPurchaseService
    {
        bool IsInitialized { get; }
        void Purchase(string productId, Action onSuccess, Action onFailed);
        void RestorePurchases(Action onComplete);
    }
}