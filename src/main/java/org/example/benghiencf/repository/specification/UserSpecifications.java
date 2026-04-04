package org.example.benghiencf.repository.specification;

import org.example.benghiencf.entity.User;
import org.springframework.data.jpa.domain.Specification;
import org.springframework.util.StringUtils;

public class UserSpecifications {

    // Lọc theo tên (like %name%)
    public static Specification<User> hasFullName(String fullName) {
        return (root, query, cb) ->
                StringUtils.hasText(fullName) ? cb.like(root.get("fullName"), "%" + fullName + "%") : null;
    }

    // Lọc theo số điện thoại
    public static Specification<User> hasPhone(String phone) {
        return (root, query, cb) ->
                StringUtils.hasText(phone) ? cb.equal(root.get("phone"), phone) : null;
    }

    // Lọc theo Role Name
    public static Specification<User> hasRoleName(String roleName) {
        return (root, query, cb) -> {
            if (!StringUtils.hasText(roleName)) return null;
            // Join sang bảng Role để check name
            return cb.equal(root.join("role").get("name"), roleName);
        };
    }
}