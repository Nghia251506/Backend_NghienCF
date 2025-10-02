FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore Backend_Nghiencf.csproj
RUN dotnet publish Backend_Nghiencf.csproj -c Release -o /app/out

# ---- runtime stage (ASP.NET .NET 9) ----
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/out .
ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT}
EXPOSE 8080
CMD ["dotnet", "Backend_Nghiencf.dll"]
