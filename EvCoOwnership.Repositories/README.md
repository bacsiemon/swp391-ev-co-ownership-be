Ở đây chứa các repository dùng để truy xuất dữ liệu từ Database.


## Commands
Scaffold bằng Package Manager Console
>`Scaffold-DbContext "<ConnectionString>" Npgsql.EntityFrameworkCore.PostgreSQL -o Models`

Scaffold bằng CLI 
>`dotnet ef dbcontext scaffold "<ConnectionString>" Npgsql.EntityFrameworkCore.PostgreSQL -o Models`

Các lệnh EntityFramework Core khác:

**Database:**
>`dotnet ef dbcontext info` - Hiển thị thông tin về DbContext  
>`dotnet ef dbcontext list` - Liệt kê tất cả DbContext trong project  
>`dotnet ef dbcontext script` - Tạo SQL script từ DbContext  

**Scaffold options:**
> `-f` - Ghi đè các file đã tồn tại  
>`-data-annotations` - Sử dụng Data Annotations thay vì Fluent API  
>`-context-dir <path>` - Thư mục chứa DbContext  
>`-output-dir <path>` - Thư mục output (tương đương -o)  
>`-schema <schema>` - Chỉ scaffold các table từ schema cụ thể  
>`-table <table>` - Chỉ scaffold table cụ thể  
>`-no-build` - Không build project trước khi chạy  
>`-project <project>` - Chỉ định project target  
>`-startup-project <project>` - Chỉ định startup project  

**Ví dụ scaffold nâng cao:**
>`dotnet ef dbcontext scaffold "<ConnectionString>" Npgsql.EntityFrameworkCore.PostgreSQL -o Models --context-dir Context --data-annotations -f`


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

