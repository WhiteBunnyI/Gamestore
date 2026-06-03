using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Gamestore.Models;

public class DbCtx : DbContext
{
    public DbCtx(DbContextOptions<DbCtx> options)
        : base(options)
    {
    }

    public DbSet<Country> Countries { get; set; }

    public DbSet<Developer> Developers { get; set; }

    public DbSet<Game> Games { get; set; }

    public DbSet<GameDeveloper> GameDevelopers { get; set; }

    public DbSet<GameGenre> GameGenres { get; set; }

    public DbSet<GameUser> GameUsers { get; set; }

    public DbSet<GameVersion> GameVersions { get; set; }

    public DbSet<Genre> Genres { get; set; }

    public DbSet<Publisher> Publishers { get; set; }

    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Country>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("country_pkey");
        });

        modelBuilder.Entity<Developer>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("developer_pkey");
        });

        modelBuilder.Entity<Game>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("game_pkey");

            entity.HasOne(d => d.Publisher).WithMany(p => p.Games)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("game_publisher_id_fkey");
        });

        modelBuilder.Entity<GameDeveloper>(entity =>
        {
            entity.HasOne(d => d.Developer).WithMany()
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("game_developer_developer_id_fkey");

            entity.HasOne(d => d.Game).WithMany()
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("game_developer_game_id_fkey");
        });

        modelBuilder.Entity<GameGenre>(entity =>
        {
            entity.HasOne(d => d.Game).WithMany()
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("game_genre_game_id_fkey");

            entity.HasOne(d => d.Genre).WithMany()
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("game_genre_genre_id_fkey");
        });

        modelBuilder.Entity<GameUser>(entity =>
        {
            entity.HasOne(d => d.Game).WithMany()
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("game_user_game_id_fkey");

            entity.HasOne(d => d.User).WithMany()
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("game_user_user_id_fkey");
        });

        modelBuilder.Entity<GameVersion>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("game_version_pkey");

            entity.HasOne(d => d.Game).WithMany(p => p.GameVersions)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("game_version_game_id_fkey");
        });

        modelBuilder.Entity<Genre>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("genre_pkey");
        });

        modelBuilder.Entity<Publisher>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("publisher_pkey");

            entity.HasOne(d => d.Country).WithMany(p => p.Publishers)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("publisher_country_id_fkey");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("user_pkey");
        });
    }
}
