using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManager.Context;
using TaskManager.DTOs.Category;
using TaskManager.Models;

namespace TaskManager.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly AppDbContext _context;
        public CategoriesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CategoryDto>>> Get()
        {
            var result = await _context.Categories
                .OrderBy(c => c.Name)
                .Select(c => new CategoryDto
                {
                    Id = c.Id,
                    Name = c.Name
                })
                .ToListAsync();

            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<CategoryDto>> Create([FromBody] CreateCategoryRequest request)
        {
            // Si usas [ApiController], ModelState se valida automáticamente.
            // Igual puedes explicar que si falla devuelve 400.

            var entity = new Category
            {
                Name = request.Name.Trim()
            };

            _context.Categories.Add(entity);
            await _context.SaveChangesAsync();

            var dto = new CategoryDto
            {
                Id = entity.Id,
                Name = entity.Name
            };

            return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
        }

        [HttpGet("{id:int}")] //9/EnE/26
        public async Task<ActionResult<CategoryDto>> GetById(int id)
        {
            var entity = await _context.Categories.FindAsync(id);
            if (entity == null) return NotFound();

            var dto = new CategoryDto
            {
                Id = entity.Id,
                Name = entity.Name
            };

            return Ok(dto);
        }
        //Importación Excel 260126
        //Todo va migrado a Service
        [HttpPost("import-excel")] //Siempre se envian archivos por metodo POST
        public async Task<IActionResult> ImportFromExcel(IFormFile file) //IFormFile: Libreria incluida 260126
        {
            if (file == null || file.Length == 0)
                return BadRequest("No se recibió ningún archivo o está vacío.");

            var categories = new List<Category>();

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
                        if (isHeader)
                        {
                            // Saltar la fila de cabeceras
                            isHeader = false;
                            continue;
                        }

                        var name = row.Cell(1).GetString();      // Columna A
                        var code = row.Cell(2).GetString();      // Columna B
                        var isActiveCell = row.Cell(3).GetString(); // Columna C

                        bool isActive = true;
                        if (!string.IsNullOrWhiteSpace(isActiveCell))
                        {
                            // TRUE/FALSE, 1/0, Sí/No... aquí se puede refinar
                            bool.TryParse(isActiveCell, out isActive);
                        }

                        if (string.IsNullOrWhiteSpace(name))
                        {
                            // Se puede decidir saltar o romper
                            continue;
                        }

                        var category = new Category
                        {
                            Name = name.Trim(),
                            Code = code?.Trim(),
                            IsActive = isActive
                        };

                        categories.Add(category);
                    }
                }
            }
            //Validaciones
            // Opcional: filtrar duplicados por Name en la misma importación
            categories = categories
                .GroupBy(c => c.Name.ToLower())
                .Select(g => g.First())
                .ToList();

            // Opcional: evitar insertar categorías que ya existan en la BD
            var existingNames = _context.Categories
                .Select(c => c.Name.ToLower())
                .ToHashSet();

            var newCategories = categories
                .Where(c => !existingNames.Contains(c.Name.ToLower()))
                .ToList();


            // Guardar en base de datos
            _context.Categories.AddRange(newCategories);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = $"Se importaron {categories.Count} categorías."
            });
        }

    }
}