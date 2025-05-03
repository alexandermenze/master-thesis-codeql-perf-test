#!/usr/bin/env bash
set -euo pipefail

# Usage-Funktion
usage() {
  cat <<EOF
Usage: $0 -s <source-root>

  -s  Pfad zum Quellcode (source-root)
  -h  Diese Hilfe anzeigen
EOF
  exit 1
}

# Parameter parsen
while getopts ":s:h" opt; do
  case ${opt} in
    s) SOURCE_ROOT="$OPTARG" ;;
    h|*) usage ;;
  esac
done

# Pflichtparameter prüfen
if [ -z "${SOURCE_ROOT:-}" ]; then
  echo "Fehler: source-root muss angegeben werden." >&2
  usage
fi

# Skript-Startzeit in ms
SCRIPT_START_MS=$(date +%s%3N)

# Funktion zur Ausgabe der Zeiten
print_timing() {
  local step_name=$1
  local step_start=$2
  local step_end=$3
  local offset=$((step_start - SCRIPT_START_MS))
  local duration=$((step_end - step_start))
  printf -- "-> %s gestartet bei +%d ms, Dauer: %d ms\n\n" \
         "$step_name" "$offset" "$duration"
}

# Schritt 0: dotnet restore
STEP="Schritt 0: dotnet restore"
echo "$STEP"
STEP0_START=$(date +%s%3N)
dotnet restore "$SOURCE_ROOT"
STEP0_END=$(date +%s%3N)
print_timing "$STEP" "$STEP0_START" "$STEP0_END"

# Schritt 1: dotnet build
STEP="Schritt 1: dotnet build"
echo "$STEP"
STEP1_START=$(date +%s%3N)
dotnet build "$SOURCE_ROOT"
STEP1_END=$(date +%s%3N)
print_timing "$STEP" "$STEP1_START" "$STEP1_END"

# Gesamtdauer
SCRIPT_END_MS=$(date +%s%3N)
print_timing "Gesamtskript" "$SCRIPT_START_MS" "$SCRIPT_END_MS"
