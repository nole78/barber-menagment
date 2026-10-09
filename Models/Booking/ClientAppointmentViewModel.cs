namespace BarberMenagment.Models.Booking;

public class ClientAppointmentViewModel
{
    public int Id { get; init; }

    public DateTime AppointmentDateTime { get; init; }

    public required string BarberName { get; init; }

    public required string ServiceName { get; init; }

    public decimal Price { get; init; }
}
