namespace BarberMenagment.Models.Booking;

public class BookingConfirmationViewModel
{
    public int BarberId { get; init; }

    public int ServiceId { get; init; }

    public DateTime AppointmentDateTime { get; init; }

    public required string BarberName { get; init; }

    public required string ServiceName { get; init; }

    public decimal Price { get; init; }

    public int DurationMinutes { get; init; }
}
