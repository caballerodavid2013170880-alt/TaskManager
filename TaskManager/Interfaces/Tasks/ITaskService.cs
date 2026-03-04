using Microsoft.AspNetCore.Http; // Necesario para IFormFile
using Microsoft.AspNetCore.Mvc;
using TaskManager.DTOs;
using TaskManager.DTOs.Task;

namespace TaskManager.Interfaces.Tasks
{
    public interface ITaskService
    {
        Task<PagedResultDto<TaskWithCategoryDto>> AdvancedSearchAsync(string? text, bool? completed, int? step, int? categoryId, string? categoryName, int page, int pageSize);

        // --- NUEVOS MÉTODOS MIGRADOS ---

        // CRUD Básico
        Task<List<TaskItemResponse>> GetAllAsync();
        Task<TaskItemResponse?> GetByIdAsync(int id);
        Task<TaskItemResponse> CreateAsync(CreateTaskRequest request);
        Task<bool> UpdateAsync(int id, UpdateTaskRequest request);
        Task<bool> DeleteAsync(int id);

        // Búsquedas y Listados Especiales
        Task<List<TaskSearchResult>> SearchAsync(TaskSearchRequest request);
        Task<List<TaskSearchResult>> GetPagedAsync(int page, int pageSize);
        Task<List<TaskWithCategoryDto>> GetWithCategoryAsync();
        Task<object> AjaxSearchAsync(string? text); // Retorna anónimo/object según tu código original

        // Importación
        Task<string> ImportFromExcelAsync(IFormFile file);
    }
}