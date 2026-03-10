using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SimpleExample.Domain.Entities;
using SimpleExample.Infrastructure.Data;
using SimpleExample.Infrastructure.Repositories;

namespace SimpleExample.Tests.Infrastructure;

public class UserRepositoryIntegrationTests
{
    [Fact]
    public async Task AddAsync_ShouldPersistUserAndSetIdentityAndTimestamps()
    {
        // Arrange
        string databaseName = Guid.NewGuid().ToString();
        User user = new User("Matti", "Meikalainen", "matti@example.com");

        await using ApplicationDbContext arrangeContext = CreateContext(databaseName);
        UserRepository repository = new UserRepository(arrangeContext);

        // Act
        User result = await repository.AddAsync(user);

        // Assert
        result.Id.Should().NotBe(Guid.Empty);
        result.CreatedAt.Should().NotBe(default);
        result.UpdatedAt.Should().NotBe(default);
        result.UpdatedAt.Should().BeOnOrAfter(result.CreatedAt);

        await using ApplicationDbContext assertContext = CreateContext(databaseName);
        User? savedUser = await assertContext.Users.SingleOrDefaultAsync(x => x.Id == result.Id);

        savedUser.Should().NotBeNull();
        savedUser!.FirstName.Should().Be("Matti");
        savedUser.LastName.Should().Be("Meikalainen");
        savedUser.Email.Should().Be("matti@example.com");
    }

    [Fact]
    public async Task GetByIdAsync_WhenUserExists_ShouldReturnUser()
    {
        // Arrange
        string databaseName = Guid.NewGuid().ToString();
        User seededUser = await SeedUserAsync(databaseName, "Matti", "Meikalainen", "matti@example.com");

        await using ApplicationDbContext context = CreateContext(databaseName);
        UserRepository repository = new UserRepository(context);

        // Act
        User? result = await repository.GetByIdAsync(seededUser.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(seededUser.Id);
        result.Email.Should().Be("matti@example.com");
    }

    [Fact]
    public async Task GetByIdAsync_WhenUserDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        string databaseName = Guid.NewGuid().ToString();

        await using ApplicationDbContext context = CreateContext(databaseName);
        UserRepository repository = new UserRepository(context);

        // Act
        User? result = await repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllPersistedUsers()
    {
        // Arrange
        string databaseName = Guid.NewGuid().ToString();
        await SeedUserAsync(databaseName, "Matti", "Meikalainen", "matti@example.com");
        await SeedUserAsync(databaseName, "Maija", "Virtanen", "maija@example.com");

        await using ApplicationDbContext context = CreateContext(databaseName);
        UserRepository repository = new UserRepository(context);

        // Act
        IEnumerable<User> result = await repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Select(x => x.Email).Should().Contain(new[] { "matti@example.com", "maija@example.com" });
    }

    [Fact]
    public async Task GetByEmailAsync_WhenUserExists_ShouldReturnMatchingUser()
    {
        // Arrange
        string databaseName = Guid.NewGuid().ToString();
        await SeedUserAsync(databaseName, "Matti", "Meikalainen", "matti@example.com");

        await using ApplicationDbContext context = CreateContext(databaseName);
        UserRepository repository = new UserRepository(context);

        // Act
        User? result = await repository.GetByEmailAsync("matti@example.com");

        // Assert
        result.Should().NotBeNull();
        result!.FirstName.Should().Be("Matti");
    }

    [Fact]
    public async Task GetByEmailAsync_WhenUserDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        string databaseName = Guid.NewGuid().ToString();
        await SeedUserAsync(databaseName, "Matti", "Meikalainen", "matti@example.com");

        await using ApplicationDbContext context = CreateContext(databaseName);
        UserRepository repository = new UserRepository(context);

        // Act
        User? result = await repository.GetByEmailAsync("missing@example.com");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_ShouldPersistModifiedValuesAndRefreshUpdatedAt()
    {
        // Arrange
        string databaseName = Guid.NewGuid().ToString();
        User seededUser = await SeedUserAsync(databaseName, "Matti", "Meikalainen", "matti@example.com");

        await using ApplicationDbContext actContext = CreateContext(databaseName);
        UserRepository repository = new UserRepository(actContext);
        User userToUpdate = (await repository.GetByIdAsync(seededUser.Id))!;
        DateTime originalUpdatedAt = userToUpdate.UpdatedAt;

        userToUpdate.UpdateBasicInfo("Maija", "Virtanen");
        userToUpdate.UpdateEmail("maija@example.com");

        // Act
        User result = await repository.UpdateAsync(userToUpdate);

        // Assert
        result.UpdatedAt.Should().BeAfter(originalUpdatedAt);

        await using ApplicationDbContext assertContext = CreateContext(databaseName);
        User? updatedUser = await assertContext.Users.SingleOrDefaultAsync(x => x.Id == seededUser.Id);

        updatedUser.Should().NotBeNull();
        updatedUser!.FirstName.Should().Be("Maija");
        updatedUser.LastName.Should().Be("Virtanen");
        updatedUser.Email.Should().Be("maija@example.com");
        updatedUser.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public async Task DeleteAsync_WhenUserExists_ShouldRemoveUser()
    {
        // Arrange
        string databaseName = Guid.NewGuid().ToString();
        User seededUser = await SeedUserAsync(databaseName, "Matti", "Meikalainen", "matti@example.com");

        await using ApplicationDbContext context = CreateContext(databaseName);
        UserRepository repository = new UserRepository(context);

        // Act
        await repository.DeleteAsync(seededUser.Id);

        // Assert
        await using ApplicationDbContext assertContext = CreateContext(databaseName);
        User? deletedUser = await assertContext.Users.SingleOrDefaultAsync(x => x.Id == seededUser.Id);
        deletedUser.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_WhenUserDoesNotExist_ShouldLeaveExistingDataUnchanged()
    {
        // Arrange
        string databaseName = Guid.NewGuid().ToString();
        User seededUser = await SeedUserAsync(databaseName, "Matti", "Meikalainen", "matti@example.com");

        await using ApplicationDbContext context = CreateContext(databaseName);
        UserRepository repository = new UserRepository(context);

        // Act
        await repository.DeleteAsync(Guid.NewGuid());

        // Assert
        await using ApplicationDbContext assertContext = CreateContext(databaseName);
        (await assertContext.Users.CountAsync()).Should().Be(1);
        (await assertContext.Users.AnyAsync(x => x.Id == seededUser.Id)).Should().BeTrue();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ExistsAsync_ShouldReturnExpectedResult(bool userExists)
    {
        // Arrange
        string databaseName = Guid.NewGuid().ToString();
        Guid userId = userExists
            ? (await SeedUserAsync(databaseName, "Matti", "Meikalainen", "matti@example.com")).Id
            : Guid.NewGuid();

        await using ApplicationDbContext context = CreateContext(databaseName);
        UserRepository repository = new UserRepository(context);

        // Act
        bool result = await repository.ExistsAsync(userId);

        // Assert
        result.Should().Be(userExists);
    }

    private static ApplicationDbContext CreateContext(string databaseName)
    {
        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        return new ApplicationDbContext(options);
    }

    private static async Task<User> SeedUserAsync(
        string databaseName,
        string firstName,
        string lastName,
        string email)
    {
        await using ApplicationDbContext context = CreateContext(databaseName);
        UserRepository repository = new UserRepository(context);

        return await repository.AddAsync(new User(firstName, lastName, email));
    }
}