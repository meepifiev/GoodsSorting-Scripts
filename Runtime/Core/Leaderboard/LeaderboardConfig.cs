using UnityEngine;

namespace _Project.Core.Leaderboard
{
    [CreateAssetMenu(fileName = "LeaderboardConfig", menuName = "Configs/Leaderboard Config")]
    public class LeaderboardConfig : ScriptableObject
    {
        [SerializeField] private string _boardId = "Leaderboard";
        [SerializeField] private int _topCount = 10;

        public string BoardId => _boardId;
        public int TopCount => _topCount;
    }
}
