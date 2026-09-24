namespace algo.Domain.Sales.Entities;

public class Payment
{
    public long Id { get; set; }

    public long OrderId { get; set; }

    public string CurrencyCode { get; set; } = string.Empty;

    public string GatewayName { get; set; } = string.Empty;

    public string GatewayTransactionId { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string PaymentStatus { get; set; } = string.Empty;

    public virtual Order Order { get; set; } = null!;
}
