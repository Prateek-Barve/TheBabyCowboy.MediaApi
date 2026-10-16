
using Microsoft.AspNetCore.Mvc;
using TheBabyCowBoy.MediaApi.DTOs;
using TheBabyCowBoy.MediaApi.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using TheBabyCowBoy.MediaApi.Exceptions;
using Microsoft.AspNetCore.Authorization;
namespace TheBabyCowBoy.MediaApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MediaController : ControllerBase
{
    private readonly IMediaService _mediaService;

    public MediaController(IMediaService mediaService)
    {
        _mediaService = mediaService;
    }

    // GET: /api/media
    //[HttpGet]
    //public async Task<ActionResult<List<MediaItemResponse>>> GetAll(
    //    [FromQuery] string? category,
    //    [FromQuery] string? status,
    //    [FromQuery] string? genre)
    //{
    //    try
    //    {
    //        var items = await _mediaService.GetAllAsync(
    //            category,
    //            status,
    //            genre);

    //        return Ok(items);
    //    }
    //    catch (ArgumentException ex)
    //    {
    //        return BadRequest(new { message = ex.Message });
    //    }
    //}
    [HttpGet]
    public async Task<ActionResult<PagedResponse<MediaItemResponse>>> GetAll(
    [FromQuery] string? category,
    [FromQuery] string? status,
    [FromQuery] string? genre,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 10)
    {
        try
        {
            var result = await _mediaService.GetAllAsync(
                category,
                status,
                genre,
                page,
                pageSize);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // GET: /api/media/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<MediaItemResponse>> GetById(Guid id)
    {
        var item = await _mediaService.GetByIdAsync(id);

        if (item is null)
        {
            return NotFound();
        }

        return Ok(item);
    }

    // POST: /api/media
    //[HttpPost]
    //public async Task<ActionResult<MediaItemResponse>> Create(
    //    [FromBody] MediaItemCreateRequest request)
    //{
    //    try
    //    {
    //        var item = await _mediaService.CreateAsync(request);

    //        return CreatedAtAction(
    //            nameof(GetById),
    //            new { id = item.Id },
    //            item);
    //    }
    //    catch (ArgumentException ex)
    //    {
    //        return BadRequest(new { message = ex.Message });
    //    }
    //}
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<MediaItemResponse>> Create(
    [FromBody] MediaItemCreateRequest request)
    {
        try
        {
            var item = await _mediaService.CreateAsync(request);

            return CreatedAtAction(
                nameof(GetById),
                new { id = item.Id },
                item);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (DbUpdateException ex)
        {
            if (DatabaseExceptionHandler.IsUniqueViolation(
                ex, out var constraintName))
            {
                return Conflict(new
                {
                    message = GetDuplicateMessage(constraintName)
                });
            }

            throw;
        }
    }

    private static string GetDuplicateMessage(string? constraintName)
    {
        return constraintName switch
        {
            "IX_PlaylistDetails_SpotifyPlaylistId" =>
                "A playlist with this Spotify playlist ID already exists.",

            _ =>
                "A record with the same unique value already exists."
        };
    }

    // PUT: /api/media/{id}
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<MediaItemResponse>> Update(
        Guid id,
        [FromBody] MediaItemUpdateRequest request)
    {
        try
        {
            var item = await _mediaService.UpdateAsync(id, request);

            if (item is null)
            {
                return NotFound();
            }

            return Ok(item);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // DELETE: /api/media/{id}
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _mediaService.DeleteAsync(id);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}