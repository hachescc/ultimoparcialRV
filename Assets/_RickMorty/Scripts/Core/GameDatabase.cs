using System;
using System.Collections.Generic;
using UnityEngine;

namespace RickMorty
{
    // ------------------------------------------------------------------
    //  Modelos de datos (se cargan desde Resources/Data/*.json)
    // ------------------------------------------------------------------

    /// <summary>
    /// Un objeto del juego (ingrediente, resultado de mezcla, arma, criatura...) tal como viene en items.json.
    /// Lo usan: GameDatabase, ItemFactory (para crearlo), MixableItem, el holograma y el reloj.
    /// </summary>
    [Serializable]
    public class ItemDef
    {
        public string id;
        public string name;
        public string category;
        public string shape;
        public string color;
        public float size = 0.1f;
        public float mass = 0.5f;
        public string behaviour;
        public string description;

        public bool IsBase => !string.IsNullOrEmpty(category) && category.StartsWith("base");

        public Color Color
        {
            get
            {
                return ColorUtility.TryParseHtmlString(color, out var c) ? c : Color.white;
            }
        }
    }

    /// <summary>Contenedor de items.json (JsonUtility necesita una clase raiz). Lo usan: GameDatabase.Load.</summary>
    [Serializable] public class ItemList { public List<ItemDef> items = new List<ItemDef>(); }

    /// <summary>
    /// Una receta: lista de ingredientes (ids) y el id del resultado.
    /// Lo usan: GameDatabase, el tazon (MixingBowl), GameManager (misiones) y ScoreManager.
    /// </summary>
    [Serializable]
    public class RecipeDef
    {
        public string result;
        public List<string> ingredients = new List<string>();

        public string Key => GameDatabase.MakeKey(ingredients);
    }

    /// <summary>Contenedor de recipes.json: recetas normales y la receta final. Lo usan: GameDatabase.Load.</summary>
    [Serializable]
    public class RecipeList
    {
        public List<RecipeDef> recipes = new List<RecipeDef>();
        public RecipeDef finalRecipe;
    }

    /// <summary>
    /// Una linea de dialogo de dialogues.json (hablante y texto, o varias variantes al azar).
    /// Lo usan: GameDatabase y RickComms (Say), ademas de Talker, Tio Steve y Rick Caleno (textos flotantes).
    /// </summary>
    [Serializable]
    public class DialogueLine
    {
        public string id;
        public string speaker = "Rick";
        public string text;
        public List<string> variants = new List<string>();

        public string PickText()
        {
            if (variants != null && variants.Count > 0)
                return variants[UnityEngine.Random.Range(0, variants.Count)];
            return text;
        }
    }

    /// <summary>Contenedor de dialogues.json. Lo usan: GameDatabase.Load.</summary>
    [Serializable] public class DialogueList { public List<DialogueLine> lines = new List<DialogueLine>(); }

    /// <summary>
    /// Base de datos del juego. Lee items, recetas y dialogos desde JSON para que
    /// el equipo pueda modificarlos sin tocar codigo.
    /// Lo usan: casi todos los sistemas (tazon, fabrica de objetos, dialogos, holograma, puntaje).
    /// </summary>
    public static class GameDatabase
    {
        static bool s_Loaded;
        static readonly Dictionary<string, ItemDef> s_Items = new Dictionary<string, ItemDef>();
        static readonly List<ItemDef> s_ItemOrder = new List<ItemDef>();
        static readonly Dictionary<string, RecipeDef> s_RecipesByKey = new Dictionary<string, RecipeDef>();
        static readonly Dictionary<string, RecipeDef> s_RecipeByResult = new Dictionary<string, RecipeDef>();
        static readonly List<RecipeDef> s_Recipes = new List<RecipeDef>();
        static readonly Dictionary<string, DialogueLine> s_Dialogues = new Dictionary<string, DialogueLine>();
        static RecipeDef s_Final;

        public static IReadOnlyList<ItemDef> Items { get { Load(); return s_ItemOrder; } }
        public static IReadOnlyList<RecipeDef> Recipes { get { Load(); return s_Recipes; } }
        public static RecipeDef FinalRecipe { get { Load(); return s_Final; } }

        /// <summary>Objetos que se pueden descubrir (todo lo que no es ingrediente base), en el orden de items.json.</summary>
        public static List<ItemDef> Discoverables
        {
            get
            {
                Load();
                var result = new List<ItemDef>();
                foreach (var item in s_ItemOrder)
                    if (!item.IsBase) result.Add(item);
                return result;
            }
        }

        public static int DiscoverableCount => Discoverables.Count;

        public static void Load()
        {
            if (s_Loaded) return;
            s_Loaded = true;

            var itemsAsset = Resources.Load<TextAsset>("Data/items");
            var recipesAsset = Resources.Load<TextAsset>("Data/recipes");
            var dialoguesAsset = Resources.Load<TextAsset>("Data/dialogues");

            if (itemsAsset != null)
            {
                var list = JsonUtility.FromJson<ItemList>(itemsAsset.text);
                foreach (var it in list.items)
                {
                    s_Items[it.id] = it;
                    s_ItemOrder.Add(it);
                }
            }
            else Debug.LogError("[GameDatabase] Falta Resources/Data/items.json");

            if (recipesAsset != null)
            {
                var list = JsonUtility.FromJson<RecipeList>(recipesAsset.text);
                foreach (var r in list.recipes)
                {
                    s_Recipes.Add(r);
                    var key = r.Key;
                    if (s_RecipesByKey.ContainsKey(key))
                        Debug.LogWarning($"[GameDatabase] Receta duplicada para {key} ({r.result})");
                    s_RecipesByKey[key] = r;
                    if (!s_RecipeByResult.ContainsKey(r.result)) s_RecipeByResult[r.result] = r;
                }
                s_Final = list.finalRecipe;
                if (s_Final != null) s_RecipeByResult[s_Final.result] = s_Final;
            }
            else Debug.LogError("[GameDatabase] Falta Resources/Data/recipes.json");

            if (dialoguesAsset != null)
            {
                var list = JsonUtility.FromJson<DialogueList>(dialoguesAsset.text);
                foreach (var d in list.lines) s_Dialogues[d.id] = d;
            }
            else Debug.LogError("[GameDatabase] Falta Resources/Data/dialogues.json");

            // Validacion: toda receta debe apuntar a items existentes
            foreach (var r in s_Recipes)
            {
                if (!s_Items.ContainsKey(r.result)) Debug.LogWarning($"[GameDatabase] Resultado desconocido: {r.result}");
                foreach (var ing in r.ingredients)
                    if (!s_Items.ContainsKey(ing)) Debug.LogWarning($"[GameDatabase] Ingrediente desconocido: {ing} en {r.result}");
            }
        }

        /// <summary>Clave unica de una mezcla: los ids ordenados alfabeticamente y unidos con "+"
        /// (asi el orden en que se meten los ingredientes no importa).</summary>
        public static string MakeKey(IEnumerable<string> ids)
        {
            var sorted = new List<string>(ids);
            sorted.Sort(StringComparer.Ordinal);
            return string.Join("+", sorted);
        }

        public static ItemDef GetItem(string id)
        {
            Load();
            return id != null && s_Items.TryGetValue(id, out var d) ? d : null;
        }

        /// <summary>Nombre visible de un objeto (si no existe o no tiene nombre, devuelve el id).</summary>
        public static string NameOf(string id)
        {
            var item = GetItem(id);
            if (item != null && item.name != null) return item.name;
            return id;
        }

        public static RecipeDef FindRecipe(IEnumerable<string> ingredientIds)
        {
            Load();
            s_RecipesByKey.TryGetValue(MakeKey(ingredientIds), out var r);
            return r;
        }

        public static bool IsFinalCombo(IEnumerable<string> ingredientIds)
        {
            Load();
            return s_Final != null && MakeKey(ingredientIds) == s_Final.Key;
        }

        public static RecipeDef RecipeFor(string resultId)
        {
            Load();
            s_RecipeByResult.TryGetValue(resultId, out var r);
            return r;
        }

        /// <summary>
        /// Devuelve true si el conjunto (multiconjunto) de ingredientes es parte de alguna receta valida.
        /// Se usa para la retroalimentacion haptica "la mezcla va bien / va a explotar".
        /// </summary>
        public static bool IsPartialOfAnyRecipe(List<string> ids)
        {
            Load();
            if (ids.Count == 0) return true;
            foreach (var r in s_Recipes)
                if (FitsIn(ids, r)) return true;
            if (s_Final != null && FitsIn(ids, s_Final)) return true;
            return false;
        }

        /// <summary>true si todos los ids caben en los ingredientes de la receta (respetando repetidos).</summary>
        static bool FitsIn(List<string> ids, RecipeDef r)
        {
            if (ids.Count > r.ingredients.Count) return false;
            var pool = new List<string>(r.ingredients);
            foreach (var id in ids)
                if (!pool.Remove(id)) return false;
            return true;
        }

        public static DialogueLine GetLine(string id)
        {
            Load();
            return id != null && s_Dialogues.TryGetValue(id, out var d) ? d : null;
        }

        public static string RecipeText(string resultId)
        {
            var r = RecipeFor(resultId);
            if (r == null) return "";
            var names = new List<string>();
            foreach (var id in r.ingredients) names.Add(NameOf(id));
            return string.Join(" + ", names);
        }
    }
}
