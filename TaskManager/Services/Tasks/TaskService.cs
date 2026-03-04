/* 13 ene: Se añade capa */

using ClosedXML.Excel;
using DocumentFormat.OpenXml.Office2021.DocumentTasks;
using Microsoft.EntityFrameworkCore;
using TaskManager.Context;
using TaskManager.DTOs;
using TaskManager.DTOs.Task;
using TaskManager.Interfaces.Tasks;
using TaskManager.Models;

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

    // 1. GET ALL
    public async Task<List<TaskItemResponse>> GetAllAsync()
    {
        return await _context.Tasks
            .Select(t => new TaskItemResponse
            {
                Id = t.Id,
                Title = t.Title,
                IsCompleted = t.IsCompleted
            })
            .ToListAsync();
    }

    // 2. GET BY ID
    public async Task<TaskItemResponse?> GetByIdAsync(int id)
    {
        var task = await _context.Tasks.FindAsync(id);

        if (task == null) return null;

        return new TaskItemResponse
        {
            Id = task.Id,
            Title = task.Title,
            IsCompleted = task.IsCompleted,
            Step = task.Step,                       //270226 Se agrega porque faltan estos datos al enviarlos al Front
            CategoryId = (int)task.CategoryId       //270226
        };
    }

    // 3. CREATE
    public async Task<TaskItemResponse> CreateAsync(CreateTaskRequest request)
    {
        // Validaciones de negocio movidas desde el controlador
        // throw new BusinessException("La categoría no existe.", 404); // 15 Enero Interrupción

        var categoryExists = await _context.Categories.AnyAsync(c => c.Id == request.CategoryId);
        if (!categoryExists)
            throw new Exception("CategoryId no existe."); // Reemplazamos BadRequest por Excepción

        var entity = new TaskItem
        {
            Title = request.Title.Trim(),
            IsCompleted = false,
            CategoryId = request.CategoryId,
            CategoryName = ""
        };

        //Registro
        _context.Tasks.Add(entity);
        await _context.SaveChangesAsync();

        return new TaskItemResponse
        {
            Id = entity.Id,
            Title = entity.Title,
            IsCompleted = entity.IsCompleted,
            Step = entity.Step,                       //40326 Se agrega porque faltan estos datos al enviarlos al Front
            CategoryId = (int)entity.CategoryId       //40326
        };
    }

    // 4. UPDATE
    public async Task<bool> UpdateAsync(int id, UpdateTaskRequest request)
    {
        var task = await _context.Tasks.FindAsync(id);
        if (task == null) return false;

        // 1. Actualizar el Título
        task.Title = request.Title.Trim();

        // 2. Actualizar el Estado
        if (request.IsCompleted.HasValue)
        {
            task.IsCompleted = request.IsCompleted.Value;
        }

        // 3. Actualiza el Step (si viene en la petición)
        if (request.Step.HasValue)
        {
            task.Step = request.Step.Value;
        }

        // 4. Actualiza la Categoría (si viene en la petición)
        if (request.CategoryId.HasValue)
        {
            // Si mandan un 0 o null desde el front, se puede asignar null o procesarlo
            task.CategoryId = request.CategoryId.Value > 0 ? request.CategoryId.Value : null;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    // 5. DELETE
    public async Task<bool> DeleteAsync(int id)
    {
        var task = await _context.Tasks.FindAsync(id);
        if (task == null) return false;

        _context.Tasks.Remove(task);
        await _context.SaveChangesAsync();
        return true;
    }

    // 6. SEARCH (Lógica del Switch preservada)
    public async Task<List<TaskSearchResult>> SearchAsync(TaskSearchRequest request)
    {
        // 1. Base de la consulta
        var query = _context.Tasks.AsQueryable();

        // 2. Filtros (Where)
        if (!string.IsNullOrWhiteSpace(request.text))
            query = query.Where(t => t.Title.Contains(request.text));
        if (request.completed.HasValue)
            query = query.Where(t => t.IsCompleted == request.completed);
        if (request.step.HasValue)
            query = query.Where(t => t.Step == request.step);

        // 3. Ordenamiento (OrderBy)
        query = request.orderBy switch
        {
            "title" => query.OrderBy(t => t.Title),
            "title_desc" => query.OrderByDescending(t => t.Title),
            //  "date" => query.OrderBy(t => t.CreatedAt),
            //   "date_desc" => query.OrderByDescending(t => t.CreatedAt),
            "step" => query.OrderBy(t => t.Step),
            "step_desc" => query.OrderByDescending(t => t.Step),
            _ => query.OrderBy(t => t.Id) // _ para valores vacíos
        };

        // -Integración -
        // Aplicar la paginación a la query original
        //Ejercicio Enero 6: Agregar lógica de paginación
        var pagedQuery = query
            .Skip((request.page - 1) * request.pageSize).Take(request.pageSize);

        return await pagedQuery
            .Select(t => new TaskSearchResult
            {
                Identificador = t.Id,
                Titulo = t.Title,
                Completada = t.IsCompleted,
                PasoActual = t.Step,
                //Fecha_creacion = t.CreatedAt
            })
            .ToListAsync();
    }

    // 7. GET PAGED
    public async Task<List<TaskSearchResult>> GetPagedAsync(int page, int pageSize)
    {
        var query = _context.Tasks.OrderBy(t => t.Id).Skip((page - 1) * pageSize).Take(pageSize); // lógica del query: ordenamiento ascendente por ID

        return await query.Select(t => new TaskSearchResult // ejecución del query en la BD
        {
            // mapea solo de los campos necesarios al objeto DTO
            Identificador = t.Id,
            Titulo = t.Title,
            Completada = t.IsCompleted,
            PasoActual = t.Step,
            //  Fecha_creacion = t.CreatedAt
        }).ToListAsync();
    }

    // 8. GET WITH CATEGORY
    public async Task<List<TaskWithCategoryDto>> GetWithCategoryAsync()
    {
        return await _context.Tasks
             .Include(t => t.Category)
             .OrderBy(t => t.Id)
             .Select(t => new TaskWithCategoryDto
             {
                 Id = t.Id,
                 Title = t.Title,
                 IsCompleted = t.IsCompleted,
                 Step = t.Step,
                 //   CreatedAt = t.CreatedAt, 
                 //0 para evitar excepción
                 CategoryId = t.CategoryId ?? 0,
                 CategoryName = t.Category.Name
             })
             .ToListAsync();
    }

    // 9. IMPORT EXCEL (Lógica pesada)
    public async Task<string> ImportFromExcelAsync(IFormFile file)
    {
        var taskitems = new List<TaskItem>();

        using (var stream = new MemoryStream())
        {
            await file.CopyToAsync(stream);
            stream.Position = 0; // Nos aseguramos de ir al inicio

            using (var workbook = new XLWorkbook(stream))//Abrir el archivo dese la memoria
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
        //Validaciones
        // Opcional: filtrar duplicados por Name en la misma importación
        taskitems = taskitems
            .GroupBy(c => c.Title.ToLower())
            .Select(g => g.First())
            .ToList();

        // Opcional: evitar insertar categorías que ya existan en la BD
        var existingNames = _context.Tasks
            .Select(c => c.Title.ToLower())
            .ToHashSet();

        var newTasks = taskitems
            .Where(c => !existingNames.Contains(c.Title.ToLower()))
            .ToList();

        // Guardar en base de datos
        _context.Tasks.AddRange(newTasks);
        await _context.SaveChangesAsync();

        return $"Se importaron {taskitems.Count} tareas.";
    }

    // 10. AJAX SEARCH (050226)
    public async Task<object> AjaxSearchAsync(string? text)
    {
        var query = _context.Tasks.AsQueryable();

        if (!string.IsNullOrWhiteSpace(text))
            query = query.Where(t => t.Title.Contains(text));

        return await query
            .OrderBy(t => t.Id)
            .Take(50)
            .Select(t => new
            {
                t.Id,
                t.Title,
                t.CategoryName,
                t.IsCompleted,
                t.Step
            })
            .ToListAsync();
    }
}