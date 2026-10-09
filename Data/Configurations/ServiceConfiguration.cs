using BarberMenagment.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BarberMenagment.Data.Configurations;

public class ServiceConfiguration : IEntityTypeConfiguration<Service>
{
    public void Configure(EntityTypeBuilder<Service> builder)
    {
        builder.ToTable("Services");
        builder.HasKey(service => service.Id);

        builder.Property(service => service.Name).IsRequired().HasMaxLength(150);
        builder.Property(service => service.Description).IsRequired(false);
        builder.Property(service => service.Price)
            .IsRequired()
            .HasPrecision(10, 2);
        builder.Property(service => service.DurationMinutes).IsRequired();
    }
}
