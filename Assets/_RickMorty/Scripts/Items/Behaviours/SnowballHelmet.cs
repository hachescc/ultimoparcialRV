using System.Collections;
using UnityEngine;

namespace RickMorty
{
    public class SnowballHelmet : ItemBehaviour, IHeadInteractable
    {
        public float duration = 40f;
        bool m_Worn;

        public void OnTouchHead(PlayerHead head)
        {
            if (m_Worn || !Item.IsHeld) return;
            StartCoroutine(CoWear(head.transform));
        }

        IEnumerator CoWear(Transform head)
        {
            m_Worn = true;
            Item.ForceRelease();
            Item.Unavailable = true;
            yield return null;
            Body.isKinematic = true;
            foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
            transform.SetParent(head, false);
            transform.localPosition = new Vector3(0, 0.16f, -0.02f);
            transform.localRotation = Quaternion.identity;

            AudioFX.Play("bark", head.position, 0.8f);
            RickComms.Instance?.EnableDogMode(duration);
            ScreenFX.Instance?.SetMuffled(true, 900f);
            WristWatch.Instance?.Notify("CASCO DE SNOWBALL EQUIPADO", new Color(0.8f, 0.8f, 1f), 3f);
            RickComms.Instance?.Say("snowball_on");

            yield return new WaitForSeconds(duration);

            ScreenFX.Instance?.SetMuffled(false);
            transform.SetParent(null, true);
            foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = true;
            Body.isKinematic = false;
            Item.Unavailable = false;
            m_Worn = false;
        }

        void OnDestroy()
        {
            var fx = ScreenFX.Instance;
            if (m_Worn && fx != null) fx.SetMuffled(false);
        }
    }
}
