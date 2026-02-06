using Backend.Data;
using Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;


namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;

        public AuthController(AppDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        [HttpPost("signup")]
        public async Task<IActionResult> SignUp([FromBody] User user)
        {
            var existingUser = await _db.Users.FirstOrDefaultAsync(u => u.Email == user.Email);
            if (existingUser != null)
                return BadRequest(new {msg = "Email already in use"});
            
            var passwordHasher = new PasswordHasher<User>();
            user.PasswordHash = passwordHasher.HashPassword(user, user.PasswordHash);

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            var player = new Player
            {
                UserId = user.Id
            };
            _db.Players.Add(player);
            await _db.SaveChangesAsync();   

            return Ok(new {msg = "User created successfully"});
        }

        private string GenerateJwtToken(User user)
        {
            var keyBytes = Encoding.UTF8.GetBytes(_config["Jwt:Key"]);
            var SecurityKey = new SymmetricSecurityKey(keyBytes);
            var credentials = new SigningCredentials(SecurityKey, SecurityAlgorithms.HmacSha256);

            
            return "";
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] User loginData)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == loginData.Email);
            if (user == null)
                return BadRequest(new {msg = "Invalid email"});

            var passwordHasher = new PasswordHasher<User>();
            var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, loginData.PasswordHash);
            if (result == PasswordVerificationResult.Failed)
                return BadRequest(new {msg = "Invalid password"});

            var token = GenerateJwtToken(user);
            return Ok(new {msg = "Login successful", userId = user.Id, role = user.Role, token = token});
        }

    }
}
