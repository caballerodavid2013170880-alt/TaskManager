/* 13 ene: Se agrega capa */
using TaskManager.DTOs;
using TaskManager.DTOs.Task;

namespace TaskManager.Interfaces.Tasks
{
    public interface ITaskService
    {
        /* 13 ene
         * Interfaz de servicio o contrato: capa inter entre controlador y servicio
         * - Define qué va a hacer el servicio (cuáles son los métodos disponibles, pero NO el cómo) 
         * - 
         * AdvancedSearhAsync = búsquedas asíncronas en bd
         */
        Task<PagedResultDto<TaskWithCategoryDto>> AdvancedSearchAsync(
            string? text,
            bool? completed,
            int? step,
            int? categoryId,
            string? categoryName,
            int page,
            int pageSize
        );
    }
}