namespace AutoPartShop.Domain.Entities;

/// <summary>
/// A shop holiday (Eid, national holidays, etc.). One row per calendar date.
/// </summary>
public class Holiday : AuditableEntity
{
    public DateTime Date { get; private set; }
    public string Name { get; private set; } = string.Empty;

    private Holiday() { }

    public static Holiday Create(DateTime date, string name)
    {
        if (date == default)
            throw new ArgumentException("Date is required", nameof(date));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));

        return new Holiday
        {
            Date = date.Date,
            Name = name.Trim()
        };
    }

    public void Update(DateTime date, string name)
    {
        if (date == default)
            throw new ArgumentException("Date is required", nameof(date));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));

        Date = date.Date;
        Name = name.Trim();
    }
}
