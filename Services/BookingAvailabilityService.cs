using BarberMenagment.Data;
using BarberMenagment.Models;
using BarberMenagment.Models.Booking;
using Microsoft.EntityFrameworkCore;

namespace BarberMenagment.Services;

public class BookingAvailabilityService(ApplicationDbContext dbContext)
{
    public async Task<IReadOnlyList<SlotViewModel>> GetAvailableSlotsAsync(
        int barberId,
        int serviceId,
        DateOnly date,
        DateTime now)
    {
        var service = await dbContext.Services
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == serviceId);
        if (service is null)
        {
            return [];
        }

        var shift = await dbContext.BarberShifts
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.BarberId == barberId && item.Date == date);
        if (shift is null || shift.ShiftType == ShiftType.DayOff)
        {
            return [];
        }

        var (shiftStart, shiftEnd) = GetShiftWindow(date, shift.ShiftType);
        var shiftStartUtc = TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(shiftStart, DateTimeKind.Unspecified));
        var shiftEndUtc = TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(shiftEnd, DateTimeKind.Unspecified));
        var activeAppointments = await dbContext.Appointments
            .AsNoTracking()
            .Where(appointment =>
                appointment.BarberId == barberId &&
                appointment.Status == AppointmentStatus.Active &&
                appointment.AppointmentDateTime < shiftEndUtc &&
                appointment.AppointmentDateTime.AddMinutes(appointment.Service.DurationMinutes) > shiftStartUtc)
            .Include(appointment => appointment.Service)
            .ToListAsync();

        var slots = new List<SlotViewModel>();
        for (var slotStart = shiftStart;
             slotStart.AddMinutes(service.DurationMinutes) <= shiftEnd;
             slotStart = slotStart.AddMinutes(service.DurationMinutes))
        {
            if (date == DateOnly.FromDateTime(now) && slotStart <= now)
            {
                continue;
            }

            var slotEnd = slotStart.AddMinutes(service.DurationMinutes);
            var overlaps = activeAppointments.Any(appointment =>
            {
                var appointmentStart = appointment.AppointmentDateTime.ToLocalTime();
                return appointmentStart < slotEnd &&
                       appointmentStart.AddMinutes(appointment.Service.DurationMinutes) > slotStart;
            });
            if (!overlaps)
            {
                slots.Add(new SlotViewModel { Start = slotStart });
            }
        }

        return slots;
    }

    public static (DateTime Start, DateTime End) GetShiftWindow(DateOnly date, ShiftType shiftType)
    {
        var startTime = shiftType == ShiftType.FirstShift
            ? new TimeOnly(8, 0)
            : new TimeOnly(14, 0);
        var endTime = shiftType == ShiftType.FirstShift
            ? new TimeOnly(14, 0)
            : new TimeOnly(20, 0);

        return (date.ToDateTime(startTime), date.ToDateTime(endTime));
    }
}
