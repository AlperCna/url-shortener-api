using Microsoft.EntityFrameworkCore;
using UrlShortener.Core.Entities;

namespace UrlShortener.Infrastructure.Persistence;

public class UrlShortenerDbContext(DbContextOptions<UrlShortenerDbContext> options) : DbContext(options)
{
    public DbSet<ShortLink> ShortLinks => Set<ShortLink>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(UrlShortenerDbContext).Assembly);
    }
}
