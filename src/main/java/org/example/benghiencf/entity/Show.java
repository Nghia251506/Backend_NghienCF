package org.example.benghiencf.entity;
import jakarta.persistence.*;
import lombok.*;
import org.example.benghiencf.common.base.BaseEntity;

import java.time.LocalDateTime;

@Entity
@Table(name = "shows")
@Getter
@Setter
@AllArgsConstructor
@NoArgsConstructor
@Builder
public class Show extends BaseEntity{
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    @Column(name = "id")
    private Long id;
    @Column(name = "title", nullable = true)
    private String title;
    @Column(name = "date",  nullable = true)
    private LocalDateTime date;
    @Column(name = "location")
    private String localtion;
    @Column(name = "image_url")
    private String image;
    @Column(name = "description", columnDefinition = "LONGTEXT")
    private String description;
    @Column(name = "slogan")
    private String slogan;
    @Column(name = "is_default")
    private Boolean isDefault;
    @Enumerated(EnumType.STRING)
    @Column(name = "status", length = 20)
    @Builder.Default
    private ShowStatus status = ShowStatus.ACTIVE;
    @Column(name = "total_seat")
    private Long totalSeat;
    @Column(name = "remain_seat")
    private Long remainSeat;
}
