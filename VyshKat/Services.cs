using VyshKat.Models;

namespace VyshKat.Services;

public interface IServiceCenter
{
    bool Inspect(Transport transport);
}

public class ServiceCenter : IServiceCenter
{
    public bool Inspect(Transport transport)
    {
        return transport.BrakesWorking && transport.WheelsWorking;
    }
}

public class FleetService
{
    private readonly IServiceCenter serviceCenter;
    private readonly List<IInventoryObject> inventory = new();

    public FleetService(IServiceCenter serviceCenter)
    {
        this.serviceCenter = serviceCenter;
    }

    public IReadOnlyList<IInventoryObject> Inventory => inventory.AsReadOnly();
    public int TransportCount => inventory.OfType<Transport>().Count();
    public int ThingCount => inventory.OfType<Thing>().Count();

    public bool AddTransport(Transport transport)
    {
        CheckInventoryNumber(transport.InventoryNumber);

        if (!serviceCenter.Inspect(transport))
        {
            return false;
        }

        inventory.Add(transport);
        return true;
    }

    public void AddThing(Thing thing)
    {
        CheckInventoryNumber(thing.InventoryNumber);
        inventory.Add(thing);
    }

    public decimal GetTotalEnergyConsumption()
    {
        return inventory.OfType<IEnergyConsumer>().Sum(item => item.DailyEnergyConsumption);
    }

    public IReadOnlyList<Transport> GetBeginnerTransport()
    {
        return inventory.OfType<Transport>().Where(transport => transport.BeginnerEase >= 6).ToList();
    }

    private void CheckInventoryNumber(string inventoryNumber)
    {
        if (inventory.Any(item => string.Equals(item.InventoryNumber, inventoryNumber,
            StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Этот инвентарный номер уже используется.");
        }
    }
}
