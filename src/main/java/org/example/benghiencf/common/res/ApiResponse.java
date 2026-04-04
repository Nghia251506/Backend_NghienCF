package org.example.benghiencf.common.res;

import com.fasterxml.jackson.annotation.JsonInclude;
import lombok.AllArgsConstructor;
import lombok.Builder;
import lombok.Data;
import lombok.NoArgsConstructor;

import java.time.Instant;
import java.util.Map;

@Data
@Builder
@AllArgsConstructor
@NoArgsConstructor
@JsonInclude(JsonInclude.Include.NON_NULL)
public class ApiResponse<T> {
    @Builder.Default
    private int code = 200; // Mặc định là thành công
    private String message;
    private T data;
    private Map<String, String> errors;

    @Builder.Default
    private Long timestamp = Instant.now().toEpochMilli();
}