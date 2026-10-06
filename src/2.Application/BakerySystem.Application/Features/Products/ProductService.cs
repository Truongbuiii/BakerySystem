using BakerySystem.Application.Common.Interfaces;
using BakerySystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BakerySystem.Application.Features.Products;

public interface IProductService
{
    Task<List<ProductDto>> GetProductsAsync(string? search = null, int? categoryId = null, bool? isActive = null, string? stockStatus = null, CancellationToken cancellationToken = default);
    Task<ProductStatsDto> GetProductStatsAsync(CancellationToken cancellationToken = default);
    Task<List<CategoryDto>> GetCategoriesAsync(bool? isActive = null, CancellationToken cancellationToken = default);
    Task<ProductDto?> GetProductByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ProductDto> CreateProductAsync(CreateProductDto dto, CancellationToken cancellationToken = default);
    Task<bool> UpdateProductAsync(UpdateProductDto dto, CancellationToken cancellationToken = default);
    Task<bool> ToggleProductStatusAsync(int id, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> DeleteOrArchiveProductAsync(int id, CancellationToken cancellationToken = default);
    Task<CategoryDto> CreateCategoryAsync(string categoryName, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> UpdateCategoryAsync(int categoryId, string categoryName, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> ToggleCategoryStatusAsync(int categoryId, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> DeleteOrArchiveCategoryAsync(int categoryId, CancellationToken cancellationToken = default);
}

public class ProductService : IProductService
{
    private readonly IBakeryDbContext _context;

    public ProductService(IBakeryDbContext context)
    {
        _context = context;
    }

    public async Task<List<ProductDto>> GetProductsAsync(
        string? search = null, 
        int? categoryId = null, 
        bool? isActive = null, 
        string? stockStatus = null, 
        CancellationToken cancellationToken = default)
    {
        var query = _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .AsQueryable();

        // 1. Tìm kiếm theo tên sản phẩm
        if (!string.IsNullOrWhiteSpace(search))
        {
            var trimmedSearch = search.Trim();
            query = query.Where(p => p.ProductName.Contains(trimmedSearch) || (p.Description != null && p.Description.Contains(trimmedSearch)));
        }

        // 2. Lọc theo danh mục
        if (categoryId.HasValue && categoryId.Value > 0)
        {
            query = query.Where(p => p.CategoryID == categoryId.Value);
        }

        // 3. Lọc theo trạng thái kinh doanh
        if (isActive.HasValue)
        {
            query = query.Where(p => p.IsActive == isActive.Value);
        }

        // 4. Lọc theo tình trạng tồn kho
        if (!string.IsNullOrWhiteSpace(stockStatus) && stockStatus != "All")
        {
            if (stockStatus == "InStock")
            {
                query = query.Where(p => p.Quantity > 5);
            }
            else if (stockStatus == "LowStock")
            {
                query = query.Where(p => p.Quantity > 0 && p.Quantity <= 5);
            }
            else if (stockStatus == "OutOfStock")
            {
                query = query.Where(p => p.Quantity <= 0);
            }
        }

        var products = await query
            .OrderByDescending(p => p.ProductID)
            .Select(p => new ProductDto
            {
                ProductID = p.ProductID,
                CategoryID = p.CategoryID,
                CategoryName = p.Category != null ? p.Category.CategoryName : "Chưa phân loại",
                ProductName = p.ProductName,
                Description = p.Description,
                Price = p.Price,
                Quantity = p.Quantity,
                ImageURL = p.ImageURL,
                IsActive = p.IsActive,
                SoldCount = p.OrderDetails.Sum(od => od.Quantity)
            })
            .ToListAsync(cancellationToken);

        return products;
    }

    public async Task<ProductStatsDto> GetProductStatsAsync(CancellationToken cancellationToken = default)
    {
        var total = await _context.Products.CountAsync(cancellationToken);
        var active = await _context.Products.CountAsync(p => p.IsActive, cancellationToken);
        var inactive = total - active;
        var lowStock = await _context.Products.CountAsync(p => p.Quantity > 0 && p.Quantity <= 5, cancellationToken);
        var outOfStock = await _context.Products.CountAsync(p => p.Quantity <= 0, cancellationToken);
        var avgPrice = total > 0 ? await _context.Products.AverageAsync(p => p.Price, cancellationToken) : 0m;

        return new ProductStatsDto
        {
            TotalProducts = total,
            ActiveProducts = active,
            InactiveProducts = inactive,
            LowStockProducts = lowStock,
            OutOfStockProducts = outOfStock,
            AveragePrice = avgPrice
        };
    }

    public async Task<List<CategoryDto>> GetCategoriesAsync(bool? isActive = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Categories.AsNoTracking();
        if (isActive.HasValue)
        {
            query = query.Where(c => c.IsActive == isActive.Value);
        }

        return await query
            .OrderBy(c => c.CategoryName)
            .Select(c => new CategoryDto
            {
                CategoryID = c.CategoryID,
                CategoryName = c.CategoryName,
                ProductCount = c.Products.Count,
                IsActive = c.IsActive
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ProductDto?> GetProductByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var p = await _context.Products
            .AsNoTracking()
            .Include(x => x.Category)
            .Include(x => x.OrderDetails)
            .FirstOrDefaultAsync(x => x.ProductID == id, cancellationToken);

        if (p == null) return null;

        return new ProductDto
        {
            ProductID = p.ProductID,
            CategoryID = p.CategoryID,
            CategoryName = p.Category?.CategoryName ?? "Chưa phân loại",
            ProductName = p.ProductName,
            Description = p.Description,
            Price = p.Price,
            Quantity = p.Quantity,
            ImageURL = p.ImageURL,
            IsActive = p.IsActive,
            SoldCount = p.OrderDetails.Sum(od => od.Quantity)
        };
    }

    public async Task<ProductDto> CreateProductAsync(CreateProductDto dto, CancellationToken cancellationToken = default)
    {
        var product = new Product
        {
            ProductName = dto.ProductName.Trim(),
            CategoryID = dto.CategoryID,
            Price = dto.Price,
            Quantity = dto.InitialQuantity,
            Description = dto.Description?.Trim(),
            ImageURL = string.IsNullOrWhiteSpace(dto.ImageURL) ? "/assets/images/products/s1.jpg" : dto.ImageURL.Trim(),
            IsActive = dto.IsActive
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync(cancellationToken);

        // Load category name
        var category = await _context.Categories.FindAsync(new object[] { product.CategoryID }, cancellationToken);

        return new ProductDto
        {
            ProductID = product.ProductID,
            CategoryID = product.CategoryID,
            CategoryName = category?.CategoryName ?? "Chưa phân loại",
            ProductName = product.ProductName,
            Description = product.Description,
            Price = product.Price,
            Quantity = product.Quantity,
            ImageURL = product.ImageURL,
            IsActive = product.IsActive,
            SoldCount = 0
        };
    }

    public async Task<bool> UpdateProductAsync(UpdateProductDto dto, CancellationToken cancellationToken = default)
    {
        var product = await _context.Products.FindAsync(new object[] { dto.ProductID }, cancellationToken);
        if (product == null) return false;

        product.ProductName = dto.ProductName.Trim();
        product.CategoryID = dto.CategoryID;
        product.Price = dto.Price;
        product.Description = dto.Description?.Trim();
        if (!string.IsNullOrWhiteSpace(dto.ImageURL))
        {
            product.ImageURL = dto.ImageURL.Trim();
        }
        product.IsActive = dto.IsActive;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> ToggleProductStatusAsync(int id, CancellationToken cancellationToken = default)
    {
        var product = await _context.Products.FindAsync(new object[] { id }, cancellationToken);
        if (product == null) return false;

        product.IsActive = !product.IsActive;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<(bool Success, string Message)> DeleteOrArchiveProductAsync(int id, CancellationToken cancellationToken = default)
    {
        var product = await _context.Products
            .Include(p => p.OrderDetails)
            .Include(p => p.ImportReceiptDetails)
            .FirstOrDefaultAsync(p => p.ProductID == id, cancellationToken);

        if (product == null)
        {
            return (false, "Không tìm thấy sản phẩm.");
        }

        // Nếu sản phẩm đã từng phát sinh hóa đơn hoặc phiếu nhập, thực hiện Soft Delete (Ngừng bán) để bảo toàn lịch sử
        if (product.OrderDetails.Any() || product.ImportReceiptDetails.Any())
        {
            product.IsActive = false;
            await _context.SaveChangesAsync(cancellationToken);
            return (true, $"Sản phẩm '{product.ProductName}' đã có lịch sử đơn hàng/nhập kho nên đã được chuyển sang trạng thái [Ngừng kinh doanh] để bảo toàn dữ liệu.");
        }

        _context.Products.Remove(product);
        await _context.SaveChangesAsync(cancellationToken);
        return (true, $"Đã xóa vĩnh viễn sản phẩm '{product.ProductName}' khỏi hệ thống.");
    }

    public async Task<CategoryDto> CreateCategoryAsync(string categoryName, CancellationToken cancellationToken = default)
    {
        var trimmed = categoryName.Trim();
        var existing = await _context.Categories.FirstOrDefaultAsync(c => c.CategoryName.ToLower() == trimmed.ToLower(), cancellationToken);
        if (existing != null)
        {
            return new CategoryDto
            {
                CategoryID = existing.CategoryID,
                CategoryName = existing.CategoryName,
                ProductCount = await _context.Products.CountAsync(p => p.CategoryID == existing.CategoryID, cancellationToken)
            };
        }

        var category = new Category
        {
            CategoryName = trimmed
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync(cancellationToken);

        return new CategoryDto
        {
            CategoryID = category.CategoryID,
            CategoryName = category.CategoryName,
            ProductCount = 0,
            IsActive = true
        };
    }

    public async Task<(bool Success, string Message)> UpdateCategoryAsync(int categoryId, string categoryName, CancellationToken cancellationToken = default)
    {
        var trimmed = categoryName?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return (false, "Tên loại sản phẩm không được để trống.");
        }

        var category = await _context.Categories.FindAsync(new object[] { categoryId }, cancellationToken);
        if (category == null)
        {
            return (false, "Không tìm thấy loại sản phẩm cần cập nhật.");
        }

        // Kiểm tra xem tên mới có trùng với loại sản phẩm khác không
        var duplicate = await _context.Categories
            .AnyAsync(c => c.CategoryID != categoryId && c.CategoryName.ToLower() == trimmed.ToLower(), cancellationToken);
        if (duplicate)
        {
            return (false, $"Tên loại sản phẩm '{trimmed}' đã tồn tại trong hệ thống.");
        }

        category.CategoryName = trimmed;
        await _context.SaveChangesAsync(cancellationToken);
        return (true, $"Đã cập nhật tên loại sản phẩm thành '{trimmed}'.");
    }

    public async Task<(bool Success, string Message)> ToggleCategoryStatusAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        var category = await _context.Categories
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.CategoryID == categoryId, cancellationToken);

        if (category == null)
        {
            return (false, "Không tìm thấy loại sản phẩm.");
        }

        category.IsActive = !category.IsActive;

        // ĐỒNG BỘ: Khi ẩn loại sản phẩm, ngưng hoạt động luôn toàn bộ sản phẩm thuộc loại đó
        // Khi mở lại loại sản phẩm, kích hoạt lại toàn bộ sản phẩm thuộc loại đó
        int affectedCount = 0;
        if (category.Products != null && category.Products.Any())
        {
            foreach (var p in category.Products)
            {
                p.IsActive = category.IsActive;
            }
            affectedCount = category.Products.Count;
        }

        await _context.SaveChangesAsync(cancellationToken);

        if (!category.IsActive)
        {
            return (true, $"Đã ẩn loại '{category.CategoryName}' và ngưng hoạt động đồng bộ {affectedCount} món bánh thuộc nhóm này.");
        }
        else
        {
            return (true, $"Đã kích hoạt lại loại '{category.CategoryName}' và mở lại {affectedCount} món bánh trong thực đơn.");
        }
    }

    public async Task<(bool Success, string Message)> DeleteOrArchiveCategoryAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        var category = await _context.Categories
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.CategoryID == categoryId, cancellationToken);

        if (category == null)
        {
            return (false, "Không tìm thấy loại sản phẩm.");
        }

        // LUẬT: Nếu loại sản phẩm đã có sản phẩm thuộc nhóm này -> Không xóa, chuyển sang ẨN và NGƯNG HOẠT ĐỘNG TOÀN BỘ SẢN PHẨM LIÊN QUAN
        if (category.Products.Any())
        {
            category.IsActive = false;
            foreach (var p in category.Products)
            {
                p.IsActive = false;
            }
            await _context.SaveChangesAsync(cancellationToken);
            return (true, $"Loại sản phẩm '{category.CategoryName}' đang có {category.Products.Count} món bánh thuộc nhóm này. Theo quy định, hệ thống đã chuyển loại sản phẩm và đồng thời ngưng hoạt động toàn bộ {category.Products.Count} món bánh để bảo toàn dữ liệu.");
        }

        // Nếu chưa có sản phẩm nào -> Được phép XÓA VĨNH VIỄN
        _context.Categories.Remove(category);
        await _context.SaveChangesAsync(cancellationToken);
        return (true, $"Đã xóa vĩnh viễn loại sản phẩm '{category.CategoryName}' khỏi hệ thống thành công vì chưa có sản phẩm nào thuộc nhóm này.");
    }
}
