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
        public DateTime CreatedAt { get; set; } = DateTime.Now; //Gestión de creación del registro

        public bool IsDeleted { get; set; } = false;// 15 Enero 
        public int? CategoryId { get; set; } = 0; //FK y campo de categorias (campo)
        public Category? Category { get; set; } // Navegación en C#, apuntar a algún modelo (tabla)
        
    }
}