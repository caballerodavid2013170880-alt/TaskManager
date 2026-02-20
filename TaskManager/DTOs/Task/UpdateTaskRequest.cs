namespace TaskManager.DTOs.Task
{
    public class UpdateTaskRequest
    {
        public string Title { get; set; }
        public bool? IsCompleted { get; set; }

        // recepción de Step y la Categoría
        public int? Step { get; set; }
        public int? CategoryId { get; set; }
    }
}