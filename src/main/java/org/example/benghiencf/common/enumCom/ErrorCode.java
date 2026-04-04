package org.example.benghiencf.common.enumCom;

import lombok.Getter;
import org.springframework.http.HttpStatus;

@Getter
public enum ErrorCode {
    // --- 1. Nhóm lỗi hệ thống (System Errors) ---
    UNCATEGORIZED_EXCEPTION(9999, "Lỗi hệ thống chưa xác định", HttpStatus.INTERNAL_SERVER_ERROR),
    INVALID_KEY(1003, "Cấu hình Error Key không hợp lệ", HttpStatus.INTERNAL_SERVER_ERROR),
    DATABASE_ERROR(9001, "Lỗi truy vấn cơ sở dữ liệu", HttpStatus.SERVICE_UNAVAILABLE),

    // --- 2. Nhóm xác thực & Phân quyền (Auth & Security) ---
    UNAUTHENTICATED(1001, "Vui lòng đăng nhập để tiếp tục", HttpStatus.UNAUTHORIZED),
    UNAUTHORIZED(1002, "Bạn không có quyền thực hiện hành động này", HttpStatus.FORBIDDEN),
    INVALID_TOKEN(4003, "Mã token không hợp lệ hoặc đã bị giả mạo", HttpStatus.UNAUTHORIZED),
    TOKEN_EXPIRED(4004, "Phiên làm việc đã hết hạn, vui lòng đăng nhập lại", HttpStatus.UNAUTHORIZED),
    VALIDATE_LOGIN(1010, "Tài khoản hoặc mật khẩu không chính xác", HttpStatus.UNAUTHORIZED),

    // --- 3. Nhóm lỗi dữ liệu đầu vào (Validation Errors) ---
    INVALID_MESSAGE(1004, "Dữ liệu gửi lên không đúng định dạng", HttpStatus.BAD_REQUEST),
    INVALID_PASSWORD_FORMAT(1012, "Mật khẩu không đạt yêu cầu bảo mật", HttpStatus.BAD_REQUEST),
    OLD_PASSWORD_INCORRECT(1008, "Mật khẩu cũ không chính xác", HttpStatus.BAD_REQUEST),

    // --- 4. Nhóm Doanh Nghiệp (Tenant Errors) ---
    TENANT_NOT_FOUND(4041, "Không tìm thấy doanh nghiệp yêu cầu", HttpStatus.NOT_FOUND),
    SLUG_ALREADY_EXISTS(4001, "Đường dẫn (Slug) này đã được sử dụng", HttpStatus.CONFLICT), // 409 Conflict chuẩn hơn 400
    TENANT_NOT_VERIFIED(4002, "Đoàn lân này chưa hoàn tất xác thực", HttpStatus.FORBIDDEN),
    REGISTRATION_TIMEOUT(1011, "Hết thời gian kích hoạt, dữ liệu đăng ký đã bị xóa", HttpStatus.GONE), // 410 Gone

    // --- 5. Nhóm Người dùng (User & Role Errors) ---
    USER_NOT_FOUND(1007, "Người dùng không tồn tại trong hệ thống", HttpStatus.NOT_FOUND),
    USER_ALREADY_EXISTS(1005, "Tên đăng nhập đã tồn tại", HttpStatus.CONFLICT),
    EMAIL_ALREADY_EXISTS(1006, "Email đã được đăng ký trước đó", HttpStatus.CONFLICT),
    ROLE_NOT_FOUND(1009, "Vai trò (Role) không tồn tại trên hệ thống", HttpStatus.NOT_FOUND),
    USER_NOT_ACTIVE(1013, "Tài khoản hiện đang bị khóa", HttpStatus.FORBIDDEN);
    private final int code;
    private final String message;
    private final HttpStatus statusCode;

    ErrorCode(int code, String message, HttpStatus statusCode) {
        this.code = code;
        this.message = message;
        this.statusCode = statusCode;
    }
}

