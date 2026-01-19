namespace TaskManager.Models
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>(); //Si null, indica que la lista esta vacia

        public bool IsDeleted { get; set; } = false;
    }
}
