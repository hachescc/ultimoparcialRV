using System;
using System.Collections.Generic;
using UnityEngine;

namespace RickMorty
{
    /// <summary>
    /// Registro de modelos 3D reales por id de objeto. Si un id tiene prefab asignado, la fabrica lo usa en vez del placeholder de primitivas. Se llena solo desde el menu Rick y Morty, "Actualizar registro de modelos" (RickMortySetup.CreateVisualRegistry), con los GLB de Models/Items (Assets/_RickMorty/Resources/ItemVisualRegistry.asset).
    /// Lo usan: ItemFactory (modelo de cada objeto), Consumable y MeeseeksNPC; lo llena RickMortySetup.
    /// </summary>
    [CreateAssetMenu(menuName = "Rick y Morty/Item Visual Registry")]
    public class ItemVisualRegistry : ScriptableObject
    {
        /// <summary>Un modelo del registro: id del objeto, prefab y ajustes de escala.</summary>
        [Serializable]
        public class Entry
        {
            public string id;
            public GameObject prefab;
            [Tooltip("Escala aplicada al modelo")]
            public float scale = 1f;
            public Vector3 positionOffset;
            public Vector3 rotationOffset;
            [Tooltip("Si el modelo no trae colliders se genera una caja automaticamente")]
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
