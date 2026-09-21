using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using TabuKA.Entities;

namespace TabuKA.Data;

public class TabuKADbContext : DbContext
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Word> Words => Set<Word>();
    public DbSet<GameSettings> GameSettings => Set<GameSettings>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<Player> Players => Set<Player>();
    public DbSet<GameSession> GameSessions => Set<GameSession>();
    public DbSet<Round> Rounds => Set<Round>();
    public DbSet<AppSettings> AppSettings => Set<AppSettings>();

    public TabuKADbContext(DbContextOptions<TabuKADbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();
        });

        modelBuilder.Entity<GameSettings>(entity =>
        {
        });

        modelBuilder.Entity<AppSettings>(entity =>
        {
            entity.HasIndex(e => e.Key).IsUnique();
        });

        modelBuilder.Entity<Word>(entity =>
        {
            entity.HasIndex(e => new { e.MainWord, e.CategoryId }).IsUnique();
        });

        modelBuilder.Entity<Team>(entity =>
        {
        });
    }
}