using Api.Models;
using Api.Services;
using Microsoft.Extensions.Configuration;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using Xunit;

namespace Api.Tests;

public class JwtTokenServiceTests
{
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly JwtTokenService _service;
    private readonly string _testSecretKey = "ThisIsAVeryLongSecretKeyForTestingPurposesOnly123456789";

    public JwtTokenServiceTests()
    {
        _mockConfiguration = new Mock<IConfiguration>();
        _mockConfiguration.Setup(c => c["Jwt:SecretKey"]).Returns(_testSecretKey);
        _mockConfiguration.Setup(c => c["Jwt:Issuer"]).Returns("TestIssuer");
        _mockConfiguration.Setup(c => c["Jwt:Audience"]).Returns("TestAudience");
        _mockConfiguration.Setup(c => c["Jwt:ExpirationHours"]).Returns("1");

        _service = new JwtTokenService(_mockConfiguration.Object);
    }

    [Fact]
    public void GenerateToken_ReturnsNonEmptyToken_WhenUserProvided()
    {
        //arrange
        var user = new User
        {
            Id = "user123",
            Email = "test@example.com",
            FirstName = "John",
            LastName = "Doe"
        };

        //act
        var token = _service.GenerateToken(user);

        //assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);
    }

    [Fact]
    public void GenerateToken_ReturnsValidJwtFormat_WhenUserProvided()
    {
        //arrange
        var user = new User
        {
            Id = "user123",
            Email = "test@example.com",
            FirstName = "John",
            LastName = "Doe"
        };

        //act
        var token = _service.GenerateToken(user);

        //assert
        var parts = token.Split('.');
        Assert.Equal(3, parts.Length); // JWT has 3 parts: header.payload.signature
    }

    [Fact]
    public void GenerateToken_IncludesUserId_InToken()
    {
        //arrange
        var user = new User
        {
            Id = "user123",
            Email = "test@example.com",
            FirstName = "John",
            LastName = "Doe"
        };

        //act
        var token = _service.GenerateToken(user);
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        //assert
        var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "userId");
        Assert.NotNull(userIdClaim);
        Assert.Equal("user123", userIdClaim.Value);
    }

    [Fact]
    public void GenerateToken_IncludesEmail_InToken()
    {
        //arrange
        var user = new User
        {
            Id = "user123",
            Email = "test@example.com",
            FirstName = "John",
            LastName = "Doe"
        };

        //act
        var token = _service.GenerateToken(user);
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        //assert
        var emailClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Email);
        Assert.NotNull(emailClaim);
        Assert.Equal("test@example.com", emailClaim.Value);
    }

    [Fact]
    public void GenerateToken_IncludesName_InToken()
    {
        //arrange
        var user = new User
        {
            Id = "user123",
            Email = "test@example.com",
            FirstName = "John",
            LastName = "Doe"
        };

        //act
        var token = _service.GenerateToken(user);
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        //assert
        var firstNameClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.GivenName);
        var lastNameClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Surname);
        Assert.NotNull(firstNameClaim);
        Assert.NotNull(lastNameClaim);
        Assert.Equal("John", firstNameClaim.Value);
        Assert.Equal("Doe", lastNameClaim.Value);
    }

    [Fact]
    public void GenerateToken_SetsExpirationTime_BasedOnConfiguration()
    {
        //arrange
        var user = new User
        {
            Id = "user123",
            Email = "test@example.com",
            FirstName = "John",
            LastName = "Doe"
        };
        var beforeGeneration = DateTime.UtcNow;

        //act
        var token = _service.GenerateToken(user);
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        //assert
        Assert.True(jwtToken.ValidTo > beforeGeneration.AddMinutes(59));
        Assert.True(jwtToken.ValidTo <= beforeGeneration.AddHours(1).AddMinutes(1));
    }

    [Fact]
    public void GenerateToken_ReturnsDifferentTokens_ForSameUser()
    {
        //arrange
        var user = new User
        {
            Id = "user123",
            Email = "test@example.com",
            FirstName = "John",
            LastName = "Doe"
        };

        //act
        var token1 = _service.GenerateToken(user);
        System.Threading.Thread.Sleep(1000); // Wait to ensure different timestamp
        var token2 = _service.GenerateToken(user);

        //assert
        Assert.NotEqual(token1, token2); // Different timestamps should produce different tokens
    }
}
