using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TabuKA.Entities;
using TabuKA.Services;

namespace TabuKA.ViewModels;

public partial class WordManagementViewModel : ViewModelBase
{
    private readonly IDatabaseService _databaseService;
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial Category? SelectedFilterCategory { get; set; }

    [ObservableProperty]
    public partial List<Category> Categories { get; set; } = new();

    [ObservableProperty]
    public partial ObservableCollection<WordItem> Words { get; set; } = new();

    [ObservableProperty]
    public partial WordItem? SelectedWord { get; set; }

    [ObservableProperty]
    public partial bool IsEditing { get; set; }

    [ObservableProperty]
    public partial string EditMainWord { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string EditForbiddenWords { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int EditDifficulty { get; set; } = 1;

    [ObservableProperty]
    public partial Category? EditCategory { get; set; }

    [ObservableProperty]
    public partial string NewCategoryName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial Category? SelectedCategoryToDelete { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    private readonly List<int> _difficultyLevels = new() { 1, 2, 3 };
    public IReadOnlyList<int> DifficultyLevels => _difficultyLevels;

    private Word? _editingWord;

    public WordManagementViewModel(IDatabaseService databaseService, INavigationService navigationService)
    {
        _databaseService = databaseService;
        _navigationService = navigationService;

        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        await LoadCategoriesAsync();
        await LoadWordsAsync();
    }

    partial void OnSearchTextChanged(string value)
    {
        _ = LoadWordsAsync();
    }

    partial void OnSelectedFilterCategoryChanged(Category? value)
    {
        _ = LoadWordsAsync();
    }

    private async Task LoadCategoriesAsync()
    {
        var categories = await _databaseService.GetCategoriesAsync();
        Categories = categories;
        OnPropertyChanged(nameof(Categories));

        if (EditCategory != null && !categories.Any(c => c.Id == EditCategory.Id))
        {
            EditCategory = null;
        }
    }

    private async Task LoadWordsAsync()
    {
        IsLoading = true;
        try
        {
            var search = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim();
            int? categoryId = SelectedFilterCategory?.Id;

            var words = await _databaseService.SearchWordsAsync(search, categoryId);

            Words.Clear();
            foreach (var word in words)
            {
                Words.Add(new WordItem
                {
                    Id = word.Id,
                    MainWord = word.MainWord,
                    ForbiddenWords = word.ForbiddenWords,
                    Difficulty = word.Difficulty,
                    CategoryName = word.Category?.Name ?? Categories.FirstOrDefault(c => c.Id == word.CategoryId)?.Name ?? "Bilinmeyen",
                    CategoryId = word.CategoryId
                });
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void StartAdd()
    {
        _editingWord = null;
        EditMainWord = string.Empty;
        EditForbiddenWords = string.Empty;
        EditDifficulty = 1;
        EditCategory = SelectedFilterCategory ?? Categories.FirstOrDefault();
        IsEditing = true;
        StatusMessage = string.Empty;
    }

    [RelayCommand]
    private void StartEdit()
    {
        if (SelectedWord == null)
        {
            StatusMessage = "Önce listeden bir kelime seçin";
            return;
        }

        _editingWord = new Word
        {
            Id = SelectedWord.Id,
            MainWord = SelectedWord.MainWord,
            CategoryId = SelectedWord.CategoryId,
            IsActive = true
        };
        if (SelectedWord.ForbiddenWords is { Count: > 0 })
            _editingWord.ForbiddenWords = SelectedWord.ForbiddenWords;
        _editingWord.Difficulty = SelectedWord.Difficulty;

        EditMainWord = SelectedWord.MainWord;
        EditForbiddenWords = string.Join(", ", SelectedWord.ForbiddenWords);
        EditDifficulty = SelectedWord.Difficulty;
        EditCategory = Categories.FirstOrDefault(c => c.Id == SelectedWord.CategoryId);
        IsEditing = true;
        StatusMessage = string.Empty;
    }

    [RelayCommand]
    private void CancelEdit()
    {
        IsEditing = false;
        _editingWord = null;
        EditMainWord = string.Empty;
        EditForbiddenWords = string.Empty;
        StatusMessage = string.Empty;
    }

    [RelayCommand]
    private async Task SaveWordAsync()
    {
        if (string.IsNullOrWhiteSpace(EditMainWord))
        {
            StatusMessage = "Ana kelime boş olamaz";
            return;
        }

        if (EditCategory == null)
        {
            StatusMessage = "Kategori seçilmedi";
            return;
        }

        var forbidden = EditForbiddenWords
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(w => !string.IsNullOrWhiteSpace(w))
            .ToList();

        try
        {
            if (_editingWord == null)
            {
                await _databaseService.AddWordAsync(new Word
                {
                    MainWord = EditMainWord.Trim(),
                    ForbiddenWords = forbidden,
                    CategoryId = EditCategory.Id,
                    Difficulty = EditDifficulty
                });
                StatusMessage = $"'{EditMainWord}' eklendi";
            }
            else
            {
                _editingWord.MainWord = EditMainWord.Trim();
                _editingWord.ForbiddenWords = forbidden;
                _editingWord.CategoryId = EditCategory.Id;
                _editingWord.Difficulty = EditDifficulty;
                await _databaseService.UpdateWordAsync(_editingWord);
                StatusMessage = $"'{EditMainWord}' güncellendi";
            }

            IsEditing = false;
            _editingWord = null;
            await LoadWordsAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Hata: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task DeleteWordAsync()
    {
        if (SelectedWord == null)
        {
            StatusMessage = "Önce listeden bir kelime seçin";
            return;
        }

        var word = SelectedWord;
        try
        {
            await _databaseService.DeleteWordAsync(word.Id);
            Words.Remove(word);
            StatusMessage = $"'{word.MainWord}' silindi";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Hata: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task AddCategoryAsync()
    {
        if (string.IsNullOrWhiteSpace(NewCategoryName))
        {
            StatusMessage = "Kategori adı boş olamaz";
            return;
        }

        try
        {
            await _databaseService.AddCategoryAsync(NewCategoryName.Trim(), null);
            NewCategoryName = string.Empty;
            await LoadCategoriesAsync();
            StatusMessage = "Kategori eklendi";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Hata: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task DeleteCategoryAsync()
    {
        if (SelectedCategoryToDelete == null)
        {
            StatusMessage = "Önce silinecek kategoriyi seçin";
            return;
        }

        var category = SelectedCategoryToDelete;
        try
        {
            await _databaseService.DeleteCategoryAsync(category.Id);
            await LoadCategoriesAsync();
            await LoadWordsAsync();
            StatusMessage = $"'{category.Name}' kategorisi silindi";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Hata: {ex.Message}";
        }
    }

    [RelayCommand]
    private void GoBack()
    {
        _navigationService.NavigateTo(Services.ViewType.Settings);
    }
}

public partial class WordItem : ObservableObject
{
    public int Id { get; set; }
    public string MainWord { get; set; } = string.Empty;
    public List<string> ForbiddenWords { get; set; } = new();
    public int Difficulty { get; set; } = 1;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string ForbiddenText => string.Join(", ", ForbiddenWords);
}