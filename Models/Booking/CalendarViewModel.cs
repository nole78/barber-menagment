namespace BarberMenagment.Models.Booking;

public class CalendarViewModel
{
    public int BarberId { get; init; }

    public int ServiceId { get; init; }

    public required string BarberName { get; init; }

    public required string ServiceName { get; init; }

    public decimal Price { get; init; }

    public int DurationMinutes { get; init; }

    public DateOnly Month { get; init; }

    public DateOnly SelectedDate { get; init; }

    public DateOnly CurrentMonth { get; init; }

    public DateOnly NextMonth { get; init; }

    public IReadOnlyList<CalendarDayViewModel> Days { get; init; } = [];

    public IReadOnlyList<SlotViewModel> AvailableSlots { get; init; } = [];
}

public class CalendarDayViewModel
{
    public DateOnly Date { get; init; }

    public bool IsToday { get; init; }

    public bool IsSelected { get; init; }

    public bool IsInCurrentMonth { get; init; }

    public bool HasAvailableSlots { get; init; }
}

public class SlotViewModel
{
    public DateTime Start { get; init; }

    public string DisplayTime => Start.ToString("HH:mm");
}
