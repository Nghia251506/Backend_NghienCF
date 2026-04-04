package org.example.benghiencf.dto.res.auth;

import lombok.*;

@Getter
@Setter
@Builder
@NoArgsConstructor
@AllArgsConstructor
public class AuthResponse {

    private String accessToken;
    private String refreshToken;

    private String username;
    private String role; // Trả về "ADMIN", "USER"... để Client check quyền nhanh

    @Builder.Default
    private String tokenType = "Bearer";
}
