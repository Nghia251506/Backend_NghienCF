package org.example.benghiencf.exception;//package com.example.bezma.exception;
//
//import lombok.extern.slf4j.Slf4j;
//import com.example.bezma.repository.RefreshTokenRepository;
//import com.example.bezma.repository.TenantRepository;
//import com.example.bezma.repository.UserRepository;
//import org.springframework.data.redis.connection.Message;
//import org.springframework.data.redis.listener.KeyExpirationEventMessageListener;
//import org.springframework.data.redis.listener.RedisMessageListenerContainer;
//import org.springframework.stereotype.Component;
//import org.springframework.transaction.annotation.Transactional;
//
//@Component
//@Slf4j
//public class RedisExpirationListener extends KeyExpirationEventMessageListener {
//
//    private final TenantRepository tenantRepository;
//    private final UserRepository userRepository;
//    private final RefreshTokenRepository refreshTokenRepository;
//
//    public RedisExpirationListener(RedisMessageListenerContainer listenerContainer,
//                                   TenantRepository tenantRepository,
//                                   UserRepository userRepository,
//                                   RefreshTokenRepository refreshTokenRepository) {
//        super(listenerContainer);
//        this.tenantRepository = tenantRepository;
//        this.userRepository = userRepository;
//        this.refreshTokenRepository = refreshTokenRepository;
//    }
//
//    @Override
//    @Transactional // Quan trọng để commit việc xóa vào DB
//    public void onMessage(Message message, byte[] pattern) {
//        String expiredKey = message.toString();
//
//        if (expiredKey.startsWith("registration_checkpoint:")) {
//            try {
//                Long tenantId = Long.parseLong(expiredKey.split(":")[1]);
//                log.warn("Kích hoạt hết hạn! Đang dọn dẹp Tenant ID: {}", tenantId);
//
//                // 1. Xóa RefreshToken và User liên quan đến Tenant
//                // Ông giáo nhớ viết thêm các hàm deleteByTenantId trong các Repo nhé
//                refreshTokenRepository.deleteByTenantId(tenantId);
//                userRepository.deleteByTenantId(tenantId);
//
//                // 2. Xóa Tenant cuối cùng
//                tenantRepository.deleteById(tenantId);
//
//                log.info("Đã xóa sạch dữ liệu Tenant rác thành công.");
//            } catch (Exception e) {
//                log.error("Lỗi khi tự hủy dữ liệu: {}", e.getMessage());
//            }
//        }
//    }
//}
