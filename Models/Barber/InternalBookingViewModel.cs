using System.ComponentModel.DataAnnotations;
using BarberMenagment.Models.Booking;

namespace BarberMenagment.Models.Barber;

public class InternalBookingViewModel
{
    [Required]
    [Display(Name = "Usluga")]
    public int ServiceId { get; set; }

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Datum")]
    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Required]
    [Display(Name = "Termin")]
    public DateTime? AppointmentDateTime { get; set; }

    [EmailAddress]
    [Display(Name = "E-mail registrovanog klijenta")]
    public string? ClientEmail { get; set; }

    public string BarberName { get; init; } = string.Empty;

    public IReadOnlyList<InternalBookingServiceOption> Services { get; init; } = [];

    public IReadOnlyList<InternalBookingSlotOption> AvailableSlots { get; init; } = [];

    public DateOnly Month { get; init; }

    public DateOnly CurrentMonth { get; init; }

    public DateOnly NextMonth { get; init; }

    public IReadOnlyList<CalendarDayViewModel> Days { get; init; } = [];
}

public class InternalBookingServiceOption
{
    public int Id { get; init; }

    public required string Name { get; init; }

    public decimal Price { get; init; }

    public int DurationMinutes { get; init; }
}

public class InternalBookingSlotOption
{
    public DateTime Start { get; init; }

    public string DisplayTime => Start.ToString("HH:mm");
}

public class BarberDashboardViewModel
{
    public required string BarberName { get; init; }

    public int UpcomingAppointmentCount { get; init; }

    public int TodayAppointmentCount { get; init; }

    public int DefinedShiftCount { get; init; }
}

public class BarberAppointmentListItemViewModel
{
    public int Id { get; init; }

    public DateTime AppointmentDateTime { get; init; }

    public required string ServiceName { get; init; }

    public required string ClientName { get; init; }

    public required string AppointmentType { get; init; }

    public decimal Price { get; init; }
}

public class CancelAppointmentViewModel
{
    public int AppointmentId { get; set; }

    [Required]
    [StringLength(1000)]
    [Display(Name = "Razlog otkazivanja")]
    public string Reason { get; set; } = string.Empty;
}

public class BarberShiftViewModel
{
    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Datum")]
    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Required]
    [Display(Name = "Tip smene")]
    public ShiftType ShiftType { get; set; }

    public IReadOnlyList<BarberShift> UpcomingShifts { get; init; } = [];
}

public class ServiceEditViewModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    [Display(Name = "Naziv")]
    public string Name { get; set; } = string.Empty;

    [StringLength(2000)]
    [Display(Name = "Opis")]
    public string? Description { get; set; }

    [Range(0.01, 100000)]
    [Display(Name = "Cena (RSD)")]
    public decimal Price { get; set; }

    [Range(1, 1440)]
    [Display(Name = "Trajanje (minuti)")]
    public int DurationMinutes { get; set; }

    [Display(Name = "Frizeri")]
    public List<int> BarberIds { get; set; } = [];

    public IReadOnlyList<BarberOption> Barbers { get; set; } = [];
}

public class BarberOption
{
    public int Id { get; init; }

    public required string FullName { get; init; }
}

public class ServiceListItemViewModel
{
    public int Id { get; init; }

    public required string Name { get; init; }

    public string? Description { get; init; }

    public decimal Price { get; init; }

    public int DurationMinutes { get; init; }

    public int AssignedBarberCount { get; init; }
}
