using Microsoft.Extensions.DependencyInjection;
using VyshKat.Models;
using VyshKat.Services;
using Xunit;

namespace VyshKat.Tests;

public class FleetServiceTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddTransport_UsesInspectionResultFromDi(bool accepted)
    {
        var fake = new FakeServiceCenter(accepted);
        var services = new ServiceCollection();
        services.AddSingleton<IServiceCenter>(fake);
        services.AddSingleton<FleetService>();
        using ServiceProvider provider = services.BuildServiceProvider();
        FleetService fleet = provider.GetRequiredService<FleetService>();
        var scooter = new Scooter("Самокат", "S1", 7, true, true, 2);

        bool result = fleet.AddTransport(scooter);

        Assert.Equal(accepted, result);
        Assert.Same(scooter, fake.LastInspected);
        Assert.Equal(accepted ? 1 : 0, fleet.TransportCount);
        Assert.Equal(accepted ? 2m : 0m, fleet.GetTotalEnergyConsumption());
    }

    [Fact]
    public void EmptyFleet_HasEmptyReports()
    {
        var fleet = new FleetService(new ServiceCenter());

        Assert.Empty(fleet.Inventory);
        Assert.Empty(fleet.GetBeginnerTransport());
        Assert.Equal(0, fleet.TransportCount);
        Assert.Equal(0, fleet.ThingCount);
        Assert.Equal(0m, fleet.GetTotalEnergyConsumption());
    }

    [Fact]
    public void TotalEnergy_IncludesTransportAndChargingStations()
    {
        var fleet = new FleetService(new ServiceCenter());
        fleet.AddTransport(new Scooter("Самокат", "S1", 7, true, true, 2.5m));
        fleet.AddTransport(new ElectricBike("Электровелосипед", "E1", 5, true, true, 1.5m));
        fleet.AddTransport(new Bicycle("Велосипед", "B1", 8, true, true));
        fleet.AddThing(new Helmet("Шлем", "H1"));
        fleet.AddThing(new ChargingStation("Станция", "C1", 4));

        Assert.Equal(8m, fleet.GetTotalEnergyConsumption());
        Assert.Equal(3, fleet.TransportCount);
        Assert.Equal(2, fleet.ThingCount);
        Assert.Equal(5, fleet.Inventory.Count);
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(5, false)]
    [InlineData(6, true)]
    [InlineData(10, true)]
    public void BeginnerTransport_UsesThresholdSix(int ease, bool expected)
    {
        var fleet = new FleetService(new ServiceCenter());
        var bicycle = new Bicycle("Велосипед", "B1", ease, true, true);
        fleet.AddTransport(bicycle);
        fleet.AddThing(new Helmet("Шлем", "H1"));

        Assert.Equal(expected, fleet.GetBeginnerTransport().Contains(bicycle));
        Assert.Equal(expected ? 1 : 0, fleet.GetBeginnerTransport().Count);
    }

    [Fact]
    public void RejectedTransport_IsAbsentFromAllReports()
    {
        var fleet = new FleetService(new ServiceCenter());
        var scooter = new Scooter("Сломанный самокат", "S1", 10, false, true, 5);

        Assert.False(fleet.AddTransport(scooter));
        Assert.Empty(fleet.Inventory);
        Assert.Empty(fleet.GetBeginnerTransport());
        Assert.Equal(0m, fleet.GetTotalEnergyConsumption());
    }

    [Fact]
    public void Inventory_ContainsNamesAndNumbersOfTransportAndThings()
    {
        var fleet = new FleetService(new ServiceCenter());
        fleet.AddTransport(new Bicycle("Велосипед", "B1", 7, true, true));
        fleet.AddThing(new Helmet("Шлем", "H1"));

        Assert.Equal(new[] { "Велосипед", "Шлем" }, fleet.Inventory.Select(item => item.Name));
        Assert.Equal(new[] { "B1", "H1" }, fleet.Inventory.Select(item => item.InventoryNumber));
    }

    [Fact]
    public void DuplicateNumber_IsRejectedAcrossTransportAndThings()
    {
        var fake = new FakeServiceCenter(true);
        var fleet = new FleetService(fake);
        fleet.AddThing(new Helmet("Шлем", "A1"));

        Assert.Throws<InvalidOperationException>(() =>
            fleet.AddTransport(new Bicycle("Велосипед", "a1", 7, true, true)));
        Assert.Throws<InvalidOperationException>(() => fleet.AddThing(new Helmet("Другой шлем", " A1 ")));
        Assert.Null(fake.LastInspected);
        Assert.Single(fleet.Inventory);
    }

    [Fact]
    public void RejectedNumber_CanBeUsedForAnotherTransport()
    {
        var fleet = new FleetService(new ServiceCenter());

        Assert.False(fleet.AddTransport(new Bicycle("Сломанный", "B1", 7, false, true)));
        Assert.True(fleet.AddTransport(new Bicycle("Исправный", "B1", 7, true, true)));
        Assert.Single(fleet.Inventory);
    }

    private sealed class FakeServiceCenter : IServiceCenter
    {
        private readonly bool result;
        public Transport? LastInspected { get; private set; }

        public FakeServiceCenter(bool result)
        {
            this.result = result;
        }

        public bool Inspect(Transport transport)
        {
            LastInspected = transport;
            return result;
        }
    }
}

public class ModelTests
{
    [Theory]
    [InlineData(true, true, true)]
    [InlineData(false, true, false)]
    [InlineData(true, false, false)]
    [InlineData(false, false, false)]
    public void ServiceCenter_ChecksBrakesAndWheels(bool brakes, bool wheels, bool expected)
    {
        var bicycle = new Bicycle("Велосипед", "B1", 7, brakes, wheels);

        Assert.Equal(expected, new ServiceCenter().Inspect(bicycle));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    public void Transport_RejectsInvalidEase(int ease)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Bicycle("Велосипед", "B1", ease, true, true));
    }

    [Theory]
    [InlineData("", "H1")]
    [InlineData("Шлем", " ")]
    public void InventoryObject_RequiresNameAndNumber(string name, string number)
    {
        Assert.Throws<ArgumentException>(() => new Helmet(name, number));
    }

    [Fact]
    public void EnergyConsumers_RejectNegativeConsumption()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Scooter("Самокат", "S1", 7, true, true, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ElectricBike("Электровелосипед", "E1", 7, true, true, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChargingStation("Станция", "C1", -1));
    }

    [Fact]
    public void InventoryObject_TrimsNameAndNumber()
    {
        var helmet = new Helmet(" Шлем ", " H1 ");

        Assert.Equal("Шлем", helmet.Name);
        Assert.Equal("H1", helmet.InventoryNumber);
    }
}

public class ConsoleAppTests
{
    [Fact]
    public void Menu_AcceptsInputAndShowsAllMainScenarios()
    {
        string[] lines =
        {
            "3", "4", "5",
            "1", "1", "Самокат", "S1", "6", "1", "1", "2,5",
            "1", "2", "Велосипед", "B1", "5", "1", "1",
            "1", "3", "Электровелосипед", "E1", "8", "1", "1", "1.5",
            "1", "1", "Сломанный", "S2", "8", "0", "1", "3",
            "2", "1", "Шлем", "H1",
            "2", "2", "Станция", "C1", "4",
            "3", "4", "5", "0"
        };
        var fleet = new FleetService(new ServiceCenter());
        using var input = new StringReader(string.Join('\n', lines));
        using var output = new StringWriter();

        new ConsoleApp(fleet, input, output).Run();

        string result = output.ToString();
        Assert.Contains("Список пуст.", result);
        Assert.Contains("Транспорт принят в парк.", result);
        Assert.Contains("Транспорт отклонён.", result);
        Assert.Contains("Транспорт: 3", result);
        Assert.Contains("Вещи: 2", result);
        Assert.Contains("Всего единиц: 5", result);
        Assert.Contains("Суточный расход энергии: 8.0 кВт·ч", result);
        Assert.Contains("Самокат | S1", result);
        Assert.Contains("Станция | C1", result);
        Assert.DoesNotContain("Сломанный | S2", result);
        Assert.Equal(2, fleet.GetBeginnerTransport().Count);
    }

    [Theory]
    [InlineData("9\n0", "Нет такого действия.")]
    [InlineData("1\n9\n0", "Нет такого типа транспорта.")]
    [InlineData("2\n9\n0", "Нет такого типа вещи.")]
    [InlineData("1\nabc\n0", "Ошибка:")]
    [InlineData("2\n1\n \n0", "Значение не должно быть пустым.")]
    [InlineData("1\n2\nВелосипед\nB1\n7\n2\n0", "Введите 1 или 0.")]
    [InlineData("1\n1\nСамокат\nS1\n7\n1\n1\n-1\n0", "Расход энергии не может быть отрицательным.")]
    public void Menu_HandlesInvalidInput(string script, string expected)
    {
        var fleet = new FleetService(new ServiceCenter());
        using var input = new StringReader(script);
        using var output = new StringWriter();

        new ConsoleApp(fleet, input, output).Run();

        Assert.Contains(expected, output.ToString());
        Assert.Empty(fleet.Inventory);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1\n2")]
    public void Menu_StopsWhenInputEnds(string script)
    {
        using var input = new StringReader(script);
        using var output = new StringWriter();
        var fleet = new FleetService(new ServiceCenter());

        new ConsoleApp(fleet, input, output).Run();

        Assert.Empty(fleet.Inventory);
    }
}
