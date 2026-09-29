# Kiến trúc hệ thống BakerySystem (Clean Architecture)

Tài liệu thiết kế kiến trúc kỹ thuật hệ thống **BakerySystem**, áp dụng mô hình **Clean Architecture** (Onion / Hexagonal Architecture) trên nền tảng **.NET 10**.

---

## 1. Tổng quan Kiến trúc

Mục tiêu cốt lõi của Clean Architecture trong BakerySystem là:
- **Độc lập với Frameworks**: Logic nghiệp vụ không bị phụ thuộc vào ASP.NET Core, Entity Framework Core hay bất kỳ thư viện UI nào.
- **Dễ dàng Kiểm thử (Testable)**: Các quy tắc nghiệp vụ (Business Rules) có thể kiểm thử độc lập mà không cần database, web server hay UI.
- **Độc lập với Giao diện (UI-Independent)**: Hệ thống phục vụ 2 đối tượng người dùng qua 2 ứng dụng độc lập:
  - **Quản trị viên & Nhân viên**: Quản lý qua ứng dụng Web Admin (`BakerySystem.WebAdmin` - Blazor).
  - **Khách hàng**: Đặt bánh qua ứng dụng Di động (`BakerySystem.MobileApp` - .NET MAUI Blazor Hybrid).
- **Độc lập với Cơ sở dữ liệu (Database-Independent)**: Có thể chuyển đổi giữa SQL Server, PostgreSQL hoặc NoSQL mà không ảnh hưởng tới Domain và Application.

---

## 2. Sơ đồ Luồng Phụ thuộc (Dependency Flow)

```mermaid
flowchart TD
    subgraph Presentation["5. Presentation Layer"]
        MobileApp["BakerySystem.MobileApp (MAUI Blazor dành cho Khách hàng)"]
        WebAdmin["BakerySystem.WebAdmin (Blazor dành cho Quản trị viên)"]
    end

    subgraph API["4. API Layer"]
        WebAPI["BakerySystem.API (ASP.NET Core Minimal API / Controller)"]
    end

    subgraph Infrastructure["3. Infrastructure Layer"]
        Infra["BakerySystem.Infrastructure (EF Core, Repositories, Caching)"]
        Integration["BakerySystem.Integration (Payments, Email, SMS)"]
    end

    subgraph Application["2. Application Layer"]
        App["BakerySystem.Application (CQRS, MediatR, DTOs, Interfaces, Validators)"]
    end

    subgraph Core["1. Core Layer"]
        Domain["BakerySystem.Domain (Entities, Value Objects, Domain Events)"]
        Shared["BakerySystem.Shared (Common Utils, Constants, Enums)"]
    end

    MobileApp -->|HTTP REST Client| WebAPI
    WebAdmin -->|HTTP / Service Call| WebAPI

    WebAPI -->|References| App
    WebAPI -->|References DI only| Infra
    WebAPI -->|References DI only| Integration

    Infra -->|Implements Interfaces| App
    Integration -->|Implements Interfaces| App

    App -->|References| Domain
    App -->|References| Shared
    Domain -.-> Shared
```

---

## 3. Trách nhiệm chi tiết của từng tầng

### 3.1. Tầng 1: Core (`1.Core`)
- **`BakerySystem.Domain`**:
  - Trái tim của hệ thống, chứa các mô hình nghiệp vụ độc lập:
    - **Entities**: Bánh (Cake/BakeryItem), Đơn hàng (Order), Khách hàng (Customer), Hóa đơn (Invoice), v.v.
    - **Value Objects**: Tiền tệ (Money), Địa chỉ giao hàng (DeliveryAddress).
    - **Domain Events**: `OrderPlacedEvent`, `PaymentCompletedEvent`.
    - **Domain Exceptions**: Ngoại lệ logic nghiệp vụ đặc thù.
  - *Nguyên tắc*: Không tham chiếu bất kỳ thư viện bên ngoài hoặc tầng nào khác.
- **`BakerySystem.Shared`**:
  - Chứa các định nghĩa dùng chung, hằng số, helper thuần túy (DateTime utils, string extensions, pagination wrapper).

### 3.2. Tầng 2: Application (`2.Application`)
- **`BakerySystem.Application`**:
  - Chứa các Ca sử dụng (Use Cases) của hệ thống:
    - Triển khai mô hình **CQRS** (Command Query Responsibility Segregation).
    - Commands: `CreateOrderCommand`, `UpdateProductCommand`.
    - Queries: `GetOrderByIdQuery`, `ListPopularCakesQuery`.
    - DTOs (Data Transfer Objects) và Mappings.
    - Interfaces cho Repositories (vd: `IOrderRepository`, `IUnitOfWork`) và Services (vd: `IEmailService`, `IPaymentGateway`).
    - Validation dữ liệu đầu vào sử dụng **FluentValidation**.
  - *Nguyên tắc*: Chỉ phụ thuộc vào `Domain` và `Shared`.

### 3.3. Tầng 3: Infrastructure (`3.Infrastructure`)
- **`BakerySystem.Infrastructure`**:
  - Hiện thực (Implementation) các interfaces được định nghĩa ở tầng Application:
    - **Persistence**: `ApplicationDbContext` (Entity Framework Core), Migrations, Entity Configurations.
    - **Repositories**: Cụ thể hóa `OrderRepository`, `UnitOfWork`.
    - **Identity & Auth**: JWT Token generator, Identity services.
    - **Caching**: Redis / Memory Cache.
- **`BakerySystem.Integration`**:
  - Tích hợp dịch vụ bên thứ 3:
    - Cổng thanh toán (VNPay, MoMo, Stripe, ZaloPay).
    - Dịch vụ gửi thông báo/email (SendGrid, Twilio).

### 3.4. Tầng 4: API (`4.API`)
- **`BakerySystem.API`**:
  - Cổng kết nối HTTP Backend RESTful API:
    - ASP.NET Core Web API (Controllers hoặc Minimal APIs).
    - Cấu hình Middleware (Global Exception Handler, CORS, Rate Limiting, Authentication/Authorization).
    - Swagger / OpenAPI documentation.
    - Cấu hình Dependency Injection (Wiring giữa Application và Infrastructure trong `Program.cs`).

### 3.5. Tầng 5: Presentation (`5.Presentation`)
Chỉ bao gồm đúng **2 giao diện người dùng**:
1. **`BakerySystem.WebAdmin`**: Giao diện Quản trị viên & Nhân viên (Blazor Server) quản lý đơn hàng, kho bánh, danh mục, doanh thu.
2. **`BakerySystem.MobileApp`**: Ứng dụng di động dành riêng cho Khách hàng (.NET MAUI Blazor Hybrid) để xem menu bánh, đặt hàng, nhận thông báo đẩy và thanh toán trực tuyến trên iOS và Android.

---

## 4. Luồng xử lý một Request (Request Pipeline)

```mermaid
sequenceDiagram
    autonumber
    actor Customer as Khách Hàng (MobileApp)
    actor Admin as Quản Trị Viên (WebAdmin)
    participant API as BakerySystem.API
    participant App as BakerySystem.Application
    participant Infra as BakerySystem.Infrastructure
    participant DB as Database (SQL Server/Postgres)

    Customer->>API: HTTP POST /api/orders (Tạo đơn đặt bánh)
    API->>API: Validate Token & Model
    API->>App: Gửi CreateOrderCommand
    App->>App: FluentValidation kiểm tra hợp lệ
    App->>Infra: Lưu đơn vào Repository
    Infra->>DB: SaveChangesAsync()
    DB-->>Infra: Thành công
    App-->>API: Trả về kết quả OrderId
    API-->>Customer: HTTP 201 Created (Đặt bánh thành công)

    Admin->>API: HTTP GET /api/orders (Xem đơn mới)
    API->>App: Gửi GetPendingOrdersQuery
    App->>Infra: Truy vấn danh sách
    Infra->>DB: Query Database
    DB-->>Infra: List<Order>
    App-->>API: List<OrderDto>
    API-->>Admin: Hiển thị trên WebAdmin Dashboard
```

---

## 5. Quy tắc mở rộng & phát triển
1. **Không vi phạm Dependency Rule**: Không bao giờ đưa logic database (EF Core, SQL) vào tầng Domain hay Application.
2. **Interface ở đâu, ai triển khai?**: Interface luôn đặt ở tầng trong (`Application`), triển khai nằm ở tầng ngoài (`Infrastructure`).
3. **Mã hóa bất biến**: Dùng `record` cho Commands/Queries/DTOs; Entities phải bảo toàn tính hợp lệ (invariants) qua private setters và public methods.
