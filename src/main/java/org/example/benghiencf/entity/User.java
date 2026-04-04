package org.example.benghiencf.entity;
import com.fasterxml.jackson.annotation.JsonIgnore;
import com.fasterxml.jackson.annotation.JsonIgnoreProperties;
import jakarta.persistence.*;
import lombok.*;
import org.example.benghiencf.common.base.BaseEntity;
import org.springframework.security.core.GrantedAuthority;
import org.springframework.security.core.authority.SimpleGrantedAuthority;
import org.springframework.security.core.userdetails.UserDetails;

import java.util.Collection;
import java.util.HashSet;
import java.util.Set;

@Entity
@Table(name = "users", indexes = {
        @Index(name = "idx_user_fullname", columnList = "full_name"),
        @Index(name = "idx_user_phone", columnList = "phone")
})
@Getter
@Setter
@Builder
@NoArgsConstructor
@AllArgsConstructor
@JsonIgnoreProperties({"hibernateLazyInitializer", "handler"})
public class User extends BaseEntity implements UserDetails {
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    @Column(name = "id")
    private Long id;

    @Column(name = "full_name", nullable = false)
    private String fullName;
    @Column(name = "phone", nullable = false)
    private String phone;
    @Column(name = "user_name", nullable = false)
    private String username;
    @Column(name = "password", nullable = false)
    private String password;
    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(name = "role_id")
    private Role role;
    @Override
    @JsonIgnore
    public Collection<? extends GrantedAuthority> getAuthorities() {
        // Dùng Set để tự động loại bỏ các quyền trùng lặp
        Set<SimpleGrantedAuthority> authorities = new HashSet<>();

        // 1. Lấy quyền từ Role
        if (this.role != null ){
            authorities.add(new SimpleGrantedAuthority("ROLE_" + this.role.getName()));
        }

        return authorities;
    }

    @Override
    @JsonIgnore
    public String getPassword() { return this.password; }

    // Các hàm này trả về true và không liên quan tới DB
    @Override
    @Transient // Báo cho Hibernate: "Đừng có tìm cột này trong DB"
    @JsonIgnore
    public boolean isAccountNonExpired() { return true; }

    @Override
    @Transient
    @JsonIgnore
    public boolean isAccountNonLocked() { return true; }

    @Override
    @Transient
    @JsonIgnore
    public boolean isCredentialsNonExpired() { return true; }

    @Override
    @Transient
    @JsonIgnore
    public boolean isEnabled() { return true; }
}
