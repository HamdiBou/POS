using System;
using Postgrest.Attributes;
using Postgrest.Models;

namespace CoffeeShopPOS.Models
{
    [Table("employees")]
    public class Employee : BaseModel
    {
        [PrimaryKey("id", false)]
        public Guid Id { get; set; }

        [Column("name")]
        public string Name { get; set; }

        [Column("role")]
        public string Role { get; set; }

        [Column("active")]
        public bool Active { get; set; }
    }

    [Table("shifts")]
    public class Shift : BaseModel
    {
        [PrimaryKey("id", false)]
        public Guid Id { get; set; }

        [Column("employee_id")]
        public Guid EmployeeId { get; set; }

        [Column("opened_at")]
        public DateTime OpenedAt { get; set; }

        [Column("closed_at")]
        public DateTime? ClosedAt { get; set; }

        [Column("opening_cash")]
        public decimal OpeningCash { get; set; }

        [Column("closing_cash")]
        public decimal? ClosingCash { get; set; }
    }

    [Table("articles")]
    public class Article : BaseModel
    {
        [PrimaryKey("id", false)]
        public Guid Id { get; set; }

        [Column("name")]
        public string Name { get; set; }

        [Column("price")]
        public decimal Price { get; set; }

        [Column("category")]
        public string Category { get; set; }

        [Column("requires_coffee")]
        public bool RequiresCoffee { get; set; }

        [Column("active")]
        public bool Active { get; set; }

        [Column("expected_yield")]
        public int? ExpectedYield { get; set; }

        [Column("is_sellable")]
        public bool IsSellable { get; set; } = true;
    }

    [Table("orders")]
    public class Order : BaseModel
    {
        [PrimaryKey("id", false)]
        public Guid Id { get; set; }

        [Column("shift_id")]
        public Guid? ShiftId { get; set; }

        [Column("employee_id")]
        public Guid EmployeeId { get; set; }

        [Column("source")]
        public string Source { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("total")]
        public decimal Total { get; set; }

        [Column("status")]
        public string Status { get; set; }
    }

    [Table("order_items")]
    public class OrderItem : BaseModel
    {
        [PrimaryKey("id", false)]
        public Guid Id { get; set; }

        [Column("order_id")]
        public Guid OrderId { get; set; }

        [Column("article_id")]
        public Guid? ArticleId { get; set; }

        [Column("article_name")]
        public string ArticleName { get; set; }

        [Column("unit_price")]
        public decimal UnitPrice { get; set; }

        [Column("quantity")]
        public int Quantity { get; set; }
    }

    [Table("bean_bags")]
    public class BeanBag : BaseModel
    {
        [PrimaryKey("id", false)]
        public Guid Id { get; set; }

        [Column("employee_id")]
        public Guid EmployeeId { get; set; }

        [Column("shift_id")]
        public Guid ShiftId { get; set; }

        [Column("started_at")]
        public DateTime StartedAt { get; set; }

        [Column("ended_at")]
        public DateTime? EndedAt { get; set; }

        [Column("expected_yield")]
        public int ExpectedYield { get; set; }

        [Column("coffee_count")]
        public int CoffeeCount { get; set; }

        [Column("flagged")]
        public bool Flagged { get; set; }
    }

    [Table("article_costs")]
    public class ArticleCost : BaseModel
    {
        [PrimaryKey("article_id", false)]
        public Guid ArticleId { get; set; }

        [Column("unit_cost")]
        public decimal UnitCost { get; set; }
    }

    [Table("order_item_costs")]
    public class OrderItemCost : BaseModel
    {
        [PrimaryKey("order_item_id", false)]
        public Guid OrderItemId { get; set; }

        [Column("unit_cost")]
        public decimal UnitCost { get; set; }
    }

    [Table("settings")]
    public class Setting : BaseModel
    {
        [PrimaryKey("key", false)]
        public string Key { get; set; }

        [Column("value")]
        public string Value { get; set; }
    }

    [Table("notifications")]
    public class Notification : BaseModel
    {
        [PrimaryKey("id", false)]
        public Guid Id { get; set; }

        [Column("employee_id")]
        public Guid? EmployeeId { get; set; }

        [Column("type")]
        public string Type { get; set; }

        [Column("message")]
        public string Message { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}
