using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace RickMorty
{
    public class EasterEgg : MonoBehaviour
    {
        public string eggId = "egg";
        public string displayName = "Easter Egg";
        [TextArea] public string rickComment;

        bool m_Found;

        void Start()
        {
            var inter = GetComponent<XRBaseInteractable>();
            if (inter != null)
            {
                inter.selectEntered.AddListener(_ => Found());
                inter.activated.AddListener(_ => Found());
            }
        }

        public void Found()
        {
            if (m_Found) return;
            m_Found = true;
            GameEvents.RaiseEasterEgg(displayName);
            WristWatch.Instance?.Notify("¡SECRETO ENCONTRADO! " + displayName.ToUpper(), new Color(1f, 0.8f, 0.3f), 3.5f);
            AudioFX.Play("arcade_win", transform.position, 0.6f, 1.3f);
            if (!string.IsNullOrEmpty(rickComment)) RickComms.Instance?.SayText(rickComment);
        }
    }
}
