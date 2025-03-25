using AutoMapper.QueryableExtensions;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using PeliculasAPI.DTOs;
using PeliculasAPI.Utilidades;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using PeliculasAPI.Entidades;

namespace PeliculasAPI.Controllers
{
    [Route("api/usuarios")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "esadmin")]
    public class UsuariosController : ControllerBase
    {
        private readonly UserManager<IdentityUser> userManager;
        private readonly SignInManager<IdentityUser> signInManager;
        private readonly IConfiguration configuration;
        private readonly ApplicationDbContext context;
        private readonly IMapper mapper;

        public UsuariosController(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager,
            IConfiguration configuration, ApplicationDbContext context, IMapper mapper)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.configuration = configuration;
            this.context = context;
            this.mapper = mapper;
        }

        [HttpGet("ListadoUsuarios")]
        public async Task<ActionResult<List<UsuarioDTO>>> ListadoUsuarios([FromQuery] PaginacionDTO paginacionDTO)
        {
            var queryable = context.Users.AsQueryable();
            await HttpContext.InsertarParametrosPaginacionEnCabecera(queryable);
            var usuarios = await queryable.ProjectTo<UsuarioDTO>(mapper.ConfigurationProvider)
                .OrderBy(x => x.Email).Paginar(paginacionDTO).ToListAsync();

            return usuarios;
        }

        [HttpPost("registrar")]
        [AllowAnonymous]
        public async Task<ActionResult<RespuestaAutenticacionDTO>> Registrar(CredencialesUsuarioDTO credencialesUsuarioDTO)
        {
            var usuario = new IdentityUser
            {
                Email = credencialesUsuarioDTO.Email,
                UserName = credencialesUsuarioDTO.Email
            };

            var resultado = await userManager.CreateAsync(usuario, credencialesUsuarioDTO.Password);

            if (resultado.Succeeded)
            {
                return await ConstruirToken(usuario);
            }
            else
            {
                return BadRequest(resultado.Errors);
            }
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<RespuestaAutenticacionDTO>> Login(CredencialesUsuarioDTO credencialesUsuarioDTO)
        {
            var usuario = await userManager.FindByEmailAsync(credencialesUsuarioDTO.Email);

            if (usuario is null)
            {
                var errores = ConstruirLoginIncorrecto();
                return BadRequest(errores);
            }

            var resultado = await signInManager.CheckPasswordSignInAsync(usuario,
                credencialesUsuarioDTO.Password, lockoutOnFailure: false);

            if (resultado.Succeeded)
            {
                return await ConstruirToken(usuario);
            }
            else
            {
                var errores = ConstruirLoginIncorrecto();
                return BadRequest(errores);
            }
        }

        [HttpPost("logout")]
        [AllowAnonymous]
        public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
        {
            if (string.IsNullOrEmpty(request.RefreshToken))
            {
                return BadRequest(new { message = "Refresh token requerido." });
            }

            var refreshTokenEntity = await context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken);

            if (refreshTokenEntity == null)
            {
                return BadRequest(new { message = "Refresh token no encontrado." });
            }

            if (refreshTokenEntity.Expiration < DateTime.UtcNow)
            {
                return BadRequest(new { message = "El refresh token ha expirado." });
            }

            if (refreshTokenEntity.Revoked)
            {
                return BadRequest(new { message = "El refresh token ya ha sido revocado." });
            }

            // Revocar el refresh token usado
            await RevocarRefreshToken(refreshTokenEntity);

            return Ok(new { message = "Sesión cerrada en este dispositivo." });
        }

        [HttpPost("HacerAdmin")]
        public async Task<IActionResult> HacerAdmin(EditarClaimDTO editarClaimDTO)
        {
            var usuario = await userManager.FindByEmailAsync(editarClaimDTO.Email);

            if (usuario is null)
            {
                return NotFound();
            }

            await userManager.AddClaimAsync(usuario, new Claim("esadmin", "true"));
            return NoContent();
        }

        [HttpPost("RemoverAdmin")]
        public async Task<IActionResult> RemoverAdmin(EditarClaimDTO editarClaimDTO)
        {
            var usuario = await userManager.FindByEmailAsync(editarClaimDTO.Email);

            if (usuario is null)
            {
                return NotFound();
            }

            await userManager.RemoveClaimAsync(usuario, new Claim("esadmin", "true"));
            return NoContent();
        }

        private IEnumerable<IdentityError> ConstruirLoginIncorrecto()
        {
            var identityError = new IdentityError() { Description = "Login incorrecto" };
            var errores = new List<IdentityError>();
            errores.Add(identityError);
            return errores;
        }

        private async Task<RespuestaAutenticacionDTO> ConstruirToken(IdentityUser identityUser)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, identityUser.Id), // ID único del usuario (de la BD)
                new Claim("email", identityUser.Email!),
                new Claim("lo que yo quiera", "cualquier valor")
            };

            var claimsDB = await userManager.GetClaimsAsync(identityUser);

            claims.AddRange(claimsDB);

            var issuer = configuration["Jwt:Issuer"];
            var audience = configuration["Jwt:Audience"];
            var secretKey = configuration["Jwt:SecretKey"]!;
            var llave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(llave, SecurityAlgorithms.HmacSha256);
            var expiracion = DateTime.UtcNow.AddMinutes(configuration.GetValue<int>("JWT:ExpirationInMinutes"));
            var expiracionRefreshToken = DateTime.UtcNow.AddDays(7);

            var tokenDeSeguridad = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expiracion,
                signingCredentials: creds
            );

            var accessToken = new JwtSecurityTokenHandler().WriteToken(tokenDeSeguridad);

            // Generar y guardar refresh token en la base de datos
            var refreshToken = Guid.NewGuid().ToString("N");
            await GuardarRefreshTokenEnBaseDeDatos(identityUser.Id, refreshToken, expiracionRefreshToken);

            return new RespuestaAutenticacionDTO
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
            };
        }

        [HttpPost("renovar-token")]
        [AllowAnonymous]
        public async Task<ActionResult<RespuestaAutenticacionDTO>> RenovarToken([FromBody] RefreshTokenRequest request)
        {
            if (string.IsNullOrEmpty(request?.RefreshToken))
            {
                return BadRequest(new { message = "El refresh token es requerido." });
            }

            var refreshTokenEntity = await context.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken);

            if (refreshTokenEntity == null || refreshTokenEntity.Expiration < DateTime.UtcNow || refreshTokenEntity.Revoked)
            {
                return BadRequest(new { message = "Refresh token no encontrado, expirado o revocado." });
            }

            var user = await userManager.FindByIdAsync(refreshTokenEntity.UserId);
            if (user == null)
            {
                return BadRequest(new { message = "Usuario no encontrado." });
            }

            // Revocar el refresh token usado
            await RevocarRefreshToken(refreshTokenEntity);

            var respuesta = await ConstruirToken(user);
            return Ok(respuesta);
        }

        private async Task GuardarRefreshTokenEnBaseDeDatos(string userId, string refreshToken, DateTime expiration)
        {
            var refreshTokenEntity = new RefreshToken
            {
                UserId = userId,
                Token = refreshToken,
                Expiration = expiration
            };

            await context.RefreshTokens.AddAsync(refreshTokenEntity);
            await context.SaveChangesAsync();
        }

        private async Task RevocarRefreshToken(RefreshToken refreshToken)
        {
            refreshToken.RevokedAt = DateTime.UtcNow;
            refreshToken.Revoked = true;
            await context.SaveChangesAsync();
        }
    }
}
