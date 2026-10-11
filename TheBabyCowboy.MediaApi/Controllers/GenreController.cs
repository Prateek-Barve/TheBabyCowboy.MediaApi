using Microsoft.AspNetCore.Mvc;
using TheBabyCowBoy.MediaApi.DTOs;
using TheBabyCowBoy.MediaApi.Services.Interfaces;

namespace TheBabyCowBoy.MediaApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GenresController : ControllerBase
{
    private readonly IGenreService _genreService;

    public GenresController(IGenreService genreService)
    {
        _genreService = genreService;
    }

    // GET: /api/genres
    [HttpGet]
    public async Task<ActionResult<List<GenreResponse>>> GetAll()
    {
        var genres = await _genreService.GetAllAsync();

        return Ok(genres);
    }

    // GET: /api/genres/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GenreResponse>> GetById(Guid id)
    {
        var genre = await _genreService.GetByIdAsync(id);

        if (genre is null)
        {
            return NotFound();
        }

        return Ok(genre);
    }

    // POST: /api/genres
    [HttpPost]
    public async Task<ActionResult<GenreResponse>> Create(
        [FromBody] GenreCreateRequest request)
    {
        try
        {
            var genre = await _genreService.CreateAsync(request);

            return CreatedAtAction(
                nameof(GetById),
                new { id = genre.Id },
                genre);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}