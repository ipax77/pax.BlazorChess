namespace pax.BlazorChess.Models;
public record GameRequest
{
    public List<SortOrder> SortOrders { get; set; } = new List<SortOrder>() { new SortOrder("UTCDate", false) };
    public string? SearchString { get; set; }
    public int StartIndex { get; set; }
    public int Count { get; set; }
}

public record SortOrder
{
    public string Sort { get; set; }
    public bool Order { get; set; }

    public SortOrder(string sort, bool order)
    {
        Sort = sort;
        Order = order;
    }
}