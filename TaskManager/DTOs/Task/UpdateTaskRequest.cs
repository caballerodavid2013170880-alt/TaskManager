namespace TaskManager.DTOs.Task
{
    public class UpdateTaskRequest
    {
        public string Title { get; set; }
        public bool? IsCompleted { get; set; }
    }
}