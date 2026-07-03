namespace algo.Domain.Catalog.Entities;

public class Category
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? ImageUrl { get; set; }

    public DateTimeOffset? TrashedAt { get; set; }

    public DateTimeOffset? TrashExpiresAt { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
