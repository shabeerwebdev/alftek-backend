namespace AlfTekPro.Domain.Enums;

/// <summary>
/// Type of salary component
/// </summary>
public enum SalaryComponentType
{
    /// <summary>
    /// Earning component (e.g., Basic Salary, HRA, Allowances)
    /// Adds to gross salary
    /// </summary>
    Earning = 0,

    /// <summary>
    /// Deduction component (e.g., Tax, PF, Loan EMI)
    /// Subtracts from gross salary
    /// </summary>
    Deduction = 1
}
