using System.Diagnostics;
using LibreHardwareMonitor.Hardware;
using System.Security.Cryptography;
using System.Text;
using System;

namespace bot_for_tasks
{
    class computerControl
    {
        private const string _storedHash = "eb7b0811944629fe0a03fc17946ff70343992963a6bb1fafdd2bc244f2cdd707";
        public static bool IsAuthenticated { get; private set; }
        public static bool CheckPassword(string inputPassword)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(inputPassword);
                byte[] hashBytes = sha256.ComputeHash(inputBytes);
                string inputHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();

                IsAuthenticated = (inputHash == _storedHash);
                return IsAuthenticated;
            }
        }
        public static void Reboot()
        {
            Process.Start("shutdown", "/r /t 0");
        }
        public static void Shutdown()
        {
            Process.Start("shutdown", "/s /t 0");
        }
        public static List<string> Status(bool choice)
        {
            List<string> statusMessages = new List<string>();
            Computer computer = new Computer
            {
                IsCpuEnabled = true,
                IsGpuEnabled = true,
                IsMotherboardEnabled = true,
                IsMemoryEnabled = true
            };
            computer.Open();
            if (choice == false)
            {
                var cpu = computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Cpu);
                if (cpu != null)
                {
                    cpu.Update();
                    var cpuLoad = cpu.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Load);
                    var cpuTemperature = cpu.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Temperature);
                    statusMessages.Add($"Процессор: {cpu.Name}");
                    statusMessages.Add($"Загрузка: {cpuLoad?.Value:F1}%");
                    statusMessages.Add($"Температура: {(cpuTemperature?.Value.HasValue == true ? cpuTemperature.Value.Value : "Нет данных")} °C");
                    statusMessages.Add("");
                }

                var gpu = computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.GpuNvidia);
                if (gpu != null)
                {
                    gpu.Update();
                    var gpuLoad = gpu.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Load);
                    var gpuTemperature = gpu.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Temperature);
                    var gpuPower = gpu.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Power);
                    statusMessages.Add($"Видеокарта: {gpu.Name}");
                    statusMessages.Add($"Загрузка: {gpuLoad?.Value:F1}%");
                    statusMessages.Add($"Температура: {gpuTemperature?.Value:F1} °C");
                    statusMessages.Add($"Потребляемая мощность: {gpuPower?.Value:F1} W");
                    statusMessages.Add("");
                }

                var memory = computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Memory);
                if (memory != null)
                {
                    memory.Update();
                    var memoryLoad = memory.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Load);
                    statusMessages.Add($"Память: {memory.Name}");
                    statusMessages.Add($"Загрузка: {memoryLoad?.Value:F1}%");
                    statusMessages.Add("");

                }

                return statusMessages;
            }
            else
            {
                foreach (var hardware in computer.Hardware)
                {
                    hardware.Update();
                    statusMessages.Add($"Устройство: {hardware.Name} (Тип: {hardware.HardwareType})");

                    foreach (var sensor in hardware.Sensors)
                    {
                        if (sensor.Value.HasValue)
                        {

                            statusMessages.Add($" {sensor.Name}: {sensor.Value} {sensor.SensorType}");

                        }
                    }
                    statusMessages.Add("");
                }
                return statusMessages;
            }
        }
        public static List<string> Processes(string options)
        {
            List<string> statusProcess = new List<string>();

            if (options == "Запущенные процессы")
            {
                var groupedProcesses = Process.GetProcesses()
                    .GroupBy(p => p.ProcessName)
                    .OrderBy(g => g.Key);

                foreach (var group in groupedProcesses)
                {
                    statusProcess.Add($"=== {group.Key} ===");

                    foreach (var process in group)
                    {
                        try
                        {
                            string processInfo = $"  └─ ID: {process.Id} | Память: {process.WorkingSet64 / 1024} KB";

                            try
                            {
                                string path = process.MainModule?.FileName ?? "Путь недоступен";
                                processInfo += $" | Путь: {path}";
                            }
                            catch
                            {
                                processInfo += " | Путь: (нет доступа)";
                            }

                            statusProcess.Add(processInfo);
                        }
                        catch (Exception ex)
                        {
                            statusProcess.Add($"  └─ Ошибка чтения процесса {group.Key}: {ex.Message}");
                        }
                    }

                    statusProcess.Add("");
                }
            }
            return statusProcess;
        }

        public static List<string> InfoProcess(int? id = null, string name = null)
        {
            var result = new List<string>();

            try
            {
                Process[] processes;

                if (id.HasValue)
                {
                    processes = new Process[] { Process.GetProcessById(id.Value) };
                }
                else if (!string.IsNullOrEmpty(name))
                {
                    processes = Process.GetProcessesByName(name);

                    if (processes.Length == 0)
                    {
                        result.Add($"⚠️ Процессы с именем '{name}' не найдены");
                        return result;
                    }
                }
                else
                {
                    result.Add("⚠️ Укажите ID или имя процесса");
                    return result;
                }

                foreach (var process in processes)
                {
                    try
                    {
                        using (process)
                        {
                            if (process.HasExited)
                            {
                                result.Add($"⚠️ Процесс {process.Id} ({process.ProcessName}) уже завершился");
                                continue;
                            }

                            result.Add($"""
                    ID: {process.Id}
                    Имя: {process.ProcessName}
                    Память: {process.WorkingSet64 / (1024 * 1024)} MB
                    Путь: {GetSafePath(process)}
                    Время запуска: {process.StartTime:yyyy-MM-dd HH:mm:ss}
                    Приоритет: {process.BasePriority}
                    """);

                            if (processes.Length > 1)
                                result.Add("────────────────────");
                        }
                    }
                    catch (Exception ex)
                    {
                        result.Add($"⚠️ Ошибка при обработке процесса {process?.Id}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex) when (
                ex is ArgumentException ||
                ex is InvalidOperationException ||
                ex is System.ComponentModel.Win32Exception)
            {
                result.Add("❌ " + ex.Message.Replace("System.ComponentModel.Win32Exception: ", ""));
            }
            catch (Exception ex)
            {
                result.Add("❌ Не удалось получить информацию о процессе: " + ex.Message);
            }

            return result;
        }

        private static string GetSafePath(Process process)
        {
            try
            {
                return process.MainModule?.FileName ?? "Недоступно";
            }
            catch
            {
                return "❌ Доступ запрещен";
            }
        }

        public static string KillProcess(int? processId = null, string processName = null)
        {
            try
            {
                if (processId.HasValue)
                {
                    Process process = Process.GetProcessById(processId.Value);

                    if (IsSystemProcess(process.ProcessName))
                    {
                        return "⚠️ Нельзя завершить системный процесс!";
                    }

                    string processInfo = $"ID: {process.Id}, Имя: {process.ProcessName}";
                    process.Kill();
                    return $"✅ Процесс {processInfo} был успешно завершен!";
                }
                else if (!string.IsNullOrEmpty(processName))
                {
                    Process[] processes = Process.GetProcessesByName(processName);

                    if (processes.Length == 0)
                        return "⚠️ Процессы с таким именем не найдены!";

                    if (IsSystemProcess(processName))
                        return "⚠️ Нельзя завершить системный процесс!";

                    int killedCount = 0;
                    foreach (Process process in processes)
                    {
                        try
                        {
                            process.Kill();
                            killedCount++;
                        }
                        catch { }
                    }

                    return killedCount == processes.Length
                        ? $"✅ Все процессы ({processes.Length}) с именем '{processName}' завершены!"
                        : $"⚠️ Завершено {killedCount} из {processes.Length} процессов!";
                }
                else
                {
                    return "⚠️ Укажите ID или имя процесса!";
                }
            }
            catch (Exception ex)
            {
                return $"⚠️ Ошибка: {ex.Message}";
            }
        }

        private static bool IsSystemProcess(string processName)
        {
            string[] systemProcesses = { "svchost", "wininit", "csrss", "winlogon", "system" };
            return systemProcesses.Contains(processName, StringComparer.OrdinalIgnoreCase);
        }

        public static string StartProcess(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    Process.Start(path);
                    return $"✅ Процесс успешно запущен: {path}";
                }
                else
                {
                    return $"⚠️ Путь к файлу не найден";
                }
            }
            catch (Exception ex)
            {
                return $"⚠️ Ошибка при запуске: {ex.Message}";
            }
        }
    }
}
