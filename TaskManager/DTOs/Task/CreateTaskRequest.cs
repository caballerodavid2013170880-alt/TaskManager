using System.ComponentModel.DataAnnotations;

namespace TaskManager.DTOs
{
    //Recepción al tratar de guardar un registro
    public class CreateTaskRequest
    {
        //Extiende a todos los lugares donde s eusa CreateTaskRequest
        [Required(ErrorMessage = "El titulo es obligatorio.")]
        [MaxLength(200, ErrorMessage = "El titulo no puede superar los 200 caracteres.")]
        public string Title { get; set; }
        public bool IsCompleted { get; set; }

        [Required(ErrorMessage = "CategoryId es requerido.")]
        public int CategoryId { get; set; }
        public int Step { get; set; }
    }
}