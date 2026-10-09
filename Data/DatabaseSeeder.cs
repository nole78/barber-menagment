using BarberMenagment.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BarberMenagment.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(
        IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();

        await dbContext.Database.MigrateAsync(cancellationToken);

        var barberOne = await GetOrCreateUserAsync(
            dbContext,
            passwordHasher,
            new SeedUser("Marko", "Marković", "marko.barber@radisav.local", "0601000001", UserRole.Barber),
            "admin123",
            cancellationToken);
        var barberTwo = await GetOrCreateUserAsync(
            dbContext,
            passwordHasher,
            new SeedUser("Nikola", "Nikolić", "nikola.barber@radisav.local", "0601000002", UserRole.Barber),
            "barber123",
            cancellationToken);
        var clientOne = await GetOrCreateUserAsync(
            dbContext,
            passwordHasher,
            new SeedUser("Petar", "Petrović", "petar.client@radisav.local", "0602000001", UserRole.Client),
            "client123",
            cancellationToken);
        var clientTwo = await GetOrCreateUserAsync(
            dbContext,
            passwordHasher,
            new SeedUser("Jovan", "Jovanović", "jovan.client@radisav.local", "0602000002", UserRole.Client),
            "client123",
            cancellationToken);

        var haircut = await GetOrCreateServiceAsync(
            dbContext,
            "Muško šišanje",
            "Klasično muško šišanje.",
            1200m,
            30,
            cancellationToken);
        var fade = await GetOrCreateServiceAsync(
            dbContext,
            "Skin fade",
            "Precizno šišanje sa postepenim prelazom.",
            1800m,
            45,
            cancellationToken);
        var beard = await GetOrCreateServiceAsync(
            dbContext,
            "Sređivanje brade",
            "Oblikovanje i sređivanje brade.",
            1000m,
            30,
            cancellationToken);

        await AssignServiceAsync(dbContext, barberOne, haircut, cancellationToken);
        await AssignServiceAsync(dbContext, barberTwo, haircut, cancellationToken);
        await AssignServiceAsync(dbContext, barberOne, fade, cancellationToken);
        await AssignServiceAsync(dbContext, barberTwo, beard, cancellationToken);

        var startDate = DateOnly.FromDateTime(DateTime.Today);
        for (var offset = 0; offset < 7; offset++)
        {
            var date = startDate.AddDays(offset);
            await UpsertShiftAsync(
                dbContext,
                barberOne,
                date,
                offset % 2 == 0 ? ShiftType.FirstShift : ShiftType.SecondShift,
                cancellationToken);
            await UpsertShiftAsync(
                dbContext,
                barberTwo,
                date,
                offset % 2 == 0 ? ShiftType.SecondShift : ShiftType.FirstShift,
                cancellationToken);
        }

        await AddAppointmentIfMissingAsync(
            dbContext,
            clientOne,
            barberOne,
            haircut,
            startDate.AddDays(1).ToDateTime(new TimeOnly(9, 0)),
            AppointmentType.OnlineClient,
            cancellationToken);
        await AddAppointmentIfMissingAsync(
            dbContext,
            clientOne,
            barberTwo,
            beard,
            startDate.AddDays(2).ToDateTime(new TimeOnly(15, 0)),
            AppointmentType.OnlineClient,
            cancellationToken);
        await AddAppointmentIfMissingAsync(
            dbContext,
            clientTwo,
            barberOne,
            fade,
            startDate.AddDays(3).ToDateTime(new TimeOnly(10, 0)),
            AppointmentType.OnlineClient,
            cancellationToken);
        await AddAppointmentIfMissingAsync(
            dbContext,
            clientTwo,
            barberTwo,
            haircut,
            startDate.AddDays(4).ToDateTime(new TimeOnly(16, 0)),
            AppointmentType.OnlineClient,
            cancellationToken);

        await AddAppointmentIfMissingAsync(
            dbContext,
            null,
            barberOne,
            beard,
            startDate.AddDays(5).ToDateTime(new TimeOnly(11, 0)),
            AppointmentType.InternalBarber,
            cancellationToken);
        await AddAppointmentIfMissingAsync(
            dbContext,
            null,
            barberTwo,
            haircut,
            startDate.AddDays(5).ToDateTime(new TimeOnly(17, 0)),
            AppointmentType.InternalBarber,
            cancellationToken);
        await AddAppointmentIfMissingAsync(
            dbContext,
            null,
            barberOne,
            haircut,
            startDate.AddDays(6).ToDateTime(new TimeOnly(9, 30)),
            AppointmentType.InternalBarber,
            cancellationToken);
        await AddAppointmentIfMissingAsync(
            dbContext,
            null,
            barberTwo,
            beard,
            startDate.AddDays(6).ToDateTime(new TimeOnly(15, 30)),
            AppointmentType.InternalBarber,
            cancellationToken);
    }

    private static async Task<User> GetOrCreateUserAsync(
        ApplicationDbContext dbContext,
        IPasswordHasher<User> passwordHasher,
        SeedUser seedUser,
        string password,
        CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .SingleOrDefaultAsync(item => item.Email == seedUser.Email, cancellationToken);
        if (user is not null)
        {
            return user;
        }

        user = new User
        {
            FirstName = seedUser.FirstName,
            LastName = seedUser.LastName,
            Email = seedUser.Email,
            PhoneNumber = seedUser.PhoneNumber,
            PasswordHash = string.Empty,
            Role = seedUser.Role
        };
        user.PasswordHash = passwordHasher.HashPassword(user, password);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);
        return user;
    }

    private static async Task<Service> GetOrCreateServiceAsync(
        ApplicationDbContext dbContext,
        string name,
        string description,
        decimal price,
        int durationMinutes,
        CancellationToken cancellationToken)
    {
        var service = await dbContext.Services
            .SingleOrDefaultAsync(item => item.Name == name, cancellationToken);
        if (service is not null)
        {
            return service;
        }

        service = new Service
        {
            Name = name,
            Description = description,
            Price = price,
            DurationMinutes = durationMinutes
        };
        dbContext.Services.Add(service);
        await dbContext.SaveChangesAsync(cancellationToken);
        return service;
    }

    private static async Task AssignServiceAsync(
        ApplicationDbContext dbContext,
        User barber,
        Service service,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.BarberServices.AnyAsync(
            item => item.BarberId == barber.Id && item.ServiceId == service.Id,
            cancellationToken);
        if (!exists)
        {
            dbContext.BarberServices.Add(new BarberService
            {
                BarberId = barber.Id,
                ServiceId = service.Id
            });
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private static async Task UpsertShiftAsync(
        ApplicationDbContext dbContext,
        User barber,
        DateOnly date,
        ShiftType shiftType,
        CancellationToken cancellationToken)
    {
        var shift = await dbContext.BarberShifts
            .SingleOrDefaultAsync(
                item => item.BarberId == barber.Id && item.Date == date,
                cancellationToken);
        if (shift is null)
        {
            dbContext.BarberShifts.Add(new BarberShift
            {
                BarberId = barber.Id,
                Date = date,
                ShiftType = shiftType
            });
        }
        else
        {
            shift.ShiftType = shiftType;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task AddAppointmentIfMissingAsync(
        ApplicationDbContext dbContext,
        User? client,
        User barber,
        Service service,
        DateTime localStart,
        AppointmentType appointmentType,
        CancellationToken cancellationToken)
    {
        var clientId = client?.Id;
        var appointmentStartUtc = TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(localStart, DateTimeKind.Unspecified));
        var exists = await dbContext.Appointments.AnyAsync(
            item =>
                item.BarberId == barber.Id &&
                item.ClientId == clientId &&
                item.ServiceId == service.Id &&
                item.AppointmentDateTime == appointmentStartUtc &&
                item.AppointmentType == appointmentType,
            cancellationToken);
        if (exists)
        {
            return;
        }

        dbContext.Appointments.Add(new Appointment
        {
            ClientId = client?.Id,
            BarberId = barber.Id,
            ServiceId = service.Id,
            AppointmentDateTime = appointmentStartUtc,
            Price = service.Price,
            AppointmentType = appointmentType,
            Status = AppointmentStatus.Active
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private sealed record SeedUser(
        string FirstName,
        string LastName,
        string Email,
        string PhoneNumber,
        UserRole Role);
}
