using System.Globalization;
using VyshKat.Models;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using VyshKat;
using VyshKat.Services;

Console.InputEncoding = Encoding.UTF8;
Console.OutputEncoding = Encoding.UTF8;

var services = new ServiceCollection();
services.AddSingleton<IServiceCenter, ServiceCenter>();
services.AddSingleton<FleetService>();
services.AddSingleton<TextReader>(Console.In);
services.AddSingleton<TextWriter>(Console.Out);
services.AddTransient<ConsoleApp>();

using ServiceProvider provider = services.BuildServiceProvider();
provider.GetRequiredService<ConsoleApp>().Run();

namespace VyshKat
{
    public class ConsoleApp
    {
        private readonly FleetService fleet;
        private readonly TextReader input;
        private readonly TextWriter output;

        public ConsoleApp(FleetService fleet, TextReader input, TextWriter output)
        {
            this.fleet = fleet;
            this.input = input;
            this.output = output;
        }

        public void Run()
        {
            while (true)
            {
                output.WriteLine("\nВышКат");
                output.WriteLine("1. Добавить транспорт");
                output.WriteLine("2. Добавить вещь");
                output.WriteLine("3. Отчёт по количеству и энергии");
                output.WriteLine("4. Транспорт для новичков");
                output.WriteLine("5. Весь инвентарь");
                output.WriteLine("0. Выход");
                output.Write("Выберите действие: ");

                string? choice = input.ReadLine();
                if (choice is null || choice == "0")
                {
                    return;
                }

                try
                {
                    switch (choice)
                    {
                        case "1":
                            AddTransport();
                            break;
                        case "2":
                            AddThing();
                            break;
                        case "3":
                            ShowReport();
                            break;
                        case "4":
                            ShowInventory(fleet.GetBeginnerTransport());
                            break;
                        case "5":
                            ShowInventory(fleet.Inventory);
                            break;
                        default:
                            output.WriteLine("Нет такого действия.");
                            break;
                    }
                }
                catch (EndOfStreamException)
                {
                    output.WriteLine("Ввод завершён.");
                    return;
                }
                catch (Exception exception) when (exception is ArgumentException
                    or FormatException or OverflowException or InvalidOperationException)
                {
                    output.WriteLine($"Ошибка: {exception.Message}");
                }
            }
        }

        private void AddTransport()
        {
            output.WriteLine("1. Электросамокат\n2. Велосипед\n3. Электровелосипед");
            int type = ReadInt("Тип транспорта: ");
            if (type < 1 || type > 3)
            {
                throw new ArgumentException("Нет такого типа транспорта.");
            }

            string name = ReadText("Название: ");
            string number = ReadText("Инвентарный номер: ");
            int ease = ReadInt("Простота для новичка (1-10): ");
            bool brakes = ReadBool("Тормоза исправны? (1 - да, 0 - нет): ");
            bool wheels = ReadBool("Колёса исправны? (1 - да, 0 - нет): ");

            Transport transport;
            if (type == 2)
            {
                transport = new Bicycle(name, number, ease, brakes, wheels);
            }
            else
            {
                decimal energy = ReadEnergy();
                transport = type == 1
                    ? new Scooter(name, number, ease, brakes, wheels, energy)
                    : new ElectricBike(name, number, ease, brakes, wheels, energy);
            }

            output.WriteLine(fleet.AddTransport(transport)
                ? "Транспорт принят в парк."
                : "Техосмотр не пройден. Транспорт отклонён.");
        }

        private void AddThing()
        {
            output.WriteLine("1. Шлем\n2. Зарядная станция");
            int type = ReadInt("Тип вещи: ");
            if (type < 1 || type > 2)
            {
                throw new ArgumentException("Нет такого типа вещи.");
            }

            string name = ReadText("Название: ");
            string number = ReadText("Инвентарный номер: ");
            Thing thing = type == 1
                ? new Helmet(name, number)
                : new ChargingStation(name, number, ReadEnergy());

            fleet.AddThing(thing);
            output.WriteLine("Вещь добавлена на баланс.");
        }

        private void ShowReport()
        {
            output.WriteLine($"Транспорт: {fleet.TransportCount}");
            output.WriteLine($"Вещи: {fleet.ThingCount}");
            output.WriteLine($"Всего единиц: {fleet.Inventory.Count}");
            string energy = fleet.GetTotalEnergyConsumption().ToString(CultureInfo.InvariantCulture);
            output.WriteLine($"Суточный расход энергии: {energy} кВт·ч");
        }

        private void ShowInventory(IEnumerable<IInventoryObject> items)
        {
            bool found = false;
            foreach (IInventoryObject item in items)
            {
                output.WriteLine($"{item.Name} | {item.InventoryNumber}");
                found = true;
            }

            if (!found)
            {
                output.WriteLine("Список пуст.");
            }
        }

        private string ReadText(string prompt)
        {
            output.Write(prompt);
            string value = input.ReadLine() ?? throw new EndOfStreamException();
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Значение не должно быть пустым.");
            }

            return value.Trim();
        }

        private int ReadInt(string prompt)
        {
            return int.Parse(ReadText(prompt), CultureInfo.InvariantCulture);
        }

        private bool ReadBool(string prompt)
        {
            return ReadInt(prompt) switch
            {
                1 => true,
                0 => false,
                _ => throw new ArgumentException("Введите 1 или 0.")
            };
        }

        private decimal ReadEnergy()
        {
            string value = ReadText("Суточный расход энергии (кВт·ч): ").Replace(',', '.');
            return decimal.Parse(value, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture);
        }
    }
}
