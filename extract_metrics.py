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
        'avg_cpu': arr_cpu.mean() if arr_cpu.size else np.nan,
        'max_cpu': arr_cpu.max() if arr_cpu.size else np.nan,
        'median_cpu': np.median(arr_cpu) if arr_cpu.size else np.nan,
        'avg_mem': arr_mem.mean() if arr_mem.size else np.nan,
        'max_mem': arr_mem.max() if arr_mem.size else np.nan,
        'median_mem': np.median(arr_mem) if arr_mem.size else np.nan
    }

def parse_cloc_json(json_path):
    """Extract C# file and code-line counts."""
    data = json.load(open(json_path, 'r', encoding='utf-8'))
    cs = data.get('C#', {})
    return cs.get('nFiles', np.nan), cs.get('code', np.nan)

def parse_results_csv(csv_path):
    """Count distinct processes and total dataflows."""
    df = pd.read_csv(csv_path)
    first_col = df.columns[0]
    return df[first_col].nunique(), len(df)

def process_repo(repo_path):
    metrics = {}
    # Build metrics
    build_out = parse_build_output(os.path.join(repo_path, 'build', 'output.log'))
    metrics['Normal Total (s)'] = build_out
    build_ps = parse_psrecord(os.path.join(repo_path, 'build', 'psrecord.log'))
    metrics.update({f"Build {k.replace('_', ' ').title()}": v for k, v in build_ps.items()})

    # Analysis metrics
    analysis_outs = parse_analysis_output(os.path.join(repo_path, 'measure', 'output.log'))
    metrics['Analysis Total (s)'] = analysis_outs[0]
    metrics['Analysis Database (s)'] = analysis_outs[1]
    metrics['Analysis Query (s)'] = analysis_outs[2]
    metrics['Analysis Decode (s)'] = analysis_outs[3]
    measure_ps = parse_psrecord(os.path.join(repo_path, 'measure', 'psrecord.log'))
    metrics.update({f"Analysis {k.replace('_', ' ').title()}": v for k, v in measure_ps.items()})

    # cloc metrics
    nfiles, nlines = parse_cloc_json(os.path.join(repo_path, 'measure', 'cloc.json'))
    metrics['# Code Files'] = nfiles
    metrics['# Code Lines'] = nlines

    # results.csv metrics
    res_procs, res_flows = parse_results_csv(os.path.join(repo_path, 'results.csv'))
    metrics['# Result Processes'] = res_procs
    metrics['# Result Dataflows'] = res_flows

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
    print(f"Metrics written to {output_file}")

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Extract metrics from build/analysis logs")
    parser.add_argument("top_directory", help="Top-level folder containing multiple repo subfolders")
    parser.add_argument("--output", "-o", default="aggregated_metrics.csv",
                        help="Output CSV file name (default: aggregated_metrics.csv)")
    args = parser.parse_args()
    main(args.top_directory, args.output)


