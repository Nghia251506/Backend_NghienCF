package org.example.benghiencf.mapper;

import org.example.benghiencf.dto.res.auth.AuthResponse;
import org.example.benghiencf.entity.User;
import org.mapstruct.Mapper;
import org.mapstruct.Mapping;
import org.mapstruct.factory.Mappers;

@Mapper(componentModel = "spring") // Để Spring quản lý như một Bean (@Autowired được)
public interface AuthMapper {

    // Nếu ông giáo không dùng @Autowired, có thể dùng INSTANCE này
    AuthMapper INSTANCE = Mappers.getMapper(AuthMapper.class);

    @Mapping(target = "accessToken", ignore = true)  // Token sẽ được set thủ công từ JWT Service
    @Mapping(target = "refreshToken", ignore = true) // Tương tự token
    @Mapping(target = "role", source = "role.name")  // Lấy tên Role gán vào String role
    AuthResponse toAuthResponse(User user);

    // Thêm phương thức để update token vào response sau khi đã có object cơ bản
    @Mapping(target = "accessToken", source = "accessToken")
    @Mapping(target = "refreshToken", source = "refreshToken")
    @Mapping(target = "role", source = "user.role.name")
    AuthResponse toAuthResponse(User user, String accessToken, String refreshToken);
}