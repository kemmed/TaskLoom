using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.InMemory;
using taskloom.Data;
using taskloom.Models;
using Xunit;

[CollectionDefinition("DatabaseCollection")]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture> { }

public class DatabaseFixture : IDisposable
{
    public taskloomContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<taskloomContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var context = new taskloomContext(options);
        SeedData(context);
        return context;
    }

    public void Dispose()
    {
    }

    private void SeedData(taskloomContext context)
    {
        var users = new List<User>
        {
            new User
            {
                ID = 1,
                Email = "test@example.com",
                HashPass = "hashed_password",
                IsActive = true,
                FName = "Иван",
                LName = "Иванов"
            },
            new User
            {
                ID = 2,
                Email = "inactive@example.com",
                HashPass = "hashed_password",
                IsActive = false,
                FName = "Петр",
                LName = "Петров"
            }
        };
        context.User.AddRange(users);
        context.SaveChanges();
    }
}