package org.example.benghiencf;

import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.data.jpa.repository.config.EnableJpaAuditing;

@SpringBootApplication
@EnableJpaAuditing
public class BenghiencfApplication {

    public static void main(String[] args) {
        SpringApplication.run(BenghiencfApplication.class, args);
    }

}
