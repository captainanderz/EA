using System.Collections.Generic;

namespace ProjectHorizon.ApplicationCore.DTOs
{
    public class PagedResult<T>
    {
        public int AllItemsCount { get; set; }

        public IEnumerable<T> PageItems { get; set; }
    }
}
