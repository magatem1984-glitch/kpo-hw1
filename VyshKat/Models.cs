namespace VyshKat.Models;

public interface IInventoryObject
{
    string Name { get; }
    string InventoryNumber { get; }
}

public interface IEnergyConsumer
{
    decimal DailyEnergyConsumption { get; }
}

public abstract class InventoryObject : IInventoryObject
{
    public string Name { get; }
    public string InventoryNumber { get; }

    protected InventoryObject(string name, string inventoryNumber)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(inventoryNumber))
        {
            throw new ArgumentException("Название и инвентарный номер не должны быть пустыми.");
        }

        Name = name.Trim();
        InventoryNumber = inventoryNumber.Trim();
    }
}

public abstract class Transport : InventoryObject
{
    public int BeginnerEase { get; }
    public bool BrakesWorking { get; }
    public bool WheelsWorking { get; }

    protected Transport(string name, string inventoryNumber, int beginnerEase,
        bool brakesWorking, bool wheelsWorking) : base(name, inventoryNumber)
    {
        if (beginnerEase < 1 || beginnerEase > 10)
        {
            throw new ArgumentOutOfRangeException(nameof(beginnerEase), "Простота должна быть от 1 до 10.");
        }

        BeginnerEase = beginnerEase;
        BrakesWorking = brakesWorking;
        WheelsWorking = wheelsWorking;
    }
}

public abstract class ElectricTransport : Transport, IEnergyConsumer
{
    public decimal DailyEnergyConsumption { get; }

    protected ElectricTransport(string name, string inventoryNumber, int beginnerEase,
        bool brakesWorking, bool wheelsWorking, decimal dailyEnergyConsumption)
        : base(name, inventoryNumber, beginnerEase, brakesWorking, wheelsWorking)
    {
        if (dailyEnergyConsumption < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dailyEnergyConsumption), "Расход энергии не может быть отрицательным.");
        }

        DailyEnergyConsumption = dailyEnergyConsumption;
    }
}

public class Scooter : ElectricTransport
{
    public Scooter(string name, string inventoryNumber, int beginnerEase,
        bool brakesWorking, bool wheelsWorking, decimal dailyEnergyConsumption)
        : base(name, inventoryNumber, beginnerEase, brakesWorking, wheelsWorking, dailyEnergyConsumption)
    {
    }
}

public class Bicycle : Transport
{
    public Bicycle(string name, string inventoryNumber, int beginnerEase,
        bool brakesWorking, bool wheelsWorking)
        : base(name, inventoryNumber, beginnerEase, brakesWorking, wheelsWorking)
    {
    }
}

public class ElectricBike : ElectricTransport
{
    public ElectricBike(string name, string inventoryNumber, int beginnerEase,
        bool brakesWorking, bool wheelsWorking, decimal dailyEnergyConsumption)
        : base(name, inventoryNumber, beginnerEase, brakesWorking, wheelsWorking, dailyEnergyConsumption)
    {
    }
}

public abstract class Thing : InventoryObject
{
    protected Thing(string name, string inventoryNumber) : base(name, inventoryNumber)
    {
    }
}

public class Helmet : Thing
{
    public Helmet(string name, string inventoryNumber) : base(name, inventoryNumber)
    {
    }
}

public class ChargingStation : Thing, IEnergyConsumer
{
    public decimal DailyEnergyConsumption { get; }

    public ChargingStation(string name, string inventoryNumber, decimal dailyEnergyConsumption)
        : base(name, inventoryNumber)
    {
        if (dailyEnergyConsumption < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dailyEnergyConsumption), "Расход энергии не может быть отрицательным.");
        }

        DailyEnergyConsumption = dailyEnergyConsumption;
    }
}
