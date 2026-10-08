using System;
using UnityEngine;

namespace _Project.Features.Abilities.Effects
{
    public interface IHammerFx
    {
        void PlayAt(Vector3 worldPosition, Action onImpact);
    }
}
