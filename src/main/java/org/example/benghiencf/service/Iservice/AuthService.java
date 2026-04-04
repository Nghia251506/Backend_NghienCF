package org.example.benghiencf.service.Iservice;

import org.example.benghiencf.dto.res.auth.AuthResponse;
import org.example.benghiencf.dto.req.auth.LoginRequest;
import org.example.benghiencf.dto.req.auth.RegisterRequest;

public interface AuthService {

    /**
     * Xử lý đăng nhập, trả về bộ đôi Access & Refresh Token
     */
    AuthResponse login(LoginRequest request);

    /**
     * Đăng ký tài khoản mới (thường mặc định Role là USER)
     */
    AuthResponse register(RegisterRequest request);

    /**
     * Làm mới Access Token bằng Refresh Token khi Access Token hết hạn
     */
    AuthResponse refreshToken(String refreshToken);

    /**
     * Đăng xuất (Xóa token hoặc cho vào blacklist nếu cần)
     */
    void logout(String refreshToken);
}