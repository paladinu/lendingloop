using Api.Controllers;
using Api.DTOs;
using Api.Models;
using Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Xunit;

namespace Api.Tests;

public class AuthControllerTests
{
    private readonly Mock<IUserService> _mockUserService;
    private readonly Mock<IPasswordService> _mockPasswordService;
    private readonly Mock<IJwtTokenService> _mockJwtTokenService;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<ILoopService> _mockLoopService;
    private readonly Mock<ILogger<AuthController>> _mockLogger;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _mockUserService = new Mock<IUserService>();
        _mockPasswordService = new Mock<IPasswordService>();
        _mockJwtTokenService = new Mock<IJwtTokenService>();
        _mockEmailService = new Mock<IEmailService>();
        _mockLoopService = new Mock<ILoopService>();
        _mockLogger = new Mock<ILogger<AuthController>>();

        _controller = new AuthController(
            _mockUserService.Object,
            _mockPasswordService.Object,
            _mockJwtTokenService.Object,
            _mockEmailService.Object,
            _mockLoopService.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task Register_ReturnsOk_WhenRegistrationIsSuccessful()
    {
        //arrange
        var request = new RegisterRequest
        {
            Email = "newuser@example.com",
            Password = "ValidPass123!",
            FirstName = "John",
            LastName = "Doe",
            StreetAddress = "123 Main St"
        };

        _mockPasswordService.Setup(s => s.ValidatePassword(It.IsAny<string>()))
            .Returns(new List<System.ComponentModel.DataAnnotations.ValidationResult>());
        _mockUserService.Setup(s => s.GetUserByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((User)null!);
        _mockUserService.Setup(s => s.CreateUserAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) => { u.Id = "user123"; return u; });
        _mockEmailService.Setup(s => s.SendVerificationEmailAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        //act
        var result = await _controller.Register(request);

        //assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<RegisterResponse>(okResult.Value);
        Assert.True(response.Success);
        Assert.NotNull(response.User);
        Assert.Equal(request.Email, response.User.Email);
    }

    [Fact]
    public async Task Register_ReturnsConflict_WhenUserAlreadyExists()
    {
        //arrange
        var request = new RegisterRequest
        {
            Email = "existing@example.com",
            Password = "ValidPass123!",
            FirstName = "John",
            LastName = "Doe",
            StreetAddress = "123 Main St"
        };

        _mockPasswordService.Setup(s => s.ValidatePassword(It.IsAny<string>()))
            .Returns(new List<System.ComponentModel.DataAnnotations.ValidationResult>());
        _mockUserService.Setup(s => s.GetUserByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(new User { Email = request.Email });

        //act
        var result = await _controller.Register(request);

        //assert
        Assert.IsType<ConflictObjectResult>(result.Result);
    }

    [Fact]
    public async Task Register_ReturnsBadRequest_WhenPasswordIsInvalid()
    {
        //arrange
        var request = new RegisterRequest
        {
            Email = "newuser@example.com",
            Password = "weak",
            FirstName = "John",
            LastName = "Doe",
            StreetAddress = "123 Main St"
        };

        _mockPasswordService.Setup(s => s.ValidatePassword(It.IsAny<string>()))
            .Returns(new List<System.ComponentModel.DataAnnotations.ValidationResult>
            {
                new System.ComponentModel.DataAnnotations.ValidationResult("Password must be at least 8 characters long")
            });

        //act
        var result = await _controller.Register(request);

        //assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Login_ReturnsOk_WhenCredentialsAreValid()
    {
        //arrange
        var request = new LoginRequest
        {
            Email = "user@example.com",
            Password = "ValidPass123!"
        };

        var user = new User
        {
            Id = "user123",
            Email = request.Email,
            PasswordHash = "hashedpassword",
            FirstName = "John",
            LastName = "Doe",
            IsEmailVerified = true
        };

        _mockUserService.Setup(s => s.GetUserByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(user);
        _mockPasswordService.Setup(s => s.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);
        _mockJwtTokenService.Setup(s => s.GenerateToken(It.IsAny<User>()))
            .Returns("jwt-token");
        _mockJwtTokenService.Setup(s => s.GetTokenExpirationHours())
            .Returns(1);
        _mockUserService.Setup(s => s.UpdateUserAsync(It.IsAny<string>(), It.IsAny<User>()))
            .ReturnsAsync(user);

        //act
        var result = await _controller.Login(request);

        //assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<AuthResponse>(okResult.Value);
        Assert.NotNull(response.Token);
        Assert.NotNull(response.User);
        Assert.Equal(request.Email, response.User.Email);
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_WhenUserNotFound()
    {
        //arrange
        var request = new LoginRequest
        {
            Email = "nonexistent@example.com",
            Password = "ValidPass123!"
        };

        _mockUserService.Setup(s => s.GetUserByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((User)null!);

        //act
        var result = await _controller.Login(request);

        //assert
        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_WhenPasswordIsInvalid()
    {
        //arrange
        var request = new LoginRequest
        {
            Email = "user@example.com",
            Password = "WrongPassword123!"
        };

        var user = new User
        {
            Id = "user123",
            Email = request.Email,
            PasswordHash = "hashedpassword",
            IsEmailVerified = true
        };

        _mockUserService.Setup(s => s.GetUserByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(user);
        _mockPasswordService.Setup(s => s.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(false);

        //act
        var result = await _controller.Login(request);

        //assert
        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }

    [Fact]
    public async Task Login_ReturnsForbidden_WhenEmailNotVerified()
    {
        //arrange
        var request = new LoginRequest
        {
            Email = "user@example.com",
            Password = "ValidPass123!"
        };

        var user = new User
        {
            Id = "user123",
            Email = request.Email,
            PasswordHash = "hashedpassword",
            IsEmailVerified = false
        };

        _mockUserService.Setup(s => s.GetUserByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(user);
        _mockPasswordService.Setup(s => s.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);

        //act
        var result = await _controller.Login(request);

        //assert
        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(403, statusResult.StatusCode);
    }

    [Fact]
    public async Task VerifyEmail_ReturnsOk_WhenTokenIsValid()
    {
        //arrange
        var request = new VerifyEmailRequest { Token = "valid-token" };
        var user = new User
        {
            Id = "user123",
            Email = "user@example.com",
            FirstName = "John",
            LastName = "Doe",
            IsEmailVerified = true
        };

        _mockUserService.Setup(s => s.VerifyEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(user);

        //act
        var result = await _controller.VerifyEmail(request);

        //assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<VerificationResponse>(okResult.Value);
        Assert.True(response.Success);
        Assert.NotNull(response.User);
        Assert.True(response.User.IsEmailVerified);
    }

    [Fact]
    public async Task VerifyEmail_ReturnsBadRequest_WhenTokenIsInvalid()
    {
        //arrange
        var request = new VerifyEmailRequest { Token = "invalid-token" };

        _mockUserService.Setup(s => s.VerifyEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((User)null!);

        //act
        var result = await _controller.VerifyEmail(request);

        //assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var response = Assert.IsType<VerificationResponse>(badRequestResult.Value);
        Assert.False(response.Success);
    }

    [Fact]
    public void Logout_ReturnsOk_WhenUserIsAuthenticated()
    {
        //arrange
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Email, "user@example.com"),
            new Claim("userId", "user123")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        //act
        var result = _controller.Logout();

        //assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetCurrentUser_ReturnsOk_WhenUserExists()
    {
        //arrange
        var userId = "user123";
        var user = new User
        {
            Id = userId,
            Email = "user@example.com",
            FirstName = "John",
            LastName = "Doe",
            StreetAddress = "123 Main St",
            IsEmailVerified = true
        };

        var claims = new List<Claim>
        {
            new Claim("userId", userId),
            new Claim(ClaimTypes.Email, user.Email)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        _mockUserService.Setup(s => s.GetUserByIdAsync(userId))
            .ReturnsAsync(user);

        //act
        var result = await _controller.GetCurrentUser();

        //assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var userProfile = Assert.IsType<UserProfile>(okResult.Value);
        Assert.Equal(user.Email, userProfile.Email);
        Assert.Equal(user.FirstName, userProfile.FirstName);
    }

    [Fact]
    public async Task GetCurrentUser_ReturnsUnauthorized_WhenUserIdClaimMissing()
    {
        //arrange
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Email, "user@example.com")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        //act
        var result = await _controller.GetCurrentUser();

        //assert
        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetCurrentUser_ReturnsNotFound_WhenUserDoesNotExist()
    {
        //arrange
        var userId = "nonexistent";
        var claims = new List<Claim>
        {
            new Claim("userId", userId),
            new Claim(ClaimTypes.Email, "user@example.com")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        _mockUserService.Setup(s => s.GetUserByIdAsync(userId))
            .ReturnsAsync((User)null!);

        //act
        var result = await _controller.GetCurrentUser();

        //assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }
}
