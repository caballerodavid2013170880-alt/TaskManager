using Microsoft.AspNetCore.Mvc;
using TaskManager.DTOs;
using TaskManager.DTOs.Task;
using TaskManager.Interfaces.Tasks;

namespace TaskManager.Controllers
{
    [ApiController]
    [Route("api/tasks")]
    public class TaskItemsController : ControllerBase
    {
        private readonly ITaskService _taskService; // 13/Enero

        // Inyectamos solo el servicio, ya no el DbContext
        public TaskItemsController(ITaskService taskService)
        {
            _taskService = taskService;
        }

        [HttpGet]
        public async Task<ActionResult<List<TaskItemResponse>>> Get()
        {
            var tasks = await _taskService.GetAllAsync();
            return Ok(tasks);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<TaskItemResponse>> GetById(int id)
        {
            var task = await _taskService.GetByIdAsync(id);
            if (task == null) return NotFound();
            return Ok(task);
        }

        [HttpPost]
        public async Task<ActionResult<TaskItemResponse>> Create([FromBody] CreateTaskRequest request)
        {
            if (request == null) return BadRequest("Body requerido.");
            if (string.IsNullOrWhiteSpace(request.Title)) return BadRequest("Title es requerido.");

            // La validación de CategoryId ahora ocurre dentro del servicio y lanza BusinessException si falla
            var dto = await _taskService.CreateAsync(request);

            return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
        }

        // Modificación PUT para ampliar envio a FRONT 190226
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateTaskRequest request)
        {
            if (request == null) return BadRequest("Body requerido.");
            if (string.IsNullOrWhiteSpace(request.Title)) return BadRequest("Title es requerido.");

            var updated = await _taskService.UpdateAsync(id, request);
            if (!updated) return NotFound();

            return NoContent(); // 204
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _taskService.DeleteAsync(id);
            if (!deleted) return NotFound();

            return NoContent();
        }

        // Integración
        // Vista en uso, compatible con AdvancedSearchAsync
        [HttpGet("search")]
        public async Task<ActionResult> Search([FromQuery] TaskSearchRequest request)
        {
            // Mapeamos el request antiguo al nuevo AdvancedSearchAsync del servicio
            // Nota: request.page y request.pageSize vienen en el objeto
            var result = await _taskService.AdvancedSearchAsync(
                request.text,
                request.completed,
                request.step,
                null, // categoryId no estaba en el request simple
                null, // categoryName no estaba
                request.page > 0 ? request.page : 1,
                request.pageSize > 0 ? request.pageSize : 10
            );

            // El antiguo endpoint devolvía una lista plana, aquí devolvemos Items para mantener estructura similar
            return Ok(result.Items);
        }

        // Método que responde peticiones GET en la ruta nombrada como "paged"
        [HttpGet("paged")]
        public async Task<ActionResult<IEnumerable<TaskSearchResult>>> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            // Reutilizamos la lógica centralizada
            var result = await _taskService.AdvancedSearchAsync(null, null, null, null, null, page, pageSize);

            // Mapeo rápido para cumplir con el tipo de retorno antiguo TaskSearchResult
            var mapped = result.Items.Select(t => new TaskSearchResult
            {
                Identificador = t.Id,
                Titulo = t.Title,
                Completada = t.IsCompleted,
                PasoActual = t.Step
            });

            return Ok(mapped);
        }

        [HttpGet("with-category")]
        public async Task<ActionResult<IEnumerable<TaskWithCategoryDto>>> GetWithCategory()
        {
            // Esto podría optimizarse en un método específico del servicio si se usa mucho
            // AdvancedSearch sin filtros y gran paginación
            var result = await _taskService.AdvancedSearchAsync(null, null, null, null, null, 1, 1000);
            return Ok(result.Items);
        }

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
        {
            // 13 enero:
            // throw new BusinessException("La categoría no existe."); // 1602 Interrupción
            if (page <= 0) return BadRequest("Page debe ser mayor a 0.");
            if (pageSize <= 0 || pageSize > 100) return BadRequest("PageSize debe estar entre 1 y 100.");

            var result = await _taskService.AdvancedSearchAsync(text, completed, step, categoryId, categoryName, page, pageSize);
            return Ok(result);
        }

        // Mi Importar Excel2 2601226
        // Importación Excel 260126
        [HttpPost("import-excel")] // Siempre se envian archivos por metodo POST
        public async Task<IActionResult> ImportFromExcel2(IFormFile file) // IFormFile: Libreria incluida 260126
        {
            if (file == null || file.Length == 0)
                return BadRequest("No se recibió ningún archivo o está vacío.");

            // Toda la lógica compleja se movió al servicio
            var message = await _taskService.ImportFromExcelAsync(file);

            return Ok(new { Message = message });
        }

        // 050226
        [HttpGet("ajax-search")]
        public async Task<IActionResult> AjaxSearch([FromQuery] string? text)
        {
            var results = await _taskService.AjaxSearchAsync(text);

            // Mapeo anónimo para mantener la firma exacta que esperaba el frontend anterior
            var anonymousResult = results.Select(t => new
            {
                t.Id,
                t.Title,
                t.CategoryName,
                t.IsCompleted,
                t.Step
            });

            return Ok(anonymousResult);
        }
    }
}