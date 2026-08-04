using AutoPartShop.Application.Common;
using AutoPartShop.Domain.Enums.HR;

namespace AutoPartShop.Application.HR.Dtos
{
    public class EmployeeQuery : BaseQuery
    {
        public EmployeeStatus? Status { get; set; }
        public string Department { get; set; } = "";
    }
}
