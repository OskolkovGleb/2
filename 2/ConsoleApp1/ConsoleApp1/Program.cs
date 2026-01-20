// Программа сериализации игровых сохранений
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

// Перечисление пола
public enum GameSex
{
    [XmlEnum("m")] Male,
    [XmlEnum("f")] Female,
    [XmlEnum("n")] None
}

// Класс предмета
public class GameItem
{
    public string Name { get; set; } = "";
    public int Quantity { get; set; }
}

// Класс игрока
public class GamePlayer
{
    [XmlAttribute("rank")]
    public int Level { get; set; }

    public string Name { get; set; } = "";

    [JsonConverter(typeof(SexConverter))]
    public GameSex Gender { get; set; }
}

// Класс сохранения
[XmlRoot("GameSave")]
public class GameSaveData
{
    public string Location { get; set; } = "";

    [XmlElement("u")]
    [JsonPropertyName("u")]
    public GamePlayer Player { get; set; } = new();

    public GameItem[] Items { get; set; } = Array.Empty<GameItem>();

    [XmlIgnore]
    [JsonIgnore]
    public (double X, double Y) Coordinates { get; set; }

    public DateTime Created { get; set; }
    public string FileName { get; set; } = "";
}

// Конвертер для пола
public class SexConverter : JsonConverter<GameSex>
{
    public override GameSex Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string code = reader.GetString() ?? "n";
        return code switch
        {
            "m" => GameSex.Male,
            "f" => GameSex.Female,
            _ => GameSex.None
        };
    }

    public override void Write(Utf8JsonWriter writer, GameSex value, JsonSerializerOptions options)
    {
        string code = value switch
        {
            GameSex.Male => "m",
            GameSex.Female => "f",
            _ => "n"
        };
        writer.WriteStringValue(code);
    }
}

// Главный класс
public class Program
{
    // Создать тестовое сохранение
    static GameSaveData CreateTestSave()
    {
        return new GameSaveData
        {
            Location = "Темный лес",
            Player = new GamePlayer
            {
                Level = 15,
                Name = "Рыцарь",
                Gender = GameSex.Male
            },
            Items = new[]
            {
                new GameItem { Name = "Меч дракона", Quantity = 1 },
                new GameItem { Name = "Щит", Quantity = 2 },
                new GameItem { Name = "Зелье здоровья", Quantity = 5 }
            },
            Coordinates = (100.5, 200.3),
            Created = DateTime.Now,
            FileName = "forest_save"
        };
    }

    // Создать сохранение с ошибкой
    static GameSaveData CreateErrorSave()
    {
        return new GameSaveData
        {
            Location = "Пещера",
            Player = new GamePlayer
            {
                Level = 8,
                Name = "Маг",
                Gender = GameSex.Female
            },
            Items = new[]
            {
                new GameItem { Name = "Посох", Quantity = 1 },
                new GameItem { Name = "Свиток", Quantity = -3 } // Ошибка!
            },
            Coordinates = (50.1, 75.6),
            Created = DateTime.Now,
            FileName = "cave_save"
        };
    }

    // Проверить сохранение
    static void ValidateSave(GameSaveData save)
    {
        if (save == null)
            throw new ArgumentNullException(nameof(save));

        foreach (var item in save.Items)
        {
            if (item.Quantity < 0)
            {
                throw new InvalidOperationException(
                    $"Ошибка: предмет '{item.Name}' имеет отрицательное количество: {item.Quantity}");
            }
        }
    }

    // Сохранить в JSON
    static void SaveJson(GameSaveData save, string path)
    {
        ValidateSave(save);

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        string json = JsonSerializer.Serialize(save, options);
        File.WriteAllText(path, json, Encoding.UTF8);
        Console.WriteLine($"✓ JSON сохранен: {Path.GetFileName(path)}");
    }

    // Сохранить в XML
    static void SaveXml(GameSaveData save, string path)
    {
        ValidateSave(save);

        var serializer = new XmlSerializer(typeof(GameSaveData));
        using var writer = new StreamWriter(path, false, Encoding.UTF8);

        var ns = new XmlSerializerNamespaces();
        ns.Add("", "");

        serializer.Serialize(writer, save, ns);
        Console.WriteLine($"✓ XML сохранен: {Path.GetFileName(path)}");
    }

    // Главная функция
    static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.WriteLine("=== ТЕСТ СЕРИАЛИЗАЦИИ ИГРЫ ===\n");

        // Создаем папку для результатов
        string outputDir = "GameSaves";
        Directory.CreateDirectory(outputDir);

        // Тест 1: Корректные данные
        Console.WriteLine("ТЕСТ 1: Корректное сохранение");
        Console.WriteLine(new string('-', 40));

        var goodSave = CreateTestSave();

        try
        {
            string jsonPath = Path.Combine(outputDir, "good_save.json");
            string xmlPath = Path.Combine(outputDir, "good_save.xml");

            SaveJson(goodSave, jsonPath);
            SaveXml(goodSave, xmlPath);

            // Показать содержимое JSON
            Console.WriteLine("\nСодержимое JSON файла:");
            Console.WriteLine(new string('-', 40));
            Console.WriteLine(File.ReadAllText(jsonPath));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка: {ex.Message}");
        }

        // Тест 2: Данные с ошибкой
        Console.WriteLine("\n\nТЕСТ 2: Сохранение с ошибкой");
        Console.WriteLine(new string('-', 40));

        var badSave = CreateErrorSave();

        try
        {
            SaveJson(badSave, Path.Combine(outputDir, "bad_save.json"));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при сохранении JSON: {ex.Message}");
        }

        try
        {
            SaveXml(badSave, Path.Combine(outputDir, "bad_save.xml"));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при сохранении XML: {ex.Message}");
        }

        // Показать созданные файлы
        Console.WriteLine("\n" + new string('=', 50));
        Console.WriteLine("Созданные файлы:");

        if (Directory.Exists(outputDir))
        {
            var files = Directory.GetFiles(outputDir);
            foreach (var file in files)
            {
                var info = new FileInfo(file);
                Console.WriteLine($"  • {info.Name} ({info.Length} байт)");
            }
        }

        Console.WriteLine("\nНажмите любую клавишу для выхода...");
        Console.ReadKey();
    }
}