#!/usr/bin/env bash
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$root"

dotnet test home-gpt.slnx --no-restore "$@"

python3 - <<'PY'
import xml.etree.ElementTree as ET
from pathlib import Path

checks = [
    ("Core", Path("tests/home-gpt.Core.Tests/coverage.cobertura.xml"), "home-gpt.Core"),
    ("Cli", Path("tests/home-gpt.Cli.Tests/coverage.cobertura.xml"), "home-gpt.Cli"),
    ("Avalonia", Path("tests/home-gpt.Avalonia.Tests/coverage.cobertura.xml"), "home-gpt.Avalonia"),
]

for label, report, assembly in checks:
    if not report.exists():
        raise SystemExit(f"Missing coverage report for {label}: {report}")

    root = ET.parse(report).getroot()
    package = next(
        (pkg for pkg in root.findall("packages/package") if pkg.attrib.get("name") == assembly),
        None,
    )
    if package is None:
        raise SystemExit(f"Assembly '{assembly}' not found in {report}")

    line_rate = float(package.attrib["line-rate"]) * 100
    print(f"{label}: {line_rate:.2f}% line coverage ({assembly})")
    if line_rate < 80:
        raise SystemExit(f"{label} coverage {line_rate:.2f}% is below 80%")

print("All component coverage checks passed (>= 80%).")
PY
