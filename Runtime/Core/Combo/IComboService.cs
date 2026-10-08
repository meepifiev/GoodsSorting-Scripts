using System;
using UnityEngine;

namespace _Project.Core.Combo
{
    public interface IComboService
    {
        int Combo { get; }
        float Progress { get; }
        event Action<int> Bumped;
        event Action ProgressChanged;
        event Action Ended;

        event Action<Vector3, int> MatchScored;
    }
}
