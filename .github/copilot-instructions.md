# OrcaHello: AI-Assisted Killer Whale Notification System

OrcaHello is a real-time AI-assisted killer whale detection and notification system consisting of 5 main components: ModeratorFrontEnd (.NET 8 Blazor), NotificationSystem (.NET 8 Azure Functions), InferenceSystem (Python/Docker), ModelTraining (Python/Jupyter), and ModelEvaluation (benchmarking).

Always reference these instructions first and fallback to search or bash commands only when you encounter unexpected information that does not match the info here.

## Working Effectively

### Prerequisites and Environment Setup
- .NET 8.0 SDK (confirmed available in environment)
- Python 3.8+ for ML components (Python 3.12+ available)
- Docker for InferenceSystem deployment
- Access to Azure resources for full functionality

### Bootstrap and Build the Repository
Follow these steps in order for a complete development setup:

1. **Clone and navigate to repository**
   ```bash
   cd /home/runner/work/aifororcas-livesystem/aifororcas-livesystem
   ```

2. **Build NotificationSystem (.NET Azure Functions)**
   ```bash
   cd NotificationSystem
   dotnet restore                    # ~36 seconds
   dotnet build --no-restore -c Release  # ~11 seconds, NEVER CANCEL
   dotnet test --no-restore --filter "Category!=Cosmos"  # ~7 seconds
   cd ..
   ```

3. **Build ModeratorFrontEnd/OrcaHello (.NET 8 Blazor)**
   ```bash
   cd ModeratorFrontEnd/OrcaHello
   dotnet restore                    # ~14 seconds
   dotnet build --no-restore -c Release  # ~10 seconds, expect 80+ warnings, NEVER CANCEL
   dotnet test --no-restore          # ~10 seconds, 177 tests
   cd ../..
   ```

4. **Build ModeratorFrontEnd/AIForOrcas (Legacy .NET Implementation)**
   ```bash
   cd ModeratorFrontEnd/AIForOrcas
   dotnet restore                    # ~10 seconds
   dotnet build --no-restore -c Release  # ~4 seconds, expect 40+ warnings
   cd ../..
   ```

5. **Setup Python Environment for ML Components (requires manual model download)**
   ```bash
   cd ModelTraining
   python3 -m venv venv
   source venv/bin/activate
   pip install --upgrade pip
   # Note: pip install -r requirements.txt may fail due to version conflicts
   # Use conda or uv pip install for better dependency resolution
   cd ..
   ```

### Important Build Notes and Warnings
- **NEVER CANCEL** any dotnet build or test commands - let them complete fully
- Builds complete in under 15 seconds each, tests in under 10 seconds
- All .NET builds generate warnings but complete successfully  
- Python ML dependencies have version compatibility issues with Python 3.12+
- Docker builds require network access to package repositories
- InferenceSystem requires model.zip download from Azure storage

## Validation

### Manual Validation Requirements
After making changes, always run these validation steps:

1. **Build validation**
   ```bash
   # Validate each component builds without errors
   cd NotificationSystem && dotnet build --no-restore -c Release && cd ..
   cd ModeratorFrontEnd/OrcaHello && dotnet build --no-restore -c Release && cd ../..
   cd ModeratorFrontEnd/AIForOrcas && dotnet build --no-restore -c Release && cd ../..
   ```

2. **Test validation**  
   ```bash
   # Run unit tests for .NET components
   cd NotificationSystem && dotnet test --no-restore --filter "Category!=Cosmos" && cd ..
   cd ModeratorFrontEnd/OrcaHello && dotnet test --no-restore && cd ../..
   ```

3. **Always check that CI/CD will pass**
   - All builds must complete without errors
   - Unit tests must pass (177 tests in OrcaHello, 1 test in NotificationSystem)
   - Warnings are acceptable and expected (80+ in OrcaHello, 40+ in AIForOrcas)

### Functional Validation Scenarios
- **Web APIs**: Verify Swagger endpoints are accessible and documented
- **Blazor UI**: Check that detection moderation workflows function properly
- **Azure Functions**: Validate notification triggers work with test data
- **Python Components**: Verify model loading and inference pipelines

## Component-Specific Information

### ModeratorFrontEnd
- **OrcaHello**: Modern .NET 8 implementation deployed to aifororcasdetections2.azurewebsites.net
- **AIForOrcas**: Legacy implementation deployed to aifororcas.azurewebsites.net  
- Both use Blazor Server with Azure CosmosDB backend
- Authentication via Azure AD for moderator access
- Test coverage: 177 unit tests in OrcaHello, no tests in AIForOrcas

### NotificationSystem  
- .NET 8 Azure Functions for email notifications
- Deployed to "orcanotification" function app
- Requires local.settings.json for local development (never commit this file)
- Test coverage: 1 unit test (Cosmos tests excluded in CI)

### InferenceSystem
- Python-based AI inference with Docker deployment
- Requires model.zip download from Azure blob storage
- Uses Python 3.7+ (tested), may have issues with Python 3.12+
- Docker build fails without network access to apt repositories
- Processes audio from Orcasound S3 buckets for whale call detection

### ModelTraining & ModelEvaluation
- Jupyter notebooks for ML model development
- Requires Python 3.8+ with conda environment recommended
- FastAI, PyTorch, librosa dependencies with version conflicts on newer Python
- Use `uv pip install` for better dependency resolution

## Common Tasks

### Working with GitHub Actions
All components have automated CI/CD via GitHub Actions:
- `OrcaHello.Web.Api.yaml` - OrcaHello API deployment
- `OrcaHello.Web.UI.yaml` - OrcaHello UI deployment  
- `NotificationSystem.yaml` - Azure Functions deployment
- `AIForOrcas.Server.yaml` & `AIForOrcas.Client.Web.yaml` - Legacy deployments

### Local Development Gotchas
- **Python Dependencies**: Use Python 3.8 with conda for ML components
- **Model Files**: InferenceSystem requires manual model.zip download
- **Azure Resources**: Most functionality requires Azure access credentials
- **Network Issues**: Docker builds may fail in restricted environments
- **Configuration**: Never commit local.settings.json or environment files

### Build Times and Timeouts
- NotificationSystem: restore ~36s, build ~11s, test ~7s
- OrcaHello: restore ~14s, build ~10s, test ~10s  
- AIForOrcas: restore ~10s, build ~4s
- **Always set timeouts to 60+ seconds minimum for any build operation**

### Repository Structure
```
/ModeratorFrontEnd/     # Blazor web applications (OrcaHello + AIForOrcas)
/NotificationSystem/    # Azure Functions for email notifications
/InferenceSystem/      # Python AI inference with Docker
/ModelTraining/        # Jupyter notebooks for ML model training
/ModelEvaluation/      # Model benchmarking and evaluation
/.github/workflows/    # CI/CD automation
```

Always verify your changes don't break existing functionality by running the full build and test suite before committing.