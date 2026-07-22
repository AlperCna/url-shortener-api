using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UrlShortener.Core.Entities;
using UrlShortener.Core.Codes;

namespace UrlShortener.Infrastructure.Persistence.Configurations;

public class ShortLinkConfiguration : IEntityTypeConfiguration<ShortLink>
{
    // Matches the maximum URL length enforced by validation (see url-kisaltici-plan.md).
    private const int MaxOriginalUrlLength = 2048;

    public void Configure(EntityTypeBuilder<ShortLink> builder)
    {
        builder.ToTable("ShortLinks");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Code)
            .HasMaxLength(Base62CodeGenerator.DefaultLength)
            .IsRequired();

        builder.HasIndex(l => l.Code)
            .IsUnique();

        builder.Property(l => l.OriginalUrl)
            .HasMaxLength(MaxOriginalUrlLength)
            .IsRequired();

        builder.Property(l => l.CreatedAt)
            .IsRequired();

        builder.Property(l => l.IsOneTime)
            .IsRequired();

        builder.Property(l => l.IsActive)
            .IsRequired();

        builder.Property(l => l.ClickCount)
            .IsRequired();
    }
}
