using Microsoft.AspNetCore.Mvc;
using TaskManager.DTOs;
using TaskManager.DTOs.Task;
using TaskManager.Interfaces.Tasks;
// using TaskManagerAPI.Utilities.Exceptions; 

namespace TaskManager.Controllers
{
    [ApiController]
    [Route("api/tasks")]
    public class TaskItemsController : ControllerBase
    {
        // Se elimina _context. El controlador ya no toca la BD.
        private readonly ITaskService _taskService; // 13/Enero

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
            var dto = await _taskService.GetByIdAsync(id);
            if (dto == null) return NotFound();

            return Ok(dto);
        }

        [HttpPost]
        public async Task<ActionResult<TaskItemResponse>> Create([FromBody] CreateTaskRequest request)
        {
            if (request == null) return BadRequest("Body requerido.");
            if (string.IsNullOrWhiteSpace(request.Title)) return BadRequest("Title es requerido.");

            try
            {
                var dto = await _taskService.CreateAsync(request);
                return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
            }
            catch (Exception ex) // Captura excepciones de negocio (ej. Categoría no existe)
            {
                return BadRequest(ex.Message);
            }
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

        //Integración
        [HttpGet("search")]
        public async Task<ActionResult> Search([FromQuery] TaskSearchRequest request)
        {
            var results = await _taskService.SearchAsync(request);
            return Ok(results);
        }

        [HttpGet("paged")] // Método que responde peticiones Get en la ruta nombrada como "paged"
        public async Task<ActionResult<IEnumerable<TaskSearchResult>>> GetPaged(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _taskService.GetPagedAsync(page, pageSize);
            return Ok(result);
        }

        [HttpGet("with-category")]
        public async Task<ActionResult<IEnumerable<TaskWithCategoryDto>>> GetWithCategory()
        {
            var result = await _taskService.GetWithCategoryAsync();
            return Ok(result);
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

            try
            {
                var message = await _taskService.ImportFromExcelAsync(file);
                return Ok(new { Message = message });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        //Fin Importa Excel2

        //050226
        [HttpGet("ajax-search")]
        public async Task<IActionResult> AjaxSearch([FromQuery] string? text)
        {
            var results = await _taskService.AjaxSearchAsync(text);
            return Ok(results);
        }
    }
}