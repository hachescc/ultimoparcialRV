using System;

namespace RickMorty
{
    public enum GameState { Intro, Onboarding, Tutorial, RickLeaving, FreePlay, PortalGunReady, Ending, GameOver }

    public enum DamageType { Firearm, Melee, Explosion }

    public static class GameEvents
    {
        public static event Action<GameState> StateChanged;
        public static event Action<RecipeDef, bool> MixSucceeded;
        public static event Action<string> MixFailed;
        public static event Action<ItemDef> ItemDiscovered;
        public static event Action<Creature, bool> CreatureKilled;
        public static event Action<string, int> Penalty;
        public static event Action<string, int> Bonus;
        public static event Action<string> EasterEggFound;
        public static event Action<string> ItemActivated;
        public static event Action<string> ItemGrabbed;
        public static event Action<string> UnexpectedEvent;

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

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void ResetAll()
        {
            StateChanged = null; MixSucceeded = null; MixFailed = null; ItemDiscovered = null;
            CreatureKilled = null; Penalty = null; Bonus = null; EasterEggFound = null;
            ItemActivated = null; ItemGrabbed = null; UnexpectedEvent = null;
        }
    }
}
