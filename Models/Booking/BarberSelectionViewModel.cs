namespace BarberMenagment.Models.Booking;

public class BarberSelectionViewModel
{
    public IReadOnlyList<BarberOptionViewModel> Barbers { get; init; } = [];
}

public class BarberOptionViewModel
{
    public int Id { get; init; }

    public required string FullName { get; init; }
}
