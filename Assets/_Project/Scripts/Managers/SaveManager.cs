// SaveManager.cs
using System.IO;
using UnityEngine;

namespace AIAirHockey
{
    public class SaveManager : Singleton<SaveManager>
    {
        // The in-memory copy of the save. Always access through here.
        public SaveData Data { get; private set; }

        // Full path to the save file on the device.
        private string FilePath => Path.Combine(Application.persistentDataPath, "save.json");

        protected override void Awake()
        {
            base.Awake();
            // Awake might run on a duplicate that gets destroyed; guard it.
            if (Instance != this) return;
            Load();
        }

        // Reads the file if it exists, else creates fresh defaults.
        public void Load()
        {
            if (File.Exists(FilePath))
            {
                string json = File.ReadAllText(FilePath);
                Data = JsonUtility.FromJson<SaveData>(json);
                if (Data == null) Data = new SaveData(); // corrupt file safety
            }
            else
            {
                Data = new SaveData();
                Save();
            }
        }

        // Writes the current Data to disk.
        public void Save()
        {
            string json = JsonUtility.ToJson(Data, true);
            File.WriteAllText(FilePath, json);
        }
    }
}