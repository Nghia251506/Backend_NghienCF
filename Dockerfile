
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build  # Thay về phiên bản 8.0 nếu Railway không hỗ trợ .NET 9.0


WORKDIR /src


COPY Backend_Nghiencf.csproj ./
RUN dotnet restore Backend_Nghiencf.csproj


COPY . ./
RUN dotnet publish Backend_Nghiencf.csproj -c Release -o /app/out


FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime  # Thay về phiên bản 8.0 nếu Railway không hỗ trợ .NET 9.0


WORKDIR /app


COPY --from=build /app/out ./


ENV ASPNETCORE_URLS=http://0.0.0.0:80 
EXPOSE 80


CMD ["dotnet", "Backend_Nghiencf.dll"]
