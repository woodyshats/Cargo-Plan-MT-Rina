using System;
using System.IO;
using Newtonsoft.Json;
using CargoPlanApp.Models;

namespace CargoPlanApp.Services
{
    public class JsonDataService
    {
        private readonly string _filePath;
        public JsonDataService(string fileName)
        {
            _filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
        }

        public void Save(CargoPlanData data)
        {
            string json = JsonConvert.SerializeObject(data, Formatting.Indented);
            File.WriteAllText(_filePath, json);
        }

        public CargoPlanData Load()
        {
            if (!File.Exists(_filePath)) return null;
            return JsonConvert.DeserializeObject<CargoPlanData>(File.ReadAllText(_filePath));
        }
    }
}