using AutoPartShop.Application.Common;

namespace AutoPartShop.Application.HR.Dtos
{
    public class EmployeeQuery : BaseQuery
    {
        public string Status { get; set; } = "";
        public string Department { get; set; } = "";
    }
}
