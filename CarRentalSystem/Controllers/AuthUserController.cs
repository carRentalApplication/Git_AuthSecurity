using CarRentalDatabase.DatabaseContext;
using CarRentalEntities;
using CarRentalSystem.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarRentalSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthUserController : ControllerBase
    {

        private readonly CarRentalDbContext _dbContext;

        public readonly IConfiguration _configuration;

        public AuthUserController(
            CarRentalDbContext dbContext,IConfiguration configuration)
        {
            this._dbContext=dbContext;
            this._configuration = configuration;
        }

        [HttpPost("register")]
        [AllowAnonymous]

        public async Task<IActionResult> insertUser([FromBody] AuthUser user)
        {
            if (_dbContext.AuthUsers.Where(u => u.Email == user.Email).FirstOrDefault() != null)
            {
                return Ok();
            }
           user.UserId = Guid.NewGuid();
            user.MemberSince = DateTime.Now;
            user.Role = "user";
            await _dbContext.AuthUsers.AddAsync(user);
            await _dbContext.SaveChangesAsync();
            return Ok(user);
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

    }
}
