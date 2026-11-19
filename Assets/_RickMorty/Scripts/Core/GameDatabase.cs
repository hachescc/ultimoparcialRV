using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RickMorty
{
    // ------------------------------------------------------------------
    //  Modelos de datos (se cargan desde Resources/Data/*.json)
    // ------------------------------------------------------------------

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
        public bool IsLiquid => category == "base_liquido";

        public Color Color
        {
            get
            {
                return ColorUtility.TryParseHtmlString(color, out var c) ? c : Color.white;
            }
        }
    }

    [Serializable] public class ItemList { public List<ItemDef> items = new List<ItemDef>(); }

    [Serializable]
    public class RecipeDef
    {
        public string result;
        public List<string> ingredients = new List<string>();

        public string Key => GameDatabase.MakeKey(ingredients);
    }

    [Serializable]
    public class RecipeList
    {
        public List<RecipeDef> recipes = new List<RecipeDef>();
        public RecipeDef finalRecipe;
    }

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

    [Serializable] public class DialogueList { public List<DialogueLine> lines = new List<DialogueLine>(); }

    /// <summary>
    /// Base de datos del juego. Lee items, recetas y dialogos desde JSON para que
    /// el equipo pueda modificarlos sin tocar codigo.
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

        /// <summary>Objetos que se pueden descubrir (todo lo que no es ingrediente base).</summary>
        public static IEnumerable<ItemDef> Discoverables { get { Load(); return s_ItemOrder.Where(i => !i.IsBase); } }
        public static int DiscoverableCount => Discoverables.Count();

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

        public static string MakeKey(IEnumerable<string> ids)
        {
            return string.Join("+", ids.OrderBy(s => s, StringComparer.Ordinal));
        }

        public static ItemDef GetItem(string id)
        {
            Load();
            return id != null && s_Items.TryGetValue(id, out var d) ? d : null;
        }

        public static string NameOf(string id) => GetItem(id)?.name ?? id;

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
            foreach (var r in s_Recipes.Concat(s_Final != null ? new[] { s_Final } : Array.Empty<RecipeDef>()))
            {
                if (ids.Count > r.ingredients.Count) continue;
                var pool = new List<string>(r.ingredients);
                bool ok = true;
                foreach (var id in ids)
                {
                    if (!pool.Remove(id)) { ok = false; break; }
                }
                if (ok) return true;
            }
            return false;
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
            return string.Join(" + ", r.ingredients.Select(NameOf));
        }
    }
}
