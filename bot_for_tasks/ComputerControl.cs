using System;
using System.Diagnostics;
using LibreHardwareMonitor.Hardware;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

namespace bot_helping_with_tasks_bot
{
    internal class ComputerControl
    {
        public void Reboot()
        {
            Process.Start("shutdown", "/r /t 0");
        }
        public void Shutdown()
        {
            Process.Start("shutdown", "/s /t 0");
        }
        public List<string> Status()
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
}
