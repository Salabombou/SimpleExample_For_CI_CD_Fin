using FluentAssertions;
using Moq;
using SimpleExample.Application.DTOs;
using SimpleExample.Application.Interfaces;
using SimpleExample.Application.Services;
using SimpleExample.Domain.Entities;
using Xunit;

namespace SimpleExample.Tests.Application;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _mockRepository;
    private readonly UserService _service;

    public UserServiceTests()
    {
        _mockRepository = new Mock<IUserRepository>();
        _service = new UserService(_mockRepository.Object);
    }

    [Fact]
    public async Task CreateAsync_WithValidData_ShouldCreateUser()
    {
        // Arrange
        CreateUserDto dto = new CreateUserDto
        {
            FirstName = "Matti",
            LastName = "Meikäläinen",
            Email = "matti@example.com"
        };

        // Mock: Email ei ole käytössä
        _mockRepository
            .Setup(x => x.GetByEmailAsync(dto.Email))
            .ReturnsAsync((User?)null);

        _mockRepository
            .Setup(x => x.AddAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) => u);

        // Act
        UserDto result = await _service.CreateAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.FirstName.Should().Be("Matti");
        result.LastName.Should().Be("Meikäläinen");
        result.Email.Should().Be("matti@example.com");

        // Varmista että AddAsync kutsuttiin kerran
        _mockRepository.Verify(x => x.AddAsync(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateEmail_ShouldThrowInvalidOperationException()
    {
        // Arrange
        CreateUserDto dto = new CreateUserDto
        {
            FirstName = "Matti",
            LastName = "Meikäläinen",
            Email = "existing@example.com"
        };

        User existingUser = new User("Maija", "Virtanen", "existing@example.com");

        // Mock: Email on jo käytössä!
        _mockRepository
            .Setup(x => x.GetByEmailAsync(dto.Email))
            .ReturnsAsync(existingUser);

        // Act
        Func<Task> act = async () => await _service.CreateAsync(dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*jo olemassa*");

        // Varmista että AddAsync EI kutsuttu
        _mockRepository.Verify(x => x.AddAsync(It.IsAny<User>()), Times.Never);
    }

    // TEHTÄVÄ: Kirjoita itse testit seuraaville:
    // 1. GetByIdAsync - löytyy
    // 2. GetByIdAsync - ei löydy
    // 3. GetAllAsync - palauttaa listan
    // 4. UpdateAsync - onnistuu
    // 5. UpdateAsync - käyttäjää ei löydy
    // 6. DeleteAsync - onnistuu
    // 7. DeleteAsync - käyttäjää ei löydy

    [Fact]
    public async Task GetByIdAsync_WithExistingUser_ShouldReturnUser()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        User existingUser = new User("Maija", "Virtanen", "maija@example.com");
        existingUser.Id = userId;

        _mockRepository
            .Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(existingUser);

        // Act
        UserDto? result = await _service.GetByIdAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(userId);
        result.FirstName.Should().Be("Maija");
        result.LastName.Should().Be("Virtanen");
        result.Email.Should().Be("maija@example.com");

        _mockRepository.Verify(x => x.GetByIdAsync(userId), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingUser_ShouldReturnNull()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        _mockRepository
            .Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync((User?)null);

        // Act
        UserDto? result = await _service.GetByIdAsync(userId);

        // Assert
        result.Should().BeNull();
        _mockRepository.Verify(x => x.GetByIdAsync(userId), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnListOfUsers()
    {
        // Arrange
        List<User> users = new List<User>();
        for (int i = 1; i <= 3; i++)
        {
            User user = new User($"First{i}", $"Last{i}", $"email{i}@example.com");
            users.Add(user);
        }

        _mockRepository
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(users);

        // Act
        IEnumerable<UserDto> result = await _service.GetAllAsync();

        // Assert
        result.Should().NotBeNull();
        UserDto[] userDtos = result.ToArray();
        userDtos.Should().HaveCount(3);
        userDtos.Select(x => x.FirstName).Should().ContainInOrder("First1", "First2", "First3");
        userDtos.Select(x => x.LastName).Should().ContainInOrder("Last1", "Last2", "Last3");
        userDtos.Select(x => x.Email).Should().ContainInOrder(
            "email1@example.com",
            "email2@example.com",
            "email3@example.com");
        _mockRepository.Verify(x => x.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithExistingUser_ShouldUpdateAndReturnUser()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        User existingUser = new User("Maija", "Virtanen", "maija@example.com");
        existingUser.Id = userId;
        User? updatedRepositoryUser = null;

        _mockRepository
            .Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(existingUser);
        _mockRepository
            .Setup(x => x.UpdateAsync(It.IsAny<User>()))
            .Callback<User>(user => updatedRepositoryUser = user)
            .ReturnsAsync((User u) => u);

        UpdateUserDto updateDto = new UpdateUserDto
        {
            FirstName = "MaijaUpdated",
            LastName = "VirtanenUpdated",
            Email = "maijaupdated@example.com"
        };

        // Act
        UserDto? result = await _service.UpdateAsync(userId, updateDto);

        // Assert
        result.Should().NotBeNull();
        result!.FirstName.Should().Be("MaijaUpdated");
        result.LastName.Should().Be("VirtanenUpdated");
        result.Email.Should().Be("maijaupdated@example.com");
        result.Id.Should().Be(userId);
        updatedRepositoryUser.Should().NotBeNull();
        updatedRepositoryUser!.Id.Should().Be(userId);
        updatedRepositoryUser.FirstName.Should().Be("MaijaUpdated");
        updatedRepositoryUser.LastName.Should().Be("VirtanenUpdated");
        updatedRepositoryUser.Email.Should().Be("maijaupdated@example.com");
        _mockRepository.Verify(x => x.GetByIdAsync(userId), Times.Once);
        _mockRepository.Verify(x => x.UpdateAsync(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistingUser_ShouldReturnNull()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        _mockRepository
            .Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync((User?)null);
        UpdateUserDto updateDto = new UpdateUserDto
        {
            FirstName = "MaijaUpdated",
            LastName = "VirtanenUpdated",
            Email = "maijaupdated@example.com"
        };

        // Act
        UserDto? result = await _service.UpdateAsync(userId, updateDto);

        // Assert
        result.Should().BeNull();
        _mockRepository.Verify(x => x.GetByIdAsync(userId), Times.Once);
        _mockRepository.Verify(x => x.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WithExistingUser_ShouldDeleteAndReturnTrue()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        _mockRepository
            .Setup(x => x.ExistsAsync(userId))
            .ReturnsAsync(true);

        // Act
        bool result = await _service.DeleteAsync(userId);

        // Assert
        result.Should().BeTrue();
        _mockRepository.Verify(x => x.ExistsAsync(userId), Times.Once);
        _mockRepository.Verify(x => x.DeleteAsync(userId), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistingUser_ShouldReturnFalse()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        _mockRepository
            .Setup(x => x.ExistsAsync(userId))
            .ReturnsAsync(false);

        // Act
        bool result = await _service.DeleteAsync(userId);

        // Assert
        result.Should().BeFalse();
        _mockRepository.Verify(x => x.ExistsAsync(userId), Times.Once);
        _mockRepository.Verify(x => x.DeleteAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithEmptyFirstName_ShouldThrowArgumentException()
    {
        // Arrange
        CreateUserDto dto = new CreateUserDto
        {
            FirstName = "",
            LastName = "Meikäläinen",
            Email = "valid@example.com"
        };

        // Act
        Func<Task> act = async () => await _service.CreateAsync(dto);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("firstName");
        _mockRepository.Verify(x => x.GetByEmailAsync(It.IsAny<string>()), Times.Once);
        _mockRepository.Verify(x => x.AddAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithInvalidEmail_ShouldThrowArgumentException()
    {
        // Arrange
        CreateUserDto dto = new CreateUserDto
        {
            FirstName = "Matti",
            LastName = "Meikäläinen",
            Email = "invalid-email"
        };

        // Act
        Func<Task> act = async () => await _service.CreateAsync(dto);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("email");
        _mockRepository.Verify(x => x.GetByEmailAsync(It.IsAny<string>()), Times.Once);
        _mockRepository.Verify(x => x.AddAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WithEmptyFirstName_ShouldThrowArgumentException()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        User existingUser = new User("Maija", "Virtanen", "maija@example.com");
        _mockRepository
            .Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(existingUser);
        UpdateUserDto updateDto = new UpdateUserDto
        {
            FirstName = "",
            LastName = "VirtanenUpdated",
            Email = "maijaupdated@example.com"
        };

        // Act
        Func<Task> act = async () => await _service.UpdateAsync(userId, updateDto);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("firstName");
        _mockRepository.Verify(x => x.GetByIdAsync(userId), Times.Once);
        _mockRepository.Verify(x => x.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WithInvalidEmail_ShouldThrowArgumentException()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        User existingUser = new User("Maija", "Virtanen", "maija@example.com");
        _mockRepository
            .Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(existingUser);
        UpdateUserDto updateDto = new UpdateUserDto
        {
            FirstName = "MaijaUpdated",
            LastName = "VirtanenUpdated",
            Email = "invalid-email"
        };

        // Act
        Func<Task> act = async () => await _service.UpdateAsync(userId, updateDto);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("email");
        _mockRepository.Verify(x => x.GetByIdAsync(userId), Times.Once);
        _mockRepository.Verify(x => x.UpdateAsync(It.IsAny<User>()), Times.Never);
    }
}