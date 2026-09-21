using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TabuKA.Entities;

public class Round
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int GameSessionId { get; set; }

    [ForeignKey(nameof(GameSessionId))]
    public virtual GameSession GameSession { get; set; } = null!;

    public int TeamId { get; set; }

    [ForeignKey(nameof(TeamId))]
    public virtual Team Team { get; set; } = null!;

    public int WordId { get; set; }

    [ForeignKey(nameof(WordId))]
    public virtual Word Word { get; set; } = null!;

    public int RoundNumber { get; set; } = 0;

    public RoundResult Result { get; set; } = RoundResult.Pending;

    public int ScoreGained { get; set; } = 0;

    public int PassUsed { get; set; } = 0;

    public int TimeUsedSeconds { get; set; } = 0;

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    public DateTime? EndedAt { get; set; }

    public string? Notes { get; set; }
}

public enum RoundResult
{
    Pending = 0,
    Correct = 1,
    Passed = 2,
    Taboo = 3,
    TimeOut = 4,
    Skipped = 5
}