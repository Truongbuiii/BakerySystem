CREATE DATABASE BakerySystem;
GO

USE BakerySystem;
GO

/* =========================================================
   1. ACCOUNT
   ========================================================= */
CREATE TABLE Account (
    AccountID INT IDENTITY(1,1) PRIMARY KEY,
    Username VARCHAR(50) NOT NULL,
    Password VARCHAR(255) NOT NULL,
    Role VARCHAR(20) NOT NULL,
    Status VARCHAR(20) NOT NULL DEFAULT 'Active',

    CONSTRAINT UQ_Account_Username UNIQUE (Username),
    CONSTRAINT CK_Account_Role CHECK (Role IN ('Admin', 'Employee', 'Customer')),
    CONSTRAINT CK_Account_Status CHECK (Status IN ('Active', 'Inactive'))
);
GO

/* =========================================================
   2. CUSTOMER
   ========================================================= */
CREATE TABLE Customer (
    CustomerID INT IDENTITY(1,1) PRIMARY KEY,
    AccountID INT NULL, -- Cho phép NULL nếu khách vãng lai hoặc không đăng ký tài khoản
    FullName NVARCHAR(100) NOT NULL,
    Phone VARCHAR(20),
    Address NVARCHAR(255),

    CONSTRAINT UQ_Customer_Account UNIQUE (AccountID),
    CONSTRAINT FK_Customer_Account FOREIGN KEY (AccountID) REFERENCES Account(AccountID)
);
GO

/* =========================================================
   3. EMPLOYEE
   ========================================================= */
CREATE TABLE Employee (
    EmployeeID INT IDENTITY(1,1) PRIMARY KEY,
    AccountID INT NOT NULL,
    FullName NVARCHAR(100) NOT NULL,
    Phone VARCHAR(20),

    CONSTRAINT UQ_Employee_Account UNIQUE (AccountID),
    CONSTRAINT FK_Employee_Account FOREIGN KEY (AccountID) REFERENCES Account(AccountID)
);
GO

/* =========================================================
   4. SUPPLIER
   ========================================================= */
CREATE TABLE Supplier (
    SupplierID INT IDENTITY(1,1) PRIMARY KEY,
    SupplierName NVARCHAR(150) NOT NULL,
    Phone VARCHAR(20),
    Address NVARCHAR(255)
);
GO

/* =========================================================
   5. CATEGORY
   ========================================================= */
CREATE TABLE Category (
    CategoryID INT IDENTITY(1,1) PRIMARY KEY,
    CategoryName NVARCHAR(100) NOT NULL,

    CONSTRAINT UQ_Category_CategoryName UNIQUE (CategoryName)
);
GO

/* =========================================================
   6. PRODUCT
   ========================================================= */
CREATE TABLE Product (
    ProductID INT IDENTITY(1,1) PRIMARY KEY,
    CategoryID INT NOT NULL,
    ProductName NVARCHAR(150) NOT NULL,
    Description NVARCHAR(500),
    Price DECIMAL(18,2) NOT NULL,
    Quantity INT NOT NULL DEFAULT 0,
    ImageURL VARCHAR(500),
    IsActive BIT NOT NULL DEFAULT 1,

    CONSTRAINT FK_Product_Category FOREIGN KEY (CategoryID) REFERENCES Category(CategoryID),
    CONSTRAINT CK_Product_Price CHECK (Price >= 0),
    CONSTRAINT CK_Product_Quantity CHECK (Quantity >= 0)
);
GO

/* =========================================================
   7. PROMOTION (Đã đồng bộ sang tiếng Anh & thêm PromotionCode)
   ========================================================= */
CREATE TABLE Promotion (
    PromotionID INT IDENTITY(1,1) PRIMARY KEY,
    PromotionCode VARCHAR(50) NOT NULL, -- Mã voucher nhập khi thanh toán
    PromotionName NVARCHAR(200) NOT NULL,
    StartDate DATE NOT NULL,
    EndDate DATE NOT NULL,
    Description NVARCHAR(500),
    Status VARCHAR(20) NOT NULL DEFAULT 'Active',

    CONSTRAINT UQ_Promotion_PromotionCode UNIQUE (PromotionCode),
    CONSTRAINT CK_Promotion_Date CHECK (EndDate >= StartDate),
    CONSTRAINT CK_Promotion_Status CHECK (Status IN ('Active', 'Inactive'))
);
GO

/* =========================================================
   8. PROMOTION INVOICE (Khuyến mãi trên tổng hóa đơn)
   ========================================================= */
CREATE TABLE PromotionInvoice (
    PromotionID INT PRIMARY KEY,
    MinOrderAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
    DiscountPercent DECIMAL(5,2) NOT NULL,
    MaxDiscountAmount DECIMAL(18,2),

    CONSTRAINT FK_PromotionInvoice_Promotion FOREIGN KEY (PromotionID) REFERENCES Promotion(PromotionID),
    CONSTRAINT CK_PromotionInvoice_MinOrderAmount CHECK (MinOrderAmount >= 0),
    CONSTRAINT CK_PromotionInvoice_DiscountPercent CHECK (DiscountPercent > 0 AND DiscountPercent <= 100),
    CONSTRAINT CK_PromotionInvoice_MaxDiscount CHECK (MaxDiscountAmount IS NULL OR MaxDiscountAmount >= 0)
);
GO

/* =========================================================
   9. PROMOTION PRODUCT (Khuyến mãi theo từng món bánh)
   ========================================================= */
CREATE TABLE PromotionProduct (
    PromotionID INT NOT NULL,
    ProductID INT NOT NULL,
    DiscountPercent DECIMAL(5,2) NOT NULL,
    MaxDiscountAmount DECIMAL(18,2),

    CONSTRAINT PK_PromotionProduct PRIMARY KEY (PromotionID, ProductID),
    CONSTRAINT FK_PromotionProduct_Promotion FOREIGN KEY (PromotionID) REFERENCES Promotion(PromotionID),
    CONSTRAINT FK_PromotionProduct_Product FOREIGN KEY (ProductID) REFERENCES Product(ProductID),
    CONSTRAINT CK_PromotionProduct_DiscountPercent CHECK (DiscountPercent > 0 AND DiscountPercent <= 100),
    CONSTRAINT CK_PromotionProduct_MaxDiscount CHECK (MaxDiscountAmount IS NULL OR MaxDiscountAmount >= 0)
);
GO

/* =========================================================
   10. IMPORT RECEIPT
   ========================================================= */
CREATE TABLE ImportReceipt (
    ReceiptID INT IDENTITY(1,1) PRIMARY KEY,
    SupplierID INT NOT NULL,
    EmployeeID INT NOT NULL,
    ReceiptDate DATETIME NOT NULL DEFAULT GETDATE(),
    TotalAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
    Note NVARCHAR(500),

    CONSTRAINT FK_ImportReceipt_Supplier FOREIGN KEY (SupplierID) REFERENCES Supplier(SupplierID),
    CONSTRAINT FK_ImportReceipt_Employee FOREIGN KEY (EmployeeID) REFERENCES Employee(EmployeeID),
    CONSTRAINT CK_ImportReceipt_TotalAmount CHECK (TotalAmount >= 0)
);
GO

/* =========================================================
   11. IMPORT RECEIPT DETAIL
   ========================================================= */
CREATE TABLE ImportReceiptDetail (
    ReceiptID INT NOT NULL,
    ProductID INT NOT NULL,
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(18,2) NOT NULL,
    SubTotal DECIMAL(18,2) NOT NULL,

    CONSTRAINT PK_ImportReceiptDetail PRIMARY KEY (ReceiptID, ProductID),
    CONSTRAINT FK_ImportReceiptDetail_Receipt FOREIGN KEY (ReceiptID) REFERENCES ImportReceipt(ReceiptID),
    CONSTRAINT FK_ImportReceiptDetail_Product FOREIGN KEY (ProductID) REFERENCES Product(ProductID),
    CONSTRAINT CK_ImportReceiptDetail_Quantity CHECK (Quantity > 0),
    CONSTRAINT CK_ImportReceiptDetail_UnitPrice CHECK (UnitPrice >= 0),
    CONSTRAINT CK_ImportReceiptDetail_SubTotal CHECK (SubTotal >= 0)
);
GO

/* =========================================================
   12. ORDERS (Đã fix EmployeeID NULL & thêm thông tin giao hàng)
   ========================================================= */
CREATE TABLE Orders (
    OrderID INT IDENTITY(1,1) PRIMARY KEY,
    CustomerID INT NOT NULL,
    EmployeeID INT NULL, -- Cho phép NULL khi khách đặt online chưa được duyệt
    PromotionID INT NULL,
    OrderDate DATETIME NOT NULL DEFAULT GETDATE(),
    TotalAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
    DiscountAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
    FinalAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
    
    -- Thông tin giao hàng & ghi chú bánh
    ReceiverName NVARCHAR(100),
    ReceiverPhone VARCHAR(20),
    ShippingAddress NVARCHAR(255),
    Note NVARCHAR(500), -- Ví dụ: Ghi chữ lên bánh, thời gian giao bánh cụ thể
    
    -- Thanh toán & Trạng thái
    PaymentMethod VARCHAR(30) NOT NULL DEFAULT 'COD', -- 'COD', 'BankTransfer', 'VNPAY', etc.
    PaymentStatus VARCHAR(20) NOT NULL DEFAULT 'Unpaid', -- 'Unpaid', 'Paid'
    Status VARCHAR(20) NOT NULL DEFAULT 'Pending',

    CONSTRAINT FK_Orders_Customer FOREIGN KEY (CustomerID) REFERENCES Customer(CustomerID),
    CONSTRAINT FK_Orders_Employee FOREIGN KEY (EmployeeID) REFERENCES Employee(EmployeeID),
    CONSTRAINT FK_Orders_PromotionInvoice FOREIGN KEY (PromotionID) REFERENCES PromotionInvoice(PromotionID),
    CONSTRAINT CK_Orders_TotalAmount CHECK (TotalAmount >= 0),
    CONSTRAINT CK_Orders_DiscountAmount CHECK (DiscountAmount >= 0),
    CONSTRAINT CK_Orders_FinalAmount CHECK (FinalAmount >= 0),
    CONSTRAINT CK_Orders_Discount_Not_Exceed_Total CHECK (DiscountAmount <= TotalAmount),
    CONSTRAINT CK_Orders_PaymentStatus CHECK (PaymentStatus IN ('Unpaid', 'Paid', 'Refunded')),
    CONSTRAINT CK_Orders_Status CHECK (Status IN ('Pending', 'Confirmed', 'Shipping', 'Completed', 'Cancelled'))
);
GO

/* =========================================================
   13. ORDER DETAIL
   ========================================================= */
CREATE TABLE OrderDetail (
    OrderID INT NOT NULL,
    ProductID INT NOT NULL,
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(18,2) NOT NULL,
    DiscountAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
    SubTotal DECIMAL(18,2) NOT NULL,

    CONSTRAINT PK_OrderDetail PRIMARY KEY (OrderID, ProductID),
    CONSTRAINT FK_OrderDetail_Order FOREIGN KEY (OrderID) REFERENCES Orders(OrderID),
    CONSTRAINT FK_OrderDetail_Product FOREIGN KEY (ProductID) REFERENCES Product(ProductID),
    CONSTRAINT CK_OrderDetail_Quantity CHECK (Quantity > 0),
    CONSTRAINT CK_OrderDetail_UnitPrice CHECK (UnitPrice >= 0),
    CONSTRAINT CK_OrderDetail_Discount CHECK (DiscountAmount >= 0),
    CONSTRAINT CK_OrderDetail_SubTotal CHECK (SubTotal >= 0)
);
GO