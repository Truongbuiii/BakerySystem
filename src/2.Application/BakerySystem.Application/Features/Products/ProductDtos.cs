namespace BakerySystem.Application.Features.Products;

public class ProductDto
{
    public int ProductID { get; set; }
    public int CategoryID { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public string? ImageURL { get; set; }
    public bool IsActive { get; set; }
    public int SoldCount { get; set; }
    public string StockStatus => Quantity <= 0 ? "Hết hàng" : (Quantity <= 5 ? "Sắp hết" : "Còn hàng");
}

public class ProductStatsDto
{
    public int TotalProducts { get; set; }
    public int ActiveProducts { get; set; }
    public int InactiveProducts { get; set; }
    public int LowStockProducts { get; set; }
    public int OutOfStockProducts { get; set; }
    public decimal AveragePrice { get; set; }
}

public class CreateProductDto
{
    public string ProductName { get; set; } = string.Empty;
    public int CategoryID { get; set; }
    public decimal Price { get; set; }
    public int InitialQuantity { get; set; } = 0;
    public string? Description { get; set; }
    public string? ImageURL { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateProductDto
{
    public int ProductID { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int CategoryID { get; set; }
    public decimal Price { get; set; }
    public string? Description { get; set; }
    public string? ImageURL { get; set; }
    public bool IsActive { get; set; }
}

public class CategoryDto
{
    public int CategoryID { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int ProductCount { get; set; }
    public bool IsActive { get; set; } = true;
}
