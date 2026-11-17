FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY DocumentManagementSystem.GenAIWorker/GenAIWorker/GenAIWorker.csproj DocumentManagementSystem.GenAIWorker/GenAIWorker/
RUN dotnet restore DocumentManagementSystem.GenAIWorker/GenAIWorker/GenAIWorker.csproj
COPY DocumentManagementSystem.GenAIWorker/GenAIWorker/ DocumentManagementSystem.GenAIWorker/GenAIWorker/
RUN dotnet publish DocumentManagementSystem.GenAIWorker/GenAIWorker/GenAIWorker.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "GenAIWorker.dll"]