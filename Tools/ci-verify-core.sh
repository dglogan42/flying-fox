#!/usr/bin/env bash
# CI / local: compile pure Core (no Unity) and run a smoke console check.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
CORE="$ROOT/unity/Assets/_Project/Scripts/Core"
OUT="$ROOT/unity/Builds/ci-core"
mkdir -p "$OUT"

if ! command -v dotnet >/dev/null 2>&1; then
  echo "dotnet SDK required for Core CI verify" >&2
  exit 1
fi

# Generate a temporary classlib that only references Core sources
PROJ="$OUT/FlyingFox.Core.CI.csproj"
cat > "$PROJ" <<'XML'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.1</TargetFramework>
    <LangVersion>9.0</LangVersion>
    <Nullable>disable</Nullable>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
    <RootNamespace>FlyingFox.Core</RootNamespace>
    <AssemblyName>FlyingFox.Core</AssemblyName>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="../../Assets/_Project/Scripts/Core/**/*.cs" />
  </ItemGroup>
</Project>
XML

# Fix relative path from OUT
# Project lives in unity/Builds/ci-core → Core is ../../Assets/...
echo "==> Restoring & building Core (netstandard2.1)"
dotnet build "$PROJ" -c Release -v q

# Smoke program
SMOKE_DIR="$OUT/smoke"
mkdir -p "$SMOKE_DIR"
cat > "$SMOKE_DIR/Smoke.csproj" <<XML
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <LangVersion>10.0</LangVersion>
    <Nullable>disable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="$PROJ" />
  </ItemGroup>
</Project>
XML

cat > "$SMOKE_DIR/Program.cs" <<'CS'
using System;
using FlyingFox.Core;

static class Program
{
    static int Main()
    {
        var n = HexCoord.Origin.Neighbor(0);
        if (n.Q != 1 || n.R != 0) { Console.Error.WriteLine("hex fail"); return 1; }

        var seed = DailySeed.FromKey("FlyingFoxDaily|2026-07-30");
        var r1 = new SplitMix64Rng(seed);
        var r2 = new SplitMix64Rng(seed);
        for (int i = 0; i < 5; i++)
            if (r1.Next(0, 1000) != r2.Next(0, 1000)) { Console.Error.WriteLine("rng fail"); return 2; }

        var run = new RunController();
        run.Start(new RunConfig { Seed = 42 }, new SplitMix64Rng(42));
        if (run.Hand.Count != 3 || run.Board.Count != 1)
        {
            Console.Error.WriteLine($"run fail hand={run.Hand.Count} board={run.Board.Count}");
            return 3;
        }

        var slots = run.Board.GetEmptyAdjacent();
        if (slots.Count == 0 || !run.TryPlace(slots[0]))
        {
            Console.Error.WriteLine("place fail");
            return 4;
        }

        Console.WriteLine($"OK core smoke seed={seed} score={run.Score} board={run.Board.Count}");
        return 0;
    }
}
CS

echo "==> Smoke run"
dotnet run --project "$SMOKE_DIR/Smoke.csproj" -c Release -v q
echo "==> Core CI verify passed"
