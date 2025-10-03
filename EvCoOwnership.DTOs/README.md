Ở đây chứa các DTOs (Data Transfer Objects) dùng để truyền dữ liệu giữa các lớp.


## Extension Methods
Có thể tạo Extension Methods ở folder "Mapping" tầng Service để Map DTO sang entity, đỡ phải dài dòng ở mấy class service hoặc xài Automapper.

Ví dụ:
```csharp
public static class UserExtensions
{
	public static UserDto ToDto(this User user)
	{
		return new UserDto
		{
			Id = user.Id,
			Name = user.Name,
			Email = user.Email
			// Map các thuộc tính khác
		};
	}
	public static User ToEntity(this UserDto userDto)
	{
		return new User
		{
			Id = userDto.Id,
			Name = userDto.Name,
			Email = userDto.Email
			// Map các thuộc tính khác
		};
	}
}
```
Sử dụng trong service
``` csharp
var userDto = user.ToDto();
var user = userDto.ToEntity();
```
