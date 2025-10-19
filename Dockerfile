# --------- BUILD STAGE ---------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Chỉ copy file .csproj trước để tận dụng cache của Docker layer
COPY Backend_Nghiencf.csproj ./
RUN dotnet restore Backend_Nghiencf.csproj

# Copy toàn bộ mã nguồn
COPY . ./
RUN dotnet publish Backend_Nghiencf.csproj -c Release -o /app/out

# --------- RUNTIME STAGE ---------
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Copy từ build stage
COPY --from=build /app/out ./

# Railway sẽ inject biến PORT, ta dùng biến đó để cấu hình ASP.NET Core
ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT}

# Expose đúng cổng Railway sử dụng (Railway thường dùng PORT hoặc 8080)
EXPOSE 8080

# Lệnh chạy ứng dụng
ENTRYPOINT ["dotnet", "Backend_Nghiencf.dll"]
