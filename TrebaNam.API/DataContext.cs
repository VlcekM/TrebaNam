using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TrebaNam.API.Auth;
using TrebaNam.API.Households;
using TrebaNam.API.Items;
using TrebaNam.API.ShoppingLists;
using TrebaNam.API.ShoppingRecords;

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

    public DbSet<HouseholdCategoryEntity> HouseholdCategories { get; set; }

    public DbSet<ShoppingListEntity> ShoppingLists { get; set; }

    public DbSet<ItemEntity> Items { get; set; }

    public DbSet<ItemFavouriteEntity> ItemFavourites { get; set; }

    public DbSet<ShoppingRecordEntity> ShoppingRecords { get; set; }

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

        // Kod je identita skupiny v ramci domacnosti - podla neho na nu ukazuju polozky.
        modelBuilder.Entity<HouseholdCategoryEntity>()
            .HasIndex(c => new { c.HouseholdID, c.Code })
            .IsUnique();

        modelBuilder.Entity<HouseholdCategoryEntity>()
            .HasOne<HouseholdEntity>()
            .WithMany()
            .HasForeignKey(c => c.HouseholdID)
            .OnDelete(DeleteBehavior.Cascade);

        // Prepinac zoznamov sa cita cely pre jednu domacnost.
        modelBuilder.Entity<ShoppingListEntity>()
            .HasIndex(l => l.HouseholdID);

        modelBuilder.Entity<ShoppingListEntity>()
            .HasOne<HouseholdEntity>()
            .WithMany()
            .HasForeignKey(l => l.HouseholdID)
            .OnDelete(DeleteBehavior.Cascade);

        // Zoznam sa vzdy cita cely pre jednu domacnost.
        modelBuilder.Entity<ItemEntity>()
            .HasIndex(i => i.HouseholdID);

        modelBuilder.Entity<ItemEntity>()
            .HasIndex(i => i.ListID);

        // Polozka bez zoznamu nema kde stat, takze so zmazanym zoznamom odchadza aj ona.
        modelBuilder.Entity<ItemEntity>()
            .HasOne<ShoppingListEntity>()
            .WithMany()
            .HasForeignKey(i => i.ListID)
            .OnDelete(DeleteBehavior.Cascade);

        // So zrusenou domacnostou nema jej zoznam co robit.
        modelBuilder.Entity<ItemEntity>()
            .HasOne<HouseholdEntity>()
            .WithMany()
            .HasForeignKey(i => i.HouseholdID)
            .OnDelete(DeleteBehavior.Cascade);

        // Hviezdicka plati pre domacnost a pre jednu vec v nej prave raz.
        modelBuilder.Entity<ItemFavouriteEntity>()
            .HasIndex(f => new { f.HouseholdID, f.NameKey })
            .IsUnique();

        modelBuilder.Entity<ItemFavouriteEntity>()
            .HasOne<HouseholdEntity>()
            .WithMany()
            .HasForeignKey(f => f.HouseholdID)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ShoppingRecordEntity>()
            .HasIndex(r => r.HouseholdID);

        modelBuilder.Entity<ShoppingRecordEntity>()
            .HasOne<HouseholdEntity>()
            .WithMany()
            .HasForeignKey(r => r.HouseholdID)
            .OnDelete(DeleteBehavior.Cascade);

        // Bez toho by tabulka dostala nazov podla triedy, teda shopping_record_entity.
        modelBuilder.Entity<ShoppingRecordItemEntity>()
            .ToTable("shopping_record_items");

        // Riadky nakupu nemaju zivot mimo svojho zaznamu, tak sa citaju aj mazu s nim.
        modelBuilder.Entity<ShoppingRecordEntity>()
            .HasMany(r => r.Items)
            .WithOne()
            .HasForeignKey(i => i.ShoppingRecordID)
            .OnDelete(DeleteBehavior.Cascade);

        // Bez navigacnej vlastnosti - clenov citame dotazom, nie cez graf objektov.
        // Zmazanie domacnosti necha ludi bez nej, nie zmazanych.
        modelBuilder.Entity<UserEntity>()
            .HasOne<HouseholdEntity>()
            .WithMany()
            .HasForeignKey(u => u.HouseholdID)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
