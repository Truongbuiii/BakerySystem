namespace BakerySystem.Application.Features.Dashboard;

public class DashboardStatsDto
{
    public decimal TodayRevenue { get; set; }
    public double TodayRevenueGrowthPercent { get; set; } = 15.2;
    public int TodayOrdersCount { get; set; }
    public int BakingOrdersCount { get; set; }
    public int ShippingOrdersCount { get; set; }
    public int TotalProductsCount { get; set; }
    public int BestSellerProductsCount { get; set; }
    public int TotalCustomersCount { get; set; }
    public int NewCustomersThisMonthCount { get; set; }

    public List<TopSellingProductDto> TopSellingProducts { get; set; } = new();
    public List<RecentActivityDto> RecentActivities { get; set; } = new();
    public List<CategoryBreakupDto> CategoryBreakups { get; set; } = new();
    public decimal TotalCategoryRevenue { get; set; }
}

public class TopSellingProductDto
{
    public int Rank { get; set; }
    public int ProductID { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string ChefName { get; set; } = "Bếp trưởng Bakery";
    public string AvatarUrl { get; set; } = "/assets/images/profile/user-2.jpg";
    public int QuantitySold { get; set; }
    public decimal TotalRevenue { get; set; }
    public string LevelBadge { get; set; } = "Bán chạy";
}

public class RecentActivityDto
{
    public string TimeText { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public string ColorType { get; set; } = "primary"; // primary, secondary, success, warning, error
}

public class CategoryBreakupDto
{
    public string CategoryName { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public int Percentage { get; set; }
    public string ColorHex { get; set; } = "#5d87ff";
}
