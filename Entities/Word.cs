using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TabuKA.Entities;

public class Word
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string MainWord { get; set; } = string.Empty;

    [Required]
    [MaxLength(1000)]
    public string ForbiddenWordsJson { get; set; } = "[]";

    [NotMapped]
    public List<string> ForbiddenWords
    {
        get => System.Text.Json.JsonSerializer.Deserialize<List<string>>(ForbiddenWordsJson) ?? new List<string>();
        set => ForbiddenWordsJson = System.Text.Json.JsonSerializer.Serialize(value ?? new List<string>());
    }

    public int CategoryId { get; set; }

    [ForeignKey(nameof(CategoryId))]
    public virtual Category Category { get; set; } = null!;

    public int Difficulty { get; set; } = 1;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}