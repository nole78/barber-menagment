using BarberMenagment.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BarberMenagment.Data.Configurations;

public class BarberServiceConfiguration : IEntityTypeConfiguration<BarberService>
{
    public void Configure(EntityTypeBuilder<BarberService> builder)
    {
        builder.ToTable("BarberServices");
        builder.HasKey(barberService => new { barberService.BarberId, barberService.ServiceId });

        builder.HasOne(barberService => barberService.Barber)
            .WithMany(user => user.BarberServices)
            .HasForeignKey(barberService => barberService.BarberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(barberService => barberService.Service)
            .WithMany(service => service.BarberServices)
            .HasForeignKey(barberService => barberService.ServiceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
