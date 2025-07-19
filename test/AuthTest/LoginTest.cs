using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using src.Domain.Entities.UserEntity;
using src.Domain.Valueobject;
using src.Feature.User.Auth.Login;
using src.Infrastructure.Abstractions.IRepositories;
using src.Infrastructure.Abstractions.IServices;
using src.Infrastructure.Services;
using Xunit;

namespace test.AuthTest;

public class LoginTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IPasswordService> _mockPasswordService;
    private readonly Mock<ITokenService> _mockTokenService;
    private readonly Mock<ICookieService> _mockCookieService;
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly Mock<ILogger<LoginRequestHandler>> _mockLogger;
    private readonly LoginRequestHandler _handler;

    public LoginTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockPasswordService = new Mock<IPasswordService>();
        _mockTokenService = new Mock<ITokenService>();
        _mockCookieService = new Mock<ICookieService>();
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        _mockLogger = new Mock<ILogger<LoginRequestHandler>>();

        _handler = new LoginRequestHandler(
            _mockUserRepository.Object,
            _mockPasswordService.Object,
            _mockLogger.Object,
            _mockTokenService.Object,
            _mockCookieService.Object,
            _mockHttpContextAccessor.Object
        );
    }

    private void SetupHttpContext()
    {
        var mockHttpContext = new Mock<HttpContext>();
        var mockRequest = new Mock<HttpRequest>();
        var mockConnection = new Mock<ConnectionInfo>();
        mockRequest.Setup(r => r.Headers).Returns(new HeaderDictionary());
        mockConnection.Setup(c => c.RemoteIpAddress).Returns(System.Net.IPAddress.Parse("127.0.0.1"));
        mockHttpContext.Setup(c => c.Request).Returns(mockRequest.Object);
        mockHttpContext.Setup(c => c.Connection).Returns(mockConnection.Object);
        _mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(mockHttpContext.Object);
    }

    [Fact]
    public async Task Handle_WithValidCredentials_ShouldGenerateTokensAndReturnSuccess()
    {
        // Arrange
        SetupHttpContext();
        var request = new LoginRequest("test@example.com", "Password123!");
        var user = User.Create("Test User", request.email, "hashed_password");
        var tokens = new JwtTokens("access_token","refresh_token", DateTimeOffset.UtcNow.AddHours(1),  DateTimeOffset.UtcNow.AddDays(7));

        _mockUserRepository.Setup(r => r.GetUserByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mockPasswordService.Setup(s => s.VerifyPassword(request.password, user.PasswordHash)).Returns(true);
        _mockTokenService.Setup(s => s.GenerateTokensAsync(user, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tokens);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(tokens.ExpirationAccessToken, result.Value.accessTokenExpiresAt);
        _mockCookieService.Verify(s => s.SetAuthCookies(It.IsAny<HttpContext>(), tokens), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldReturnNotFoundError()
    {
        // Arrange
        SetupHttpContext();
        var request = new LoginRequest("nouser@example.com", "password");
        _mockUserRepository.Setup(r => r.GetUserByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User)null);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("User not found", result.Error.Title);
    }

    [Fact]
    public async Task Handle_WithInvalidPassword_ShouldReturnUnauthorizedError()
    {
        // Arrange
        SetupHttpContext();
        var request = new LoginRequest("test@example.com", "wrong_password");
        var user = User.Create("Test User", request.email, "hashed_password");

        _mockUserRepository.Setup(r => r.GetUserByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mockPasswordService.Setup(s => s.VerifyPassword(request.password, user.PasswordHash)).Returns(false);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Unauthorized", result.Error.Title);
    }
}
