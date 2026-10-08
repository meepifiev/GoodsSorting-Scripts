using UnityEngine;

namespace _Project.UI.Gameplay.Abilities
{
    public class AbilityPanel : MonoBehaviour
    {
        public AbilityView[] AbilityViews => GetComponentsInChildren<AbilityView>(true);
    }
}
