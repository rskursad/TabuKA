using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TabuKA.Data;
using TabuKA.Entities;
using TabuKA.Services;
using TabuKA.ViewModels;
using Xunit;

namespace TabuKA.Tests;

public class CategorySelectionTests
{
    private (GameSetupViewModel vm, DatabaseService dbService) CreateViewModel(List<Category> categories)
    {
        var (contextFactory, gameService, settings) = GamePlayFlowTests.Setup(GamePlayFlowTests.BuildWords(10));
        
        using (var ctx = contextFactory.CreateDbContext())
        {
            ctx.Categories.AddRange(categories);
            ctx.SaveChanges();
        }

        var dbService = new DatabaseService(contextFactory, NullLogger<DatabaseService>.Instance);
        var settingsService = new AudioProximityAndPermissionTests.FakeSettingsService();
        var audio = new AudioService();
        var vosk = new VoskSpeechRecognitionService(audio, System.IO.Path.GetTempPath());
        var manager = new SpeechRecognitionManager(settingsService, vosk);
        var perm = new DesktopPermissionService(audio);

        var vm = new GameSetupViewModel(
            dbService, settingsService, gameService, new NavigationService(), contextFactory, perm, manager);

        return (vm, dbService);
    }

    [Fact]
    public async Task CategorySelection_SingleCategoryToggle_UpdatesSelectionAndCount()
    {
        var cats = new List<Category>
        {
            new() { Name = "Kategori A", IsActive = true, SortOrder = 1 },
            new() { Name = "Kategori B", IsActive = true, SortOrder = 2 },
            new() { Name = "Kategori C", IsActive = true, SortOrder = 3 }
        };

        var (vm, _) = CreateViewModel(cats);
        // Wait for LoadDataAsync
        await Task.Delay(150);

        Assert.True(vm.Categories.Count >= 3);
        Assert.Empty(vm.SelectedCategories);

        // Select single category
        var targetWrapper = vm.Categories[0];
        targetWrapper.IsSelected = true;

        Assert.Single(vm.SelectedCategories);
        Assert.Equal(targetWrapper.Category.Id, vm.SelectedCategories[0].Id);

        // Select another category
        var secondWrapper = vm.Categories[1];
        secondWrapper.IsSelected = true;

        Assert.Equal(2, vm.SelectedCategories.Count);
        Assert.Contains(targetWrapper.Category, vm.SelectedCategories);
        Assert.Contains(secondWrapper.Category, vm.SelectedCategories);

        // Deselect first category
        targetWrapper.IsSelected = false;

        Assert.Single(vm.SelectedCategories);
        Assert.DoesNotContain(targetWrapper.Category, vm.SelectedCategories);
        Assert.Contains(secondWrapper.Category, vm.SelectedCategories);
    }

    [Fact]
    public async Task CategorySelection_ToggleCategoryCommand_TogglesWrapperAndUpdatesList()
    {
        var cats = new List<Category>
        {
            new() { Name = "Spor", IsActive = true, SortOrder = 1 },
            new() { Name = "Bilim", IsActive = true, SortOrder = 2 }
        };

        var (vm, _) = CreateViewModel(cats);
        await Task.Delay(150);

        var wrapper = vm.Categories[0];
        Assert.False(wrapper.IsSelected);

        vm.ToggleCategoryCommand.Execute(wrapper);
        Assert.True(wrapper.IsSelected);
        Assert.Single(vm.SelectedCategories);

        vm.ToggleCategoryCommand.Execute(wrapper);
        Assert.False(wrapper.IsSelected);
        Assert.Empty(vm.SelectedCategories);
    }

    [Fact]
    public async Task CategorySelection_SelectAllAndDeselectAll_AndIndividualDeselect_Works()
    {
        var cats = new List<Category>
        {
            new() { Name = "Sinema", IsActive = true, SortOrder = 1 },
            new() { Name = "Müzik", IsActive = true, SortOrder = 2 },
            new() { Name = "Tarih", IsActive = true, SortOrder = 3 }
        };

        var (vm, _) = CreateViewModel(cats);
        await Task.Delay(150);

        // Select all
        vm.SelectAllCategoriesCommand.Execute(null);
        Assert.All(vm.Categories, c => Assert.True(c.IsSelected));
        Assert.Equal(vm.Categories.Count, vm.SelectedCategories.Count);

        // Deselect a single category from the all-selected state
        var deselected = vm.Categories[1];
        deselected.IsSelected = false;

        Assert.False(deselected.IsSelected);
        Assert.Equal(vm.Categories.Count - 1, vm.SelectedCategories.Count);
        Assert.DoesNotContain(deselected.Category, vm.SelectedCategories);

        // Deselect all
        vm.DeselectAllCategoriesCommand.Execute(null);
        Assert.All(vm.Categories, c => Assert.False(c.IsSelected));
        Assert.Empty(vm.SelectedCategories);
    }
}
