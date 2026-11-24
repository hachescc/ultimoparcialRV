using UnityEngine;

namespace RickMorty
{
    public class EndingPortal : MonoBehaviour
    {
        bool m_Used;

        public void PlayerEntered()
        {
            if (m_Used) return;
            m_Used = true;
            GameManager.Instance?.EnterFinalPortal();
        }
    }
}
