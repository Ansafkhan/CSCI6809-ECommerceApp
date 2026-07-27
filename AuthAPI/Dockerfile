FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app
COPY AuthAPI.csproj .
RUN dotnet restore AuthAPI.csproj
COPY . .
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/out .
EXPOSE 80
ENTRYPOINT ["dotnet", "AuthAPI.dll"]