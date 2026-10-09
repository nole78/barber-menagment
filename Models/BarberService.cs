namespace BarberMenagment.Models;

public class BarberService
{
    public int BarberId { get; set; }

    public int ServiceId { get; set; }

    public User Barber { get; set; } = null!;

    public Service Service { get; set; } = null!;
}
