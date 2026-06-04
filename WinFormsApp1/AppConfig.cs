using System;
using System.IO;
using System.Text.Json;

public class AppConfig
{
    // Свойство для хранения языка (например, "RU" или "EN")
    public string Language { get; set; } = "RU";
    public bool FastPaste { get; set; } = false;

    // Статическое имя файла настроек рядом с экзешником
    private static readonly string ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");

    // Метод для сохранения настроек в файл
    public void Save()
    {
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true }; // Красивый отступ в файле
            string jsonString = JsonSerializer.Serialize(this, options);
            File.WriteAllText(ConfigPath, jsonString);
        }
        catch { /* Ошибки записи можно проигнорировать или залогировать */ }
    }

    // Метод для загрузки настроек с диска
    public static AppConfig Load()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                string jsonString = File.ReadAllText(ConfigPath);
                var config = JsonSerializer.Deserialize<AppConfig>(jsonString);
                return config ?? new AppConfig();
            }
        }
        catch { }

        // Если файла нет или он поврежден — возвращаем настройки по умолчанию и создаем файл
        var defaultConfig = new AppConfig();
        defaultConfig.Save();
        return defaultConfig;
    }
}