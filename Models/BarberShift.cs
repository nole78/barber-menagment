namespace BarberMenagment.Models;

public class BarberShift
{
    public int Id { get; set; }

    public int BarberId { get; set; }

    public DateOnly Date { get; set; }

    public ShiftType ShiftType { get; set; }

    public User Barber { get; set; } = null!;
}
