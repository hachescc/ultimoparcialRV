using UnityEngine;

namespace RickMorty
{
    public enum SurfaceKind { Ground, Wood, Metal, Glass, Cork }

    public class SurfaceType : MonoBehaviour
    {
        public SurfaceKind kind = SurfaceKind.Ground;
    }
}
