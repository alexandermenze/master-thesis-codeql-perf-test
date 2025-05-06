import os
import re
import json
import argparse
import pandas as pd
import numpy as np

def parse_build_output(log_path):
    """Extract total build time in seconds."""
    text = open(log_path, 'r', encoding='utf-8').read()
    match = re.search(r"Process finished \((\d+(?:\.\d+)?) seconds\)", text)
    return float(match.group(1)) if match else np.nan


def parse_analysis_output(log_path):
    """Extract analysis times (total, DB creation, query execution, result decode) in seconds."""
    text = open(log_path, 'r', encoding='utf-8').read()
    total_match = re.search(r"Process finished \((\d+(?:\.\d+)?) seconds\)", text)
    total = float(total_match.group(1)) if total_match else np.nan

    db_match = re.search(r"Erstelle CodeQL-Datenbank gestartet bei .+? Dauer: (\d+) ms", text)
    db = float(db_match.group(1)) / 1000 if db_match else np.nan

    query_match = re.search(r"Führe Query aus gestartet bei .+? Dauer: (\d+) ms", text)
    query = float(query_match.group(1)) / 1000 if query_match else np.nan

    decode_match = re.search(r"Dekodiere Ergebnisse nach CSV gestartet bei .+? Dauer: (\d+) ms", text)
    decode = float(decode_match.group(1)) / 1000 if decode_match else np.nan

    return total, db, query, decode


def parse_psrecord(log_path):
    """Compute avg, max, median for CPU (%) and Real memory (MB)."""
    cpu_vals, mem_vals = [], []
    with open(log_path, 'r', encoding='utf-8') as f:
        for line in f:
            if line.strip() and not line.startswith('#'):
                parts = line.split()
                if len(parts) >= 4:
                    try:
                        cpu_vals.append(float(parts[1]))
                        mem_vals.append(float(parts[2]))
                    except ValueError:
                        continue
    arr_cpu = np.array(cpu_vals)
    arr_mem = np.array(mem_vals)
    return {
        'V2_Avg Cpu': arr_cpu.mean() if arr_cpu.size else np.nan,
        'V2_Max Cpu': arr_cpu.max() if arr_cpu.size else np.nan,
        'V2_Median Cpu': np.median(arr_cpu) if arr_cpu.size else np.nan,
        'V2_Avg Mem': arr_mem.mean() if arr_mem.size else np.nan,
        'V2_Max Mem': arr_mem.max() if arr_mem.size else np.nan,
        'V2_Median Mem': np.median(arr_mem) if arr_mem.size else np.nan
    }


def parse_cloc_json(json_path):
    """Extract C# file and code-line counts."""
    data = json.load(open(json_path, 'r', encoding='utf-8'))
    cs = data.get('C#', {})
    return {
        'V2_# Code Files': cs.get('nFiles', np.nan),
        'V2_# Code Lines': cs.get('code', np.nan)
    }


def parse_results_csv(csv_path):
    """Count distinct processes and total dataflows."""
    df = pd.read_csv(csv_path)
    first_col = df.columns[0]
    return {
        'V2_# Result Processes': df[first_col].nunique(),
        'V2_# Result Dataflows': len(df)
    }


def process_repo(repo_path):
    metrics = {}
    # Build metrics (optional)
    build_dir = os.path.join(repo_path, 'build')
    if os.path.isdir(build_dir):
        out_path = os.path.join(build_dir, 'output.log')
        ps_path = os.path.join(build_dir, 'psrecord.log')
        metrics['V2_Normal Total (s)'] = parse_build_output(out_path) if os.path.exists(out_path) else np.nan
        build_ps = parse_psrecord(ps_path) if os.path.exists(ps_path) else {k: np.nan for k in ['V2_Avg Cpu','V2_Max Cpu','V2_Median Cpu','V2_Avg Mem','V2_Max Mem','V2_Median Mem']}
        metrics.update(build_ps)
    else:
        # fill with NaNs
        metrics['V2_Normal Total (s)'] = np.nan
        for col in ['V2_Avg Cpu','V2_Max Cpu','V2_Median Cpu','V2_Avg Mem','V2_Max Mem','V2_Median Mem']:
            metrics[col] = np.nan

    # Analysis metrics
    measure_dir = os.path.join(repo_path, 'measure')
    if os.path.isdir(measure_dir):
        # timings
        output_path = os.path.join(measure_dir, 'output.log')
        if os.path.exists(output_path):
            total, db, query, decode = parse_analysis_output(output_path)
        else:
            total = db = query = decode = np.nan
        metrics['V2_Analysis Total (s)'] = total
        metrics['V2_Analysis Database (s)'] = db
        metrics['V2_Analysis Query (s)'] = query
        metrics['V2_Analysis Decode (s)'] = decode
        # psrecord
        ps_path = os.path.join(measure_dir, 'psrecord.log')
        measure_ps = parse_psrecord(ps_path) if os.path.exists(ps_path) else {k: np.nan for k in ['V2_Avg Cpu','V2_Max Cpu','V2_Median Cpu','V2_Avg Mem','V2_Max Mem','V2_Median Mem']}
        metrics.update({f"V2_{k.split('_',1)[1]}": v for k, v in measure_ps.items()})
        # cloc
        cloc_path = os.path.join(measure_dir, 'cloc.json')
        if os.path.exists(cloc_path):
            metrics.update(parse_cloc_json(cloc_path))
        else:
            metrics['V2_# Code Files'] = np.nan
            metrics['V2_# Code Lines'] = np.nan
    else:
        # no measure folder
        for key in ['V2_Analysis Total (s)','V2_Analysis Database (s)','V2_Analysis Query (s)','V2_Analysis Decode (s)',
                    'V2_# Code Files','V2_# Code Lines'] + ['V2_Avg Cpu','V2_Max Cpu','V2_Median Cpu','V2_Avg Mem','V2_Max Mem','V2_Median Mem']:
            metrics[key] = np.nan

    # results.csv (optional)
    results_path = os.path.join(repo_path, 'results.csv')
    if os.path.exists(results_path):
        metrics.update(parse_results_csv(results_path))
    else:
        metrics['V2_# Result Processes'] = np.nan
        metrics['V2_# Result Dataflows'] = np.nan

    # Repo name
    metrics['Repo'] = os.path.basename(repo_path)
    return metrics


def main(top_dir, output_file):
    rows = []
    for entry in os.scandir(top_dir):
        if entry.is_dir():
            try:
                rows.append(process_repo(entry.path))
            except Exception as e:
                print(f"Skipping {entry.name}: {e}")
    df = pd.DataFrame(rows)
    df = df.set_index('Repo')
    df.to_csv(output_file)
    print(f"V2 Metrics written to {output_file}")

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Extract V2 metrics from logs")
    parser.add_argument("top_directory", help="Top-level folder containing repo subfolders")
    parser.add_argument("--output", "-o", default="aggregated_metrics_v2.csv",
                        help="Output CSV file name (default: aggregated_metrics_v2.csv)")
    args = parser.parse_args()
    main(args.top_directory, args.output)
