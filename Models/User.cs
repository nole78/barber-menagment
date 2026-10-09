namespace BarberMenagment.Models;

public class User
{
    public int Id { get; set; }

    public required string FirstName { get; set; }

    public required string LastName { get; set; }

    public required string Email { get; set; }

    public required string PhoneNumber { get; set; }

    public required string PasswordHash { get; set; }

    public UserRole Role { get; set; }

    public ICollection<BarberService> BarberServices { get; set; } = [];

    public ICollection<BarberShift> BarberShifts { get; set; } = [];

    public ICollection<Appointment> ClientAppointments { get; set; } = [];

    public ICollection<Appointment> BarberAppointments { get; set; } = [];
}
