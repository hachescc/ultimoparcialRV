using UnityEngine;

namespace RickMorty
{
    /// <summary>
    /// Tipos de superficie del escenario (piso, madera, metal, vidrio, corcho).
    /// Lo usan: SurfaceType, BaseIngredient y ThrowingKnife; el builder de escena lo asigna.
    /// </summary>
    public enum SurfaceKind { Ground, Wood, Metal, Glass, Cork }

    /// <summary>
    /// Marca el tipo de superficie de un collider del escenario (en vez de usar tags).
    /// Ground: los frascos base reaparecen al tocarlo. Wood/Cork: los cuchillos se clavan.
    /// Lo usan: BaseIngredient (piso) y ThrowingKnife (madera y corcho); lo pone el builder de escena.
    /// </summary>
    public class SurfaceType : MonoBehaviour
    {
        public SurfaceKind kind = SurfaceKind.Ground;
    }
}
