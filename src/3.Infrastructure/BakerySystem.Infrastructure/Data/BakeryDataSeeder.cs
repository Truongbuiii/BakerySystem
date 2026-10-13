using BakerySystem.Domain.Entities;
using BakerySystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BakerySystem.Infrastructure.Data;

public static class BakeryDataSeeder
{
    public static async Task SeedAsync(BakeryDbContext context)
    {
        // 1. Seed Accounts & Roles if empty
        if (!await context.Accounts.AnyAsync())
        {
            var adminAccount = new Account
            {
                Username = "admin",
                Password = BCrypt.Net.BCrypt.HashPassword("admin123"),
                Role = AccountRole.Admin,
                Status = AccountStatus.Active
            };

            var chefAccount = new Account
            {
                Username = "chefbakery",
                Password = BCrypt.Net.BCrypt.HashPassword("staff123"),
                Role = AccountRole.Employee,
                Status = AccountStatus.Active
            };

            var staffAccount = new Account
            {
                Username = "staffbakery",
                Password = BCrypt.Net.BCrypt.HashPassword("staff123"),
                Role = AccountRole.Employee,
                Status = AccountStatus.Active
            };

            var customerAccount1 = new Account
            {
                Username = "hoanganh",
                Password = BCrypt.Net.BCrypt.HashPassword("customer123"),
                Role = AccountRole.Customer,
                Status = AccountStatus.Active
            };

            var customerAccount2 = new Account
            {
                Username = "thuha",
                Password = BCrypt.Net.BCrypt.HashPassword("customer123"),
                Role = AccountRole.Customer,
                Status = AccountStatus.Active
            };

            context.Accounts.AddRange(adminAccount, chefAccount, staffAccount, customerAccount1, customerAccount2);
            await context.SaveChangesAsync();

            // 2. Seed Employees
            var chef = new Employee
            {
                AccountID = chefAccount.AccountID,
                FullName = "Nguyễn Văn Bách",
                Phone = "0908123456"
            };

            var staff = new Employee
            {
                AccountID = staffAccount.AccountID,
                FullName = "Lê Thị Mai",
                Phone = "0908654321"
            };

            context.Employees.AddRange(chef, staff);

            // 3. Seed Customers
            var cust1 = new Customer
            {
                AccountID = customerAccount1.AccountID,
                LastName = "Nguyễn Hoàng",
                FirstName = "Anh",
                Phone = "0912345678",
                Address = "123 Lê Lợi, Phường Bến Thành, Quận 1, TP.HCM"
            };

            var cust2 = new Customer
            {
                AccountID = customerAccount2.AccountID,
                LastName = "Trần Thị Thu",
                FirstName = "Hà",
                Phone = "0934567890",
                Address = "456 Nguyễn Huệ, Phường Bến Nghé, Quận 1, TP.HCM"
            };

            var cust3 = new Customer
            {
                AccountID = null,
                LastName = "Lê Tuấn",
                FirstName = "Kiệt",
                Phone = "0977889900",
                Address = "789 Võ Văn Ngân, TP. Thủ Đức, TP.HCM"
            };

            var custWalkIn = new Customer
            {
                AccountID = null, // Khách vãng lai mua tại quầy không có tài khoản
                LastName = "Khách",
                FirstName = "vãng lai",
                Phone = null,
                Address = "Mua trực tiếp tại tiệm bánh"
            };

            context.Customers.AddRange(cust1, cust2, cust3, custWalkIn);
            await context.SaveChangesAsync();
        }
        else
        {
            // Tự động nâng cấp tài khoản cũ (nếu có mật khẩu chưa hash) sang chuẩn BCrypt
            var existingAccounts = await context.Accounts.ToListAsync();
            bool hasMigration = false;
            foreach (var acc in existingAccounts)
            {
                if (!acc.Password.StartsWith("$2a$") && !acc.Password.StartsWith("$2b$") && !acc.Password.StartsWith("$2y$"))
                {
                    if (acc.Username == "admin") acc.Password = BCrypt.Net.BCrypt.HashPassword("admin123");
                    else if (acc.Username is "chefbakery" or "staffbakery") acc.Password = BCrypt.Net.BCrypt.HashPassword("staff123");
                    else acc.Password = BCrypt.Net.BCrypt.HashPassword("customer123");
                    hasMigration = true;
                }
            }
            if (hasMigration)
            {
                await context.SaveChangesAsync();
            }
        }

        // 4. Seed Categories if empty
        if (!await context.Categories.AnyAsync())
        {
            var catCake = new Category { CategoryName = "Bánh kem sinh nhật" };
            var catPastry = new Category { CategoryName = "Bánh ngọt & Mousse" };
            var catBread = new Category { CategoryName = "Bánh mì & Croissant" };

            context.Categories.AddRange(catCake, catPastry, catBread);
            await context.SaveChangesAsync();

            // 5. Seed Products
            var products = new List<Product>
            {
                new Product
                {
                    CategoryID = catPastry.CategoryID,
                    ProductName = "Bánh Tiramisu Ý Hoàng Gia",
                    Description = "Lớp bánh cà phê Mascarpone Ý thơm ngậy, rắc bột cacao nguyên chất cao cấp.",
                    Price = 350000m,
                    Quantity = 45,
                    ImageURL = "/assets/images/products/tiramisu.jpg",
                    IsActive = true
                },
                new Product
                {
                    CategoryID = catBread.CategoryID,
                    ProductName = "Set Croissant Bơ Pháp (Hộp 6)",
                    Description = "Bánh sừng bò nướng giòn xốp nhiều lớp với bơ sữa thượng hạng vùng Normandie.",
                    Price = 180000m,
                    Quantity = 60,
                    ImageURL = "/assets/images/products/croissant.jpg",
                    IsActive = true
                },
                new Product
                {
                    CategoryID = catCake.CategoryID,
                    ProductName = "Bánh Kem Bắp Phô Mai 24cm",
                    Description = "Cốt bánh bông lan vani mềm mịn, kem sữa tươi ngọt thanh xen kẽ hạt bắp ngọt và phô mai nướng.",
                    Price = 420000m,
                    Quantity = 20,
                    ImageURL = "/assets/images/products/corn-cake.jpg",
                    IsActive = true
                },
                new Product
                {
                    CategoryID = catPastry.CategoryID,
                    ProductName = "Bánh Mousse Dâu Tây Nhật Bản",
                    Description = "Mousse dâu tây Đà Lạt tươi mát kết hợp thạch hoa hồng ngọt dịu và sốt dâu tươi.",
                    Price = 290000m,
                    Quantity = 30,
                    ImageURL = "/assets/images/products/mousse.jpg",
                    IsActive = true
                },
                new Product
                {
                    CategoryID = catBread.CategoryID,
                    ProductName = "Bánh Mì Hoa Cúc Brioche",
                    Description = "Bánh mì bơ Brioche Pháp thơm hương hoa cúc tự nhiên, mềm dẻo từng thớ bánh.",
                    Price = 120000m,
                    Quantity = 40,
                    ImageURL = "/assets/images/products/brioche.jpg",
                    IsActive = true
                },
                new Product
                {
                    CategoryID = catCake.CategoryID,
                    ProductName = "Bánh Black Forest Cherry Rừng",
                    Description = "Hương vị sô cô la đen Bỉ đậm đà hòa quyện cùng quả anh đào chua ngọt và rượu Kirsch.",
                    Price = 480000m,
                    Quantity = 15,
                    ImageURL = "/assets/images/products/black-forest.jpg",
                    IsActive = true
                }
            };

            context.Products.AddRange(products);
            await context.SaveChangesAsync();
        }

        // 6. Seed Promotions if empty
        if (!await context.Promotions.AnyAsync())
        {
            var promo1 = new Promotion
            {
                PromotionCode = "BAKERY2026",
                PromotionName = "Khai trương đầu năm mới 2026",
                PromotionType = PromotionType.Invoice,
                StartDate = DateTime.Today.AddDays(-30),
                EndDate = DateTime.Today.AddDays(60),
                Description = "Mã voucher giảm 10% cho toàn bộ hóa đơn từ 200.000đ",
                Status = PromotionStatus.Active
            };

            var promo2 = new Promotion
            {
                PromotionCode = "SWEETLOVE",
                PromotionName = "Tín Đồ Bánh Ngọt & Mousse",
                PromotionType = PromotionType.Invoice,
                StartDate = DateTime.Today.AddDays(-10),
                EndDate = DateTime.Today.AddDays(90),
                Description = "Mã voucher giảm 15% cho đơn hàng từ 300.000đ",
                Status = PromotionStatus.Active
            };

            var promo3 = new Promotion
            {
                PromotionCode = "TIEMBANH20",
                PromotionName = "Tiệc Sinh Nhật & Sự Kiện",
                PromotionType = PromotionType.Invoice,
                StartDate = DateTime.Today.AddDays(-10),
                EndDate = DateTime.Today.AddDays(120),
                Description = "Mã voucher giảm 20% cho đơn bánh từ 500.000đ",
                Status = PromotionStatus.Active
            };

            var promo4 = new Promotion
            {
                PromotionCode = "BANHTUOI8",
                PromotionName = "Bánh Tươi Mỗi Ngày",
                PromotionType = PromotionType.Invoice,
                StartDate = DateTime.Today.AddDays(-15),
                EndDate = DateTime.Today.AddDays(180),
                Description = "Mã voucher giảm 8% cho mọi đơn hàng từ 100.000đ",
                Status = PromotionStatus.Active
            };

            // Loại 2: Khuyến mãi theo mức tiền hóa đơn đạt được (Tự động)
            var promoOrderTier = new Promotion
            {
                PromotionCode = "TIER300K",
                PromotionName = "Ưu đãi tự động hóa đơn lớn trên 300K",
                PromotionType = PromotionType.OrderTier,
                StartDate = DateTime.Today.AddDays(-30),
                EndDate = DateTime.Today.AddDays(90),
                Description = "Hóa đơn đạt từ 300.000đ trở lên tự động giảm 10% (tối đa 50.000đ)",
                Status = PromotionStatus.Active
            };

            // Loại 1: Khuyến mãi trực tiếp theo sản phẩm
            var promoProduct = new Promotion
            {
                PromotionCode = "SALE_TIRAMISU",
                PromotionName = "Flash Sale Giảm 15% Bánh Tiramisu Ý",
                PromotionType = PromotionType.Product,
                StartDate = DateTime.Today.AddDays(-15),
                EndDate = DateTime.Today.AddDays(45),
                Description = "Giảm trực tiếp 15% khi mua bánh Tiramisu Ý Hoàng Gia",
                Status = PromotionStatus.Active
            };

            context.Promotions.AddRange(promo1, promo2, promo3, promo4, promoOrderTier, promoProduct);
            await context.SaveChangesAsync();

            // 6.1 Seed PromotionInvoice (Loại 3: Mã Voucher hóa đơn)
            context.PromotionInvoices.AddRange(
                new PromotionInvoice
                {
                    PromotionID = promo1.PromotionID,
                    VoucherCode = promo1.PromotionCode,
                    MinOrderAmount = 200000m,
                    DiscountPercent = 10m,
                    MaxDiscountAmount = 100000m,
                    UsageLimit = 200,
                    UsedCount = 12
                },
                new PromotionInvoice
                {
                    PromotionID = promo2.PromotionID,
                    VoucherCode = promo2.PromotionCode,
                    MinOrderAmount = 300000m,
                    DiscountPercent = 15m,
                    MaxDiscountAmount = 60000m,
                    UsageLimit = 150,
                    UsedCount = 8
                },
                new PromotionInvoice
                {
                    PromotionID = promo3.PromotionID,
                    VoucherCode = promo3.PromotionCode,
                    MinOrderAmount = 500000m,
                    DiscountPercent = 20m,
                    MaxDiscountAmount = 150000m,
                    UsageLimit = 100,
                    UsedCount = 5
                },
                new PromotionInvoice
                {
                    PromotionID = promo4.PromotionID,
                    VoucherCode = promo4.PromotionCode,
                    MinOrderAmount = 100000m,
                    DiscountPercent = 8m,
                    MaxDiscountAmount = 30000m,
                    UsageLimit = 300,
                    UsedCount = 20
                }
            );

            // 6.2 Seed PromotionOrderTier (Loại 2: Hóa đơn đạt mức tiền)
            context.PromotionOrderTiers.Add(
                new PromotionOrderTier
                {
                    PromotionID = promoOrderTier.PromotionID,
                    MinOrderAmount = 300000m,
                    DiscountPercent = 10m,
                    MaxDiscountAmount = 50000m
                }
            );

            // 6.3 Seed PromotionProduct (Loại 1: Theo món bánh)
            var firstProduct = await context.Products.FirstOrDefaultAsync();
            if (firstProduct != null)
            {
                context.PromotionProducts.Add(
                    new PromotionProduct
                    {
                        PromotionID = promoProduct.PromotionID,
                        ProductID = firstProduct.ProductID,
                        DiscountPercent = 15m,
                        MaxDiscountAmount = 60000m
                    }
                );
            }

            await context.SaveChangesAsync();
        }
        else
        {
            // Bổ sung dữ liệu mẫu cho PromotionOrderTier và PromotionProduct nếu DB cũ chưa có
            if (!await context.PromotionOrderTiers.AnyAsync())
            {
                var existingTierPromo = await context.Promotions.FirstOrDefaultAsync(p => p.PromotionCode == "TIER300K");
                if (existingTierPromo == null)
                {
                    existingTierPromo = new Promotion
                    {
                        PromotionCode = "TIER300K",
                        PromotionName = "Ưu đãi tự động hóa đơn lớn trên 300K",
                        PromotionType = PromotionType.OrderTier,
                        StartDate = DateTime.Today.AddDays(-30),
                        EndDate = DateTime.Today.AddDays(90),
                        Description = "Hóa đơn đạt từ 300.000đ trở lên tự động giảm 10% (tối đa 50.000đ)",
                        Status = PromotionStatus.Active
                    };
                    context.Promotions.Add(existingTierPromo);
                    await context.SaveChangesAsync();
                }

                context.PromotionOrderTiers.Add(new PromotionOrderTier
                {
                    PromotionID = existingTierPromo.PromotionID,
                    MinOrderAmount = 300000m,
                    DiscountPercent = 10m,
                    MaxDiscountAmount = 50000m
                });
                await context.SaveChangesAsync();
            }

            if (!await context.PromotionProducts.AnyAsync())
            {
                var firstProduct = await context.Products.FirstOrDefaultAsync();
                if (firstProduct != null)
                {
                    var existingProdPromo = await context.Promotions.FirstOrDefaultAsync(p => p.PromotionCode == "SALE_TIRAMISU");
                    if (existingProdPromo == null)
                    {
                        existingProdPromo = new Promotion
                        {
                            PromotionCode = "SALE_TIRAMISU",
                            PromotionName = "Flash Sale Giảm 15% Bánh Tiramisu Ý",
                            PromotionType = PromotionType.Product,
                            StartDate = DateTime.Today.AddDays(-15),
                            EndDate = DateTime.Today.AddDays(45),
                            Description = "Giảm trực tiếp 15% khi mua bánh Tiramisu Ý Hoàng Gia",
                            Status = PromotionStatus.Active
                        };
                        context.Promotions.Add(existingProdPromo);
                        await context.SaveChangesAsync();
                    }

                    context.PromotionProducts.Add(new PromotionProduct
                    {
                        PromotionID = existingProdPromo.PromotionID,
                        ProductID = firstProduct.ProductID,
                        DiscountPercent = 15m,
                        MaxDiscountAmount = 60000m
                    });
                    await context.SaveChangesAsync();
                }
            }
        }

        // 7. Seed Orders & OrderDetails if empty
        if (!await context.Orders.AnyAsync())
        {
            var customer = await context.Customers.FirstOrDefaultAsync();
            var employee = await context.Employees.FirstOrDefaultAsync();
            var products = await context.Products.ToListAsync();

            if (customer != null && products.Count >= 4)
            {
                // Order 1: Đã hoàn tất hôm nay
                var order1 = new Order
                {
                    CustomerID = customer.CustomerID,
                    EmployeeID = employee?.EmployeeID,
                    OrderDate = DateTime.Now.AddHours(-3),
                    TotalAmount = 700000m,
                    DiscountAmount = 70000m,
                    FinalAmount = 630000m,
                    ReceiverName = customer.FullName,
                    ReceiverPhone = customer.Phone,
                    ShippingAddress = customer.Address,
                    Note = "Ghi chữ: Chúc mừng sinh nhật Hoàng Anh 24 tuổi",
                    PaymentMethod = PaymentMethod.BankTransfer,
                    PaymentStatus = PaymentStatus.Paid,
                    Status = OrderStatus.Completed
                };

                order1.OrderDetails.Add(new OrderDetail
                {
                    ProductID = products[0].ProductID, // Tiramisu
                    Quantity = 2,
                    UnitPrice = products[0].Price,
                    DiscountAmount = 70000m,
                    SubTotal = 630000m
                });

                // Order 2: Đang nướng lò hôm nay
                var order2 = new Order
                {
                    CustomerID = customer.CustomerID,
                    EmployeeID = employee?.EmployeeID,
                    OrderDate = DateTime.Now.AddHours(-1),
                    TotalAmount = 540000m,
                    DiscountAmount = 0m,
                    FinalAmount = 540000m,
                    ReceiverName = "Trần Thị Thu Hà",
                    ReceiverPhone = "0934567890",
                    ShippingAddress = "456 Nguyễn Huệ, Quận 1",
                    Note = "Giao lúc 16:00 chiều nay, kèm nến số 25",
                    PaymentMethod = PaymentMethod.COD,
                    PaymentStatus = PaymentStatus.Unpaid,
                    Status = OrderStatus.Confirmed
                };

                order2.OrderDetails.Add(new OrderDetail
                {
                    ProductID = products[1].ProductID, // Croissant
                    Quantity = 3,
                    UnitPrice = products[1].Price,
                    DiscountAmount = 0m,
                    SubTotal = 540000m
                });

                // Order 3: Đang giao hàng
                var order3 = new Order
                {
                    CustomerID = customer.CustomerID,
                    EmployeeID = employee?.EmployeeID,
                    OrderDate = DateTime.Now.AddMinutes(-40),
                    TotalAmount = 420000m,
                    DiscountAmount = 0m,
                    FinalAmount = 420000m,
                    ReceiverName = "Lê Tuấn Kiệt",
                    ReceiverPhone = "0977889900",
                    ShippingAddress = "789 Võ Văn Ngân, Thủ Đức",
                    Note = "Bánh kem bắp giữ lạnh cẩn thận khi giao",
                    PaymentMethod = PaymentMethod.VNPAY,
                    PaymentStatus = PaymentStatus.Paid,
                    Status = OrderStatus.Shipping
                };

                order3.OrderDetails.Add(new OrderDetail
                {
                    ProductID = products[2].ProductID, // Bánh kem bắp
                    Quantity = 1,
                    UnitPrice = products[2].Price,
                    DiscountAmount = 0m,
                    SubTotal = 420000m
                });

                // Order 4: Đơn mới đặt chờ duyệt
                var order4 = new Order
                {
                    CustomerID = customer.CustomerID,
                    EmployeeID = null,
                    OrderDate = DateTime.Now.AddMinutes(-10),
                    TotalAmount = 580000m,
                    DiscountAmount = 0m,
                    FinalAmount = 580000m,
                    ReceiverName = "Nguyễn Hoàng Anh",
                    ReceiverPhone = "0912345678",
                    ShippingAddress = customer.Address,
                    Note = "2 hộp Mousse dâu tây",
                    PaymentMethod = PaymentMethod.COD,
                    PaymentStatus = PaymentStatus.Unpaid,
                    Status = OrderStatus.Pending
                };

                order4.OrderDetails.Add(new OrderDetail
                {
                    ProductID = products[3].ProductID, // Mousse
                    Quantity = 2,
                    UnitPrice = products[3].Price,
                    DiscountAmount = 0m,
                    SubTotal = 580000m
                });

                context.Orders.AddRange(order1, order2, order3, order4);
                await context.SaveChangesAsync();
            }
        }

        // 8. Seed Suppliers and ImportReceipts if empty
        if (!await context.Suppliers.AnyAsync())
        {
            var sup1 = new Supplier
            {
                SupplierName = "Công ty TNHH Tân Nhất Hương (Bột mì & Bơ)",
                Phone = "02838123456",
                Address = "KCN Tân Bình, Tây Thạnh, Tân Phú, TP.HCM"
            };
            var sup2 = new Supplier
            {
                SupplierName = "Công ty Cổ phần Sữa Puratos Grand-Place",
                Phone = "02743789123",
                Address = "KCN VSIP 1, Thuận An, Bình Dương"
            };
            var sup3 = new Supplier
            {
                SupplierName = "Công ty TNHH Fonterra Brands (Sữa New Zealand)",
                Phone = "02839456789",
                Address = "39 Lê Duẩn, Quận 1, TP.HCM"
            };

            context.Suppliers.AddRange(sup1, sup2, sup3);
            await context.SaveChangesAsync();

            var chefEmployee = await context.Employees.FirstOrDefaultAsync();
            var allProducts = await context.Products.ToListAsync();

            if (chefEmployee != null && allProducts.Any())
            {
                var receipt1 = new ImportReceipt
                {
                    SupplierID = sup1.SupplierID,
                    EmployeeID = chefEmployee.EmployeeID,
                    ReceiptDate = DateTime.Today.AddDays(-12),
                    Note = "Nhập nguyên liệu bột làm bánh và bơ lạt đầu tháng",
                    TotalAmount = 3500000m
                };
                receipt1.ImportReceiptDetails.Add(new ImportReceiptDetail
                {
                    ProductID = allProducts[0].ProductID,
                    Quantity = 20,
                    UnitPrice = 100000m,
                    SubTotal = 2000000m
                });
                if (allProducts.Count > 1)
                {
                    receipt1.ImportReceiptDetails.Add(new ImportReceiptDetail
                    {
                        ProductID = allProducts[1].ProductID,
                        Quantity = 25,
                        UnitPrice = 60000m,
                        SubTotal = 1500000m
                    });
                }

                var receipt2 = new ImportReceipt
                {
                    SupplierID = sup2.SupplierID,
                    EmployeeID = chefEmployee.EmployeeID,
                    ReceiptDate = DateTime.Today.AddDays(-3),
                    Note = "Nhập chocolate Bỉ và nguyên liệu làm bánh mousse",
                    TotalAmount = 2800000m
                };
                if (allProducts.Count > 2)
                {
                    receipt2.ImportReceiptDetails.Add(new ImportReceiptDetail
                    {
                        ProductID = allProducts[2].ProductID,
                        Quantity = 15,
                        UnitPrice = 120000m,
                        SubTotal = 1800000m
                    });
                }
                if (allProducts.Count > 3)
                {
                    receipt2.ImportReceiptDetails.Add(new ImportReceiptDetail
                    {
                        ProductID = allProducts[3].ProductID,
                        Quantity = 10,
                        UnitPrice = 100000m,
                        SubTotal = 1000000m
                    });
                }

                context.ImportReceipts.AddRange(receipt1, receipt2);
                await context.SaveChangesAsync();
            }
        }
    }
}
