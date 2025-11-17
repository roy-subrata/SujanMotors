

using AutoPartsShop.Domain.Entities;

namespace AutoPartShop.Domain.Entities;

public class Part : AuditableEntity
{
    public string Name { get; private set; } = string.Empty;
    public PartNumber PartNumber { get; private set; } = null!;
    private Part() { }
    public static Part Create(string name, PartNumber partNumber)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name cannot be empty", nameof(name));
        }
        if (partNumber is null)
        {
            throw new InvalidOperationException($"PartNumber Can Be Empty {nameof(partNumber)}");
        }
        return new()
        {
            Name = name,
            PartNumber = partNumber
        };
    }
}