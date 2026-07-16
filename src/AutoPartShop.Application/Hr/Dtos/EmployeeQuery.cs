using AutoPartShop.Application.Common;

namespace AutoPartShop.Application.Hr.Dtos
{
    public class EmployeeQuery : BaseQuery
    {
        public string Status { get; set; } = "";
        public string Department { get; set; } = "";
    }
}
