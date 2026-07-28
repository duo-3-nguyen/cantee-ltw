# Cantee LTW

## Cài đặt & Chạy

### 1. Cài đặt công cụ (dùng Scoop)

```powershell
scoop install postgresql dotnet-sdk
```

### 2. Khởi động PostgreSQL

```powershell
postgres
```

### 3. Cập nhật connection string (nếu cần)

Mặc định: `Host=localhost;Database=cantee_ltw;Username=postgres;Password=`

Sửa trong `appsettings.json` nếu bạn dùng mật khẩu.

### 4. Tạo Migration & cập nhật database

```powershell
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### 5. Chạy project

```powershell
dotnet run
```

Mở trình duyệt: `http://localhost:5138/login.html`
Mở trình duyệt: `http://localhost:5138/scalar/v1`
