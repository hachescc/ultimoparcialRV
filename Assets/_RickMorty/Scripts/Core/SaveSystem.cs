using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace RickMorty
{
    [Serializable]
    public class RunRecord
    {
        public string date;
        public string result;      // "Victoria" / "Atrapado por el FBI"
        public int score;
        public string rank;
        public float seconds;
        public int discoveries;
        public int explosions;
        public int kills;
        public int easterEggs;
    }

    [Serializable]
    public class SaveData
    {
        public List<string> collection = new List<string>();   // objetos descubiertos alguna vez (coleccion global)
        public int bestScore;
        public int runsPlayed;
        public int victories;
        public bool comfortVignette = true;
        public bool smoothTurn;
        public List<RunRecord> runs = new List<RunRecord>();
    }

    /// <summary>
    /// Persistencia en JSON (Application.persistentDataPath/rickmorty_save.json).
    /// Guarda la coleccion del multiverso, el mejor puntaje, el historial de partidas y opciones.
    /// </summary>
    public static class SaveSystem
    {
        const string FileName = "rickmorty_save.json";
        const int MaxRuns = 10;
        static SaveData s_Data;

        public static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        public static SaveData Data
        {
            get
            {
                if (s_Data == null) Load();
                return s_Data;
            }
        }

        public static void Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    s_Data = JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath)) ?? new SaveData();
                    return;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[SaveSystem] No se pudo leer el guardado: " + e.Message);
            }
            s_Data = new SaveData();
        }

        public static void Save()
        {
            try
            {
                File.WriteAllText(FilePath, JsonUtility.ToJson(Data, true));
            }
            catch (Exception e)
            {
                Debug.LogWarning("[SaveSystem] No se pudo guardar: " + e.Message);
            }
        }

        public static void AddToCollection(string id)
        {
            if (!Data.collection.Contains(id))
            {
                Data.collection.Add(id);
                Save();
            }
        }

        public static void RecordRun(RunRecord run)
        {
            Data.runsPlayed++;
            if (run.result == "Victoria") Data.victories++;
            if (run.score > Data.bestScore) Data.bestScore = run.score;
            Data.runs.Insert(0, run);
            while (Data.runs.Count > MaxRuns) Data.runs.RemoveAt(Data.runs.Count - 1);
            Save();
        }

        public static void ResetAll()
        {
            s_Data = new SaveData();
            Save();
        }
    }
}
