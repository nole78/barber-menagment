namespace BarberMenagment.Models;

public class Appointment
{
    public int Id { get; set; }

    public int? ClientId { get; set; }

    public int BarberId { get; set; }

    public int ServiceId { get; set; }

    public DateTime AppointmentDateTime { get; set; }

    public decimal Price { get; set; }

    public AppointmentType AppointmentType { get; set; }

    public AppointmentStatus Status { get; set; }

    public string? CancellationReason { get; set; }

    public User? Client { get; set; }

    public User Barber { get; set; } = null!;

    public Service Service { get; set; } = null!;
}
