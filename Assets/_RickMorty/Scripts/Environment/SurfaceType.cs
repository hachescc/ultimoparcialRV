using UnityEngine;

namespace RickMorty
{
    public enum SurfaceKind { Ground, Wood, Metal, Glass, Cork }

    /// <summary>
    /// Marca el tipo de superficie de un collider del escenario (en vez de usar tags).
    /// Ground: los frascos base reaparecen al tocarlo. Wood/Cork: los cuchillos se clavan.
    /// </summary>
    public class SurfaceType : MonoBehaviour
    {
        public SurfaceKind kind = SurfaceKind.Ground;
    }
}
