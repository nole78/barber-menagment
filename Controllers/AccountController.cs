using System.Security.Claims;
using BarberMenagment.Constants;
using BarberMenagment.Data;
using BarberMenagment.Models;
using BarberMenagment.Models.Account;
using BarberMenagment.Models.Booking;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BarberMenagment.Controllers;

public class AccountController(
    ApplicationDbContext dbContext,
    IPasswordHasher<User> passwordHasher) : Controller
{
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var email = model.Email.Trim();
        var user = await dbContext.Users.SingleOrDefaultAsync(candidate => candidate.Email == email);
        if (user is null ||
            passwordHasher.VerifyHashedPassword(user, user.PasswordHash, model.Password) ==
            PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View(model);
        }

        await SignInAsync(user);

        if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return LocalRedirect(model.ReturnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Register()
    {
        return View(new RegisterViewModel());
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var email = model.Email.Trim();
        if (await dbContext.Users.AnyAsync(user => user.Email == email))
        {
            ModelState.AddModelError(nameof(model.Email), "An account with this email already exists.");
            return View(model);
        }

        var user = new User
        {
            FirstName = model.FirstName.Trim(),
            LastName = model.LastName.Trim(),
            Email = email,
            PhoneNumber = model.PhoneNumber.Trim(),
            PasswordHash = string.Empty,
            Role = UserRole.Client
        };
        user.PasswordHash = passwordHasher.HashPassword(user, model.Password);

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        TempData["SuccessMessage"] = "Registration successful. You can now log in.";
        return RedirectToAction(nameof(Login));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        HttpContext.Session.Clear();
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    [Authorize(Roles = nameof(UserRole.Client))]
    [HttpGet]
    public async Task<IActionResult> Appointments()
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var clientId))
        {
            return Challenge();
        }

        var appointments = await dbContext.Appointments
            .AsNoTracking()
            .Where(appointment =>
                appointment.ClientId == clientId &&
                appointment.Status == AppointmentStatus.Active &&
                appointment.AppointmentDateTime >= DateTime.UtcNow)
            .OrderBy(appointment => appointment.AppointmentDateTime)
            .Select(appointment => new ClientAppointmentViewModel
            {
                Id = appointment.Id,
                AppointmentDateTime = appointment.AppointmentDateTime,
                BarberName = appointment.Barber.FirstName + " " + appointment.Barber.LastName,
                ServiceName = appointment.Service.Name,
                Price = appointment.Price
            })
            .ToListAsync();

        return View(appointments);
    }

    [Authorize(Roles = nameof(UserRole.Client))]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelAppointment(int id)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var clientId))
        {
            return Challenge();
        }

        var appointment = await dbContext.Appointments.SingleOrDefaultAsync(item =>
            item.Id == id &&
            item.ClientId == clientId &&
            item.Status == AppointmentStatus.Active);
        if (appointment is null)
        {
            return NotFound();
        }

        if (appointment.AppointmentDateTime <= DateTime.UtcNow)
        {
            TempData["AppointmentError"] = "Termin može biti otkazan samo pre početka.";
            return RedirectToAction(nameof(Appointments));
        }

        appointment.Status = AppointmentStatus.Cancelled;
        appointment.CancellationReason = "Otkazao klijent";
        await dbContext.SaveChangesAsync();
        TempData["SuccessMessage"] = "Termin je otkazan.";
        return RedirectToAction(nameof(Appointments));
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    private async Task SignInAsync(User user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };
        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));

        HttpContext.Session.SetInt32(SessionKeys.UserId, user.Id);
        HttpContext.Session.SetString(SessionKeys.UserRole, user.Role.ToString());
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal);
    }
}
