package org.example.benghiencf.entity;

import jakarta.persistence.*;
import lombok.*;
import org.example.benghiencf.common.base.BaseEntity;

import java.math.BigDecimal;

@Entity
@Table(name = "packages")
@Getter
@Setter
@AllArgsConstructor
@NoArgsConstructor
@Builder
public class Package extends BaseEntity {
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    @Column(name = "id")
    private Long id;
    @Column(name = "name")
    private String name;
    @Column(name = "price")
    private BigDecimal price;
//    @ManyToMany(fetch = FetchType.LAZY)
//    @JoinTable(name ="package_types",joinColumns = @JoinColumn(name = "user_id"),
//            inverseJoinColumns = @JoinColumn(name = "permission_id"))
//    private PackageType types;
    @Column(name = "description", columnDefinition = "LONGTEXT")
    private String description;
}
