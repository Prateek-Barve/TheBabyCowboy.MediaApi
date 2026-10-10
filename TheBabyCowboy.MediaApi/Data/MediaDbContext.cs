using Microsoft.EntityFrameworkCore;
using TheBabyCowBoy.MediaApi.Models;

namespace TheBabyCowBoy.MediaApi.Data;

public class MediaDbContext : DbContext
{
    public MediaDbContext(DbContextOptions<MediaDbContext> options)
        : base(options)
    {
    }

    public DbSet<MediaItem> MediaItems => Set<MediaItem>();
    public DbSet<BookDetails> BookDetails => Set<BookDetails>();
    public DbSet<MovieDetails> MovieDetails => Set<MovieDetails>();
    public DbSet<SeriesDetails> SeriesDetails => Set<SeriesDetails>();
    public DbSet<PlaylistDetails> PlaylistDetails => Set<PlaylistDetails>();
    public DbSet<Genre> Genres => Set<Genre>();
    public DbSet<MediaItemGenre> MediaItemGenres => Set<MediaItemGenre>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Shared media table
        modelBuilder.Entity<MediaItem>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Title)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(x => x.Category)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(x => x.Description)
                .HasMaxLength(2000);

            entity.Property(x => x.CoverImageUrl)
                .HasMaxLength(1000);

            entity.Property(x => x.CreatedAt)
                .IsRequired();

            entity.HasIndex(x => new { x.Category, x.DisplayOrder });
        });

        // Book details: one-to-one with MediaItem

        modelBuilder.Entity<BookDetails>(entity =>
        {
            entity.HasKey(x => x.MediaItemId);

            entity.Property(x => x.Author)
                .HasMaxLength(200)
                .IsRequired();

            entity.HasOne(x => x.MediaItem)
                .WithOne(x => x.BookDetails)
                .HasForeignKey<BookDetails>(x => x.MediaItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });


        // Movie details: one-to-one with MediaItem
        modelBuilder.Entity<MovieDetails>(entity =>
        {
            entity.HasKey(x => x.MediaItemId);

            entity.Property(x => x.Director)
                .HasMaxLength(200);

            entity.HasOne(x => x.MediaItem)
                .WithOne(x => x.MovieDetails)
                .HasForeignKey<MovieDetails>(x => x.MediaItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Series details: one-to-one with MediaItem
        modelBuilder.Entity<SeriesDetails>(entity =>
        {
            entity.HasKey(x => x.MediaItemId);

            entity.Property(x => x.Creator)
                .HasMaxLength(200);

            entity.HasOne(x => x.MediaItem)
                .WithOne(x => x.SeriesDetails)
                .HasForeignKey<SeriesDetails>(x => x.MediaItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Playlist details: one-to-one with MediaItem
        modelBuilder.Entity<PlaylistDetails>(entity =>
        {
            entity.HasKey(x => x.MediaItemId);

            entity.Property(x => x.SpotifyPlaylistId)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.Platform)
                .HasMaxLength(50)
                .IsRequired();

            entity.HasIndex(x => x.SpotifyPlaylistId)
                .IsUnique();

            entity.HasOne(x => x.MediaItem)
                .WithOne(x => x.PlaylistDetails)
                .HasForeignKey<PlaylistDetails>(x => x.MediaItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Genres
        modelBuilder.Entity<Genre>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.Slug)
                .HasMaxLength(100)
                .IsRequired();

            entity.HasIndex(x => x.Name).IsUnique();
            entity.HasIndex(x => x.Slug).IsUnique();
        });

        // Many-to-many linking table
        modelBuilder.Entity<MediaItemGenre>(entity =>
        {
            entity.HasKey(x => new { x.MediaItemId, x.GenreId });

            entity.HasOne(x => x.MediaItem)
                .WithMany(x => x.MediaItemGenres)
                .HasForeignKey(x => x.MediaItemId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Genre)
                .WithMany(x => x.MediaItemGenres)
                .HasForeignKey(x => x.GenreId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}