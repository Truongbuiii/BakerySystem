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
                Password = "hashed_admin_password_123", // Trong thực tế sẽ hash bằng BCrypt/Argon2
                Role = AccountRole.Admin,
                Status = AccountStatus.Active
            };

            var chefAccount = new Account
            {
                Username = "chefbakery",
                Password = "hashed_chef_password_123",
                Role = AccountRole.Employee,
                Status = AccountStatus.Active
            };

            var staffAccount = new Account
            {
                Username = "staffbakery",
                Password = "hashed_staff_password_123",
                Role = AccountRole.Employee,
                Status = AccountStatus.Active
            };

            var customerAccount1 = new Account
            {
                Username = "hoanganh",
                Password = "hashed_user_password_123",
                Role = AccountRole.Customer,
                Status = AccountStatus.Active
            };

            var customerAccount2 = new Account
            {
                Username = "thuha",
                Password = "hashed_user_password_123",
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
                FullName = "Nguyễn Hoàng Anh",
                Phone = "0912345678",
                Address = "123 Lê Lợi, Phường Bến Thành, Quận 1, TP.HCM"
            };

            var cust2 = new Customer
            {
                AccountID = customerAccount2.AccountID,
                FullName = "Trần Thị Thu Hà",
                Phone = "0934567890",
                Address = "456 Nguyễn Huệ, Phường Bến Nghé, Quận 1, TP.HCM"
            };

            var cust3 = new Customer
            {
                AccountID = null, // Khách vãng lai mua tại quầy
                FullName = "Lê Tuấn Kiệt",
                Phone = "0977889900",
                Address = "789 Võ Văn Ngân, TP. Thủ Đức, TP.HCM"
            };

            context.Customers.AddRange(cust1, cust2, cust3);
            await context.SaveChangesAsync();
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
                StartDate = DateTime.Today.AddDays(-30),
                EndDate = DateTime.Today.AddDays(60),
                Description = "Giảm 10% cho toàn bộ hóa đơn từ 200.000đ",
                Status = PromotionStatus.Active
            };

            context.Promotions.Add(promo1);
            await context.SaveChangesAsync();

            var promoInvoice = new PromotionInvoice
            {
                PromotionID = promo1.PromotionID,
                MinOrderAmount = 200000m,
                DiscountPercent = 10m,
                MaxDiscountAmount = 100000m
            };

            context.PromotionInvoices.Add(promoInvoice);
            await context.SaveChangesAsync();
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
    }
}
