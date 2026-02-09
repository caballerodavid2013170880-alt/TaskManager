/* 13 ene: Se añade capa */

using Microsoft.EntityFrameworkCore;
using TaskManager.Context;
using TaskManager.DTOs.Task;
using TaskManager.DTOs;
using TaskManager.Interfaces.Tasks;

public class TaskService : ITaskService //Puente Interfaz y Servicio
{
    private readonly AppDbContext _context;

    public TaskService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResultDto<TaskWithCategoryDto>> AdvancedSearchAsync(
        string? text,
        bool? completed,
        int? step,
        int? categoryId,
        string? categoryName,
        int page,
        int pageSize
    )
    {
        var query = _context.Tasks
            .Include(t => t.Category)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(text))
            query = query.Where(t => t.Title.Contains(text));

        if (completed.HasValue)
            query = query.Where(t => t.IsCompleted == completed);

        if (step.HasValue)
            query = query.Where(t => t.Step == step);

        if (categoryId.HasValue)
            query = query.Where(t => t.CategoryId == categoryId);

        if (!string.IsNullOrWhiteSpace(categoryName))
        {
            var name = categoryName.Trim();
            query = query.Where(t => t.Category.Name.Contains(name));
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(t => t.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TaskWithCategoryDto
            {
                Id = t.Id,
                Title = t.Title,
                IsCompleted = t.IsCompleted,
                Step = t.Step,
                CreatedAt = t.CreatedAt,
                CategoryId = t.CategoryId ?? 0,
                // Usa el '?' para evitar el error de referencia nula:
                CategoryName = t.Category != null ? t.Category.Name : "Sin Categoría"
            })
            .ToListAsync();

        return new PagedResultDto<TaskWithCategoryDto> // DTO encapsula al otro DTO, delegando responsabilidades logica del controlador
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Items = items
        };
    }
}