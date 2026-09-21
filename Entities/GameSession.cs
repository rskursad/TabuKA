using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TabuKA.Entities;

public class GameSession
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int GameSettingsId { get; set; }

    [ForeignKey(nameof(GameSettingsId))]
    public virtual GameSettings GameSettings { get; set; } = null!;

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    public DateTime? EndedAt { get; set; }

    public int CurrentTeamIndex { get; set; } = 0;

    public int CurrentRound { get; set; } = 1;

    public int TotalRounds { get; set; } = 0;

    public GameStatus Status { get; set; } = GameStatus.NotStarted;

    public string? WinnerTeamId { get; set; }

    public string GameDataJson { get; set; } = "{}";

    [NotMapped]
    public GameSessionData GameData
    {
        get => System.Text.Json.JsonSerializer.Deserialize<GameSessionData>(GameDataJson) ?? new GameSessionData();
        set => GameDataJson = System.Text.Json.JsonSerializer.Serialize(value ?? new GameSessionData());
    }

    public virtual ICollection<Round> Rounds { get; set; } = new List<Round>();
}

public enum GameStatus
{
    NotStarted = 0,
    InProgress = 1,
    Paused = 2,
    Finished = 3,
    Cancelled = 4
}

public class GameSessionData
{
    public List<int> UsedWordIds { get; set; } = new List<int>();
    public List<int> TeamIds { get; set; } = new List<int>();
    public Dictionary<int, int> TeamScores { get; set; } = new Dictionary<int, int>();
    public Dictionary<int, int> TeamPassUsed { get; set; } = new Dictionary<int, int>();
    public int CurrentWordId { get; set; } = 0;
    public DateTime? RoundStartTime { get; set; }
    public int CurrentTeamId { get; set; } = 0;
}