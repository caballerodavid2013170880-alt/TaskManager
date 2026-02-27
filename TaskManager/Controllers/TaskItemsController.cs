using ClosedXML.Excel;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Drawing.Diagrams;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManager.Context;
using TaskManager.DTOs;
using TaskManager.DTOs.Task;
using TaskManager.Interfaces.Tasks;
using TaskManager.Models;
using TaskManagerAPI.Utilities.Exceptions;

namespace TaskManager.Controllers
{
    [ApiController]
    [Route("api/tasks")]
    public class TaskItemsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ITaskService _taskService; // 13/Enero
        public TaskItemsController(AppDbContext context, ITaskService taskService)
        {
            _context = context;
            _taskService = taskService;
        }

        [HttpGet]

        public async Task<ActionResult<List<TaskItem>>> Get()
        {
            var tasks = await _context.Tasks // Access the TaskItems DbSet from the database context
            .Select(t => new TaskItemResponse
            {   
                    Id = t.Id,
                    Title = t.Title,
                    IsCompleted = t.IsCompleted
                })
              .ToListAsync();
                return Ok(tasks);
        }
        [HttpGet("{id:int}")]
        public async Task<ActionResult<TaskItemResponse>> GetById(int id)
        {
            var task = await _context.Tasks.FindAsync(id);

            if (task == null)
                return NotFound();

            var dto = new TaskItemResponse
            {
                Id = task.Id,
                Title = task.Title,
                IsCompleted = task.IsCompleted,
                Step = task.Step,                       //270226 Se agrega porque faltan estos datos al enviarlos al F
                CategoryId = (int)task.CategoryId       //270226
            };

            return Ok(dto);
        }
        
        [HttpPost]
        public async Task<ActionResult<TaskItemResponse>> Create([FromBody] CreateTaskRequest request)
        { 
            if (request == null) 
                return BadRequest("Body requerido."); 
            
            if (string.IsNullOrWhiteSpace(request.Title)) 
                return BadRequest("Title es requerido.");

            //throw new BusinessException("La categoría no existe.", 404); // 15 Enero Interrupción

            var categoryExists = await _context.Categories.AnyAsync(c => c.Id == request.CategoryId); 
            if (!categoryExists) 
                return BadRequest("CategoryId no existe.");

            var entity = new TaskItem 
            { 
                Title = request.Title.Trim(), 
                IsCompleted = false, 
                CategoryId =   request.CategoryId,
                CategoryName = ""
            }; 
            //Registro
                _context.Tasks.Add(entity); 
            await _context.SaveChangesAsync(); 
            var dto = new TaskItemResponse 
            {   Id = entity.Id, 
                Title = entity.Title, 
                IsCompleted = entity.IsCompleted 
            }; 
            return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto); 
        }
        // Modificación PUT para ampliar envio a FRONT 190226

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateTaskRequest request)
        {
            var task = await _context.Tasks.FindAsync(id);
            if (task == null) return NotFound();

            if (request == null) return BadRequest("Body requerido.");
            if (string.IsNullOrWhiteSpace(request.Title)) return BadRequest("Title es requerido.");

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

            return NoContent(); // 204
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var task = await _context.Tasks.FindAsync(id);
            if (task == null) return NotFound();

            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        /*
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<TaskSearchResult>>> Search(
            
            [FromQuery] TaskSearchRequest request //Provenientes de los parametros
)
        {
            var query = _context.Tasks.AsQueryable(); // AsQueryable: Permite agregar condiciones sin ejecutar consultas

            if (!string.IsNullOrWhiteSpace(request.text))
                query = query.Where(t => t.Title.Contains(request.text)); //Fitro por titulo con contenido

            if (request.completed.HasValue)
                query = query.Where(t => t.IsCompleted == request.completed); //Filtro con el estatus completado

            if (request.step.HasValue)
                query = query.Where(t => t.Step == request.step); //Filtro por paso
           
                query = request.orderBy switch //Asignar a query el valor de orderBy (filtros)
            {
                "title" => query.OrderBy(t => t.Title),
                "title_desc" => query.OrderByDescending(t => t.Title),
                "date" => query.OrderBy(t => t.CreatedAt),
                "date_desc" => query.OrderByDescending(t => t.CreatedAt),
                "step" => query.OrderBy(t => t.Step),
                "step_desc" => query.OrderByDescending(t => t.Step),
                _ => query.OrderBy(t => t.Id) //Para valores vacios
            };

            var results = await query
                .Select(t => new TaskSearchResult

                {
                    Identificador = t.Id,
                    Titulo = t.Title,
                    Completada = t.IsCompleted,
                    PasoActual = t.Step,
                    Fecha_creacion = t.CreatedAt
                })
                .ToListAsync();

            return Ok(results);
        }
        [HttpGet("paged")] 
        public async Task<ActionResult<IEnumerable<TaskSearchResult>>> 
            GetPaged(
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 3) 
          {
            var query = _context.Tasks
                .OrderBy(t => t.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize); 
            var result = await query.Select(t => new TaskSearchResult 
                {
                Identificador = t.Id,
                Titulo = t.Title,
                Completada = t.IsCompleted,
                PasoActual = t.Step,
                Fecha_creacion = t.CreatedAt 
            })
                .ToListAsync(); 
            return Ok(result); 
        }
        */
        //Integración
        [HttpGet("search")]
        public async Task<ActionResult> Search([FromQuery] TaskSearchRequest request)
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

            var results = await pagedQuery
                .Select(t => new TaskSearchResult
                {
                    Identificador = t.Id,
                    Titulo = t.Title,
                    Completada = t.IsCompleted,
                    PasoActual = t.Step,
                    //Fecha_creacion = t.CreatedAt
                })
                .ToListAsync();

                return Ok(results);
        }

        [HttpGet("paged")] // Método que responde peticiones Get en la ruta nombrada como "paged"
        public async Task<ActionResult<IEnumerable<TaskSearchResult>>> GetPaged(
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 10)
        {
            var query = _context.Tasks.OrderBy(t => t.Id).Skip((page - 1) * pageSize).Take(pageSize); // lógica del query: ordenamiento ascendente por ID
            var result = await query.Select(t => new TaskSearchResult // ejecución del query en la BD
            {
                // mapea solo de los campos necesarios al objeto DTO
                Identificador = t.Id,
                Titulo = t.Title,
                Completada = t.IsCompleted,
                PasoActual = t.Step,
              //  Fecha_creacion = t.CreatedAt
            }).ToListAsync(); // el resultado (result) se convierte a una lista y se envía a SQL

            return Ok(result);
        }


        [HttpGet("with-category")] 
        public async Task<ActionResult<IEnumerable<TaskWithCategoryDto>>> GetWithCategory() 
        { 
            var result = await _context.Tasks
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
            return Ok(result); }

        [HttpGet("advanced-search")]
        public async Task<ActionResult<PagedResultDto<TaskWithCategoryDto>>> AdvancedSearch(
            [FromQuery] string? text,
            [FromQuery] bool? completed,
            [FromQuery] int? step,
            [FromQuery] int? categoryId,
            [FromQuery] string? categoryName,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 5
        )
            
           //13 enero:
            {
           // throw new BusinessException("La categoría no existe."); // 1602 Interrupción

            //throw new Exception("La categoría no existe."); //14Enero2026
            if (page <= 0) return BadRequest("Page debe ser mayor a 0.");
            if (pageSize <= 0 || pageSize > 100) 
                return BadRequest("PageSize debe estar entre 1 y 100.");

            var result = await _taskService.AdvancedSearchAsync(
                text, completed, step, categoryId, categoryName, page, pageSize);

            return Ok(result);

            }                  
    


     //Mi Importar Excel2 2601226

        //Importación Excel 260126
        [HttpPost("import-excel")] //Siempre se envian archivos por metodo POST
        public async Task<IActionResult> ImportFromExcel2(IFormFile file) //IFormFile: Libreria incluida 260126
        {
            if (file == null || file.Length == 0)
                return BadRequest("No se recibió ningún archivo o está vacío.");

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

            return Ok(new
            {
                Message = $"Se importaron {taskitems.Count} tareas."
            });
        }
        //Fin Importa Excel2


        //050226
        [HttpGet("ajax-search")]
        public async Task<IActionResult> AjaxSearch([FromQuery] string? text)
        {
            var query = _context.Tasks.AsQueryable();

            if (!string.IsNullOrWhiteSpace(text))
                query = query.Where(t => t.Title.Contains(text));

            var results = await query
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

            return Ok(results);
        }


    }
}