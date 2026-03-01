/* 13 ene: Se añade capa */
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using TaskManager.Context;
using TaskManager.DTOs;
using TaskManager.DTOs.Task;
using TaskManager.Interfaces.Tasks;
using TaskManager.Models;
using TaskManagerAPI.Utilities.Exceptions; // Asegúrate de tener este using para BusinessException

namespace TaskManager.Services.Tasks
{
    public class TaskService : ITaskService
    {
        private readonly AppDbContext _context;

        public TaskService(AppDbContext context)
        {
            _context = context;
        }

        // --- Búsquedas ---

        public async Task<PagedResultDto<TaskWithCategoryDto>> AdvancedSearchAsync(string? text, bool? completed, int? step, int? categoryId, string? categoryName, int page, int pageSize)
        {
            var query = _context.Tasks.Include(t => t.Category).AsQueryable();

            if (!string.IsNullOrWhiteSpace(text)) query = query.Where(t => t.Title.Contains(text));
            if (completed.HasValue) query = query.Where(t => t.IsCompleted == completed);
            if (step.HasValue) query = query.Where(t => t.Step == step);
            if (categoryId.HasValue) query = query.Where(t => t.CategoryId == categoryId);
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
                    CategoryName = t.Category != null ? t.Category.Name : "Sin Categoría"
                })
                .ToListAsync();

            return new PagedResultDto<TaskWithCategoryDto>
            {
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                Items = items
            };
        }

        // 050226
        public async Task<IEnumerable<TaskWithCategoryDto>> AjaxSearchAsync(string? text)
        {
            var query = _context.Tasks.AsQueryable();

            if (!string.IsNullOrWhiteSpace(text))
                query = query.Where(t => t.Title.Contains(text));

            return await query
                .OrderBy(t => t.Id)
                .Take(50)
                .Select(t => new TaskWithCategoryDto
                {
                    Id = t.Id,
                    Title = t.Title,
                    CategoryName = t.CategoryName, // Usamos la propiedad directa o navegación según tu modelo
                    IsCompleted = t.IsCompleted,
                    Step = t.Step
                })
                .ToListAsync();
        }

        // --- CRUD Básico ---

        public async Task<IEnumerable<TaskItemResponse>> GetAllAsync()
        {
            return await _context.Tasks
                .Select(t => new TaskItemResponse { Id = t.Id, Title = t.Title, IsCompleted = t.IsCompleted })
                .ToListAsync();
        }

        public async Task<TaskItemResponse?> GetByIdAsync(int id)
        {
            var task = await _context.Tasks.FindAsync(id);
            if (task == null) return null;

            return new TaskItemResponse { Id = task.Id, Title = task.Title, IsCompleted = task.IsCompleted };
        }

        public async Task<TaskItemResponse> CreateAsync(CreateTaskRequest request)
        {
            // Validaciones de negocio movidas desde el controlador
            // throw new BusinessException("La categoría no existe.", 404); // 15 Enero Interrupción (Ejemplo comentado)

            var categoryExists = await _context.Categories.AnyAsync(c => c.Id == request.CategoryId);
            if (!categoryExists)
                throw new BusinessException("CategoryId no existe.", 400); // Usamos excepción de negocio

            var entity = new TaskItem
            {
                Title = request.Title.Trim(),
                IsCompleted = false,
                CategoryId = request.CategoryId,
                CategoryName = "" // Se podría buscar el nombre si fuera necesario
            };

            // Registro
            _context.Tasks.Add(entity);
            await _context.SaveChangesAsync();

            return new TaskItemResponse { Id = entity.Id, Title = entity.Title, IsCompleted = entity.IsCompleted };
        }

        public async Task<bool> UpdateAsync(int id, UpdateTaskRequest request)
        {
            var task = await _context.Tasks.FindAsync(id);
            if (task == null) return false;

            // Modificación PUT para ampliar envio a FRONT 190226
            // 1. Actualizar el Título
            task.Title = request.Title.Trim();

            // 2. Actualizar el Estado
            if (request.IsCompleted.HasValue) task.IsCompleted = request.IsCompleted.Value;

            // 3. Actualiza el Step (si viene en la petición)
            if (request.Step.HasValue) task.Step = request.Step.Value;

            // 4. Actualiza la Categoría (si viene en la petición)
            if (request.CategoryId.HasValue)
            {
                // Si mandan un 0 o null desde el front, se puede asignar null o procesarlo
                task.CategoryId = request.CategoryId.Value > 0 ? request.CategoryId.Value : null;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var task = await _context.Tasks.FindAsync(id);
            if (task == null) return false;

            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();
            return true;
        }

        // --- Importación Excel ---

        // Importación Excel 260126
        public async Task<string> ImportFromExcelAsync(IFormFile file)
        {
            var taskitems = new List<TaskItem>();

            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                stream.Position = 0; // Nos aseguramos de ir al inicio

                using (var workbook = new XLWorkbook(stream)) // Abrir el archivo desde la memoria
                {
                    var worksheet = workbook.Worksheets.First(); // Tomamos la primera hoja
                    var rows = worksheet.RangeUsed().RowsUsed();

                    bool isHeader = true;

                    foreach (var row in rows)
                    {
                        if (isHeader) { isHeader = false; continue; }

                        // Declarar variables locales para los TryParse
                        int stepValue = 0;
                        int categoryIdValue = 0;
                        bool isComplete = false;
                        bool isDelete = false;

                        // Columna B: Title (Celda 2)
                        var title = row.Cell(2).GetString();

                        // Columna C: IsCompleted (Celda 3)
                        var isCompletedStr = row.Cell(3).GetString();
                        bool.TryParse(isCompletedStr, out isComplete);

                        // Columna D: Step (Celda 4)
                        var stepCell = row.Cell(4);
                        if (!stepCell.IsEmpty())
                        {
                            stepValue = stepCell.DataType == XLDataType.Number ?
                                (int)stepCell.GetDouble() : int.TryParse(stepCell.GetString(), out int s) ? s : 0;
                        }

                        // Columna E: CategoryId (Celda 5)
                        var categoryCell = row.Cell(5);
                        if (!categoryCell.IsEmpty())
                        {
                            categoryIdValue = categoryCell.DataType == XLDataType.Number ?
                                (int)categoryCell.GetDouble() : int.TryParse(categoryCell.GetString(), out int c) ? c : 0;
                        }

                        // Columna F: IsDeleted (Celda 6)
                        var isDeletedStr = row.Cell(6).GetString();
                        bool.TryParse(isDeletedStr, out isDelete);

                        if (string.IsNullOrWhiteSpace(title)) continue;

                        var taskitem = new TaskItem
                        {
                            Title = title.Trim(),
                            IsCompleted = isComplete,
                            Step = stepValue,
                            CategoryId = categoryIdValue > 0 ? categoryIdValue : null,
                            IsDeleted = isDelete,
                            CreatedAt = DateTime.Now
                        };

                        taskitems.Add(taskitem);
                    }
                }
            }

            // Validaciones
            // Opcional: filtrar duplicados por Name en la misma importación
            taskitems = taskitems.GroupBy(c => c.Title.ToLower()).Select(g => g.First()).ToList();

            // Opcional: evitar insertar categorías que ya existan en la BD (Por titulo en este caso)
            var existingNames = _context.Tasks.Select(c => c.Title.ToLower()).ToHashSet();
            var newTasks = taskitems.Where(c => !existingNames.Contains(c.Title.ToLower())).ToList();

            // Guardar en base de datos
            _context.Tasks.AddRange(newTasks);
            await _context.SaveChangesAsync();

            return $"Se importaron {newTasks.Count} tareas."; // Corregido para retornar solo las nuevas
        }
    }
}