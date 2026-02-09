/*
namespace TaskManager.Models
{
    public class TaskItem
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public bool IsCompleted
        {
            get; set;
        }
        public int Step { get; set; }
     //   public DateTime CreatedAt { get; set; } = DateTime.Now; //Gestión de creación del registro  Integraciopn mia

        public bool IsDeleted { get; set; } = false;// 15 Enero 
        public int? CategoryId { get; set; } = 0; //FK y campo de categorias (campo)
        public Category? Category { get; set; } // Navegación en C#, apuntar a algún modelo (tabla)

    }
}
*/
namespace TaskManager.Models
{
    public class TaskItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty; // Evita CS8618
        public bool IsCompleted { get; set; }
        public int Step { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now; // Propiedad restaurada
        public int? CategoryId { get; set; }
        public bool IsDeleted { get; set; }

        public string CategoryName { get; set; }
        // Relación de navegación
        public virtual Category? Category { get; set; }
    }
}