using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TabuKA.Entities;

public class GameSettings
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = "Varsayılan";

    public int RoundTimeSeconds { get; set; } = 60;

    public int PassLimit { get; set; } = 3;

    public int TeamCount { get; set; } = 2;

    public int ScoreToWin { get; set; } = 0;

    public bool UseCustomCategories { get; set; } = false;

    public string SelectedCategoryIdsJson { get; set; } = "[]";

    [NotMapped]
    public List<int> SelectedCategoryIds
    {
        get => System.Text.Json.JsonSerializer.Deserialize<List<int>>(SelectedCategoryIdsJson) ?? new List<int>();
        set => SelectedCategoryIdsJson = System.Text.Json.JsonSerializer.Serialize(value ?? new List<int>());
    }

    public bool EnableSound { get; set; } = true;

    public bool EnableAutoTabooCheck { get; set; } = false;

    public int Volume { get; set; } = 80;

    public bool IsDefault { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}