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
}
