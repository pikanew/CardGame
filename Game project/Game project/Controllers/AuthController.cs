using Game_project.Data;
using Game_project.DTOs;
using Game_project.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using Resend;

namespace Game_project.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly GameDbContext _db;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly IResend _resend;

        public AuthController(
            GameDbContext db,
            IPasswordHasher<User> passwordHasher,
            IResend resend)
        {
            _db = db;
            _passwordHasher = passwordHasher;
            _resend = resend;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            bool usernameExists =
                await _db.Users.AnyAsync(u => u.Username == request.Username);

            if (usernameExists)
            {
                return Conflict("Username already exists.");
            }

            bool emailExists =
                await _db.Users.AnyAsync(u => u.Email == request.Email);

            if (emailExists)
            {
                return Conflict("Email already exists.");
            }

            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                EmailConfirmed = false
            };

            user.PasswordHash =
                _passwordHasher.HashPassword(user, request.Password);

            var tokenBytes = RandomNumberGenerator.GetBytes(32);
            var confirmationToken = Convert.ToBase64String(tokenBytes);

            user.EmailConfirmationToken = confirmationToken;
            user.EmailConfirmationTokenExpiresAt = DateTime.UtcNow.AddHours(24);


            user.Stats = new PlayerStats
            {
                Score = 0,
                Wins = 0,
                Losses = 0,
                Draws = 0
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            var confirmationLink =
                $"https://localhost:7091/api/Auth/confirm-email?token={Uri.EscapeDataString(confirmationToken)}";

            var message = new EmailMessage
            {
                From = "Card Game <onboarding@resend.dev>",
                Subject = "Confirm your Card Game account",
                HtmlBody = $"""
            <h2>Welcome to Card Game!</h2>

            <p>Please confirm your email address to complete your registration.</p>

            <p>
                <a href="{confirmationLink}">
                    Confirm email
                </a>
            </p>

            <p>This link expires in 24 hours.</p>
            """
            };

            message.To.Add(request.Email);

            try
            {
                await _resend.EmailSendAsync(message);
            }
            catch (Exception ex)
            {
                // Registration failed because the confirmation email could not be sent.
                _db.Users.Remove(user);
                await _db.SaveChangesAsync();

                return StatusCode(503, new
                {
                    message = "Registration failed because the confirmation email could not be sent.",
                    error = ex.Message
                });
            }

            return Ok(new
            {
                message = "Please check your email to complete registration."
            });
        }

        [HttpGet("confirm-email")]
        public async Task<IActionResult> ConfirmEmail(string token)
        {
            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.EmailConfirmationToken == token);

            if (user == null)
            {
                return BadRequest("Invalid confirmation token.");
            }

            if (user.EmailConfirmationTokenExpiresAt < DateTime.UtcNow)
            {
                return BadRequest("Confirmation token has expired.");
            }

            user.EmailConfirmed = true;

            // The token shouldn't be usable again.
            user.EmailConfirmationToken = null;
            user.EmailConfirmationTokenExpiresAt = null;

            await _db.SaveChangesAsync();

            return Ok("Email confirmed successfully.");
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request)
        {
            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            // Always return the same response.
            // This prevents people from checking which emails have accounts.
            if (user == null)
            {
                return Ok(new
                {
                    message = "If an account exists for this email, a password reset email has been sent."
                });
            }

            var tokenBytes = RandomNumberGenerator.GetBytes(32);
            var resetToken = Convert.ToBase64String(tokenBytes);

            user.PasswordResetToken = resetToken;
            user.PasswordResetTokenExpiresAt = DateTime.UtcNow.AddHours(1);

            await _db.SaveChangesAsync();

            var resetLink =
                $"https://localhost:7091/reset-password?token={Uri.EscapeDataString(resetToken)}";

            var message = new EmailMessage
            {
                From = "Card Game <onboarding@resend.dev>",
                Subject = "Reset your Card Game password",

                HtmlBody = $"""
            <h2>Password reset</h2>

            <p>
                A password reset was requested for your Card Game account.
            </p>

            <p>
                <a href="{resetLink}">
                    Reset password
                </a>
            </p>

            <p>This link expires in 1 hour.</p>

            <p>
                If you did not request a password reset,
                you can ignore this email.
            </p>
            """
            };

            message.To.Add(user.Email);

            await _resend.EmailSendAsync(message);

            return Ok(new
            {
                message = "If an account exists for this email, a password reset email has been sent."
            });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordRequest request)
        {
            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.PasswordResetToken == request.Token);

            if (user == null)
            {
                return BadRequest("Invalid password reset token.");
            }

            if (user.PasswordResetTokenExpiresAt == null ||
                user.PasswordResetTokenExpiresAt < DateTime.UtcNow)
            {
                return BadRequest("Password reset token has expired.");
            }

            var passwordCheck = _passwordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                request.NewPassword
            );

            if (passwordCheck != PasswordVerificationResult.Failed)
            {
                return BadRequest("New password cannot be the same as the old password.");
            }

            user.PasswordHash =
                _passwordHasher.HashPassword(user, request.NewPassword);

            // Make the reset token single-use.
            user.PasswordResetToken = null;
            user.PasswordResetTokenExpiresAt = null;

            await _db.SaveChangesAsync();

            return Ok(new
            {
                message = "Password reset successfully."
            });
        }
    }
}
