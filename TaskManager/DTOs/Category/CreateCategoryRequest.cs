using System.ComponentModel.DataAnnotations;

namespace TaskManager.DTOs.Category
{
    public class CreateCategoryRequest
    {
        [Required(ErrorMessage = "El titulo es obligatorio.")]
        [MaxLength(100, ErrorMessage = "El titulo no puede superar los 200 caracteres.")] //Ubicados antes de la propiedad a la quese van a aplicar
        public string Name { get; set; }
        
    }
}
