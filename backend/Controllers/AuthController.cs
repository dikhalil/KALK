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
        public async Task<IActionResult> SignUp(PlayerSignupDto dto)
        {
            var existingPlayer = await _db.Players.Include(p => p.AuthLocal).FirstOrDefaultAsync(p => p.AuthLocal.Email == dto.Email);
            if (existingPlayer != null)
                return BadRequest(new { msg = "Email already used" });

            var player = new Player
            {
                Username = dto.Username,
                AuthLocal = new AuthLocal
                {
                    Email = dto.Email, 
                    PasswordHash = new PasswordHasher<AuthLocal>().HashPassword(null, dto.Password)
                }
            };

            _db.Players.Add(player);
            await _db.SaveChangesAsync();

            return Ok(new { msg = "Player created" });
        }

        private string GenerateJwtToken(Player player)
        {
            var keyBytes = Encoding.UTF8.GetBytes(_config["Jwt:Key"]);
            var SecurityKey = new SymmetricSecurityKey(keyBytes);
            var credentials = new SigningCredentials(SecurityKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, player.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, player.AuthLocal.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),   
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(PlayerLoginDto dto)
        {
            var player = await _db.Players
                                .Include(p => p.AuthLocal)
                                .FirstOrDefaultAsync(p => p.AuthLocal.Email == dto.Email);
            if (player == null)
                return BadRequest(new { msg = "Invalid email" });

            var passwordHasher = new PasswordHasher<AuthLocal>();
            var result = passwordHasher.VerifyHashedPassword(player.AuthLocal, player.AuthLocal.PasswordHash, dto.Password);

            if (result == PasswordVerificationResult.Failed)
                return BadRequest(new { msg = "Invalid password" });

            var token = GenerateJwtToken(player);
            return Ok(new {msg = "token", token });
        }
    }
}
