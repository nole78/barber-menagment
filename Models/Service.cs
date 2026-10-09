namespace BarberMenagment.Models;

public class Service
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public int DurationMinutes { get; set; }

    public ICollection<BarberService> BarberServices { get; set; } = [];

    public ICollection<Appointment> Appointments { get; set; } = [];
}
