package org.example.benghiencf.controller;

import jakarta.validation.Valid;
import lombok.RequiredArgsConstructor;
import org.example.benghiencf.common.res.ApiResponse;
import org.example.benghiencf.dto.res.auth.AuthResponse;
import org.example.benghiencf.dto.req.auth.LoginRequest;
import org.example.benghiencf.dto.req.auth.RegisterRequest;
import org.example.benghiencf.service.Iservice.AuthService;
import org.springframework.http.HttpHeaders;
import org.springframework.http.ResponseCookie;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

@RestController
@RequestMapping("/api/v1/auth")
@RequiredArgsConstructor
public class AuthController {

    private final AuthService authService;

    @PostMapping("/login")
    public ResponseEntity<ApiResponse<AuthResponse>> login(
            @RequestBody @Valid LoginRequest request) {

        AuthResponse result = authService.login(request);

        // Tạo Cookie cho Access Token (Sống theo expiration của token)
        ResponseCookie accessCookie = createCookie("access_token",
                result.getAccessToken(), 3600000); // 1h

        // Tạo Cookie cho Refresh Token (Sống lâu hơn)
        ResponseCookie refreshCookie = createCookie("refresh_token",
                result.getRefreshToken(), 604800000); // 7 ngày

        return ResponseEntity.ok()
                .header(HttpHeaders.SET_COOKIE, accessCookie.toString())
                .header(HttpHeaders.SET_COOKIE, refreshCookie.toString())
                .body(ApiResponse.<AuthResponse>builder()
                        .data(result) // Vẫn trả về data nếu Client cần (Mobile), Web thì dùng Cookie
                        .message("Đăng nhập thành công")
                        .build());
    }

    @PostMapping("/register")
    public ApiResponse<AuthResponse> register(@RequestBody @Valid RegisterRequest request) {
        AuthResponse result = authService.register(request);
        return ApiResponse.<AuthResponse>builder()
                .data(result)
                .message("Đăng ký tài khoản thành công")
                .build();
    }

    @PostMapping("/refresh-token")
    public ApiResponse<AuthResponse> refreshToken(@RequestParam("token") String token) {
        AuthResponse result = authService.refreshToken(token);
        return ApiResponse.<AuthResponse>builder()
                .data(result)
                .message("Làm mới token thành công")
                .build();
    }

    @PostMapping("/logout")
    public ResponseEntity<ApiResponse<Void>> logout() {
        // Xóa Cookie bằng cách set Max-Age = 0
        ResponseCookie deleteAccess = createCookie("access_token", "", 0);
        ResponseCookie deleteRefresh = createCookie("refresh_token", "", 0);

        return ResponseEntity.ok()
                .header(HttpHeaders.SET_COOKIE, deleteAccess.toString())
                .header(HttpHeaders.SET_COOKIE, deleteRefresh.toString())
                .body(ApiResponse.<Void>builder()
                        .message("Đăng xuất thành công, cookie đã được xóa")
                        .build());
    }

    private ResponseCookie createCookie(String name, String value, long durationMs) {
        return ResponseCookie.from(name, value)
                .httpOnly(true)       // Chống XSS
                .secure(false)         // Để false nếu dev ở localhost, true nếu dùng HTTPS
                .path("/")
                .maxAge(durationMs / 1000)
                .sameSite("Lax")      // Chống CSRF
                .build();
    }
}