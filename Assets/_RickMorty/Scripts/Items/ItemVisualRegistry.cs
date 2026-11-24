using System;
using System.Collections.Generic;
using UnityEngine;

namespace RickMorty
{
    [CreateAssetMenu(menuName = "Rick y Morty/Item Visual Registry")]
    public class ItemVisualRegistry : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public string id;
            public GameObject prefab;
            public float scale = 1f;
            public Vector3 positionOffset;
            public Vector3 rotationOffset;
            public bool generateBoxCollider = true;
        }

        public List<Entry> entries = new List<Entry>();

        static ItemVisualRegistry s_Instance;

        public static ItemVisualRegistry Get()
        {
            if (s_Instance == null) s_Instance = Resources.Load<ItemVisualRegistry>("ItemVisualRegistry");
            return s_Instance;
        }

        public Entry Find(string id)
        {
            foreach (var e in entries) if (e != null && e.id == id && e.prefab != null) return e;
            return null;
        }
    }
}
