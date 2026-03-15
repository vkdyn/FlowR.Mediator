#!/bin/bash
# =============================================================
# FlowR NuGet Publish Script
# Usage: ./publish.sh [version] [api_key]
# Example: ./publish.sh 1.0.0 your-nuget-api-key
# =============================================================

VERSION=${1:-"1.0.0"}
API_KEY=${2:-""}
PROJECT="src/FlowR/FlowR.csproj"

echo "==================================================="
echo "  FlowR NuGet Publisher"
echo "  Version: $VERSION"
echo "==================================================="

# Run tests first
echo ""
echo ">> Running tests..."
dotnet test tests/FlowR.Tests/FlowR.Tests.csproj --configuration Release
if [ $? -ne 0 ]; then
  echo "TESTS FAILED. Aborting publish."
  exit 1
fi

# Build
echo ""
echo ">> Building..."
dotnet build $PROJECT --configuration Release -p:Version=$VERSION
if [ $? -ne 0 ]; then
  echo "BUILD FAILED. Aborting publish."
  exit 1
fi

# Pack
echo ""
echo ">> Packing..."
dotnet pack $PROJECT --configuration Release -p:Version=$VERSION --output ./nupkg
if [ $? -ne 0 ]; then
  echo "PACK FAILED. Aborting publish."
  exit 1
fi

# Push
if [ -n "$API_KEY" ]; then
  echo ""
  echo ">> Publishing to NuGet.org..."
  dotnet nuget push ./nupkg/FlowR.$VERSION.nupkg \
    --api-key $API_KEY \
    --source https://api.nuget.org/v3/index.json \
    --skip-duplicate

  # Push symbols
  dotnet nuget push ./nupkg/FlowR.$VERSION.snupkg \
    --api-key $API_KEY \
    --source https://api.nuget.org/v3/index.json \
    --skip-duplicate

  echo ""
  echo ">> Successfully published FlowR $VERSION to NuGet.org!"
  echo ">> View at: https://www.nuget.org/packages/FlowR/$VERSION"
else
  echo ""
  echo ">> Package created at ./nupkg/FlowR.$VERSION.nupkg"
  echo ">> To publish, run: dotnet nuget push ./nupkg/FlowR.$VERSION.nupkg --api-key YOUR_KEY --source https://api.nuget.org/v3/index.json"
fi
