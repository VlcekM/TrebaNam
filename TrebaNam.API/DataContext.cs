using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TrebaNam.API.Auth;

namespace TrebaNam.API;

/// <summary>
/// Okrem vlastnych tabuliek drzi aj klucenku Data Protection - tou su sifrovane
/// prihlasovacie cookies. V kontajneri nie je kam ju zapisat na disk, takze bez
/// tohto by kazdy restart vygeneroval nove kluce a odhlasil vsetkych.
/// </summary>
public class DataContext(DbContextOptions<DataContext> options) : DbContext(options), IDataProtectionKeyContext
{
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; }

    public DbSet<UserEntity> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Prihlaseneho cloveka hladame podla Google sub pri kazdom requeste.
        modelBuilder.Entity<UserEntity>()
            .HasIndex(u => u.GoogleSub)
            .IsUnique();
    }
}
