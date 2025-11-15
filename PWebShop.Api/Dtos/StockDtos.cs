using System.ComponentModel.DataAnnotations;

namespace PWebShop.Api.Dtos;

public class StockUpdateDto
{
    [Range(0, int.MaxValue)]
    public int QuantityAvailable { get; set; }
}

public class StockDto
{
    public int ProductId { get; set; }

    public int QuantityAvailable { get; set; }

    public DateTime LastUpdatedAt { get; set; }
}
