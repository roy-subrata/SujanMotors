using System.Linq;
using System.Reflection;
using AutoPartShop.Api.Controllers.Ecommerce;
using AutoPartShop.Domain.Entities.Ecommerce;
using AutoPartShop.Domain.Repositories;
using NetArchTest.Rules;
using Xunit;

namespace AutoPartShop.Api.Tests.Architecture;

/// <summary>
/// Enforces the boundary of the Ecommerce module — same pilot pattern as HR (see
/// <see cref="HrModuleBoundaryTests"/>), applied after the module's actual duplicated/buggy
/// logic (stock consumption, discount resolution, raw Product/SalesOrder lookups) was already
/// unified with Core via <c>IStockConsumptionService</c> and <c>IDiscountResolutionService</c>.
/// This test only locks in the namespace boundary and the resulting Core-repository surface —
/// it does not itself fix anything, since there was nothing left to fix by the time it was added.
///
/// Unlike HR, Ecommerce's whole job is to place customer orders through Core's own repositories
/// (create a Customer, a SalesOrder, an Invoice, a CustomerPayment, look up a Product) — so its
/// allow-list in rule 2 is deliberately larger than HR's single-entry one. The point of rule 2
/// isn't "Ecommerce should touch nothing in Core" (impossible for an order-placement channel);
/// it's "Ecommerce's Core surface is this known, reviewed list — not raw DbContext access, and
/// not silently growing without a conscious change here."
/// </summary>
public class EcommerceModuleBoundaryTests
{
    private static readonly Assembly DomainAssembly = typeof(CartReservation).Assembly;
    // Ecommerce has no Application-layer presence of its own (all its DTOs are declared inline
    // in EcommerceController) — this is the shared Application assembly, scanned in case that
    // ever changes, anchored on an arbitrary existing type in it.
    private static readonly Assembly ApplicationAssembly = typeof(AutoPartShop.Application.DTOs.DiscountDtos.DiscountResolutionResult).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(AutoPartShop.Infrastructure.Data.Configurations.Ecommerce.CartReservationConfiguration).Assembly;
    private static readonly Assembly ApiAssembly = typeof(EcommerceController).Assembly;

    private const string EcommerceEntitiesNamespace = "AutoPartShop.Domain.Entities.Ecommerce";
    private const string EcommerceConfigurationsNamespace = "AutoPartShop.Infrastructure.Data.Configurations.Ecommerce";
    private const string EcommerceControllersNamespace = "AutoPartShop.Api.Controllers.Ecommerce";

    /// <summary>
    /// The same two structural exceptions as HrModuleBoundaryTests — see the remarks there.
    /// </summary>
    private static readonly string[] StructuralCompositionRootNames =
    [
        "AutoPartDbContext",
        "Dependency",
        "Program"
    ];

    /// <summary>
    /// Core repository interfaces Ecommerce is deliberately allowed to depend on — this is the
    /// reviewed, sanctioned set as of the module-boundary pass. Ecommerce orchestrates these to
    /// place orders exactly the way SalesOrderController does; that's the job, not a leak.
    /// Extending this list is fine when a real new need shows up — it should just be a conscious
    /// change here, not a silent one.
    /// </summary>
    private static readonly string[] AllowedCoreRepositoryDependencies =
    [
        "AutoPartShop.Domain.Repositories.ICustomerRepository",
        "AutoPartShop.Domain.Repositories.ICustomerVehicleRepository",
        "AutoPartShop.Domain.Repositories.ISalesOrderRepository",
        "AutoPartShop.Domain.Repositories.IInvoiceRepository",
        "AutoPartShop.Domain.Repositories.ICustomerPaymentRepository",
        "AutoPartShop.Domain.Repositories.IProductRepository"
    ];

    /// <summary>
    /// Rule 1 — Core must not depend on Ecommerce: no type outside the Ecommerce namespaces may
    /// reference an Ecommerce entity. Protects the core domain from accidentally growing a
    /// dependency on the storefront/checkout module.
    /// </summary>
    [Fact]
    public void Core_Types_Should_Not_Depend_On_Ecommerce_Entities()
    {
        var result = Types.InAssemblies([DomainAssembly, ApplicationAssembly, InfrastructureAssembly, ApiAssembly])
            .That()
            .DoNotResideInNamespace(EcommerceEntitiesNamespace)
            .And().DoNotResideInNamespace(EcommerceConfigurationsNamespace)
            .And().DoNotResideInNamespace(EcommerceControllersNamespace)
            .And().DoNotHaveName(StructuralCompositionRootNames)
            .ShouldNot()
            .HaveDependencyOnAny(EcommerceEntitiesNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful,
            "Core (non-Ecommerce) types must not depend on Ecommerce entities, " +
            "but the following do: " + string.Join(", ", result.FailingTypeNames ?? []));
    }

    /// <summary>
    /// Rule 2 — Ecommerce's Core-repository surface is the reviewed allow-list above, not an
    /// unbounded or silently-growing set.
    /// </summary>
    [Fact]
    public void Ecommerce_Types_Should_Not_Depend_On_Core_Repository_Interfaces_Except_Allow_Listed()
    {
        var coreRepositoryInterfaces = DomainAssembly
            .GetTypes()
            .Where(t => t.IsInterface
                && t.Namespace == "AutoPartShop.Domain.Repositories" // root Core namespace only
                && t.Name.StartsWith('I') && t.Name.EndsWith("Repository")
                && !AllowedCoreRepositoryDependencies.Contains(t.FullName))
            .Select(t => t.FullName!)
            .ToArray();

        // Sanity-check the reflection query itself, so a refactor that renames the
        // Domain.Repositories namespace can't silently turn this test into a no-op that always
        // passes.
        Assert.NotEmpty(coreRepositoryInterfaces);
        Assert.DoesNotContain(typeof(ICustomerRepository).FullName, coreRepositoryInterfaces);

        var result = Types.InAssemblies([ApiAssembly])
            .That()
            .ResideInNamespace(EcommerceControllersNamespace)
            .ShouldNot()
            .HaveDependencyOnAny(coreRepositoryInterfaces)
            .GetResult();

        Assert.True(result.IsSuccessful,
            "Ecommerce controllers must not depend directly on Core repository interfaces " +
            "outside the allow-list (" + string.Join(", ", AllowedCoreRepositoryDependencies) + "), " +
            "but the following do: " + string.Join(", ", result.FailingTypeNames ?? []));
    }
}
