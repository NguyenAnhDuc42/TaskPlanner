using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using src.Domain.Entities.UserEntity;
using src.Domain.Valueobject;
using src.Feature.User.Auth.Register;
using src.Infrastructure.Abstractions.IRepositories;
using src.Infrastructure.Abstractions.IServices;
using src.Infrastructure.Data;
using Xunit;

namespace test.AuthTest;

public class RegisterTests
{
    private readonly Mock<PlannerDbContext> _mockContext;
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IPasswordService> _mockPasswordService;
    private readonly Mock<DbSet<User>> _mockUserDbSet;
    private readonly RegisterCommandHandler _handler;

    public RegisterTests()
    {
        var options = new DbContextOptionsBuilder<PlannerDbContext>().Options;
        _mockContext = new Mock<PlannerDbContext>(options);
        _mockUserRepository = new Mock<IUserRepository>();
        _mockPasswordService = new Mock<IPasswordService>();
        _mockUserDbSet = new Mock<DbSet<User>>();

        _mockContext.Setup(c => c.Users).Returns(_mockUserDbSet.Object);

        _handler = new RegisterCommandHandler(_mockContext.Object, _mockUserRepository.Object, _mockPasswordService.Object);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldCreateUserAndReturnSuccess()
    {
        // Arrange
        var command = new RegisterCommand("testuser", "test@example.com", "Password123!");
        _mockUserRepository.Setup(r => r.IsEmailExistsAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockPasswordService.Setup(s => s.HashPassword(command.password)).Returns("hashed_password");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(command.email, result.Value.email);
        _mockUserDbSet.Verify(dbSet => dbSet.Add(It.Is<User>(u => u.Email == command.email)), Times.Once);
        _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenEmailAlreadyExists_ShouldReturnConflictError()
    {
        // Arrange
        var command = new RegisterCommand("testuser", "existing@example.com", "Password123!");
        _mockUserRepository.Setup(r => r.IsEmailExistsAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Email already exists", result.Error.Title);
        _mockUserDbSet.Verify(dbSet => dbSet.Add(It.IsAny<User>()), Times.Never);
        _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
