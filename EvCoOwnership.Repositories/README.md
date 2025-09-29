Ở đây chứa các repository dùng để truy xuất dữ liệu từ Database.


## Commands
Scaffold bằng Package Manager Console
>`Scaffold-DbContext "<ConnectionString>" Npgsql.EntityFrameworkCore.PostgreSQL -o Models`

Scaffold bằng CLI 
>`dotnet ef dbcontext scaffold "<ConnectionString>" Npgsql.EntityFrameworkCore.PostgreSQL -o Models`

Tìm ConnectionString trong file `appsettings.json` của project API.  
Lưu ý: Sau khi scaffold xong, cần chỉnh sửa lại các field Enum từ kiểu `int` thành enum tương ứng.

## Cấu trúc thư mục
- Context: Chứa DbContext
- Enums: Chứa các enum
- Interfaces: Chứa các interface
- Models: Chứa các class model
- Repositories: Chứa các repository & Generic Repository để kế thừa
- UoW: unit of work, dùng quản lý đống repository
- `RepositoryConfigurations.cs` chứa các configurations.

