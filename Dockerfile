FROM public.ecr.aws/lambda/dotnet:8 AS base

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["AlgoXera.Lambda.TemplateGenerationEngine/AlgoXera.Lambda.TemplateGenerationEngine.csproj", "AlgoXera.Lambda.TemplateGenerationEngine/"]
COPY ["AlgoXera.Lambda.Shared/AlgoXera.Lambda.Shared.csproj", "AlgoXera.Lambda.Shared/"]
RUN dotnet restore "AlgoXera.Lambda.TemplateGenerationEngine/AlgoXera.Lambda.TemplateGenerationEngine.csproj"
COPY AlgoXera.Lambda.TemplateGenerationEngine/ AlgoXera.Lambda.TemplateGenerationEngine/
COPY AlgoXera.Lambda.Shared/ AlgoXera.Lambda.Shared/
RUN dotnet build "AlgoXera.Lambda.TemplateGenerationEngine/AlgoXera.Lambda.TemplateGenerationEngine.csproj" --configuration Release --output /app/build

FROM build AS publish
RUN dotnet publish "AlgoXera.Lambda.TemplateGenerationEngine/AlgoXera.Lambda.TemplateGenerationEngine.csproj" \
            --configuration Release \ 
            --runtime linux-x64 \
            --self-contained false \ 
            --output /app/publish \
            -p:PublishReadyToRun=true  

FROM base AS final
WORKDIR /var/task
COPY --from=publish /app/publish .
CMD ["AlgoXera.Lambda.TemplateGenerationEngine::AlgoXera.Lambda.TemplateGenerationEngine.Function::FunctionHandler"]

