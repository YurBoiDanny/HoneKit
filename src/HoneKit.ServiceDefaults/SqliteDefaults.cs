using Microsoft.AspNetCore.Builder;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Hosting;

// One SQLite file per data-owning service. The AppHost mounts a named volume at /data and sets
// DATA_DIR to that path when publishing; locally the file lives beside the service under app_data/
// (not data/, which would collide with each service's Data/ source folder on a case-insensitive disk).
public static class SqliteDefaults
{
    public static IHostApplicationBuilder AddServiceSqlite<TContext>(this IHostApplicationBuilder builder, string fileName)
        where TContext : DbContext
    {
        var configuredDirectory = builder.Configuration["DATA_DIR"];
        var directory = string.IsNullOrWhiteSpace(configuredDirectory)
            ? Path.Combine(builder.Environment.ContentRootPath, "app_data")
            : configuredDirectory;

        directory = Path.GetFullPath(directory);
        Directory.CreateDirectory(directory);

        var databasePath = Path.Combine(directory, fileName);
        var connectionString = new SqliteConnectionStringBuilder { DataSource = databasePath }.ToString();

        builder.Services.AddDbContext<TContext>(options => options.UseSqlite(connectionString));

        return builder;
    }

    // EnsureCreated + an idempotent seed. Each seed returns true when it inserted rows, so a wiped
    // volume reseeds on the next start and a normal restart leaves existing data alone.
    public static async Task EnsureCreatedAndSeedAsync<TContext>(
        this WebApplication app,
        Func<TContext, CancellationToken, Task<bool>> seed)
        where TContext : DbContext
    {
        await using var scope = app.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<TContext>();

        var created = await database.Database.EnsureCreatedAsync();
        var seeded = await seed(database, CancellationToken.None);

        if (created || seeded)
        {
            app.Logger.LogInformation("SQLite database {Database} is ready and seeded", typeof(TContext).Name);
        }
    }
}
