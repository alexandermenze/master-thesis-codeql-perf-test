#!/usr/bin/env bash
set -euo pipefail

# Hier liegen die ZIPs
ZIP_DIR="."
# Ziel­verzeichnisse
MEASURE_DIR="measure"
OUTPUT_CSV="results.csv"

# Schleife über alle ZIP-Dateien im aktuellen Verzeichnis
for zip in "${ZIP_DIR}"/*.zip; do
  case "$(basename "$zip")" in

    codeql-output-*.zip)
      # entpacke Inhalt (einzelne Datei) direkt nach measure/output.log
      unzip -p "$zip" > "${MEASURE_DIR}/output.log"
      echo "► ${zip} → ${MEASURE_DIR}/output.log"
      ;;

    psrecord-log-*.zip)
      unzip -p "$zip" > "${MEASURE_DIR}/psrecord.log"
      echo "► ${zip} → ${MEASURE_DIR}/psrecord.log"
      ;;

    results-cloc-*.zip)
      unzip -p "$zip" > "${MEASURE_DIR}/cloc.json"
      echo "► ${zip} → ${MEASURE_DIR}/cloc.json"
      ;;

    results-csv-*.zip)
      unzip -p "$zip" > "${OUTPUT_CSV}"
      echo "► ${zip} → ${OUTPUT_CSV}"
      ;;

    *)
      echo "⚠️ Überspringe unerwartete Datei: $zip" >&2
      ;;
  esac
done
