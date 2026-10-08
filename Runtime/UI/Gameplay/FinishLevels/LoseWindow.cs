using UnityEngine;

namespace _Project.UI.Gameplay.FinishLevels
{
    public class LoseWindow : MonoBehaviour
    {
        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
