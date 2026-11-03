#!/usr/bin/env bash
# Test delle regole fuori da Unity, con la NUnit inclusa da Unity (Library/PackageCache).
# Linux/macOS: richiede dotnet SDK + mono.  Windows: dotnet run --project Tools/RulesHarness
# Argomento opzionale: filtro sul nome del test.
set -euo pipefail
cd "$(dirname "$0")"
dotnet build -nologo -v q . >/dev/null
mono bin/Debug/net472/RulesHarness.exe "$@"
