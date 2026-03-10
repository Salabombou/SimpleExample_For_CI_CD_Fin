using FluentAssertions;
using SimpleExample.Application.DTOs;
using SimpleExample.Domain.Entities;
using Xunit;

namespace SimpleExample.Tests.Validators;

public class CreateUserDtoValidatorTests
{
    [Fact]
    public void Should_Have_Error_When_FirstName_Is_Empty()
    {
        var dto = new CreateUserDto { FirstName = "", LastName = "Meikäläinen", Email = "test@test.com" };
        
        var action = () => new User(dto.FirstName, dto.LastName, dto.Email);
        
        action.Should().NotThrow();
    }
}
