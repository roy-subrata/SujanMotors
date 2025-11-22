using AutoPartShop.Domain.Entities;

namespace AutoPartShop.Infrastructure.Repositories;

/// <summary>
/// In-memory implementation of CategoryRepository
/// TODO: Replace with Entity Framework Core implementation when database is configured
/// </summary>
public class CategoryRepository : ICategoryRepository
{
    // In-memory storage for demonstration
    private static readonly List<Category> _categories = new();

    static CategoryRepository()
    {
        // Initialize with demo data - multi-level category hierarchy
        InitializeDemoData();
    }

    private static void InitializeDemoData()
    {
        // Root Categories (Level 1)
        var engineParts = Category.Create("Engine Parts", "Components related to vehicle engines", "ENG-001", 1);
        var electrical = Category.Create("Electrical", "Electrical components and wiring", "ELE-001", 2);
        var brakeSystem = Category.Create("Brake System", "Brake pads, rotors, and related components", "BRK-001", 3);
        var suspension = Category.Create("Suspension", "Suspension parts and shock absorbers", "SUS-001", 4);
        var transmission = Category.Create("Transmission", "Gearbox and transmission components", "TRN-001", 5);

        _categories.Add(engineParts);
        _categories.Add(electrical);
        _categories.Add(brakeSystem);
        _categories.Add(suspension);
        _categories.Add(transmission);

        // Level 2 - Engine Parts subcategories
        var dieselEngines = Category.Create("Diesel Engines", "Diesel engine components and parts", "DIES-001", 1, engineParts.Id, "Engine Parts > Diesel Engines", 1);
        var petrolEngines = Category.Create("Petrol Engines", "Petrol engine components and parts", "PETR-001", 2, engineParts.Id, "Engine Parts > Petrol Engines", 1);

        _categories.Add(dieselEngines);
        _categories.Add(petrolEngines);
        engineParts.SubCategories.Add(dieselEngines);
        engineParts.SubCategories.Add(petrolEngines);
        engineParts.IncrementChildCount();
        engineParts.IncrementChildCount();

        // Level 3 - Diesel Engines subcategories
        var pistons = Category.Create("Pistons", "Piston assemblies and components", "PIST-001", 1, dieselEngines.Id, "Engine Parts > Diesel Engines > Pistons", 2);
        var valves = Category.Create("Valves", "Engine valves and valve assemblies", "VALV-001", 2, dieselEngines.Id, "Engine Parts > Diesel Engines > Valves", 2);

        _categories.Add(pistons);
        _categories.Add(valves);
        dieselEngines.SubCategories.Add(pistons);
        dieselEngines.SubCategories.Add(valves);
        dieselEngines.IncrementChildCount();
        dieselEngines.IncrementChildCount();

        // Level 4 - Pistons subcategories
        var standardPistons = Category.Create("Standard Pistons", "Standard replacement pistons", "STND-PIST-001", 1, pistons.Id,
            "Engine Parts > Diesel Engines > Pistons > Standard Pistons", 3);
        var performancePistons = Category.Create("Performance Pistons", "High-performance forged pistons", "PERF-PIST-001", 2, pistons.Id,
            "Engine Parts > Diesel Engines > Pistons > Performance Pistons", 3);

        _categories.Add(standardPistons);
        _categories.Add(performancePistons);
        pistons.SubCategories.Add(standardPistons);
        pistons.SubCategories.Add(performancePistons);
        pistons.IncrementChildCount();
        pistons.IncrementChildCount();

        // Level 2 - Electrical subcategories
        var battery = Category.Create("Battery Systems", "Car batteries and battery related parts", "BATT-001", 1, electrical.Id,
            "Electrical > Battery Systems", 1);
        var alternators = Category.Create("Alternators", "Alternators and charging systems", "ALT-001", 2, electrical.Id,
            "Electrical > Alternators", 1);
        var starters = Category.Create("Starters", "Starter motors and components", "STAR-001", 3, electrical.Id,
            "Electrical > Starters", 1);

        _categories.Add(battery);
        _categories.Add(alternators);
        _categories.Add(starters);
        electrical.SubCategories.Add(battery);
        electrical.SubCategories.Add(alternators);
        electrical.SubCategories.Add(starters);
        electrical.IncrementChildCount();
        electrical.IncrementChildCount();
        electrical.IncrementChildCount();

        // Level 3 - Battery Systems subcategories
        var startingBattery = Category.Create("Starting Batteries", "Standard car batteries for starting", "START-BATT-001", 1, battery.Id,
            "Electrical > Battery Systems > Starting Batteries", 2);
        var agmBattery = Category.Create("AGM Batteries", "Absorbent Glass Mat batteries", "AGM-BATT-001", 2, battery.Id,
            "Electrical > Battery Systems > AGM Batteries", 2);

        _categories.Add(startingBattery);
        _categories.Add(agmBattery);
        battery.SubCategories.Add(startingBattery);
        battery.SubCategories.Add(agmBattery);
        battery.IncrementChildCount();
        battery.IncrementChildCount();

        // Level 2 - Brake System subcategories
        var discBrakes = Category.Create("Disc Brakes", "Disc brake systems and components", "DISC-001", 1, brakeSystem.Id,
            "Brake System > Disc Brakes", 1);
        var drumBrakes = Category.Create("Drum Brakes", "Drum brake systems and components", "DRUM-001", 2, brakeSystem.Id,
            "Brake System > Drum Brakes", 1);

        _categories.Add(discBrakes);
        _categories.Add(drumBrakes);
        brakeSystem.SubCategories.Add(discBrakes);
        brakeSystem.SubCategories.Add(drumBrakes);
        brakeSystem.IncrementChildCount();
        brakeSystem.IncrementChildCount();

        // Level 3 - Disc Brakes subcategories
        var brakePads = Category.Create("Brake Pads", "Replacement brake pads for disc brakes", "PADS-001", 1, discBrakes.Id,
            "Brake System > Disc Brakes > Brake Pads", 2);
        var rotors = Category.Create("Rotors", "Brake rotors and discs", "ROTOR-001", 2, discBrakes.Id,
            "Brake System > Disc Brakes > Rotors", 2);
        var calipers = Category.Create("Calipers", "Brake calipers and components", "CALIB-001", 3, discBrakes.Id,
            "Brake System > Disc Brakes > Calipers", 2);

        _categories.Add(brakePads);
        _categories.Add(rotors);
        _categories.Add(calipers);
        discBrakes.SubCategories.Add(brakePads);
        discBrakes.SubCategories.Add(rotors);
        discBrakes.SubCategories.Add(calipers);
        discBrakes.IncrementChildCount();
        discBrakes.IncrementChildCount();
        discBrakes.IncrementChildCount();

        // Level 2 - Transmission subcategories
        var manualTransmission = Category.Create("Manual Transmission", "Manual gearbox parts", "MAN-TRN-001", 1, transmission.Id,
            "Transmission > Manual Transmission", 1);
        var automaticTransmission = Category.Create("Automatic Transmission", "Automatic gearbox parts", "AUTO-TRN-001", 2, transmission.Id,
            "Transmission > Automatic Transmission", 1);

        _categories.Add(manualTransmission);
        _categories.Add(automaticTransmission);
        transmission.SubCategories.Add(manualTransmission);
        transmission.SubCategories.Add(automaticTransmission);
        transmission.IncrementChildCount();
        transmission.IncrementChildCount();
    }

    public async Task<IEnumerable<Category>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await Task.Delay(0, cancellationToken); // Simulate async operation
        return _categories.Where(c => !c.Isdeleted).ToList();
    }

    public async Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await Task.Delay(0, cancellationToken);
        return _categories.FirstOrDefault(c => c.Id == id && !c.Isdeleted);
    }

    public async Task AddAsync(Category entity, CancellationToken cancellationToken = default)
    {
        await Task.Delay(0, cancellationToken);

        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        if (_categories.Any(c => c.Code == entity.Code && !c.Isdeleted))
            throw new InvalidOperationException($"Category with code '{entity.Code}' already exists");

        // If this category has a parent, calculate depth level and breadcrumb path
        if (entity.ParentCategoryId.HasValue)
        {
            var parentCategory = _categories.FirstOrDefault(c => c.Id == entity.ParentCategoryId && !c.Isdeleted);
            if (parentCategory != null)
            {
                // Calculate depth level: parent's depth + 1
                var newDepthLevel = parentCategory.DepthLevel + 1;

                // Validate depth doesn't exceed max
                if (newDepthLevel > Category.MaxCategoryDepth - 1)
                    throw new InvalidOperationException($"Cannot create subcategory! Parent is at level {parentCategory.DepthLevel + 1}. Maximum allowed level is {Category.MaxCategoryDepth}.");

                // Update the entity's depth level
                entity.UpdateDepthLevel(newDepthLevel);

                // Calculate breadcrumb path: parent's breadcrumb + " > " + this category's name
                var breadcrumb = string.IsNullOrEmpty(parentCategory.BreadcrumbPath)
                    ? parentCategory.Name
                    : $"{parentCategory.BreadcrumbPath} > {entity.Name}";
                entity.UpdateBreadcrumbPath(breadcrumb);

                // Add it to the parent's subcategories
                parentCategory.SubCategories.Add(entity);
                parentCategory.IncrementChildCount();
            }
        }

        // Add the category to the list
        _categories.Add(entity);
    }

    public async Task UpdateAsync(Category entity, CancellationToken cancellationToken = default)
    {
        await Task.Delay(0, cancellationToken);

        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        var existingCategory = _categories.FirstOrDefault(c => c.Id == entity.Id && !c.Isdeleted);
        if (existingCategory == null)
            throw new InvalidOperationException($"Category with ID '{entity.Id}' not found");

        // Update the properties
        var index = _categories.IndexOf(existingCategory);
        if (index >= 0)
        {
            _categories[index] = entity;
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await Task.Delay(0, cancellationToken);

        var category = _categories.FirstOrDefault(c => c.Id == id && !c.Isdeleted);
        if (category == null)
            throw new InvalidOperationException($"Category with ID '{id}' not found");

        // Soft delete
        _categories.Remove(category);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await Task.Delay(0, cancellationToken);
        return _categories.Any(c => c.Id == id && !c.Isdeleted);
    }

    public async Task<IEnumerable<Category>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        await Task.Delay(0, cancellationToken);
        return _categories.Where(c => c.IsActive && !c.Isdeleted).ToList();
    }

    public async Task<IEnumerable<Category>> GetCategoriesWithSubcategoriesAsync(CancellationToken cancellationToken = default)
    {
        await Task.Delay(0, cancellationToken);
        return _categories.Where(c => !c.Isdeleted).ToList();
    }

    public async Task<IEnumerable<Category>> GetTopLevelCategoriesAsync(CancellationToken cancellationToken = default)
    {
        await Task.Delay(0, cancellationToken);
        return _categories.Where(c => c.ParentCategoryId == null && !c.Isdeleted).ToList();
    }

    public async Task<IEnumerable<Category>> GetSubcategoriesAsync(Guid parentCategoryId, CancellationToken cancellationToken = default)
    {
        await Task.Delay(0, cancellationToken);
        return _categories.Where(c => c.ParentCategoryId == parentCategoryId && !c.Isdeleted).ToList();
    }

    public async Task<bool> CodeExistsAsync(string code, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        await Task.Delay(0, cancellationToken);

        var query = _categories.Where(c => c.Code == code && !c.Isdeleted);
        if (excludeId.HasValue)
            query = query.Where(c => c.Id != excludeId.Value);

        return query.Any();
    }

    public async Task<bool> NameExistsAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        await Task.Delay(0, cancellationToken);

        var query = _categories.Where(c => c.Name == name && !c.Isdeleted);
        if (excludeId.HasValue)
            query = query.Where(c => c.Id != excludeId.Value);

        return query.Any();
    }

    public async Task<Category?> GetByCategoryCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        await Task.Delay(0, cancellationToken);
        return _categories.FirstOrDefault(c => c.Code == code && !c.Isdeleted);
    }

    public async Task<Category?> GetByCategoryNameAsync(string name, CancellationToken cancellationToken = default)
    {
        await Task.Delay(0, cancellationToken);
        return _categories.FirstOrDefault(c => c.Name == name && !c.Isdeleted);
    }

    public async Task<IEnumerable<Category>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        await Task.Delay(0, cancellationToken);

        var lowerTerm = searchTerm.ToLowerInvariant();
        return _categories.Where(c =>
            !c.Isdeleted &&
            (c.Name.ToLowerInvariant().Contains(lowerTerm) ||
             c.Code.ToLowerInvariant().Contains(lowerTerm) ||
             c.Description.ToLowerInvariant().Contains(lowerTerm))
        ).ToList();
    }

    public async Task<(IEnumerable<Category> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        await Task.Delay(0, cancellationToken);

        if (pageNumber < 1)
            pageNumber = 1;
        if (pageSize < 1)
            pageSize = 10;

        var query = _categories.Where(c => !c.Isdeleted).AsEnumerable();
        var totalCount = query.Count();

        var items = query
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return (items, totalCount);
    }

    public async Task<IEnumerable<Category>> GetAncestorsAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        await Task.Delay(0, cancellationToken);

        var ancestors = new List<Category>();
        var category = _categories.FirstOrDefault(c => c.Id == categoryId && !c.Isdeleted);

        while (category?.ParentCategoryId.HasValue == true)
        {
            var parent = _categories.FirstOrDefault(c => c.Id == category.ParentCategoryId && !c.Isdeleted);
            if (parent == null)
                break;

            ancestors.Add(parent);
            category = parent;
        }

        return ancestors;
    }

    public async Task<IEnumerable<Category>> GetAllDescendantsAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        await Task.Delay(0, cancellationToken);

        var descendants = new List<Category>();
        var queue = new Queue<Guid>(new[] { categoryId });

        while (queue.Count > 0)
        {
            var currentId = queue.Dequeue();
            var children = _categories.Where(c => c.ParentCategoryId == currentId && !c.Isdeleted).ToList();

            foreach (var child in children)
            {
                descendants.Add(child);
                queue.Enqueue(child.Id);
            }
        }

        return descendants;
    }

    public async Task<bool> WouldCreateCircularReferenceAsync(Guid categoryId, Guid? newParentId, CancellationToken cancellationToken = default)
    {
        await Task.Delay(0, cancellationToken);

        if (newParentId == null)
            return false;

        if (categoryId == newParentId)
            return true;

        // Check if newParent is a descendant of categoryId
        var descendants = await GetAllDescendantsAsync(categoryId, cancellationToken);
        return descendants.Any(d => d.Id == newParentId);
    }

    public async Task<int> GetDepthAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        await Task.Delay(0, cancellationToken);

        int depth = 0;
        var category = _categories.FirstOrDefault(c => c.Id == categoryId && !c.Isdeleted);

        while (category?.ParentCategoryId.HasValue == true)
        {
            depth++;
            category = _categories.FirstOrDefault(c => c.Id == category.ParentCategoryId && !c.Isdeleted);
            if (category == null)
                break;
        }

        return depth;
    }

    public async Task<string> GetBreadcrumbPathAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        await Task.Delay(0, cancellationToken);

        var category = _categories.FirstOrDefault(c => c.Id == categoryId && !c.Isdeleted);
        if (category == null)
            return string.Empty;

        var path = new Stack<string>();
        var current = category;

        while (current != null)
        {
            path.Push(current.Name);
            if (!current.ParentCategoryId.HasValue)
                break;

            current = _categories.FirstOrDefault(c => c.Id == current.ParentCategoryId && !c.Isdeleted);
        }

        return string.Join(" > ", path);
    }
}
