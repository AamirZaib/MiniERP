using System;
using System.Collections.Generic;
using System.Text;

namespace MiniERP.Application.DTOs
{
    public class CategoryResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class CreateCategoryRequest
    {
        public string Name { get; set; } = string.Empty;
    }
    public class UpdateCategoryRequest
    {
        public string Name { get; set; } = string.Empty;
    }
}
