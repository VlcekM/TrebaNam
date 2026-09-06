using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TrebaNam.API;

/// <summary>
/// Aby `dotnet ef` fungoval bez nabootovania appky. Connection string berie
/// z prveho argumentu za `--` alebo z env premennej.
/// </summary>
public class DataContextDesignFactory : IDesignTimeDbContextFactory<DataContext>
{
    public DataContext CreateDbContext(string[] args)
    {
        var connString = args.FirstOrDefault();

        if (string.IsNullOrWhiteSpace(connString))
            connString = Environment.GetEnvironmentVariable("TREBANAM_ConnectionStrings__DefaultConnection");

        var optionsBuilder = new DbContextOptionsBuilder<DataContext>()
            .UseNpgsql(connString)
            .UseSnakeCaseNamingConvention();

        return new DataContext(optionsBuilder.Options);
    }
}
