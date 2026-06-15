// IRewardService.cs
using System;

namespace AIAirHockey
{
    // Future ads. No implementation now; this just reserves the shape
    // so adding an ad SDK later requires zero changes elsewhere.
    public interface IRewardService
    {
        bool IsRewardedReady { get; }
        void ShowRewarded(Action onRewardGranted, Action onFailed);
    }
}