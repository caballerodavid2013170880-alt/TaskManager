namespace TaskManager.DTOs.Task
{
    public class TaskItemResponse
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public bool IsCompleted
        {
            get; set;
        }
        //270226 Se agregan al modelo para completar Edit Partial y TaskFormViewModel
        public int Step { get; set; }
        public int CategoryId { get; set; }
    }
}