using CarRentalDatabase.DatabaseContext;
using CarRentalEntities;
using CarRentalSystem.Helper;
using CarRentalSystem.Security;
using CarRentalSystem.UtilityServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace CarRentalSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthUserController : ControllerBase
    {

        private readonly CarRentalDbContext _dbContext;


        public readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;

        public AuthUserController(
            CarRentalDbContext dbContext, IConfiguration configuration, IEmailService emailService)
        {
            this._dbContext = dbContext;
            this._configuration = configuration;
            _emailService = emailService;
        }

        [HttpPost("register")]
        [AllowAnonymous]

        public async Task<IActionResult> insertUser([FromBody] AuthUser user)
        {
            if (_dbContext.AuthUsers.Where(u => u.Email == user.Email).FirstOrDefault() != null)
            {
                return Ok("already present");
            }
            user.UserId = Guid.NewGuid();
            user.MemberSince = DateTime.Now;
            user.Role = "user";
            user.ResetpasswordToken = "ResetpasswordTokenValue";
            user.ResetpasswordExpiry = DateTime.Now;
            await _dbContext.AuthUsers.AddAsync(user);
            await _dbContext.SaveChangesAsync();
            return Ok("register success");
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public Task<IActionResult> LoginUser([FromBody] Login user)
        {
            var isValidUser = _dbContext.AuthUsers.Where(u => u.Email == user.Email && u.Password == user.Password).FirstOrDefault();
            if (isValidUser != null)
            {
                //generating and passing token as a string

                string finalToken = new JwtSecurity(_configuration)
                    .GenerateToken(
                    isValidUser.UserId,
                    isValidUser.Firstname,
                    isValidUser.Lastname,
                    isValidUser.Email,
                    isValidUser.MobileNumber,
                    isValidUser.Role
                    );
                return Task.FromResult<IActionResult>(Ok(finalToken));
            }
            return Task.FromResult<IActionResult>(Ok("Failed to LoginIn"));

        }

        [HttpGet]
        public async Task<IActionResult> GetAllUser()
        {
            var user = await _dbContext.AuthUsers.ToListAsync();

            return Ok(user);
        }

        [HttpGet("register-user-count")]
        [Authorize]
        public async Task<IActionResult> GetAllUsersCount()
        {
            var userCount = await _dbContext.AuthUsers.CountAsync();

            return Ok(userCount);
        }

        [HttpPut("changepassword")]
        [AllowAnonymous]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordData data)
        {
            var user = await _dbContext.AuthUsers.FirstOrDefaultAsync(u => u.Password == data.OldPassword);

            if (user == null)
            {
                return Ok("Previous password is incorrect");
            }

            user.Password = data.password;
            await _dbContext.SaveChangesAsync();

            return Ok("Password changed successfully");
        }
        [HttpPut("updatestatus")]
        [AllowAnonymous]
        public async Task<IActionResult> UpdateStatus([FromBody] UpdateAuthStatus data)
        {
            var user = await _dbContext.AuthUsers.FirstOrDefaultAsync(u => u.UserId == data.Id);

            if (user == null)
            {
                return Ok("Not Found");
            }

            if (data.Status == "true")
            {
                user.Role = "admin";
                await _dbContext.SaveChangesAsync();
                return Ok("Admin updated successfully");
            }
            else if (data.Status == "false")
            {
                user.Role = "user";
                await _dbContext.SaveChangesAsync();
                return Ok("User updated successfully");
            }

            return Ok("User updated successfully");
        }

        [HttpPost("send-reset-email/{email}")]
        public async Task<IActionResult> SendEmail(string email)
        {
            var user = await _dbContext.AuthUsers.FirstOrDefaultAsync(a => a.Email == email);
            if (user is null)
            {
                return NotFound(new
                {
                    StatusCode = 404,
                    Message = "email Doesnt exist"

                });

            }
            var tokenBytes = RandomNumberGenerator.GetBytes(64);
            var emailToken = Convert.ToBase64String(tokenBytes);
            user.ResetpasswordToken = emailToken;
            user.ResetpasswordExpiry = DateTime.Now.AddMinutes(15);
            string from = _configuration["EmailSettings:From"];
            var emailModel = new EmailModel(email, "Reset Password!!", EmailBody.EmailStringBody(email, emailToken));
            _emailService.SendEmail(emailModel);
            _dbContext.Entry(user).State = EntityState.Modified;
            await _dbContext.SaveChangesAsync();
            return Ok(new
            {
                StatusCode = 200,
                Message = "email sent"

            });
        }
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPassword resetpassword)
        {
            var newToken = resetpassword.EmailToken.Replace(" ", "+");
            var user = await _dbContext.AuthUsers.AsNoTracking().FirstOrDefaultAsync(a => a.Email == resetpassword.Email);
            if (user is null)
            {
                return NotFound(new
                {
                    StatusCode = 404,
                    Message = "email doesn not exist"
                });

            }
            var tokenCode = user.ResetpasswordToken;
            DateTime emailTokenExpiry = user.ResetpasswordExpiry;
            if (tokenCode != resetpassword.EmailToken || emailTokenExpiry < DateTime.Now)
            {
                return BadRequest(new
                {
                    StatusCode = 400,
                    Message = "Invalid reset link"
                });
            }
            //user.Password = PasswordHasher(resetpassword.NewPassword);
            user.Password = resetpassword.NewPassword;
            _dbContext.Entry(user).State = EntityState.Modified;
            await _dbContext.SaveChangesAsync();
            return Ok(new
            {
                StatusCode = 200,
                Message = "password reset successfully"
            });

        }
        private string PasswordHasher(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                var hashedPassword = BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
                return hashedPassword;
            }
        }

    }
}