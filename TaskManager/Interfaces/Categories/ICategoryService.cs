using Microsoft.AspNetCore.Mvc;
using TaskManager.DTOs.Category;

namespace TaskManager.Interfaces.Categories
{
    public interface ICategoryService
    {
        Task<IEnumerable<CategoryDto>> GetAllAsync();
        Task<CategoryDto?> GetByIdAsync(int id);
        Task<CategoryDto> CreateAsync(CreateCategoryRequest request);
        Task<string> ImportFromExcelAsync(IFormFile file);
    }
}