using System;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using VirtoCommerce.OrdersModule.Data.Model;
using VirtoCommerce.OrdersModule.Data.Repositories;

namespace VirtoCommerce.XFrontend.Tests.Infrastructure;

/// <summary>
/// The real Orders <see cref="OrderDbContext"/> and <see cref="OrderRepository"/> over an in-memory SQLite database,
/// so the statistics queries run through real EF translation. The schema comes from the EF model
/// (<c>EnsureCreated</c>), not from the migrations. The connection keeps the database alive; dispose to drop it.
/// </summary>
public sealed class SqliteOrderDatabase : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<OrderDbContext> _options;

    public SqliteOrderDatabase()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<OrderDbContext>().UseSqlite(_connection).Options;

        using var dbContext = new OrderDbContext(_options);
        dbContext.Database.EnsureCreated();
    }

    public IOrderRepository CreateRepository()
    {
        return new OrderRepository(new OrderDbContext(_options));
    }

    /// <summary>Adds an order row directly (no Orders services, so no cache token is expired) and returns its id.</summary>
    public string SeedOrder(
        string customerId,
        decimal total,
        DateTime createdDate,
        string currency = "USD",
        string organizationId = "org-1",
        string storeId = "B2B-store",
        string status = "New",
        bool isCancelled = false,
        bool isPrototype = false)
    {
        var id = Guid.NewGuid().ToString("N");

        using var dbContext = new OrderDbContext(_options);
        dbContext.Add(new CustomerOrderEntity
        {
            Id = id,
            Number = id,
            CustomerId = customerId,
            CustomerName = customerId,
            OrganizationId = organizationId,
            StoreId = storeId,
            Status = status,
            Currency = currency,
            Total = total,
            IsCancelled = isCancelled,
            IsPrototype = isPrototype,
            CreatedDate = createdDate,
            ModifiedDate = createdDate,
        });
        dbContext.SaveChanges();

        return id;
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}
