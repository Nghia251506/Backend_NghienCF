
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build


WORKDIR /src


COPY Backend_Nghiencf.csproj ./
RUN dotnet restore Backend_Nghiencf.csproj

# Copy các file khác và build
COPY . ./
RUN dotnet publish Backend_Nghiencf.csproj -c Release -o /app/out


FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime


WORKDIR /app


COPY --from=build /app/out ./


ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT}
EXPOSE 8080


CMD ["dotnet", "Backend_Nghiencf.dll"]
