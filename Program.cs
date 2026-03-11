using System;
using System.IO;
using System.Text.RegularExpressions;

namespace Laba2
{
    class Program
    {
        static IVirtualArray currentArray = null;
        static string currentFileName = null;

        static void Main(string[] args)
        { 
           Console.WriteLine("Введите 'Help' для списка команд.");
            Console.ResetColor();

            while (true)
            {
                Console.Write("VM> ");
                string input = Console.ReadLine()?.Trim();
                if (string.IsNullOrEmpty(input))
                    continue;

                string[] parts = ParseCommand(input);
                string command = parts[0].ToLower();

                try
                {
                    switch (command)
                    {
                        case "exit":
                            CloseCurrentArray();
                            Console.WriteLine("До свидания.");
                            return;

                        case "help":
                            HandleHelp(parts);
                            break;

                        case "create":
                            HandleCreate(parts);
                            break;

                        case "open":
                            HandleOpen(parts);
                            break;

                        case "input":
                            HandleInput(parts);
                            break;

                        case "print":
                            HandlePrint(parts);
                            break;

                        default:
                            PrintError("Неизвестная команда. Введите 'Help' для списка.");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    PrintError($"Ошибка: {ex.Message}");
                }
            }
        }

        // Разбивает команду на части, учитывая кавычки для строковых значений
        static string[] ParseCommand(string input)
        {
            var list = new System.Collections.Generic.List<string>();
            var regex = new Regex(@"("".*?"")|(\S+)");
            foreach (Match m in regex.Matches(input))
            {
                string val = m.Value;
                if (val.StartsWith("\"") && val.EndsWith("\""))
                    val = val.Substring(1, val.Length - 2);
                list.Add(val);
            }
            return list.ToArray();
        }

        static void HandleHelp(string[] parts)
        {
            if (parts.Length == 1)
            {
                Console.WriteLine("Доступные команды:");
                Console.WriteLine("  Create <имя_файла> (int | char(<длина>) | varchar(<макс_длина>))");
                Console.WriteLine("  Open <имя_файла>");
                Console.WriteLine("  Input <индекс> <значение>");
                Console.WriteLine("  Print <индекс>");
                Console.WriteLine("  Help [имя_файла]");
                Console.WriteLine("  Exit");
            }
            else if (parts.Length >= 2)
            {
                string helpFile = parts[1];
                try
                {
                    File.WriteAllText(helpFile, "Список команд... (можно вывести подробно)");
                    Console.WriteLine($"Справка сохранена в {helpFile}");
                }
                catch (Exception ex)
                {
                    PrintError($"Не удалось записать файл справки: {ex.Message}");
                }
            }
        }

        static void HandleCreate(string[] parts)
        {
            if (parts.Length < 3)
            {
                PrintError("Использование: Create <имя_файла> (int | char(<длина>) | varchar(<макс_длина>))");
                return;
            }

            string fileName = parts[1];
            string typeSpec = parts[2];

            CloseCurrentArray();

            // Парсим спецификацию типа
            Regex intRegex = new Regex(@"^int$", RegexOptions.IgnoreCase);
            Regex charRegex = new Regex(@"^char\((\d+)\)$", RegexOptions.IgnoreCase);
            Regex varcharRegex = new Regex(@"^varchar\((\d+)\)$", RegexOptions.IgnoreCase);

            if (intRegex.IsMatch(typeSpec))
            {
                Console.Write("Введите размер массива (количество элементов): ");
                if (!long.TryParse(Console.ReadLine(), out long size) || size <= 0)
                {
                    PrintError("Некорректный размер.");
                    return;
                }
                currentArray = new VirtualIntArray(fileName, size);
                currentFileName = fileName;
                PrintSuccess($"Создан массив целых чисел размером {size} элементов в файле {fileName}");
            }
            else if (charRegex.IsMatch(typeSpec))
            {
                var match = charRegex.Match(typeSpec);
                int strLength = int.Parse(match.Groups[1].Value);
                Console.Write("Введите размер массива (количество элементов): ");
                if (!long.TryParse(Console.ReadLine(), out long size) || size <= 0)
                {
                    PrintError("Некорректный размер.");
                    return;
                }
                currentArray = new VirtualCharArray(fileName, size, strLength);
                currentFileName = fileName;
                PrintSuccess($"Создан массив строк фиксированной длины ({strLength}) размером {size} элементов в файле {fileName}");
            }
            else if (varcharRegex.IsMatch(typeSpec))
            {
                var match = varcharRegex.Match(typeSpec);
                int maxLen = int.Parse(match.Groups[1].Value);
                Console.Write("Введите размер массива (количество элементов): ");
                if (!long.TryParse(Console.ReadLine(), out long size) || size <= 0)
                {
                    PrintError("Некорректный размер.");
                    return;
                }
                currentArray = new VirtualVarCharArray(fileName, size, maxLen);
                currentFileName = fileName;
                PrintSuccess($"Создан массив строк переменной длины (макс. {maxLen}) размером {size} элементов в файле {fileName}");
            }
            else
            {
                PrintError("Некорректная спецификация типа. Используйте: int, char(N), varchar(N)");
            }
        }

        static void HandleOpen(string[] parts)
        {
            if (parts.Length < 2)
            {
                PrintError("Использование: Open <имя_файла>");
                return;
            }

            string fileName = parts[1];
            if (!File.Exists(fileName))
            {
                PrintError($"Файл {fileName} не существует.");
                return;
            }

            CloseCurrentArray();

            // Определяем тип по сигнатуре
            using (var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                byte[] sig = new byte[2];
                fs.Read(sig, 0, 2);
                if (sig[0] != 'V' || sig[1] != 'M')
                {
                    PrintError("Файл не является корректным виртуальным массивом.");
                    return;
                }

                byte[] sizeBytes = new byte[8];
                fs.Read(sizeBytes, 0, 8);
                long arraySize = BitConverter.ToInt64(sizeBytes, 0);

                byte type = (byte)fs.ReadByte();

                if (type == 'I')
                {
                    currentArray = new VirtualIntArray(fileName, arraySize);
                    currentFileName = fileName;
                    PrintSuccess($"Открыт массив целых чисел размером {arraySize} элементов.");
                }
                else if (type == 'C')
                {
                    byte[] lenBytes = new byte[4];
                    fs.Read(lenBytes, 0, 4);
                    int strLen = BitConverter.ToInt32(lenBytes, 0);
                    currentArray = new VirtualCharArray(fileName, arraySize, strLen);
                    currentFileName = fileName;
                    PrintSuccess($"Открыт массив строк фиксированной длины ({strLen}) размером {arraySize} элементов.");
                }
                else if (type == 'V')
                {
                    byte[] lenBytes = new byte[4];
                    fs.Read(lenBytes, 0, 4);
                    int maxLen = BitConverter.ToInt32(lenBytes, 0);
                    currentArray = new VirtualVarCharArray(fileName, arraySize, maxLen);
                    currentFileName = fileName;
                    PrintSuccess($"Открыт массив строк переменной длины (макс. {maxLen}) размером {arraySize} элементов.");
                }
                else
                {
                    PrintError("Неизвестный тип массива в файле.");
                }
            }
        }

        static void HandleInput(string[] parts)
        {
            if (currentArray == null)
            {
                PrintError("Нет открытого файла. Сначала используйте Create или Open.");
                return;
            }

            if (parts.Length < 3)
            {
                PrintError("Использование: Input <индекс> <значение>");
                return;
            }

            if (!long.TryParse(parts[1], out long index) || index < 0)
            {
                PrintError("Некорректный индекс.");
                return;
            }

            object value;
            // Определяем тип текущего массива
            if (currentArray is VirtualIntArray)
            {
                if (!int.TryParse(parts[2], out int intVal))
                {
                    PrintError("Значение должно быть целым числом.");
                    return;
                }
                value = intVal;
            }
            else if (currentArray is VirtualCharArray || currentArray is VirtualVarCharArray)
            {
                value = parts[2]; // строка уже без кавычек благодаря ParseCommand
            }
            else
            {
                PrintError("Неизвестный тип массива.");
                return;
            }

            try
            {
                currentArray.Input(index, value);
                PrintSuccess($"Значение записано по индексу {index}.");
            }
            catch (Exception ex)
            {
                PrintError($"Ошибка записи: {ex.Message}");
            }
        }

        static void HandlePrint(string[] parts)
        {
            if (currentArray == null)
            {
                PrintError("Нет открытого файла. Сначала используйте Create или Open.");
                return;
            }

            if (parts.Length < 2)
            {
                PrintError("Использование: Print <индекс>");
                return;
            }

            if (!long.TryParse(parts[1], out long index) || index < 0)
            {
                PrintError("Некорректный индекс.");
                return;
            }

            try
            {
                object val = currentArray.Print(index);
                if (val == null)
                    Console.WriteLine($"[{index}] = <не записано>");
                else
                    Console.WriteLine($"[{index}] = {val}");
            }
            catch (Exception ex)
            {
                PrintError($"Ошибка чтения: {ex.Message}");
            }
        }

        static void CloseCurrentArray()
        {
            if (currentArray != null)
            {
                currentArray.Close();
                currentArray = null;
                currentFileName = null;
            }
        }

        static void PrintError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Ошибка: {message}");
            Console.ResetColor();
        }

        static void PrintSuccess(string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(message);
            Console.ResetColor();
        }
    }
}