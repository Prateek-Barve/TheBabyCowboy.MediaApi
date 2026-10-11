using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using TheBabyCowBoy.MediaApi.Data;
using TheBabyCowBoy.MediaApi.DTOs;
using TheBabyCowBoy.MediaApi.Models;
using TheBabyCowBoy.MediaApi.Services.Interfaces;

namespace TheBabyCowBoy.MediaApi.Services;

public class GenreService : IGenreService
{
    private readonly MediaDbContext _context;

    public GenreService(MediaDbContext context)
    {
        _context = context;
    }

    public async Task<List<GenreResponse>> GetAllAsync()
    {
        return await _context.Genres
            .AsNoTracking()
            .OrderBy(g => g.Name)
            .Select(g => new GenreResponse
            {
                Id = g.Id,
                Name = g.Name,
                Slug = g.Slug
            })
            .ToListAsync();
    }

    public async Task<GenreResponse?> GetByIdAsync(Guid id)
    {
        return await _context.Genres
            .AsNoTracking()
            .Where(g => g.Id == id)
            .Select(g => new GenreResponse
            {
                Id = g.Id,
                Name = g.Name,
                Slug = g.Slug
            })
            .FirstOrDefaultAsync();
    }

    public async Task<GenreResponse> CreateAsync(
        GenreCreateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException(
                "Genre name is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Slug))
        {
            throw new ArgumentException(
                "Genre slug is required.");
        }

        var name = request.Name.Trim();
        var slug = request.Slug.Trim().ToLowerInvariant();

        if (name.Length > 100)
        {
            throw new ArgumentException(
                "Genre name cannot exceed 100 characters.");
        }

        if (slug.Length > 100 ||
            !Regex.IsMatch(
                slug,
                @"^[a-z0-9]+(?:-[a-z0-9]+)*$"))
        {
            throw new ArgumentException(
                "Slug must contain lowercase letters, numbers, and single hyphens between words.");
        }

        var nameExists = await _context.Genres
            .AnyAsync(g => g.Name.ToLower() == name.ToLower());

        if (nameExists)
        {
            throw new InvalidOperationException(
                "A genre with this name already exists.");
        }

        var slugExists = await _context.Genres
            .AnyAsync(g => g.Slug == slug);

        if (slugExists)
        {
            throw new InvalidOperationException(
                "A genre with this slug already exists.");
        }

        var genre = new Genre
        {
            Name = name,
            Slug = slug
        };

        _context.Genres.Add(genre);

        await _context.SaveChangesAsync();

        return new GenreResponse
        {
            Id = genre.Id,
            Name = genre.Name,
            Slug = genre.Slug
        };
    }
}