using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TabuKA.Entities;

public class Team
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public int Score { get; set; } = 0;

    public int CurrentRound { get; set; } = 0;

    public int PassUsed { get; set; } = 0;

    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual ICollection<Player> Players { get; set; } = new List<Player>();
    public virtual ICollection<GameSession> GameSessions { get; set; } = new List<GameSession>();
}