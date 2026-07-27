FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app
COPY PriceAPI.csproj .
RUN dotnet restore PriceAPI.csproj
COPY . .
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/out .
EXPOSE 80
ENTRYPOINT ["dotnet", "PriceAPI.dll"]
