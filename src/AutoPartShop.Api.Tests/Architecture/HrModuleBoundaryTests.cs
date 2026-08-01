using System.Linq;
using System.Reflection;
using AutoPartShop.Api.Controllers.HR;
using AutoPartShop.Domain.Entities.HR;
using AutoPartShop.Domain.Repositories;
using AutoPartShop.Infrastructure.Repositories.HR;
using NetArchTest.Rules;
using Xunit;

namespace AutoPartShop.Api.Tests.Architecture;

/// <summary>
/// Enforces the boundary of the HR module — the pilot for a future "modular monolith" split
/// (HR, Ecommerce, future CRM) that could eventually be licensed/deployed independently.
///
/// One cross-module coupling was found while auditing the HR code and is deliberately
/// allowed (not hidden):
///
/// - Employee.UserId (Guid?, no navigation property) resolves against ApplicationUser/the
///   Identity "Users" table via a direct AutoPartDbContext query
///   (EmployeeReadRepository.GetLinkableUsers) and via UserManager&lt;ApplicationUser&gt;
///   (EmployeesController). Neither path goes through a Domain.Repositories.I*Repository
///   interface — there is no IApplicationUserRepository in this codebase — so this coupling
///   is not, and does not need to be, part of the allow-list in rule 2 below. It is only
///   documented here for completeness.
///
/// Two other couplings that were previously undocumented gaps have since been fixed properly
/// (not hidden by loosening this test):
///
/// - PayrollController marks a payroll run PAID and posts a SALARIES DailyExpense (a Core
///   entity) in the same transaction via IPayrollRepository.PayAsync — and SalaryAdvancesController
///   posts a SALARY_ADVANCE DailyExpense via ISalaryAdvanceRepository.GiveAsync/CancelAsync.
///   Both repository implementations now write through IDailyExpenseRepository.AddAsync (still
///   the deliberate, allow-listed HR→Core write below) rather than a raw DbContext.Add call.
///
/// - TillSessionController (a Core controller) used to query Employee/Shift directly through
///   the shared AutoPartDbContext to build a cashier display string and suggest a shift for the
///   Open Till form. It now depends only on Api.Services.ICashierProfileService — a Core-owned
///   interface implemented by the HR-namespaced adapter Api.Services.HR.CashierProfileService,
///   which resolves Employee/Shift via HR's own IEmployeeRepository/IShiftRepository. This is
///   the sanctioned dependency inversion: Core depends on an interface it owns, HR supplies the
///   implementation.
/// </summary>
public class HrModuleBoundaryTests
{
    private static readonly Assembly DomainAssembly = typeof(Employee).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(AutoPartShop.Application.HR.IEmployeeReadRepository).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(EmployeeRepository).Assembly;
    private static readonly Assembly ApiAssembly = typeof(EmployeesController).Assembly;

    private const string HrEntitiesNamespace = "AutoPartShop.Domain.Entities.HR";
    private const string HrRepositoriesNamespace = "AutoPartShop.Domain.Repositories.HR";
    private const string HrApplicationNamespace = "AutoPartShop.Application.HR";
    private const string HrInfrastructureRepositoriesNamespace = "AutoPartShop.Infrastructure.Repositories.HR";
    private const string HrConfigurationsNamespace = "AutoPartShop.Infrastructure.Data.Configurations.HR";
    private const string HrControllersNamespace = "AutoPartShop.Api.Controllers.HR";

    /// <summary>
    /// Api.Services.HR.CashierProfileService is the sanctioned dependency-inversion adapter for
    /// ICashierProfileService (a Core-owned interface — see Api/Services/ICashierProfileService.cs).
    /// It lives in the Api project rather than Infrastructure/Repositories/HR because
    /// Infrastructure has no project reference back to Api (Api → Infrastructure only, so the
    /// reverse would be a circular project reference); it is namespaced under ".HR" to keep it
    /// grouped with the rest of the HR module's Api-layer footprint (alongside Api/Controllers/HR).
    /// </summary>
    private const string HrApiServicesNamespace = "AutoPartShop.Api.Services.HR";

    /// <summary>
    /// The two known-structural exceptions to rule 1, excluded by type name rather than
    /// namespace: <c>AutoPartDbContext</c> declares a <c>DbSet&lt;T&gt;</c> for every module
    /// (inherent to the still-shared DbContext), and <c>Dependency</c>/<c>Program</c> are the
    /// Infrastructure/Api composition roots that DI-register every repository and service,
    /// HR included. Excluding these by name means the rule asserts "no *other* type outside HR
    /// depends on HR" — meaningfully green rather than permanently, ambiguously red.
    /// </summary>
    private static readonly string[] StructuralCompositionRootNames =
    [
        "AutoPartDbContext",
        "Dependency",
        "Program"
    ];

    /// <summary>
    /// The one deliberate HR→Core repository-interface coupling this pilot allows. See the
    /// class remarks above for why it is kept even though it is currently unused by HR code.
    /// </summary>
    private static readonly string[] AllowedCoreRepositoryDependencies =
    [
        "AutoPartShop.Domain.Repositories.IDailyExpenseRepository"
    ];

    /// <summary>
    /// Rule 1 — Core must not depend on HR: no type outside the HR namespaces may reference an
    /// HR entity or an HR repository interface. This protects the core domain from accidentally
    /// growing a dependency on the HR module.
    /// </summary>
    [Fact]
    public void Core_Types_Should_Not_Depend_On_Hr_Entities_Or_Repositories()
    {
        var result = Types.InAssemblies([DomainAssembly, ApplicationAssembly, InfrastructureAssembly, ApiAssembly])
            .That()
            .DoNotResideInNamespace(HrEntitiesNamespace)
            .And().DoNotResideInNamespace(HrRepositoriesNamespace)
            .And().DoNotResideInNamespace(HrApplicationNamespace)
            .And().DoNotResideInNamespace(HrInfrastructureRepositoriesNamespace)
            .And().DoNotResideInNamespace(HrConfigurationsNamespace)
            .And().DoNotResideInNamespace(HrControllersNamespace)
            .And().DoNotResideInNamespace(HrApiServicesNamespace)
            .And().DoNotHaveName(StructuralCompositionRootNames)
            .ShouldNot()
            .HaveDependencyOnAny(HrEntitiesNamespace, HrRepositoriesNamespace, HrApplicationNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful,
            "Core (non-HR) types must not depend on HR entities or HR repository interfaces, " +
            "but the following do: " + string.Join(", ", result.FailingTypeNames ?? []));
    }

    /// <summary>
    /// Rule 2 — HR must not reach into Core's data layer directly: HR controllers and HR
    /// infrastructure repositories must not depend on any Domain.Repositories.I*Repository
    /// interface that lives outside Repositories.HR, except the explicit allow-list above.
    /// </summary>
    [Fact]
    public void Hr_Types_Should_Not_Depend_On_Core_Repository_Interfaces_Except_Allow_Listed()
    {
        var coreRepositoryInterfaces = DomainAssembly
            .GetTypes()
            .Where(t => t.IsInterface
                && t.Namespace == "AutoPartShop.Domain.Repositories" // root Core namespace only — excludes .HR
                && t.Name.StartsWith('I') && t.Name.EndsWith("Repository")
                && !AllowedCoreRepositoryDependencies.Contains(t.FullName))
            .Select(t => t.FullName!)
            .ToArray();

        // Sanity-check the reflection query itself, so a refactor that renames the Domain.Repositories
        // namespace can't silently turn this test into a no-op that always passes.
        Assert.NotEmpty(coreRepositoryInterfaces);
        Assert.DoesNotContain(typeof(IDailyExpenseRepository).FullName, coreRepositoryInterfaces);

        var result = Types.InAssemblies([InfrastructureAssembly, ApiAssembly])
            .That()
            .ResideInNamespace(HrInfrastructureRepositoriesNamespace)
            .Or().ResideInNamespace(HrControllersNamespace)
            .ShouldNot()
            .HaveDependencyOnAny(coreRepositoryInterfaces)
            .GetResult();

        Assert.True(result.IsSuccessful,
            "HR controllers/repositories must not depend directly on Core repository interfaces " +
            "outside the allow-list (" + string.Join(", ", AllowedCoreRepositoryDependencies) + "), " +
            "but the following do: " + string.Join(", ", result.FailingTypeNames ?? []));
    }
}
