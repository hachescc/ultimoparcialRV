using System.Collections;
using UnityEngine;

namespace RickMorty
{
    public class BlackHoleTrash : MonoBehaviour
    {
        public Transform core;

        void Update()
        {
            if (core != null) core.Rotate(0, 120f * Time.deltaTime, 0, Space.Self);
        }

        void OnTriggerEnter(Collider other) => TrySwallow(other);

        void OnTriggerStay(Collider other) => TrySwallow(other);

        void TrySwallow(Collider other)
        {
            var item = other.GetComponentInParent<MixableItem>();
            if (item == null || item.Unavailable || item.IsHeld) return;
            Swallow(item);
        }

        public void Swallow(MixableItem item)
        {
            if (item == null) return;
            if (item.IsBase)
            {
                item.Base.ReturnHome(false, 0f);
                return;
            }
            if (item.itemId == "pistola_portales")
            {
                FinalAssemblyStation.ReturnGun(item);
                return;
            }
            item.Unavailable = true;
            StartCoroutine(CoSpaghetti(item));
        }

        IEnumerator CoSpaghetti(MixableItem item)
        {
            item.ForceRelease();
            yield return null;
            if (item == null) yield break;
            item.Body.isKinematic = true;
            foreach (var c in item.GetComponentsInChildren<Collider>()) c.enabled = false;
            AudioFX.Play("fuuump", transform.position, 1f);
            var start = item.transform.position;
            var target = core != null ? core.position : transform.position;
            var s0 = item.transform.localScale;
            float t = 0;
            while (t < 0.6f && item != null)
            {
                t += Time.deltaTime;
                float k = t / 0.6f;
                item.transform.position = Vector3.Lerp(start, target, k * k);
                item.transform.localScale = new Vector3(s0.x * (1f - k), s0.y * (1f + k * 4f), s0.z * (1f - k));
                yield return null;
            }
            if (item != null) Destroy(item.gameObject);
        }
    }
}
