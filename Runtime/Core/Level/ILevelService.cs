using System;

namespace _Project.Core.Level
{
    public interface ILevelService
    {
        int CurrentLevel { get; }
        LevelState State { get; }
        LevelLostReason LoseReason { get; }
        event Action<LevelFinishResult> Finished;
        event Action Resumed;
        void Win();
        void Lose();
        void Lose(LevelLostReason reason);
        void Revive(float extraSeconds);
        void ReviveWithSpace();
        void GoNext();
        void Restart();
    }
}
