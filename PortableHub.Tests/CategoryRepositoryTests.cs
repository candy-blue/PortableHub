using PortableHub.Core.Models;
using Xunit;

namespace PortableHub.Tests;

public class CategoryRepositoryTests
{
    [Fact]
    public async Task DeleteCategory_ShouldReassignSoftwareToFallbackCategory()
    {
        using var env = new TestEnvironment();
        await env.InitializeAsync();

        // Add custom category
        var customCat = new Category
        {
            Name = "游戏开发",
            Icon = "Game",
            Color = "#8B5CF6",
            SortOrder = 10
        };
        var customCatId = await env.CategoryRepository.AddAsync(customCat);

        // Find fallback category "其他"
        var allCats = await env.CategoryRepository.GetAllAsync();
        var fallbackCat = allCats.First(c => c.Name == "其他");

        // Add software in custom category
        var software = new Software
        {
            Name = "Godot Engine",
            ExePath = @"D:\Tools\Godot.exe",
            CategoryId = customCatId
        };
        var swId = await env.SoftwareRepository.AddAsync(software);

        // Delete custom category
        await env.CategoryRepository.DeleteAsync(customCatId, fallbackCat.Id);

        // Verify custom category is deleted
        var deletedCat = await env.CategoryRepository.GetByIdAsync(customCatId);
        Assert.Null(deletedCat);

        // Verify software was reassigned to fallback category
        var updatedSw = await env.SoftwareRepository.GetByIdAsync(swId);
        Assert.NotNull(updatedSw);
        Assert.Equal(fallbackCat.Id, updatedSw.CategoryId);
        Assert.Equal(fallbackCat.Name, updatedSw.CategoryName);
    }

    [Fact]
    public async Task UpdateSortOrders_ShouldReorderCategoriesSuccessfully()
    {
        using var env = new TestEnvironment();
        await env.InitializeAsync();

        var catA = await env.CategoryRepository.AddAsync(new Category { Name = "CatA", SortOrder = 1 });
        var catB = await env.CategoryRepository.AddAsync(new Category { Name = "CatB", SortOrder = 2 });
        var catC = await env.CategoryRepository.AddAsync(new Category { Name = "CatC", SortOrder = 3 });

        // Reverse order
        await env.CategoryRepository.UpdateSortOrdersAsync(new[]
        {
            (catA, 3),
            (catB, 2),
            (catC, 1)
        });

        var all = await env.CategoryRepository.GetAllAsync();
        var reordered = all.Where(c => c.Id == catA || c.Id == catB || c.Id == catC).OrderBy(c => c.SortOrder).ToList();
        Assert.Equal("CatC", reordered[0].Name);
        Assert.Equal("CatB", reordered[1].Name);
        Assert.Equal("CatA", reordered[2].Name);
    }

    [Fact]
    public void ShortcutHelper_ShouldNormalizeExecutableAndDirectoryPaths()
    {
        // 1. Non-existent file
        var nonExistent = PortableHub.Infrastructure.Windows.ShortcutHelper.NormalizeDroppedPath(@"C:\non_existent_folder_xyz\fake.exe");
        Assert.Null(nonExistent);

        // 2. Directory with exe
        var tempDir = Path.Combine(Path.GetTempPath(), "PortableHub_TestDir_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            var dummyExe = Path.Combine(tempDir, "MyApp.exe");
            File.WriteAllText(dummyExe, "dummy");

            var resolved = PortableHub.Infrastructure.Windows.ShortcutHelper.NormalizeDroppedPath(tempDir);
            Assert.Equal(dummyExe, resolved);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void CategoryEditViewModel_ShouldSupportSymbolSelectionAndColorCustomization()
    {
        var vm = new PortableHub.App.ViewModels.CategoryEditViewModel
        {
            Name = "常用开发",
            Color = "#10B981"
        };

        // Select an icon option
        var codeOption = vm.AvailableIconOptions.First(o => o.Key == "Code24");
        vm.SelectIconOptionCommand.Execute(codeOption);

        Assert.Equal("Code24", vm.Icon);
        Assert.Equal(Wpf.Ui.Controls.SymbolRegular.Code24, vm.SelectedSymbol);

        // Select a color
        vm.SelectColorCommand.Execute("#EC4899");
        Assert.Equal("#EC4899", vm.Color);

        // Save
        bool closed = false;
        vm.RequestClose += (s, ok) => closed = ok;
        vm.SaveCommand.Execute(null);

        Assert.True(closed);
        Assert.NotNull(vm.ResultCategory);
        Assert.Equal("常用开发", vm.ResultCategory.Name);
        Assert.Equal("#EC4899", vm.ResultCategory.Color);
        Assert.Equal("Code24", vm.ResultCategory.Icon);
    }
}
