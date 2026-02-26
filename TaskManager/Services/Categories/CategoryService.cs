using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using TaskManager.Context;
using TaskManager.DTOs.Category;
using TaskManager.Interfaces.Categories;
using TaskManager.Models;

namespace TaskManager.Services.Categories
{
    public class CategoryService : ICategoryService
    {
        private readonly AppDbContext _context;

        // Inyectamos la base de datos al servicio
        public CategoryService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<CategoryDto>> GetAllAsync()
        {
            return await _context.Categories
                .OrderBy(c => c.Name)
                .Select(c => new CategoryDto { Id = c.Id, Name = c.Name })
                .ToListAsync();
        }

        public async Task<CategoryDto?> GetByIdAsync(int id)
        {
            var entity = await _context.Categories.FindAsync(id);
            if (entity == null) return null;

            return new CategoryDto { Id = entity.Id, Name = entity.Name };
        }

        public async Task<CategoryDto> CreateAsync(CreateCategoryRequest request)
        {
            var entity = new Category { Name = request.Name.Trim() };
            _context.Categories.Add(entity);
            await _context.SaveChangesAsync();

            return new CategoryDto { Id = entity.Id, Name = entity.Name };
        }

        public async Task<string> ImportFromExcelAsync(IFormFile file)
        {
            // Toda la lógica pesada de Excel se movió aquí
            var categories = new List<Category>();

            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                stream.Position = 0;

                using (var workbook = new XLWorkbook(stream))
                {
                    var worksheet = workbook.Worksheets.First();
                    var rows = worksheet.RangeUsed().RowsUsed();
                    bool isHeader = true;

                    foreach (var row in rows)
                    {
                        if (isHeader) { isHeader = false; continue; }

                        var name = row.Cell(1).GetString();
                        var code = row.Cell(2).GetString();
                        var isActiveCell = row.Cell(3).GetString();

                        bool isActive = true;
                        if (!string.IsNullOrWhiteSpace(isActiveCell))
                            bool.TryParse(isActiveCell, out isActive);

                        if (string.IsNullOrWhiteSpace(name)) continue;

                        categories.Add(new Category
                        {
                            Name = name.Trim(),
                            Code = code?.Trim(),
                            IsActive = isActive
                        });
                    }
                }
            }

            categories = categories.GroupBy(c => c.Name.ToLower()).Select(g => g.First()).ToList();
            var existingNames = _context.Categories.Select(c => c.Name.ToLower()).ToHashSet();
            var newCategories = categories.Where(c => !existingNames.Contains(c.Name.ToLower())).ToList();

            _context.Categories.AddRange(newCategories);
            await _context.SaveChangesAsync();

            return $"Se importaron {newCategories.Count} categorías nuevas.";
        }
    }
}