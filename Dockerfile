# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY CTBSupplier.Web/CTBSupplier.Web.csproj CTBSupplier.Web/
RUN dotnet restore CTBSupplier.Web/CTBSupplier.Web.csproj
COPY CTBSupplier.Web/ CTBSupplier.Web/
WORKDIR /src/CTBSupplier.Web
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "CTBSupplier.Web.dll"]
