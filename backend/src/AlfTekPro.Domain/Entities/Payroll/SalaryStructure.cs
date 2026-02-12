using AlfTekPro.Domain.Common;

namespace AlfTekPro.Domain.Entities.Payroll;

public class SalaryStructure : BaseTenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string ComponentsJson { get; set; } = string.Empty;
    public virtual ICollection<CoreHR.EmployeeJobHistory> EmployeeJobHistories { get; set; } = new List<CoreHR.EmployeeJobHistory>();
}
