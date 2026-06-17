using GLMS.API.DTOs;
using GLMS.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace GLMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ITokenService _tokenService;
        private readonly IConfiguration _config;

        public AuthController(ITokenService tokenService, IConfiguration config)
        {
            _tokenService = tokenService;
            _config = config;
        }

        /// <summary>Login with username and password to receive a JWT token.</summary>
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginDto dto)
        {
            var validUser = _config["AdminCredentials:Username"];
            var validPass = _config["AdminCredentials:Password"];

            if (dto.Username != validUser || dto.Password != validPass)
                return Unauthorized(new { message = "Invalid credentials." });

            var token = _tokenService.GenerateToken(dto.Username);
            return Ok(new TokenDto(token, dto.Username));
        }
    }
}
