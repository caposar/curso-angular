using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using PeliculasAPI.DTOs;
using PeliculasAPI.Entidades;
using PeliculasAPI.Utilidades;

namespace PeliculasAPI.Controllers
{
    [Route("api/generos")]
    [ApiController]
    public class GenerosController : ControllerBase
    {
        private readonly IOutputCacheStore outputCacheStore;
        private readonly ApplicationDbContext context;
        private readonly IMapper mapper;
        private const string cacheTag = "generos";

        public GenerosController(IOutputCacheStore outputCacheStore, ApplicationDbContext context,
            IMapper mapper)
        {
            this.outputCacheStore = outputCacheStore;
            this.context = context;
            this.mapper = mapper;
        }

        [HttpGet] // api/generos
        [OutputCache(Tags = [cacheTag])]
        public async Task<List<GeneroDTO>> Get([FromQuery] PaginacionDTO paginacion)
        {
            var queryable = context.Generos;
            await HttpContext.InsertarParametrosPaginacionEnCabecera(queryable);

            //var generos = await context.Generos.ToListAsync();

            //var generosDTOs = mapper.Map<List<GeneroDTO>>(generos);

            //return generosDTOs;

            // Mas Eficiente porque trae solo los datos que necesita
            return await queryable
                .OrderBy(g => g.Nombre)
                .Paginar(paginacion)
                .ProjectTo<GeneroDTO>(mapper.ConfigurationProvider).ToListAsync();
        }

        [HttpGet("{id:int}", Name = "ObtenerGeneroPorId")] // api/generos/500
        [OutputCache(Tags = [cacheTag])]
        public async Task<ActionResult<GeneroDTO>> Get(int id)
        {
            var genero = await context.Generos
                .ProjectTo<GeneroDTO>(mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (genero is null)
            {
                return NotFound();
            }

            return genero;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] GeneroCreacionDTO generoCreacionDTO)
        {
            var genero = mapper.Map<Genero>(generoCreacionDTO);

            context.Add(genero);
            await context.SaveChangesAsync();
            await outputCacheStore.EvictByTagAsync(cacheTag, default);

            return CreatedAtRoute("ObtenerGeneroPorId", new { id = genero.Id }, genero);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Put(int id, [FromBody] GeneroCreacionDTO generoCreacionDTO)
        {
            var generoExiste = await context.Generos.AnyAsync(g => g.Id == id);

            if (!generoExiste)
            {
                return NotFound();
            }

            var genero = mapper.Map<Genero>(generoCreacionDTO);
            genero.Id = id;

            context.Update(genero);
            await context.SaveChangesAsync();
            await outputCacheStore.EvictByTagAsync(cacheTag, default);

            return NoContent();
        }

        [HttpDelete]
        public void Delete()
        {
            throw new NotImplementedException();
        }
    }
}
