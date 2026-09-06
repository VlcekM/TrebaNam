using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TrebaNam.API.Auth;
using TrebaNam.API.Households;

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

    public DbSet<HouseholdEntity> Households { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Prihlaseneho cloveka hladame podla Google sub pri kazdom requeste.
        modelBuilder.Entity<UserEntity>()
            .HasIndex(u => u.GoogleSub)
            .IsUnique();

        // Pozvankovy kod je zaroven vyhladavaci kluc, unikat drzi aj kolizie generatora.
        modelBuilder.Entity<HouseholdEntity>()
            .HasIndex(h => h.InviteCode)
            .IsUnique();

        // Bez navigacnej vlastnosti - clenov citame dotazom, nie cez graf objektov.
        // Zmazanie domacnosti necha ludi bez nej, nie zmazanych.
        modelBuilder.Entity<UserEntity>()
            .HasOne<HouseholdEntity>()
            .WithMany()
            .HasForeignKey(u => u.HouseholdID)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
