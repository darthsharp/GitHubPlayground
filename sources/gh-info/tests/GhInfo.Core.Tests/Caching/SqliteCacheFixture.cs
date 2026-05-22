using GhInfo.Core.Caching;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace GhInfo.Core.Tests.Caching;

/// <summary>
/// Creates an isolated <see cref="CacheDbContext"/> backed by a private in-memory SQLite database.
/// The underlying connection is kept open for the lifetime of the fixture so the schema persists.
/// </summary>
internal sealed class SqliteCacheFixture : IDisposable
{
    private readonly SqliteConnection _connection;

    public SqliteCacheFixture()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<CacheDbContext>()
            .UseSqlite(_connection)
            .Options;

        Context = new CacheDbContext(options);
        Context.Database.EnsureCreated();
    }

    /// <summary>Gets the database context bound to the in-memory database.</summary>
    public CacheDbContext Context { get; }

    /// <inheritdoc />
    public void Dispose()
    {
        Context.Dispose();
        _connection.Dispose();
    }
}
