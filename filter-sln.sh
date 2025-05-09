#!/usr/bin/env bash
set -euo pipefail

# Default-Werte
EXCLUDE_FOLDERS=""

usage() {
  cat <<EOF
Usage: $0 -s <solution.sln> [-e <proj1;proj2>] [-f <folder1;folder2>]

  -s  Pfad zur Solution-Datei
  -f  Semikolon-getrennte Liste von Ordnerpfaden (relativ zur Solution), aus denen alle .csproj entfernt werden
  -h  Diese Hilfe anzeigen
EOF
  exit 1
}

# Parameter parsen
while getopts ":s:e:f:h" opt; do
  case ${opt} in
    s) SOLUTION="$OPTARG" ;; 
    f) EXCLUDE_FOLDERS="$OPTARG" ;; 
    h|*) usage ;;
  esac
done

# Pflichtparameter prüfen
if [ -z "${SOLUTION:-}" ]; then
  echo "Fehler: Solution-Datei muss mit -s angegeben werden." >&2
  usage
fi

# --- Pfade auflösen ---
SOLUTION_DIR=$(dirname "$SOLUTION")
SOLUTION_NAME=$(basename "$SOLUTION")

echo "Solution:           $SOLUTION_NAME"
[ -n "$EXCLUDE_FOLDERS" ]  && echo "Ordner ausschließen:   $EXCLUDE_FOLDERS"
echo

# ins Solution-Verzeichnis wechseln
cd "$SOLUTION_DIR"

# --- Ordner ausschließen (über -f) ---
if [ -n "$EXCLUDE_FOLDERS" ]; then
  IFS=';' read -r -a FOLDERS <<< "$EXCLUDE_FOLDERS"
  for folder in "${FOLDERS[@]}"; do
    echo "→ Entferne alle .csproj in: $folder"
    if [ -d "$folder" ]; then
      found=false
      while IFS= read -r projfile; do
        found=true
        echo "   • $projfile"
        if ! dotnet sln "$SOLUTION_NAME" remove "$projfile" >/dev/null 2>&1; then
          echo "Fehler: Projekt '$projfile' nicht in der Solution vorhanden." >&2
          exit 1
        fi
      done < <(find "$folder" -type f -name '*.csproj')
      if [ "$found" = false ]; then
        echo "Warnung: Keine .csproj-Dateien im Ordner '$folder' gefunden." >&2
      fi
    else
      echo "Fehler: Ordner '$folder' existiert nicht." >&2
      exit 1
    fi
  done
  echo
fi

