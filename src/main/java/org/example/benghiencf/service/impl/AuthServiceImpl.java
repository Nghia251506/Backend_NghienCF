package org.example.benghiencf.service.impl;

import lombok.RequiredArgsConstructor;
import org.example.benghiencf.common.enumCom.ErrorCode;
import org.example.benghiencf.dto.res.auth.AuthResponse;
import org.example.benghiencf.dto.req.auth.LoginRequest;
import org.example.benghiencf.dto.req.auth.RegisterRequest;
import org.example.benghiencf.entity.User;
import org.example.benghiencf.exception.AppException;
import org.example.benghiencf.mapper.AuthMapper;
import org.example.benghiencf.repository.RoleRepository;
import org.example.benghiencf.repository.UserRepository;
import org.example.benghiencf.security.JwtTokenProvider;
import org.example.benghiencf.service.Iservice.AuthService;
import org.springframework.http.ResponseCookie;
import org.springframework.security.crypto.password.PasswordEncoder;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

@Service
@RequiredArgsConstructor
public class AuthServiceImpl implements AuthService {

    private final UserRepository userRepository;
    private final PasswordEncoder passwordEncoder;
    private final JwtTokenProvider jwtTokenProvider;
    private final RoleRepository roleRepository;
    private final AuthMapper authMapper;

    @Override
    public AuthResponse login(LoginRequest request) {
        // 1. Tìm user theo username
        User user = userRepository.findByUsername(request.getUsername())
                .orElseThrow(() -> new AppException(ErrorCode.VALIDATE_LOGIN));

        // 2. Kiểm tra mật khẩu
        if (!passwordEncoder.matches(request.getPassword(), user.getPassword())) {
            throw new AppException(ErrorCode.VALIDATE_LOGIN);
        }

        // 3. Tạo bộ đôi token
        String accessToken = jwtTokenProvider.generateAccessToken(user);
        String refreshToken = jwtTokenProvider.generateRefreshToken(user);

        // 4. Map sang AuthResponse và trả về
        return authMapper.toAuthResponse(user, accessToken, refreshToken);
    }

    @Override
    @Transactional
    public AuthResponse register(RegisterRequest request) {
        // 1. Check trùng username
        if (userRepository.existsByUsername(request.getUsername())) {
            throw new AppException(ErrorCode.USER_ALREADY_EXISTS);
        }

        // 2. Mã hóa mật khẩu và lưu user mới
        User user = User.builder()
                .username(request.getUsername())
                .password(passwordEncoder.encode(request.getPassword()))
                .fullName(request.getFullName())
                .phone(request.getPhone())
                 .role(roleRepository.findByName("ADMIN"))
                .build();

        userRepository.save(user);

        // 3. Đăng ký xong cho login luôn
        String accessToken = jwtTokenProvider.generateAccessToken(user);
        String refreshToken = jwtTokenProvider.generateRefreshToken(user);

        return authMapper.toAuthResponse(user, accessToken, refreshToken);
    }

    @Override
    public AuthResponse refreshToken(String refreshToken) {
        // 1. Validate refresh token
        if (!jwtTokenProvider.validateToken(refreshToken)) {
            throw new AppException(ErrorCode.TOKEN_EXPIRED);
        }

        // 2. Lấy user từ token
        String username = jwtTokenProvider.getUsernameFromToken(refreshToken);
        User user = userRepository.findByUsername(username)
                .orElseThrow(() -> new AppException(ErrorCode.USER_NOT_FOUND));

        // 3. Tạo access token mới
        String newAccessToken = jwtTokenProvider.generateAccessToken(user);

        return authMapper.toAuthResponse(user, newAccessToken, refreshToken);
    }

    @Override
    public void logout(String refreshToken) {
        // Tạm thời logout ở client bằng cách xóa token.
        // Nếu muốn gắt hơn, ông giáo có thể lưu token vào blacklist trong Redis ở đây.
    }
}