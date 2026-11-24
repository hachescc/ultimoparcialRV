using UnityEngine;

namespace RickMorty
{
    /// <summary>
    /// Portal verde final: al cruzarlo con la cabeza termina el juego (victoria).
    /// Lo usan: GameManager.ShootPortal (lo agrega al portal) y PlayerHead (detecta que la cabeza lo cruzo).
    /// </summary>
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
