using UnityEngine;

namespace RickMorty
{
    public class MaterialLibrary : ScriptableObject
    {
        public Material lit;
        public Material unlit;
        public Material transparent;
        public Material particles;

        static MaterialLibrary s_Instance;

        public static MaterialLibrary Get()
        {
            if (s_Instance == null) s_Instance = Resources.Load<MaterialLibrary>("MaterialLibrary");
            return s_Instance;
        }
    }
}
