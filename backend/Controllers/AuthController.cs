using Backend.Data;
using Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using Backend.Models.Configs;
using Backend.Models.DTOs;
using System.Text.Json;


namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;
        private readonly GoogleOAuthConfig _googleConfig;

        public AuthController(AppDbContext db, IConfiguration config, GoogleOAuthConfig googleConfig)
        {
            _db = db;
            _config = config;
            _googleConfig = googleConfig;
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

        [HttpGet("google-login")]
        public IActionResult GoogleLogin()
        {
            var clientId = _googleConfig.web.client_id;
            var redirectUri = _googleConfig.web.redirect_uris[0];
            var scope = "openid email profile";
            var authUrl = $"{_googleConfig.web.auth_uri}?client_id={clientId}&redirect_uri={redirectUri}&response_type=code&scope={scope}";
            return Ok(new { url = authUrl });
        }

        [HttpGet("google/callback")]
        public async Task<IActionResult> GoogleCallback(string code)
        {
            var clientId = _googleConfig.web.client_id;
            var clientSecret = _googleConfig.web.client_secret;
            var redirectUri = _googleConfig.web.redirect_uris[0];
        
            var tokenRequestUri = _googleConfig.web.token_uri;
            var tokenRequestBody = new Dictionary<string, string>
            {
                { "code", code },
                { "client_id", clientId },
                { "client_secret", clientSecret },
                { "redirect_uri", redirectUri },
                { "grant_type", "authorization_code" }
            };

            var requestContent = new FormUrlEncodedContent(tokenRequestBody);
            using var httpClient = new HttpClient();
            var response = await httpClient.PostAsync(tokenRequestUri, requestContent);
            if (!response.IsSuccessStatusCode)
                return BadRequest(new { msg = "Google token request failed" });   
            var responseContent = await response.Content.ReadAsStringAsync();
            
            var tokenResponse = JsonSerializer.Deserialize<GoogleTokenResponse>(responseContent);
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(tokenResponse.id_token);

            var email = jwtToken.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
            var name = jwtToken.Claims.FirstOrDefault(c => c.Type == "name")?.Value ?? "Google User";
            var googleId = jwtToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(googleId))
                return BadRequest(new { msg = "Invalid Google token" });

            var player = await _db.Players
            .Include(p => p.AuthOAuths)
            .FirstOrDefaultAsync(p => p.AuthOAuths.Any(a => a.Provider == "Google" && a.ProviderUserId == googleId));
            if (player == null)
            {
                player = new Player { Username = name };
                player.AuthOAuths.Add(new AuthOAuth { Provider = "Google", ProviderUserId = googleId });

                _db.Players.Add(player);
                await _db.SaveChangesAsync();
            }
            var token = GenerateJwtToken(player);
            return Ok(new { msg = "token", token });
        }
    }
}
