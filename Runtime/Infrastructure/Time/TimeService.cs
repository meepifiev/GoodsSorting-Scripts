using _Project.Core.Time;
using PrimeGames.SDK;

namespace _Project.Infrastructure.Time
{
    public class TimeService : ITimeService
    {
        public float DeltaTime => UnityEngine.Time.unscaledDeltaTime * PrimeSDK.Time.Scale;

        public float Time => UnityEngine.Time.time;
    }
}
