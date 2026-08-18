using System;

namespace CoffeeShopPOS.Models
{
    public class LocalArticle
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public string Category { get; set; }
        public bool RequiresCoffee { get; set; }
        public bool Active { get; set; }
        public int? ExpectedYield { get; set; }
        public bool IsSellable { get; set; } = true;
    }

    public class LocalArticleCost
    {
        public Guid ArticleId { get; set; }
        public decimal UnitCost { get; set; }
    }

    public class LocalShift
    {
        public Guid Id { get; set; }
        public Guid EmployeeId { get; set; }
        public DateTime OpenedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public decimal OpeningCash { get; set; }
        public decimal? ClosingCash { get; set; }
        public bool IsSynced { get; set; }
    }

    public class LocalOrder
    {
        public Guid Id { get; set; }
        public Guid? ShiftId { get; set; }
        public Guid EmployeeId { get; set; }
        public string Source { get; set; }
        public DateTime CreatedAt { get; set; }
        public decimal Total { get; set; }
        public string Status { get; set; }
        public bool IsSynced { get; set; }
    }

    public class LocalOrderItem
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public Guid? ArticleId { get; set; }
        public string ArticleName { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
    }

    public class LocalBeanBag
    {
        public Guid Id { get; set; }
        public Guid EmployeeId { get; set; }
        public Guid ShiftId { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public int ExpectedYield { get; set; }
        public int CoffeeCount { get; set; }
        public bool Flagged { get; set; }
        public bool IsSynced { get; set; }
    }

    public class LocalSetting
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }
}
