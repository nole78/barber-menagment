using System.Data;
using System.Globalization;
using BarberMenagment.Data;
using BarberMenagment.Models;
using BarberMenagment.Models.Booking;
using BarberMenagment.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BarberMenagment.Controllers;

public class BookingController(
    ApplicationDbContext dbContext,
    BookingAvailabilityService availabilityService,
    IEmailService emailService) : Controller
{
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> SelectBarber()
    {
        var barbers = await dbContext.Users
            .AsNoTracking()
            .Where(user => user.Role == UserRole.Barber)
            .OrderBy(user => user.FirstName)
            .ThenBy(user => user.LastName)
            .Select(user => new BarberOptionViewModel
            {
                Id = user.Id,
                FullName = user.FirstName + " " + user.LastName
            })
            .ToListAsync();

        return View(new BarberSelectionViewModel { Barbers = barbers });
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> SelectService(int barberId)
    {
        var barber = await dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == barberId && user.Role == UserRole.Barber)
            .Select(user => new
            {
                user.Id,
                FullName = user.FirstName + " " + user.LastName
            })
            .SingleOrDefaultAsync();
        if (barber is null)
        {
            return NotFound();
        }

        var services = await dbContext.BarberServices
            .AsNoTracking()
            .Where(item => item.BarberId == barberId)
            .OrderBy(item => item.Service.Name)
            .Select(item => new ServiceOptionViewModel
            {
                Id = item.ServiceId,
                Name = item.Service.Name,
                Description = item.Service.Description,
                Price = item.Service.Price,
                DurationMinutes = item.Service.DurationMinutes
            })
            .ToListAsync();

        return View(new ServiceSelectionViewModel
        {
            BarberId = barber.Id,
            BarberName = barber.FullName,
            Services = services
        });
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Calendar(int barberId, int serviceId, int? year, int? month, int? day)
    {
        var currentDate = DateOnly.FromDateTime(DateTime.Now);
        var currentMonth = new DateOnly(currentDate.Year, currentDate.Month, 1);
        var nextMonth = currentMonth.AddMonths(1);
        var requestedMonth = year.HasValue && month.HasValue
            ? new DateOnly(year.Value, month.Value, 1)
            : currentMonth;
        if (requestedMonth != currentMonth && requestedMonth != nextMonth)
        {
            return BadRequest("Only the current and next month are available.");
        }

        var selection = await GetBookingSelectionAsync(barberId, serviceId);
        if (selection is null)
        {
            return NotFound();
        }

        var selectedDate = day.HasValue
            ? new DateOnly(requestedMonth.Year, requestedMonth.Month, day.Value)
            : requestedMonth == currentMonth ? currentDate : requestedMonth;
        if (selectedDate < requestedMonth || selectedDate >= requestedMonth.AddMonths(1))
        {
            return BadRequest("The selected date is outside the requested month.");
        }

        var days = new List<CalendarDayViewModel>();
        var firstDay = requestedMonth;
        var daysInMonth = DateTime.DaysInMonth(requestedMonth.Year, requestedMonth.Month);
        for (var index = 0; index < daysInMonth; index++)
        {
            var date = firstDay.AddDays(index);
            var slots = await availabilityService.GetAvailableSlotsAsync(
                barberId,
                serviceId,
                date,
                DateTime.Now);
            days.Add(new CalendarDayViewModel
            {
                Date = date,
                IsToday = date == currentDate,
                IsSelected = date == selectedDate,
                IsInCurrentMonth = requestedMonth == currentMonth,
                HasAvailableSlots = slots.Count > 0
            });
        }

        var selectedSlots = await availabilityService.GetAvailableSlotsAsync(
            barberId,
            serviceId,
            selectedDate,
            DateTime.Now);

        var model = new CalendarViewModel
        {
            BarberId = barberId,
            ServiceId = serviceId,
            BarberName = selection.BarberName,
            ServiceName = selection.ServiceName,
            Price = selection.Price,
            DurationMinutes = selection.DurationMinutes,
            Month = requestedMonth,
            SelectedDate = selectedDate,
            CurrentMonth = currentMonth,
            NextMonth = nextMonth,
            Days = days,
            AvailableSlots = selectedSlots
        };

        if (!User.Identity?.IsAuthenticated ?? false)
        {
            TempData["LoginMessage"] = "Morate biti prijavljeni da biste izabrali termin.";
            var returnUrl = Url.Action(nameof(Calendar), new
            {
                barberId,
                serviceId,
                year = requestedMonth.Year,
                month = requestedMonth.Month,
                day = selectedDate.Day
            });
            return RedirectToAction("Login", "Account", new { returnUrl });
        }

        if (!User.IsInRole(nameof(UserRole.Client)))
        {
            return Forbid();
        }

        return View(model);
    }

    [Authorize(Roles = nameof(UserRole.Client))]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(BookingConfirmationViewModel model)
    {
        var clientId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(clientId, out var parsedClientId))
        {
            return Challenge();
        }

        var selection = await GetBookingSelectionAsync(model.BarberId, model.ServiceId);
        if (selection is null)
        {
            return NotFound();
        }

        var appointmentStart = DateTime.SpecifyKind(model.AppointmentDateTime, DateTimeKind.Unspecified);
        var today = DateOnly.FromDateTime(DateTime.Now);
        if (DateOnly.FromDateTime(appointmentStart) < today)
        {
            ModelState.AddModelError(string.Empty, "The selected date has already passed.");
            return RedirectToCalendar(model);
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var availableSlots = await availabilityService.GetAvailableSlotsAsync(
                model.BarberId,
                model.ServiceId,
                DateOnly.FromDateTime(appointmentStart),
                DateTime.Now);
            if (!availableSlots.Any(slot => slot.Start == appointmentStart))
            {
                TempData["BookingError"] = "The selected time is no longer available.";
                return RedirectToCalendar(model);
            }

            var client = await dbContext.Users
                .AsNoTracking()
                .SingleAsync(user => user.Id == parsedClientId);
            var appointmentDate = DateOnly.FromDateTime(appointmentStart);
            var hasAppointmentThatDay = await dbContext.Appointments.AnyAsync(appointment =>
                appointment.ClientId == parsedClientId &&
                appointment.Status == AppointmentStatus.Active &&
                appointment.AppointmentDateTime >=
                    TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(
                        appointmentDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified)) &&
                appointment.AppointmentDateTime <
                    TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(
                        appointmentDate.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified)));
            if (hasAppointmentThatDay)
            {
                TempData["BookingError"] = "Možete imati samo jedan aktivan termin dnevno.";
                return RedirectToCalendar(model);
            }

            var appointment = new Appointment
            {
                ClientId = parsedClientId,
                BarberId = model.BarberId,
                ServiceId = model.ServiceId,
                AppointmentDateTime = appointmentStart.ToUniversalTime(),
                Price = selection.Price,
                AppointmentType = AppointmentType.OnlineClient,
                Status = AppointmentStatus.Active
            };
            dbContext.Appointments.Add(appointment);
            await dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            var emailSent = await emailService.SendAsync(
                client.Email,
                $"{client.FirstName} {client.LastName}",
                "Appointment confirmation - Radisav Fashion",
                BuildConfirmationEmail(
                    client.FirstName,
                    selection,
                    appointmentStart),
                HttpContext.RequestAborted);
            if (!emailSent)
            {
                TempData["BookingEmailWarning"] =
                    "Your appointment was saved, but the confirmation email could not be sent.";
            }
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }

        return RedirectToAction(nameof(Confirmation), new
        {
            barberName = selection.BarberName,
            serviceName = selection.ServiceName,
            appointmentDateTime = appointmentStart,
            price = selection.Price
        });
    }

    [Authorize]
    [HttpGet]
    public IActionResult Confirmation(
        string barberName,
        string serviceName,
        DateTime appointmentDateTime,
        decimal price)
    {
        return View(new BookingConfirmationViewModel
        {
            BarberName = barberName,
            ServiceName = serviceName,
            AppointmentDateTime = appointmentDateTime,
            Price = price
        });
    }

    private async Task<BookingSelection?> GetBookingSelectionAsync(int barberId, int serviceId)
    {
        return await dbContext.BarberServices
            .AsNoTracking()
            .Where(item => item.BarberId == barberId && item.ServiceId == serviceId)
            .Select(item => new BookingSelection
            {
                BarberName = item.Barber.FirstName + " " + item.Barber.LastName,
                ServiceName = item.Service.Name,
                Price = item.Service.Price,
                DurationMinutes = item.Service.DurationMinutes
            })
            .SingleOrDefaultAsync();
    }

    private IActionResult RedirectToCalendar(BookingConfirmationViewModel model)
    {
        return RedirectToAction(nameof(Calendar), new
        {
            barberId = model.BarberId,
            serviceId = model.ServiceId,
            year = model.AppointmentDateTime.Year,
            month = model.AppointmentDateTime.Month,
            day = model.AppointmentDateTime.Day
        })!;
    }

    private static string BuildConfirmationEmail(
        string clientFirstName,
        BookingSelection selection,
        DateTime appointmentStart)
    {
        var serbianCulture = new CultureInfo("sr-RS");
        return $"""
                <h1>Appointment confirmed</h1>
                <p>Hello {System.Net.WebUtility.HtmlEncode(clientFirstName)},</p>
                <p>Your appointment at Radisav Fashion has been confirmed.</p>
                <ul>
                    <li><strong>Barber:</strong> {System.Net.WebUtility.HtmlEncode(selection.BarberName)}</li>
                    <li><strong>Service:</strong> {System.Net.WebUtility.HtmlEncode(selection.ServiceName)}</li>
                    <li><strong>Date and time:</strong> {appointmentStart:dddd, MMMM d, yyyy HH:mm}</li>
                    <li><strong>Price:</strong> {selection.Price.ToString("C", serbianCulture)}</li>
                </ul>
                """;
    }

    private sealed class BookingSelection
    {
        public required string BarberName { get; init; }

        public required string ServiceName { get; init; }

        public decimal Price { get; init; }

        public int DurationMinutes { get; init; }
    }
}
