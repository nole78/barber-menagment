using BarberMenagment.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BarberMenagment.Data.Configurations;

public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.ToTable("Appointments");
        builder.HasKey(appointment => appointment.Id);

        builder.Property(appointment => appointment.AppointmentDateTime)
            .IsRequired()
            .HasColumnType("timestamp with time zone");
        builder.Property(appointment => appointment.Price)
            .IsRequired()
            .HasPrecision(10, 2);
        builder.Property(appointment => appointment.AppointmentType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);
        builder.Property(appointment => appointment.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);
        builder.Property(appointment => appointment.CancellationReason)
            .IsRequired(false)
            .HasMaxLength(1000);

        builder.HasOne(appointment => appointment.Client)
            .WithMany(user => user.ClientAppointments)
            .HasForeignKey(appointment => appointment.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(appointment => appointment.Barber)
            .WithMany(user => user.BarberAppointments)
            .HasForeignKey(appointment => appointment.BarberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(appointment => appointment.Service)
            .WithMany(service => service.Appointments)
            .HasForeignKey(appointment => appointment.ServiceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
