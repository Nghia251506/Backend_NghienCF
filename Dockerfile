# ======== BUILD STAGE ========
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Chỉ copy .csproj trước để cache restore
COPY ["Backend_Nghiencf.csproj", "./"]
RUN dotnet restore "Backend_Nghiencf.csproj"

# Copy toàn bộ source
COPY . .

# Publish ra thư mục /app/publish (không kèm app host để image gọn)
RUN dotnet publish "./Backend_Nghiencf.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ======== RUNTIME STAGE ========
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# (tuỳ chọn) Tạo thư mục upload trong wwwroot, tiện mount volume
RUN mkdir -p /app/wwwroot/uploads

# Copy artefacts đã publish
COPY --from=build /app/publish .

# Lắng nghe port 8080 (phù hợp Fly.io)
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

ENTRYPOINT ["dotnet", "Backend_Nghiencf.dll"]
