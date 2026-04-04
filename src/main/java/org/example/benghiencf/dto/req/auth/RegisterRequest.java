package org.example.benghiencf.dto.req.auth;

import lombok.Data;

@Data
public class RegisterRequest {
    private String fullName;
    private String phone;
    private String username;
    private String password;
}
