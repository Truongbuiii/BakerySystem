# BakerySystem - Clean Architecture Solution

Hệ thống quản lý tiệm bánh BakerySystem được xây dựng theo kiến trúc **Clean Architecture** trên nền tảng **.NET 10**.

---

## 🏗 Cấu trúc Solution

Hệ thống bao gồm các lớp (Layers) theo chuẩn Clean Architecture:

```text
BakerySystem/
├── docs/                                # Tài liệu kiến trúc và thiết kế hệ thống
│   └── architecture.md
├── src/
│   ├── 1.Core/                          # Trung tâm của ứng dụng (Domain-Driven Design)
│   │   ├── BakerySystem.Domain/         # Entities, Value Objects, Domain Events, Enums, Exceptions
│   │   └── BakerySystem.Shared/         # Common utilities, constants, shared contracts
│   ├── 2.Application/                   # Ca sử dụng (Use Cases) của hệ thống
│   │   └── BakerySystem.Application/    # Commands/Queries (CQRS), DTOs, Interfaces, Validators
│   ├── 3.Infrastructure/                # Hiện thực các giao tiếp hạ tầng & dịch vụ bên ngoài
│   │   ├── BakerySystem.Infrastructure/ # Database (EF Core), Repositories, Caching, Auth
│   │   └── BakerySystem.Integration/    # Payment gateways, 3rd party APIs, Mail/SMS services
│   ├── 4.API/                           # Cổng kết nối Backend
│   │   └── BakerySystem.API/            # ASP.NET Core Web API, Middleware, Endpoints, Swagger/OpenAPI
│   └── 5.Presentation/                  # 2 Ứng dụng giao diện người dùng
│       ├── BakerySystem.WebAdmin/       # Blazor Server Web App dành cho quản trị viên & nhân viên tiệm bánh
│       └── BakerySystem.MobileApp/      # .NET MAUI Blazor Hybrid App dành cho khách hàng đặt bánh (Android/iOS)
├── .cursorrules                         # Quy chuẩn lập trình & quy tắc kiến trúc cho AI/Developers
├── .gitignore                           # Git ignore chuẩn cho dự án .NET
├── BakerySystem.sln                     # Visual Studio Solution chuẩn (Classic Format)
├── BakerySystem.slnx                    # Modern XML Solution format (.NET 9/10+)
└── README.md
```

---

## ⚡ Yêu cầu môi trường

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (hoặc tương đương)
- Visual Studio 2022 (v17.10+) hoặc Visual Studio Code / JetBrains Rider
- Môi trường .NET MAUI Workload (để phát triển Mobile App cho khách hàng):
  ```bash
  dotnet workload install maui
  ```

---

## 🚀 Hướng dẫn Build & Run

### 1. Build toàn bộ Solution
```bash
dotnet build BakerySystem.sln
```

### 2. Chạy Web API Backend
```bash
dotnet run --project src/4.API/BakerySystem.API
```

### 3. Chạy Web Quản trị (Admin)
```bash
dotnet run --project src/5.Presentation/BakerySystem.WebAdmin
```

### 4. Chạy Mobile App cho Khách hàng
```bash
dotnet build src/5.Presentation/BakerySystem.MobileApp/BakerySystem.MobileApp.csproj
```

---

## 📐 Nguyên tắc phụ thuộc (Dependency Rule)

- **Domain**: Độc lập hoàn toàn, không phụ thuộc vào bất kỳ project nào.
- **Application**: Chỉ phụ thuộc vào `Domain` (và `Shared`).
- **Infrastructure**: Triển khai các interface từ `Application`, phụ thuộc vào `Application` (và `Domain`).
- **API**: Phụ thuộc vào `Application` (gọi Use Cases) và `Infrastructure` (đăng ký Dependency Injection).
- **Presentation**:
  - `WebAdmin`: Giao diện quản trị, gọi API Backend hoặc Application.
  - `MobileApp`: Giao diện ứng dụng di động cho khách hàng, giao tiếp với Web API qua HTTP REST endpoints.
