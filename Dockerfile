FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj trước để cache restore tốt hơn (layer caching)
COPY Backend_Nghiencf.csproj ./
RUN dotnet restore Backend_Nghiencf.csproj

# Copy toàn bộ source
COPY . ./

# Publish với --no-restore để nhanh hơn
RUN dotnet publish Backend_Nghiencf.csproj -c Release -o /app/out --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/out ./

# ENV cần thiết cho production + PORT handling (Fly set PORT auto)
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT:-8080}
# Giới hạn heap để tránh OOM trên máy 256-512MB (rất quan trọng cho .NET 9 trên shared)
ENV DOTNET_GCHeapHardLimit=0x10000000  # ~256MB heap limit

EXPOSE 8080
CMD ["dotnet", "Backend_Nghiencf.dll"]
