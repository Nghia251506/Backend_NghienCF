package org.example.benghiencf.exception;

import org.example.benghiencf.common.enumCom.ErrorCode;
import org.example.benghiencf.common.res.ApiResponse;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.security.access.AccessDeniedException;
import org.springframework.web.bind.annotation.ExceptionHandler;
import org.springframework.web.bind.annotation.RestControllerAdvice;

@RestControllerAdvice
public class GlobalExceptionHandler {

    // 1. Bắt lỗi nghiệp vụ do mình tự throw
    @ExceptionHandler(value = AppException.class)
    public ResponseEntity<ApiResponse<?>> handleAppException(AppException exception) {
        ErrorCode errorCode = exception.getErrorCode();
        return ResponseEntity.status(errorCode.getStatusCode())
                .body(ApiResponse.builder()
                        .code(errorCode.getCode())
                        .message(errorCode.getMessage())
                        .build());
    }

    // 2. Bắt lỗi Validation (@Valid ở Controller) - CỰC KỲ QUAN TRỌNG
    @ExceptionHandler(value = org.springframework.web.bind.MethodArgumentNotValidException.class)
    public ResponseEntity<ApiResponse<?>> handleValidation(org.springframework.web.bind.MethodArgumentNotValidException exception) {
        // Lấy thông báo lỗi đầu tiên hoặc gom hết vào map errors
        String message = exception.getBindingResult().getFieldError().getDefaultMessage();

        return ResponseEntity.badRequest()
                .body(ApiResponse.builder()
                        .code(ErrorCode.INVALID_MESSAGE.getCode())
                        .message(message != null ? message : ErrorCode.INVALID_MESSAGE.getMessage())
                        .build());
    }

    // 3. Bắt lỗi phân quyền (Access Denied)
    @ExceptionHandler(value = AccessDeniedException.class)
    public ResponseEntity<ApiResponse<?>> handleAccessDeniedException(AccessDeniedException exception) {
        ErrorCode errorCode = ErrorCode.UNAUTHORIZED;
        return ResponseEntity.status(errorCode.getStatusCode())
                .body(ApiResponse.builder()
                        .code(errorCode.getCode())
                        .message(errorCode.getMessage())
                        .build());
    }

    // 4. Bắt tất cả các lỗi còn lại (System Error)
    @ExceptionHandler(value = Exception.class) // Đổi từ RuntimeException thành Exception
    public ResponseEntity<ApiResponse<?>> handleGenericException(Exception exception) {
        // Log lỗi ra để mình còn biết mà fix, chứ đừng nuốt chửng nó
        // log.error("Exception: ", exception);

        return ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR)
                .body(ApiResponse.builder()
                        .code(ErrorCode.UNCATEGORIZED_EXCEPTION.getCode())
                        .message(ErrorCode.UNCATEGORIZED_EXCEPTION.getMessage())
                        .build());
    }
}