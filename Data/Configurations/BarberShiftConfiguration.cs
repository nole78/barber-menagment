using BarberMenagment.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BarberMenagment.Data.Configurations;

public class BarberShiftConfiguration : IEntityTypeConfiguration<BarberShift>
{
    public void Configure(EntityTypeBuilder<BarberShift> builder)
    {
        builder.ToTable("BarberShifts");
        builder.HasKey(shift => shift.Id);

        builder.Property(shift => shift.Date).IsRequired();
        builder.Property(shift => shift.ShiftType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasIndex(shift => new { shift.BarberId, shift.Date }).IsUnique();

        builder.HasOne(shift => shift.Barber)
            .WithMany(user => user.BarberShifts)
            .HasForeignKey(shift => shift.BarberId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
