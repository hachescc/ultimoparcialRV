using UnityEngine;

namespace RickMorty
{
    /// <summary>
    /// Materiales base (se crean como assets en Resources para que los shaders de URP
    /// entren en el build). Los colores se derivan en runtime y se cachean.
    /// </summary>
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
