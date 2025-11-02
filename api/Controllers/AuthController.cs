using Api.DTOs;
using Api.Models;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Security.Cryptography;

namespace Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IPasswordService _passwordService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IEmailService _emailService;
    private readonly ILoopService _loopService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IUserService userService,
        IPasswordService passwordService,
        IJwtTokenService jwtTokenService,
        IEmailService emailService,
        ILoopService loopService,
        ILogger<AuthController> logger)
    {
        _userService = userService;
        _passwordService = passwordService;
        _jwtTokenService = jwtTokenService;
        _emailService = emailService;
        _loopService = loopService;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<ActionResult<RegisterResponse>> Register(RegisterRequest request)
    {
        try
        {
            // Validate the model
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Validate password policy
            var passwordValidationResults = _passwordService.ValidatePassword(request.Password);
            if (passwordValidationResults.Any())
            {
                foreach (var validationResult in passwordValidationResults)
                {
                    ModelState.AddModelError("Password", validationResult.ErrorMessage ?? "Invalid password");
                }
                return BadRequest(ModelState);
            }

            // Check if user already exists
            var existingUser = await _userService.GetUserByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return Conflict(new { message = "A user with this email address already exists" });
            }

            // Generate email verification token
            var verificationToken = GenerateVerificationToken();
            var verificationExpiry = DateTime.UtcNow.AddHours(24); // Token expires in 24 hours

            // Create new user
            var user = new User
            {
                Email = request.Email,
                PasswordHash = _passwordService.HashPassword(request.Password),
                FirstName = request.FirstName,
                LastName = request.LastName,
                StreetAddress = request.StreetAddress,
                IsEmailVerified = false,
                EmailVerificationToken = verificationToken,
                EmailVerificationExpiry = verificationExpiry,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Save user to database
            var createdUser = await _userService.CreateUserAsync(user);

            _logger.LogInformation("User registered successfully: {Email}", request.Email);

            // Send verification email
            var emailSent = await _emailService.SendVerificationEmailAsync(createdUser, verificationToken);
            if (!emailSent)
            {
                _logger.LogWarning("Failed to send verification email to {Email}", request.Email);
            }

            return Ok(new RegisterResponse
            {
                Success = true,
                Message = "Registration successful. Please check your email for verification instructions.",
                User = new UserProfile
                {
                    Id = createdUser.Id ?? string.Empty,
                    Email = createdUser.Email,
                    FirstName = createdUser.FirstName,
                    LastName = createdUser.LastName,
                    StreetAddress = createdUser.StreetAddress,
                    IsEmailVerified = createdUser.IsEmailVerified
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user registration for email: {Email}", request.Email);
            return StatusCode(500, new { message = "An error occurred during registration" });
        }
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        try
        {
            _logger.LogInformation("Login attempt for email: {Email}", request.Email);
            
            // Validate the model
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Login failed - invalid model state for email: {Email}", request.Email);
                return BadRequest(ModelState);
            }

            // Find user by email
            var user = await _userService.GetUserByEmailAsync(request.Email);
            if (user == null)
            {
                _logger.LogWarning("Login failed - user not found for email: {Email}", request.Email);
                return Unauthorized(new { message = "Invalid email or password" });
            }

            _logger.LogInformation("User found for email: {Email}, checking password", request.Email);
            
            // Additional validation for password (should be caught by ModelState, but adding for null safety)
            if (string.IsNullOrWhiteSpace(request.Password))
            {
                _logger.LogWarning("Login failed - password is null or empty for email: {Email}", request.Email);
                return Unauthorized(new { message = "Invalid email or password" });
            }
            
            // DEBUG: Log password hash for debugging
            _logger.LogInformation("🔍 DEBUG: Password hash from database for {Email}: {PasswordHash}", request.Email, user.PasswordHash);
            _logger.LogInformation("🔍 DEBUG: Password provided length: {Length}", request.Password.Length);

            // Verify password
            var passwordValid = _passwordService.VerifyPassword(request.Password, user.PasswordHash);
            _logger.LogInformation("Password verification result for {Email}: {Result}", request.Email, passwordValid);
            
            if (!passwordValid)
            {
                _logger.LogWarning("Login failed - invalid password for email: {Email}", request.Email);
                return Unauthorized(new { message = "Invalid email or password" });
            }

            // Check if email is verified
            if (!user.IsEmailVerified)
            {
                _logger.LogWarning("Login failed - email not verified for: {Email}", request.Email);
                return StatusCode(403, new { message = "Please verify your email address before logging in" });
            }

            // Update last login time
            user.LastLoginAt = DateTime.UtcNow;
            await _userService.UpdateUserAsync(user.Id!, user);

            // Generate JWT token
            var token = _jwtTokenService.GenerateToken(user);
            var expiresAt = DateTime.UtcNow.AddHours(_jwtTokenService.GetTokenExpirationHours());
            
            _logger.LogInformation("Generated JWT token for {Email}. Expires at: {ExpiresAt} UTC. Current time: {CurrentTime} UTC", 
                request.Email, expiresAt, DateTime.UtcNow);

            _logger.LogInformation("User logged in successfully: {Email}", request.Email);

            return Ok(new AuthResponse
            {
                Token = token,
                ExpiresAt = expiresAt,
                User = new UserProfile
                {
                    Id = user.Id ?? string.Empty,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    StreetAddress = user.StreetAddress,
                    IsEmailVerified = user.IsEmailVerified
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for email: {Email}", request.Email);
            return StatusCode(500, new { message = "An error occurred during login" });
        }
    }

    [HttpPost("verify-email")]
    public async Task<ActionResult<VerificationResponse>> VerifyEmail(VerifyEmailRequest request)
    {
        try
        {
            // Validate the model
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Verify the email using the token
            var user = await _userService.VerifyEmailAsync(request.Token);
            
            if (user == null)
            {
                return BadRequest(new VerificationResponse
                {
                    Success = false,
                    Message = "Invalid or expired verification token"
                });
            }

            _logger.LogInformation("Email verified successfully for user: {Email}", user.Email);

            return Ok(new VerificationResponse
            {
                Success = true,
                Message = "Email verified successfully. You can now log in to your account.",
                User = new UserProfile
                {
                    Id = user.Id ?? string.Empty,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    StreetAddress = user.StreetAddress,
                    IsEmailVerified = user.IsEmailVerified
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during email verification for token: {Token}", request.Token);
            return StatusCode(500, new VerificationResponse
            {
                Success = false,
                Message = "An error occurred during email verification"
            });
        }
    }

    [HttpPost("logout")]
    [Authorize]
    public ActionResult Logout()
    {
        try
        {
            // Since we're using JWT tokens, logout is handled client-side by removing the token
            // The server doesn't need to maintain session state
            // In a more advanced implementation, we could maintain a blacklist of tokens
            
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
            _logger.LogInformation("User logged out: {Email}", userEmail);

            return Ok(new { message = "Logged out successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return StatusCode(500, new { message = "An error occurred during logout" });
        }
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserProfile>> GetCurrentUser()
    {
        try
        {
            // Get user ID from JWT token claims
            var userId = User.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "Invalid token" });
            }

            // Get user from database
            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            return Ok(new UserProfile
            {
                Id = user.Id ?? string.Empty,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                StreetAddress = user.StreetAddress,
                IsEmailVerified = user.IsEmailVerified
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user");
            return StatusCode(500, new { message = "An error occurred while retrieving user information" });
        }
    }

    [HttpGet("post-login-route")]
    [Authorize]
    public async Task<ActionResult<PostLoginRouteResponse>> GetPostLoginRoute()
    {
        try
        {
            // Get user ID from JWT token claims
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "Invalid token" });
            }

            // Get user's loops
            var loops = await _loopService.GetUserLoopsAsync(userId);
            
            string route;
            if (loops.Count == 0)
            {
                // No loops, redirect to loops list (they can create one)
                route = "/loops";
            }
            else if (loops.Count == 1)
            {
                // Single loop, redirect to that loop's detail page
                route = $"/loops/{loops[0].Id}";
            }
            else
            {
                // Multiple loops, redirect to loops list
                route = "/loops";
            }

            _logger.LogInformation("Determined post-login route for user {UserId}: {Route}", userId, route);

            return Ok(new PostLoginRouteResponse
            {
                Route = route
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error determining post-login route");
            return StatusCode(500, new { message = "An error occurred while determining the post-login route" });
        }
    }

    [HttpPost("resend-verification")]
    public async Task<ActionResult> ResendVerificationEmail(ResendVerificationRequest request)
    {
        try
        {
            // Validate the model
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Find user by email
            var user = await _userService.GetUserByEmailAsync(request.Email);
            if (user == null)
            {
                // Don't reveal if email exists or not for security
                return Ok(new { message = "If an account with this email exists and is not verified, a verification email has been sent." });
            }

            // Check if user is already verified
            if (user.IsEmailVerified)
            {
                return BadRequest(new { message = "This email address is already verified." });
            }

            // Generate new verification token
            var verificationToken = GenerateVerificationToken();
            var verificationExpiry = DateTime.UtcNow.AddHours(24);

            // Update user with new token
            user.EmailVerificationToken = verificationToken;
            user.EmailVerificationExpiry = verificationExpiry;
            user.UpdatedAt = DateTime.UtcNow;

            await _userService.UpdateUserAsync(user.Id!, user);

            // Send verification email
            var emailSent = await _emailService.SendVerificationEmailAsync(user, verificationToken);
            if (!emailSent)
            {
                _logger.LogWarning("Failed to resend verification email to {Email}", request.Email);
                return StatusCode(500, new { message = "Failed to send verification email. Please try again later." });
            }

            _logger.LogInformation("Verification email resent to {Email}", request.Email);

            return Ok(new { message = "If an account with this email exists and is not verified, a verification email has been sent." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resending verification email for: {Email}", request.Email);
            return StatusCode(500, new { message = "An error occurred while resending verification email" });
        }
    }

    [HttpPost("test-email")]
    [Authorize] // Require authentication for security
    public async Task<ActionResult> TestEmailConfiguration()
    {
        try
        {
            _logger.LogInformation("Testing email configuration");
            
            var result = await _emailService.TestEmailConfigurationAsync();
            
            if (result)
            {
                return Ok(new { 
                    message = "Email configuration test successful",
                    success = true 
                });
            }
            else
            {
                return StatusCode(500, new { 
                    message = "Email configuration test failed",
                    success = false 
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing email configuration");
            return StatusCode(500, new { 
                message = "An error occurred while testing email configuration",
                success = false 
            });
        }
    }

    [HttpGet("email-health")]
    [Authorize] // Require authentication for security
    public ActionResult GetEmailHealthStatus()
    {
        try
        {
            _logger.LogInformation("Getting email health status");
            
            var status = _emailService.GetEmailHealthStatus();
            
            return Ok(new
            {
                isConfigured = status.IsConfigured,
                testMode = status.TestMode,
                smtpHost = status.SmtpHost,
                smtpPort = status.SmtpPort,
                fromEmail = status.FromEmail,
                configurationErrors = status.ConfigurationErrors,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting email health status");
            return StatusCode(500, new { 
                message = "An error occurred while retrieving email health status" 
            });
        }
    }

    [HttpPost("dev/verify-user")]
    public async Task<ActionResult> DevVerifyUser([FromBody] DevVerifyUserRequest request)
    {
        // Only allow in development environment
        if (!_logger.GetType().Assembly.GetName().Name!.Contains("Development"))
        {
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            if (env != "Development")
            {
                return NotFound();
            }
        }

        try
        {
            var user = await _userService.GetUserByEmailAsync(request.Email);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            if (user.IsEmailVerified)
            {
                return Ok(new { message = "User email is already verified", user = new { user.Email, user.IsEmailVerified } });
            }

            // Manually verify the user
            user.IsEmailVerified = true;
            user.EmailVerificationToken = null;
            user.EmailVerificationExpiry = null;
            user.UpdatedAt = DateTime.UtcNow;

            await _userService.UpdateUserAsync(user.Id!, user);

            _logger.LogInformation("DEV: Manually verified email for user: {Email}", request.Email);

            return Ok(new { 
                message = "User email verified successfully (DEV MODE)", 
                user = new { user.Email, user.IsEmailVerified } 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error manually verifying user email: {Email}", request.Email);
            return StatusCode(500, new { message = "An error occurred while verifying user" });
        }
    }

    [HttpGet("dev/user-status/{email}")]
    public async Task<ActionResult> DevGetUserStatus(string email)
    {
        // Only allow in development environment
        if (!_logger.GetType().Assembly.GetName().Name!.Contains("Development"))
        {
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            if (env != "Development")
            {
                return NotFound();
            }
        }

        try
        {
            var user = await _userService.GetUserByEmailAsync(email);
            if (user == null)
            {
                return NotFound(new { message = "User not found", email });
            }

            return Ok(new
            {
                email = user.Email,
                firstName = user.FirstName,
                lastName = user.LastName,
                isEmailVerified = user.IsEmailVerified,
                hasVerificationToken = !string.IsNullOrEmpty(user.EmailVerificationToken),
                verificationExpiry = user.EmailVerificationExpiry,
                createdAt = user.CreatedAt,
                lastLoginAt = user.LastLoginAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user status: {Email}", email);
            return StatusCode(500, new { message = "An error occurred while retrieving user status" });
        }
    }

    private static string GenerateVerificationToken()
    {
        // Generate a cryptographically secure random token
        using var rng = RandomNumberGenerator.Create();
        var tokenBytes = new byte[32];
        rng.GetBytes(tokenBytes);
        return Convert.ToBase64String(tokenBytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }
}