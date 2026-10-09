using System.Security.Claims;
using BarberMenagment.Data;
using BarberMenagment.Models;
using BarberMenagment.Models.Barber;
using BarberMenagment.Models.Booking;
using BarberMenagment.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BarberMenagment.Controllers;

[Authorize(Roles = nameof(UserRole.Barber))]
public class BarberController(
    ApplicationDbContext dbContext,
    BookingAvailabilityService availabilityService,
    IEmailService emailService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var barber = await GetCurrentBarberAsync();
        if (barber is null)
        {
            return Challenge();
        }

        var nowUtc = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(DateTime.Now);
        var todayStartUtc = TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(today.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified));
        var tomorrowStartUtc = TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(today.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified));
        var upcomingAppointments = dbContext.Appointments
            .AsNoTracking()
            .Where(appointment =>
                appointment.BarberId == barber.Id &&
                appointment.Status == AppointmentStatus.Active &&
                appointment.AppointmentDateTime >= nowUtc);

        return View(new BarberDashboardViewModel
        {
            BarberName = $"{barber.FirstName} {barber.LastName}",
            UpcomingAppointmentCount = await upcomingAppointments.CountAsync(),
            TodayAppointmentCount = await upcomingAppointments.CountAsync(appointment =>
                appointment.AppointmentDateTime >= todayStartUtc &&
                appointment.AppointmentDateTime < tomorrowStartUtc),
            DefinedShiftCount = await dbContext.BarberShifts.CountAsync(shift =>
                shift.BarberId == barber.Id && shift.Date >= today)
        });
    }

    [HttpGet]
    public async Task<IActionResult> Appointments()
    {
        var barber = await GetCurrentBarberAsync();
        if (barber is null)
        {
            return Challenge();
        }

        var appointments = await dbContext.Appointments
            .AsNoTracking()
            .Where(appointment =>
                appointment.BarberId == barber.Id &&
                appointment.Status == AppointmentStatus.Active &&
                appointment.AppointmentDateTime >= DateTime.UtcNow)
            .OrderBy(appointment => appointment.AppointmentDateTime)
            .Select(appointment => new BarberAppointmentListItemViewModel
            {
                Id = appointment.Id,
                AppointmentDateTime = appointment.AppointmentDateTime,
                ServiceName = appointment.Service.Name,
                ClientName = appointment.Client == null
                    ? "Neregistrovani klijent"
                    : appointment.Client.FirstName + " " + appointment.Client.LastName,
                AppointmentType = appointment.AppointmentType == AppointmentType.InternalBarber
                    ? "Interni"
                    : "Online",
                Price = appointment.Price
            })
            .ToListAsync();

        return View(appointments);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelAppointment(CancelAppointmentViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["AppointmentError"] = "Unesite razlog otkazivanja.";
            return RedirectToAction(nameof(Appointments));
        }

        var barber = await GetCurrentBarberAsync();
        if (barber is null)
        {
            return Challenge();
        }

        var appointment = await dbContext.Appointments
            .Include(item => item.Client)
            .Include(item => item.Service)
            .SingleOrDefaultAsync(item =>
                item.Id == model.AppointmentId &&
                item.BarberId == barber.Id &&
                item.Status == AppointmentStatus.Active);
        if (appointment is null)
        {
            return NotFound();
        }

        if (appointment.AppointmentDateTime <= DateTime.UtcNow)
        {
            TempData["AppointmentError"] = "Termin koji je već počeo ne može biti otkazan.";
            return RedirectToAction(nameof(Appointments));
        }

        appointment.Status = AppointmentStatus.Cancelled;
        appointment.CancellationReason = model.Reason.Trim();
        await dbContext.SaveChangesAsync();

        if (appointment.Client is not null)
        {
            var emailSent = await emailService.SendAsync(
                appointment.Client.Email,
                $"{appointment.Client.FirstName} {appointment.Client.LastName}",
                "Appointment cancelled - Radisav Fashion",
                BuildCancellationEmail(appointment, model.Reason),
                HttpContext.RequestAborted);
            if (!emailSent)
            {
                TempData["AppointmentWarning"] =
                    "Termin je otkazan, ali e-mail obaveštenje nije poslato.";
            }
        }

        TempData["SuccessMessage"] = "Termin je uspešno otkazan.";
        return RedirectToAction(nameof(Appointments));
    }

    [HttpGet]
    public async Task<IActionResult> Shifts(DateOnly? date)
    {
        var barber = await GetCurrentBarberAsync();
        if (barber is null)
        {
            return Challenge();
        }

        var selectedDate = date ?? DateOnly.FromDateTime(DateTime.Today);
        var shift = await dbContext.BarberShifts
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.BarberId == barber.Id && item.Date == selectedDate);

        return View(new BarberShiftViewModel
        {
            Date = selectedDate,
            ShiftType = shift?.ShiftType ?? ShiftType.DayOff,
            UpcomingShifts = await dbContext.BarberShifts
                .AsNoTracking()
                .Where(item => item.BarberId == barber.Id && item.Date >= DateOnly.FromDateTime(DateTime.Today))
                .OrderBy(item => item.Date)
                .Take(31)
                .ToListAsync()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Shifts(BarberShiftViewModel model)
    {
        var barber = await GetCurrentBarberAsync();
        if (barber is null)
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (model.Date < DateOnly.FromDateTime(DateTime.Today))
        {
            ModelState.AddModelError(nameof(model.Date), "Nije moguće menjati smenu za datum koji je prošao.");
            return View(model);
        }

        var shift = await dbContext.BarberShifts
            .SingleOrDefaultAsync(item => item.BarberId == barber.Id && item.Date == model.Date);
        if (shift is null)
        {
            dbContext.BarberShifts.Add(new BarberShift
            {
                BarberId = barber.Id,
                Date = model.Date,
                ShiftType = model.ShiftType
            });
        }
        else
        {
            shift.ShiftType = model.ShiftType;
        }

        await dbContext.SaveChangesAsync();
        TempData["SuccessMessage"] = "Smena je sačuvana.";
        return RedirectToAction(nameof(Shifts), new { date = model.Date });
    }

    [HttpGet]
    public async Task<IActionResult> Services()
    {
        var services = await dbContext.Services
            .AsNoTracking()
            .OrderBy(service => service.Name)
            .Select(service => new ServiceListItemViewModel
            {
                Id = service.Id,
                Name = service.Name,
                Description = service.Description,
                Price = service.Price,
                DurationMinutes = service.DurationMinutes,
                AssignedBarberCount = service.BarberServices.Count
            })
            .ToListAsync();

        return View(services);
    }

    [HttpGet]
    public async Task<IActionResult> CreateService()
    {
        return View(await BuildServiceModelAsync(new ServiceEditViewModel()));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateService(ServiceEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(await BuildServiceModelAsync(model));
        }

        var service = new Service
        {
            Name = model.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim(),
            Price = model.Price,
            DurationMinutes = model.DurationMinutes
        };
        dbContext.Services.Add(service);
        await dbContext.SaveChangesAsync();
        await SaveBarberAssignmentsAsync(service.Id, model.BarberIds);
        TempData["SuccessMessage"] = "Usluga je kreirana.";
        return RedirectToAction(nameof(Services));
    }

    [HttpGet]
    public async Task<IActionResult> EditService(int id)
    {
        var service = await dbContext.Services
            .AsNoTracking()
            .Include(item => item.BarberServices)
            .SingleOrDefaultAsync(item => item.Id == id);
        if (service is null)
        {
            return NotFound();
        }

        return View(await BuildServiceModelAsync(new ServiceEditViewModel
        {
            Id = service.Id,
            Name = service.Name,
            Description = service.Description,
            Price = service.Price,
            DurationMinutes = service.DurationMinutes,
            BarberIds = service.BarberServices.Select(item => item.BarberId).ToList()
        }));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditService(ServiceEditViewModel model)
    {
        var service = await dbContext.Services
            .SingleOrDefaultAsync(item => item.Id == model.Id);
        if (service is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(await BuildServiceModelAsync(model));
        }

        service.Name = model.Name.Trim();
        service.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
        service.Price = model.Price;
        service.DurationMinutes = model.DurationMinutes;
        await SaveBarberAssignmentsAsync(service.Id, model.BarberIds);
        await dbContext.SaveChangesAsync();
        TempData["SuccessMessage"] = "Usluga je izmenjena.";
        return RedirectToAction(nameof(Services));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteService(int id)
    {
        var service = await dbContext.Services
            .Include(item => item.Appointments)
            .SingleOrDefaultAsync(item => item.Id == id);
        if (service is null)
        {
            return NotFound();
        }

        if (service.Appointments.Count > 0)
        {
            TempData["ServiceError"] = "Usluga sa postojećim terminima ne može biti obrisana.";
            return RedirectToAction(nameof(Services));
        }

        dbContext.Services.Remove(service);
        await dbContext.SaveChangesAsync();
        TempData["SuccessMessage"] = "Usluga je obrisana.";
        return RedirectToAction(nameof(Services));
    }

    [HttpGet]
    public async Task<IActionResult> InternalBooking(DateOnly? date, int? serviceId, int? year, int? month)
    {
        var barber = await GetCurrentBarberAsync();
        if (barber is null)
        {
            return Challenge();
        }

        var services = await GetServiceOptionsAsync(barber.Id);
        var today = DateOnly.FromDateTime(DateTime.Today);
        var currentMonth = new DateOnly(today.Year, today.Month, 1);
        var nextMonth = currentMonth.AddMonths(1);
        var requestedMonth = year.HasValue && month.HasValue
            ? new DateOnly(year.Value, month.Value, 1)
            : date.HasValue
                ? new DateOnly(date.Value.Year, date.Value.Month, 1)
                : currentMonth;
        if (requestedMonth != currentMonth && requestedMonth != nextMonth)
        {
            return BadRequest("Dostupni su samo tekući i naredni mesec.");
        }

        var selectedDate = date ?? (requestedMonth == currentMonth ? today : requestedMonth);
        if (selectedDate < requestedMonth || selectedDate >= requestedMonth.AddMonths(1))
        {
            return BadRequest("Izabrani datum nije u traženom mesecu.");
        }
        var selectedServiceId = serviceId ?? services.FirstOrDefault()?.Id;
        var slots = selectedServiceId.HasValue
            ? await availabilityService.GetAvailableSlotsAsync(
                barber.Id,
                selectedServiceId.Value,
                selectedDate,
                DateTime.Now)
            : [];

        var days = new List<CalendarDayViewModel>();
        for (var index = 0; index < DateTime.DaysInMonth(requestedMonth.Year, requestedMonth.Month); index++)
        {
            var day = requestedMonth.AddDays(index);
            var daySlots = selectedServiceId.HasValue
                ? await availabilityService.GetAvailableSlotsAsync(
                    barber.Id, selectedServiceId.Value, day, DateTime.Now)
                : [];
            days.Add(new CalendarDayViewModel
            {
                Date = day,
                IsToday = day == today,
                IsSelected = day == selectedDate,
                IsInCurrentMonth = requestedMonth == currentMonth,
                HasAvailableSlots = daySlots.Count > 0
            });
        }

        return View(new InternalBookingViewModel
        {
            BarberName = $"{barber.FirstName} {barber.LastName}",
            Services = services,
            ServiceId = selectedServiceId ?? 0,
            Date = selectedDate,
            Month = requestedMonth,
            CurrentMonth = currentMonth,
            NextMonth = nextMonth,
            Days = days,
            AvailableSlots = slots
                .Select(slot => new InternalBookingSlotOption { Start = slot.Start })
                .ToList()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> InternalBooking(InternalBookingViewModel model)
    {
        var barber = await GetCurrentBarberAsync();
        if (barber is null)
        {
            return Challenge();
        }

        var services = await GetServiceOptionsAsync(barber.Id);
        model = await PopulateModelAsync(model, barber, services);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var service = await dbContext.Services
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == model.ServiceId);
        if (service is null ||
            !await dbContext.BarberServices.AnyAsync(item =>
                item.BarberId == barber.Id && item.ServiceId == model.ServiceId))
        {
            ModelState.AddModelError(nameof(model.ServiceId), "Izabrana usluga nije dostupna kod ovog frizera.");
            return View(await PopulateModelAsync(model, barber, services));
        }

        var slot = model.AppointmentDateTime!.Value;
        var availableSlots = await availabilityService.GetAvailableSlotsAsync(
            barber.Id,
            service.Id,
            model.Date,
            DateTime.Now);
        if (!availableSlots.Any(item => item.Start == slot))
        {
            ModelState.AddModelError(nameof(model.AppointmentDateTime), "Izabrani termin više nije slobodan.");
            return View(await PopulateModelAsync(model, barber, services));
        }

        User? client = null;
        if (!string.IsNullOrWhiteSpace(model.ClientEmail))
        {
            client = await dbContext.Users
                .SingleOrDefaultAsync(user =>
                    user.Email == model.ClientEmail.Trim() && user.Role == UserRole.Client);
            if (client is null)
            {
                ModelState.AddModelError(nameof(model.ClientEmail), "Registrovani klijent sa ovim e-mailom ne postoji.");
                return View(await PopulateModelAsync(model, barber, services));
            }
        }

        dbContext.Appointments.Add(new Appointment
        {
            ClientId = client?.Id,
            BarberId = barber.Id,
            ServiceId = service.Id,
            AppointmentDateTime = DateTime.SpecifyKind(slot, DateTimeKind.Unspecified).ToUniversalTime(),
            Price = service.Price,
            AppointmentType = AppointmentType.InternalBarber,
            Status = AppointmentStatus.Active
        });
        await dbContext.SaveChangesAsync();

        TempData["SuccessMessage"] = "Interni termin je uspešno sačuvan.";
        return RedirectToAction(nameof(Appointments));
    }

    private async Task<User?> GetCurrentBarberAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userId, out var parsedUserId)
            ? await dbContext.Users.SingleOrDefaultAsync(user =>
                user.Id == parsedUserId && user.Role == UserRole.Barber)
            : null;
    }

    private async Task<List<InternalBookingServiceOption>> GetServiceOptionsAsync(int barberId)
    {
        return await dbContext.BarberServices
            .AsNoTracking()
            .Where(item => item.BarberId == barberId)
            .OrderBy(item => item.Service.Name)
            .Select(item => new InternalBookingServiceOption
            {
                Id = item.ServiceId,
                Name = item.Service.Name,
                Price = item.Service.Price,
                DurationMinutes = item.Service.DurationMinutes
            })
            .ToListAsync();
    }

    private async Task<InternalBookingViewModel> PopulateModelAsync(
        InternalBookingViewModel model,
        User barber,
        IReadOnlyList<InternalBookingServiceOption> services)
    {
        var slots = model.ServiceId > 0
            ? await availabilityService.GetAvailableSlotsAsync(
                barber.Id,
                model.ServiceId,
                model.Date,
                DateTime.Now)
            : [];

        return new InternalBookingViewModel
        {
            ServiceId = model.ServiceId,
            Date = model.Date,
            AppointmentDateTime = model.AppointmentDateTime,
            ClientEmail = model.ClientEmail,
            BarberName = $"{barber.FirstName} {barber.LastName}",
            Services = services,
            AvailableSlots = slots
                .Select(slot => new InternalBookingSlotOption { Start = slot.Start })
                .ToList(),
            Month = new DateOnly(model.Date.Year, model.Date.Month, 1),
            CurrentMonth = new DateOnly(DateTime.Today.Year, DateTime.Today.Month, 1),
            NextMonth = new DateOnly(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(1)
        };
    }

    private async Task<ServiceEditViewModel> BuildServiceModelAsync(ServiceEditViewModel model)
    {
        model.Barbers = await dbContext.Users
            .AsNoTracking()
            .Where(user => user.Role == UserRole.Barber)
            .OrderBy(user => user.FirstName)
            .ThenBy(user => user.LastName)
            .Select(user => new BarberOption
            {
                Id = user.Id,
                FullName = user.FirstName + " " + user.LastName
            })
            .ToListAsync();
        return model;
    }

    private async Task SaveBarberAssignmentsAsync(int serviceId, IEnumerable<int> barberIds)
    {
        var validBarberIds = await dbContext.Users
            .Where(user => user.Role == UserRole.Barber && barberIds.Contains(user.Id))
            .Select(user => user.Id)
            .ToListAsync();
        var assignments = await dbContext.BarberServices
            .Where(item => item.ServiceId == serviceId)
            .ToListAsync();
        dbContext.BarberServices.RemoveRange(assignments);
        dbContext.BarberServices.AddRange(validBarberIds.Select(barberId => new BarberService
        {
            BarberId = barberId,
            ServiceId = serviceId
        }));
    }

    private static string BuildCancellationEmail(Appointment appointment, string reason)
    {
        return $"""
                    <h1>Appointment cancelled</h1>
                    <p>Your appointment at Radisav Fashion has been cancelled.</p>
                    <ul>
                        <li><strong>Service:</strong> {System.Net.WebUtility.HtmlEncode(appointment.Service.Name)}</li>
                        <li><strong>Date and time:</strong> {appointment.AppointmentDateTime.ToLocalTime():dddd, MMMM d, yyyy HH:mm}</li>
                        <li><strong>Reason:</strong> {System.Net.WebUtility.HtmlEncode(reason)}</li>
                    </ul>
                    """;
    }
}
