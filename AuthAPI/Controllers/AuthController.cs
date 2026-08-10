using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AuthAPI.Models;
using Microsoft.AspNetCore.Authorization;

namespace AuthAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly string _jwtKey = "ThisIsASecretKeyForJWTToken12345!";
        private const string MainAdmin = "admin@store.com";

        public AuthController(UserManager<AppUser> userManager)
        {
            _userManager = userManager;
        }

        // POST /auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            var existingUser = await _userManager.FindByEmailAsync(dto.Email);
            if (existingUser != null)
                return BadRequest(new[] { new { code = "DuplicateEmail",
                    description = "This email is already registered." } });

            var user = new AppUser { UserName = dto.Username, Email = dto.Email };
            var result = await _userManager.CreateAsync(user, dto.Password);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            await _userManager.AddToRoleAsync(user, "Customer");
            return Ok("User registered successfully");
        }

        // GET /auth/checkusername (Public)
        [HttpGet("checkusername")]
        public async Task<IActionResult> CheckUsername(string username)
        {
            var user = await _userManager.FindByNameAsync(username);
            return Ok(new { taken = user != null });
        }

        // GET /auth/checkemail (Public)
        [HttpGet("checkemail")]
        public async Task<IActionResult> CheckEmail(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            return Ok(new { taken = user != null });
        }

        // POST /auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null || !await _userManager.CheckPasswordAsync(user, dto.Password))
                return Unauthorized("Invalid credentials");

            var roles = await _userManager.GetRolesAsync(user);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName!),
                new Claim(ClaimTypes.Email, user.Email!),
                new Claim(ClaimTypes.Role, roles.FirstOrDefault() ?? "Customer")
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtKey));
            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: new SigningCredentials(
                    key, SecurityAlgorithms.HmacSha256)
            );

            return Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token) });
        }

        // GET /auth/users (Admin only)
        [Authorize(Roles = "Admin")]
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = _userManager.Users.ToList();
            var result = new List<object>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                result.Add(new
                {
                    id = user.Id,
                    username = user.UserName,
                    email = user.Email,
                    role = roles.FirstOrDefault() ?? "Customer"
                });
            }
            return Ok(result);
        }

        // POST /auth/assignrole (Admin only)
        [Authorize(Roles = "Admin")]
        [HttpPost("assignrole")]
        public async Task<IActionResult> AssignRole([FromBody] AssignRoleDto dto)
        {
            // Protect main admin
            if (dto.Email.ToLower() == MainAdmin)
                return BadRequest("Cannot change the role of the main admin!");

            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null) return NotFound("User not found");

            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, dto.Role);
            return Ok($"Role {dto.Role} assigned to {dto.Email}");
        }

        // DELETE /auth/users/{email} (Admin only)
        [Authorize(Roles = "Admin")]
        [HttpDelete("users/{email}")]
        public async Task<IActionResult> DeleteUser(string email)
        {
            var decodedEmail = Uri.UnescapeDataString(email);

            // Protect main admin
            if (decodedEmail.ToLower() == MainAdmin)
                return BadRequest("Cannot delete the main admin!");

            var user = await _userManager.FindByEmailAsync(decodedEmail);
            if (user == null) return NotFound("User not found");
            await _userManager.DeleteAsync(user);
            return Ok("User deleted");
        }
    }

    public record RegisterDto(string Username, string Email, string Password);
    public record LoginDto(string Email, string Password);
    public record AssignRoleDto(string Email, string Role);
}