using Backend.Features.Quizes;
using Backend.Features.GameSessions;
using Microsoft.EntityFrameworkCore;

namespace Backend.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<QuizTheme> QuizThemes => Set<QuizTheme>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<AnswerOption> AnswerOptions => Set<AnswerOption>();
    public DbSet<Quiz> Quizes => Set<Quiz>();
    public DbSet<GameSession> GameSessions => Set<GameSession>();
    public DbSet<GameSessionPlayer> GameSessionPlayers => Set<GameSessionPlayer>();
    public DbSet<GameSessionQuestion> GameSessionQuestions => Set<GameSessionQuestion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Question>(entity =>
        {
            entity.HasOne<QuizTheme>()
                .WithMany()
                .HasForeignKey(question => question.ThemeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(question => question.Options)
                .WithOne()
                .HasForeignKey(option => option.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(question => question.ThemeId);
        });

        modelBuilder.Entity<Quiz>(entity =>
        {
            entity.HasOne<QuizTheme>()
                .WithMany()
                .HasForeignKey(quiz => quiz.ThemeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(quiz => quiz.ThemeId);
        });

        modelBuilder.Entity<GameSessionPlayer>(entity =>
        {
            entity.HasOne<GameSession>()
                .WithMany(session => session.Players)
                .HasForeignKey(player => player.GameSessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<GameSessionQuestion>(entity =>
        {
            entity.HasOne<GameSession>()
                .WithMany(session => session.Questions)
                .HasForeignKey(question => question.GameSessionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(question => new { question.GameSessionId, question.Order })
                .IsUnique();
        });
    }
}
