using TaskManager.DTOs;
using TaskManager.DTOs.Task;

namespace TaskManager.Interfaces.Tasks
{
    public interface ITaskService
    {
        /* 13 ene
         * Interfaz de servicio o contrato: capa inter entre controlador y servicio
         */

        // --- Búsquedas ---
        Task<PagedResultDto<TaskWithCategoryDto>> AdvancedSearchAsync(string? text, bool? completed, int? step, int? categoryId, string? categoryName, int page, int pageSize);

        // 050226 Búsqueda rápida para Ajax
        Task<IEnumerable<TaskWithCategoryDto>> AjaxSearchAsync(string? text);

        // --- CRUD Básico ---
        Task<IEnumerable<TaskItemResponse>> GetAllAsync();
        Task<TaskItemResponse?> GetByIdAsync(int id);
        Task<TaskItemResponse> CreateAsync(CreateTaskRequest request);
        Task<bool> UpdateAsync(int id, UpdateTaskRequest request); // Retorna true si actualizó, false si no existe
        Task<bool> DeleteAsync(int id);

        // --- Procesos Masivos ---
        // Mi Importar Excel2 2601226
        Task<string> ImportFromExcelAsync(IFormFile file);
    }
}