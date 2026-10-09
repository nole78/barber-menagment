namespace BarberMenagment.Models.Booking;

public class ServiceSelectionViewModel
{
    public int BarberId { get; init; }

    public required string BarberName { get; init; }

    public IReadOnlyList<ServiceOptionViewModel> Services { get; init; } = [];
}

public class ServiceOptionViewModel
{
    public int Id { get; init; }

    public required string Name { get; init; }

    public string? Description { get; init; }

    public decimal Price { get; init; }

    public int DurationMinutes { get; init; }
}
