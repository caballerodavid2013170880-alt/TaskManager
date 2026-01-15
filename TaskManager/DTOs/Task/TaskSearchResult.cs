namespace TaskManager.DTOs.Task
{
    public class TaskSearchResult
    {
        public int Identificador { get; set; }
        public string Titulo { get; set; }
        public bool Completada
        {
            get; set;
        }
        public int PasoActual { get; set; }
        public DateTime Fecha_creacion { get; set; } = DateTime.Now; //Gestión de creación del registro
    }
}