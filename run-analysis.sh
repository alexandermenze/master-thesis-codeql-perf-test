#!/usr/bin/env bash
set -euo pipefail

# Default-Werte
RESULT_SET="#select"
OUT_BQRS="out.bqrs"
OUT_CSV="out.csv"
SLN_FILE=""

# Usage-Funktion
usage() {
  cat <<EOF
Usage: $0 -s <source-root> -d <database> -q <query-file> -l <solution-file> [-r <result-set>] [-b <bqrs-output>] [-c <csv-output>]

  -s  Pfad zum Quellcode (source-root)
  -d  Pfad zur CodeQL-Datenbank
  -q  Pfad zur CodeQL-Query (.ql)
  -l  Pfad zur Solution-Datei (.sln)
  -r  Name des Result-Sets (default: "#select")
  -b  Name der BQRS-Ausgabedatei (default: "out.bqrs")
  -c  Name der CSV-Ausgabedatei (default: "out.csv")
  -h  Diese Hilfe anzeigen
EOF
  exit 1
}

# Parameter parsen
while getopts ":s:d:q:l:r:b:c:h" opt; do
  case ${opt} in
    s) SOURCE_ROOT="$OPTARG" ;;  # Quellcode-Pfad
    d) DB_PATH="$OPTARG"     ;;  # Datenbank-Pfad
    q) QUERY_FILE="$OPTARG"  ;;  # Query-Datei
    l) SLN_FILE="$OPTARG"    ;;  # Solution-Datei (.sln)
    r) RESULT_SET="$OPTARG"  ;;  # Result-Set-Name
    b) OUT_BQRS="$OPTARG"    ;;  # BQRS-Ausgabe
    c) OUT_CSV="$OPTARG"     ;;  # CSV-Ausgabe
    h|*) usage ;;
  esac
done

# Pflichtparameter prüfen
if [ -z "${SOURCE_ROOT:-}" ] || [ -z "${DB_PATH:-}" ] || [ -z "${QUERY_FILE:-}" ] || [ -z "${SLN_FILE:-}" ]; then
  echo "Fehler: source-root, database, query-file und solution-file müssen angegeben werden." >&2
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

# Schritt 0: dotnet restore. Verhindert Abhängigkeit von Netzwerk während 'database create'.
STEP="Schritt 0: dotnet clean & restore"
echo "$STEP"
STEP0_START=$(date +%s%3N)
dotnet clean "$SOURCE_ROOT/$SLN_FILE"
dotnet restore "$SOURCE_ROOT/$SLN_FILE"
STEP0_END=$(date +%s%3N)
print_timing "$STEP" "$STEP0_START" "$STEP0_END"

# Schritt 1: Datenbank anlegen
STEP="Schritt 1: Erstelle CodeQL-Datenbank"
echo "$STEP"
STEP1_START=$(date +%s%3N)
codeql database create --language csharp --source-root "$SOURCE_ROOT" \
  --command "dotnet build --no-incremental $SLN_FILE" -- "$DB_PATH"
STEP1_END=$(date +%s%3N)
print_timing "$STEP" "$STEP1_START" "$STEP1_END"

# Schritt 2: Query ausführen
STEP="Schritt 2: Führe Query aus"
echo "$STEP"
STEP2_START=$(date +%s%3N)
codeql query run --database "$DB_PATH" --output "$OUT_BQRS" -- "$QUERY_FILE"
STEP2_END=$(date +%s%3N)
print_timing "$STEP" "$STEP2_START" "$STEP2_END"

# Schritt 3: Ergebnisse dekodieren
STEP="Schritt 3: Dekodiere Ergebnisse nach CSV"
echo "$STEP"
STEP3_START=$(date +%s%3N)
codeql bqrs decode --output "$OUT_CSV" --result-set "$RESULT_SET" \
  --format csv "$OUT_BQRS"
STEP3_END=$(date +%s%3N)
print_timing "$STEP" "$STEP3_START" "$STEP3_END"

echo "CSV-Ausgabe: $OUT_CSV"
