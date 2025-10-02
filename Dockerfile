# ---- build stage (.NET 9 SDK) ----
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /app/out

# ---- runtime stage (.NET 9 ASP.NET) ----
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/out .
# Railway cấp biến PORT, bind Kestrel vào 0.0.0.0
ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT}
EXPOSE 8080
CMD ["dotnet", "BackendNghiencf.dll"]
