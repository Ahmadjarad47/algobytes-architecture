using algo.Domain.Catalog.Entities;

namespace algo.Domain.Sales.Entities;

public class OrderItem
{
    public long Id { get; set; }

    public long OrderId { get; set; }

    public int ProductId { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;
}
