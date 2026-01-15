using System.ComponentModel.DataAnnotations;

namespace TaskManager.DTOs
{
    public class PagedResultDto<T>
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "La página debe ser mayor o igual a 1.")]
        public int Page { get; set; }


        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public IEnumerable<T> Items { get; set; }
    }
}