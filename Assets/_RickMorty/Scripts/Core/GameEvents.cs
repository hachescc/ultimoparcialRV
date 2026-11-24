using System;

namespace RickMorty
{
    /// <summary>
    /// Etapas de la partida, en orden. Ending = victoria, GameOver = atrapado por el FBI.
    /// Lo usan: GameManager (la maquina de estados) y quien pregunta en que etapa va el juego (tazon, pausa, HUD...).
    /// </summary>
    public enum GameState { Intro, Onboarding, Tutorial, RickLeaving, FreePlay, PortalGunReady, Ending, GameOver }

    /// <summary>
    /// Con que se mato a una criatura (cambia el efecto y la linea de Rick).
    /// Lo usan: Creature.Kill, los proyectiles (ItemBehaviour), sable, armas cuerpo a cuerpo, cuchillo y FX.Explosion.
    /// </summary>
    public enum DamageType { Firearm, Melee, Explosion }

    /// <summary>
    /// Bus de eventos estatico: es el "sistema reactivo" del juego. Quien produce algo llama a Raise...
    /// y quien quiere reaccionar se suscribe, sin conocerse entre si.
    /// Lo usan: GameManager, el tazon, EventDirector, criaturas, objetos y secretos (lanzan) y los suscriptores de abajo.
    /// Suscriptores actuales: ScoreManager (mezclas, explosiones, muertes, bonos, penalizaciones, secretos),
    /// WristWatch (ItemDiscovered), PetHinter (MixFailed) y GameManager (ItemGrabbed/ItemActivated en el tutorial).
    /// StateChanged y UnexpectedEvent se lanzan siempre pero hoy nadie los escucha: quedan como puntos de
    /// enganche para agregar reacciones nuevas sin tocar GameManager ni EventDirector.
    /// </summary>
    public static class GameEvents
    {
        public static event Action<GameState> StateChanged;
        public static event Action<RecipeDef, bool> MixSucceeded;          // receta, primera vez
        public static event Action<string> MixFailed;                       // motivo
        public static event Action<ItemDef> ItemDiscovered;
        public static event Action<Creature, bool> CreatureKilled;          // criatura, accidental
        public static event Action<string, int> Penalty;                    // motivo, puntos
        public static event Action<string, int> Bonus;                      // motivo, puntos
        public static event Action<string> EasterEggFound;
        public static event Action<string> ItemActivated;                  // id del item activado (gatillo)
        public static event Action<string> ItemGrabbed;                    // id del item agarrado
        public static event Action<string> UnexpectedEvent;                // nombre del evento inesperado

        public static void RaiseStateChanged(GameState s) => StateChanged?.Invoke(s);
        public static void RaiseMixSucceeded(RecipeDef r, bool first) => MixSucceeded?.Invoke(r, first);
        public static void RaiseMixFailed(string reason) => MixFailed?.Invoke(reason);
        public static void RaiseItemDiscovered(ItemDef d) => ItemDiscovered?.Invoke(d);
        public static void RaiseCreatureKilled(Creature c, bool accidental) => CreatureKilled?.Invoke(c, accidental);
        public static void RaisePenalty(string reason, int pts) => Penalty?.Invoke(reason, pts);
        public static void RaiseBonus(string reason, int pts) => Bonus?.Invoke(reason, pts);
        public static void RaiseEasterEgg(string id) => EasterEggFound?.Invoke(id);
        public static void RaiseItemActivated(string id) => ItemActivated?.Invoke(id);
        public static void RaiseItemGrabbed(string id) => ItemGrabbed?.Invoke(id);
        public static void RaiseUnexpectedEvent(string name) => UnexpectedEvent?.Invoke(name);

        /// <summary>Limpia suscriptores al iniciar Play (por si el Domain Reload esta desactivado).</summary>
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void ResetAll()
        {
            StateChanged = null; MixSucceeded = null; MixFailed = null; ItemDiscovered = null;
            CreatureKilled = null; Penalty = null; Bonus = null; EasterEggFound = null;
            ItemActivated = null; ItemGrabbed = null; UnexpectedEvent = null;
        }
    }
}
