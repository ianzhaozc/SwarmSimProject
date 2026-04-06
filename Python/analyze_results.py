import csv
from datetime import datetime
import glob
import os
import sys
from statistics import mean
from typing import Any, Dict, List

try:
    import matplotlib.pyplot as plt
    HAS_MATPLOTLIB = True
except Exception:
    HAS_MATPLOTLIB = False

SUMMARY_FILE_NAME = "summary_results.csv"
SUMMARY_ARCHIVE_DIR_NAME = "Summaries"
REQUIRED_BATTLE_COLUMNS = {
    "episode",
    "winner",
    "time",
    "attacker_alive",
    "defender_alive",
    "attacker_survival_rate",
    "defender_survival_rate",
    "attacker_total_distance",
    "defender_total_distance",
}


def load_rows(csv_path: str) -> List[Dict[str, str]]:
    rows = []
    with open(csv_path, "r", encoding="utf-8-sig", newline="") as handle:
        reader = csv.DictReader(handle)
        for row in reader:
            rows.append(row)
    return rows


def get_missing_battle_columns(rows: List[Dict[str, str]]) -> List[str]:
    if not rows:
        return []

    present_columns = set(rows[0].keys())
    return sorted(REQUIRED_BATTLE_COLUMNS - present_columns)


def to_float(row: Dict[str, str], key: str, default: float = 0.0) -> float:
    try:
        return float(row.get(key, default))
    except (TypeError, ValueError):
        return default


def to_int(row: Dict[str, str], key: str, default: int = 0) -> int:
    try:
        return int(float(row.get(key, default)))
    except (TypeError, ValueError):
        return default


def safe_mean(values: List[float]) -> float:
    return mean(values) if values else 0.0


def classify_winner(winner_text: str) -> str:
    text = (winner_text or "").strip()
    if "Attacker Win" in text:
        return "attacker"
    if "Defender Win" in text:
        return "defender"
    return "unknown"


def summarize_rows(rows: List[Dict[str, str]], source_name: str) -> Dict[str, Any]:
    total = len(rows)

    attacker_wins = 0
    defender_wins = 0
    unknown_results = 0

    times = []
    attacker_survival = []
    defender_survival = []
    attacker_distance = []
    defender_distance = []
    attacker_alive = []
    defender_alive = []

    for row in rows:
        winner_type = classify_winner(row.get("winner", ""))

        if winner_type == "attacker":
            attacker_wins += 1
        elif winner_type == "defender":
            defender_wins += 1
        else:
            unknown_results += 1

        times.append(to_float(row, "time"))
        attacker_survival.append(to_float(row, "attacker_survival_rate"))
        defender_survival.append(to_float(row, "defender_survival_rate"))
        attacker_distance.append(to_float(row, "attacker_total_distance"))
        defender_distance.append(to_float(row, "defender_total_distance"))
        attacker_alive.append(to_int(row, "attacker_alive"))
        defender_alive.append(to_int(row, "defender_alive"))

    return {
        "source": source_name,
        "total_episodes": total,
        "attacker_wins": attacker_wins,
        "defender_wins": defender_wins,
        "unknown_results": unknown_results,
        "attacker_success_rate": attacker_wins / total if total > 0 else 0.0,
        "defender_success_rate": defender_wins / total if total > 0 else 0.0,
        "avg_time": safe_mean(times),
        "avg_attacker_alive": safe_mean(attacker_alive),
        "avg_defender_alive": safe_mean(defender_alive),
        "avg_attacker_survival_rate": safe_mean(attacker_survival),
        "avg_defender_survival_rate": safe_mean(defender_survival),
        "avg_attacker_total_distance": safe_mean(attacker_distance),
        "avg_defender_total_distance": safe_mean(defender_distance),
    }


def print_single_summary(summary: Dict[str, Any]) -> None:
    print("=" * 80)
    print(f"Source: {summary['source']}")
    print(f"Total episodes: {summary['total_episodes']}")
    print(f"Attacker wins: {summary['attacker_wins']}")
    print(f"Defender wins: {summary['defender_wins']}")
    if summary["unknown_results"] > 0:
        print(f"Unknown results: {summary['unknown_results']}")
    print(f"Attacker success rate: {summary['attacker_success_rate']:.4f}")
    print(f"Defender success rate: {summary['defender_success_rate']:.4f}")
    print(f"Average mission time: {summary['avg_time']:.2f}")
    print(f"Average attackers alive: {summary['avg_attacker_alive']:.2f}")
    print(f"Average defenders alive: {summary['avg_defender_alive']:.2f}")
    print(f"Average attacker survival rate: {summary['avg_attacker_survival_rate']:.4f}")
    print(f"Average defender survival rate: {summary['avg_defender_survival_rate']:.4f}")
    print(f"Average attacker total distance: {summary['avg_attacker_total_distance']:.2f}")
    print(f"Average defender total distance: {summary['avg_defender_total_distance']:.2f}")


def print_comparison_table(summaries: List[Dict[str, Any]]) -> None:
    if len(summaries) <= 1:
        return

    print("\n" + "=" * 120)
    print("Comparison Table")
    print("=" * 120)

    headers = [
        "source", "episodes", "atk_success", "def_success", "avg_time",
        "atk_survival", "def_survival", "atk_distance", "def_distance",
    ]

    rows = []
    for summary in summaries:
        rows.append([
            summary["source"],
            str(summary["total_episodes"]),
            f"{summary['attacker_success_rate']:.4f}",
            f"{summary['defender_success_rate']:.4f}",
            f"{summary['avg_time']:.2f}",
            f"{summary['avg_attacker_survival_rate']:.4f}",
            f"{summary['avg_defender_survival_rate']:.4f}",
            f"{summary['avg_attacker_total_distance']:.2f}",
            f"{summary['avg_defender_total_distance']:.2f}",
        ])

    widths = [len(header) for header in headers]
    for row in rows:
        for i, cell in enumerate(row):
            widths[i] = max(widths[i], len(cell))

    def fmt_row(row_vals):
        return " | ".join(str(value).ljust(widths[i]) for i, value in enumerate(row_vals))

    print(fmt_row(headers))
    print("-" * (sum(widths) + 3 * (len(widths) - 1)))
    for row in rows:
        print(fmt_row(row))


def save_summary_csv(summaries: List[Dict[str, Any]], out_path: str) -> None:
    fieldnames = [
        "source",
        "total_episodes",
        "attacker_wins",
        "defender_wins",
        "unknown_results",
        "attacker_success_rate",
        "defender_success_rate",
        "avg_time",
        "avg_attacker_alive",
        "avg_defender_alive",
        "avg_attacker_survival_rate",
        "avg_defender_survival_rate",
        "avg_attacker_total_distance",
        "avg_defender_total_distance",
    ]

    with open(out_path, "w", encoding="utf-8-sig", newline="") as handle:
        writer = csv.DictWriter(handle, fieldnames=fieldnames)
        writer.writeheader()
        for summary in summaries:
            writer.writerow(summary)


def maybe_make_plots(summaries: List[Dict[str, Any]], output_dir: str) -> None:
    if not HAS_MATPLOTLIB or len(summaries) == 0:
        return

    labels = [os.path.splitext(os.path.basename(summary["source"]))[0] for summary in summaries]

    def save_bar(values, title, ylabel, filename):
        plt.figure(figsize=(10, 5))
        plt.bar(labels, values)
        plt.title(title)
        plt.ylabel(ylabel)
        plt.xticks(rotation=20)
        plt.tight_layout()
        plt.savefig(os.path.join(output_dir, filename), dpi=150)
        plt.close()

    save_bar([s["attacker_success_rate"] for s in summaries], "Attacker Success Rate", "Rate", "attacker_success_rate.png")
    save_bar([s["avg_time"] for s in summaries], "Average Mission Completion Time", "Time", "avg_time.png")
    save_bar([s["avg_attacker_survival_rate"] for s in summaries], "Average Attacker Survival Rate", "Rate", "avg_attacker_survival_rate.png")
    save_bar([s["avg_defender_survival_rate"] for s in summaries], "Average Defender Survival Rate", "Rate", "avg_defender_survival_rate.png")
    save_bar([s["avg_attacker_total_distance"] for s in summaries], "Average Attacker Total Distance", "Distance", "avg_attacker_total_distance.png")
    save_bar([s["avg_defender_total_distance"] for s in summaries], "Average Defender Total Distance", "Distance", "avg_defender_total_distance.png")


def resolve_default_results_dir() -> str:
    script_dir = os.path.dirname(os.path.abspath(__file__))
    return os.path.join(script_dir, "Results")


def resolve_summary_output_dir(results_dir: str) -> str:
    return os.path.join(results_dir, SUMMARY_ARCHIVE_DIR_NAME)


def build_summary_archive_dir(results_dir: str) -> str:
    timestamp = datetime.now().strftime("%Y%m%d_%H%M%S_%f")
    return os.path.join(resolve_summary_output_dir(results_dir), timestamp)


def collect_input_csvs(args: List[str]) -> List[str]:
    if args:
        return args

    results_dir = resolve_default_results_dir()
    if not os.path.isdir(results_dir):
        return []

    csv_paths = sorted(glob.glob(os.path.join(results_dir, "*.csv")))
    return [
        path for path in csv_paths
        if os.path.basename(path) != SUMMARY_FILE_NAME
    ]


def analyze_files(csv_paths: List[str], output_dir: str = ".") -> List[Dict[str, Any]]:
    summaries = []

    for path in csv_paths:
        if not os.path.exists(path):
            print(f"[Skip] File does not exist: {path}")
            continue

        rows = load_rows(path)
        if not rows:
            print(f"[Skip] File is empty: {path}")
            continue

        missing_columns = get_missing_battle_columns(rows)
        if missing_columns:
            print(
                f"[Skip] File does not match battle stats schema: {path}. "
                f"Missing columns: {', '.join(missing_columns)}"
            )
            continue

        summary = summarize_rows(rows, path)
        summaries.append(summary)
        print_single_summary(summary)

    if summaries:
        print_comparison_table(summaries)

        os.makedirs(output_dir, exist_ok=True)
        summary_csv_path = os.path.join(output_dir, SUMMARY_FILE_NAME)
        save_summary_csv(summaries, summary_csv_path)
        print(f"\n[Saved] Summary CSV: {summary_csv_path}")

        maybe_make_plots(summaries, output_dir)
        if HAS_MATPLOTLIB:
            print(f"[Saved] Charts directory: {os.path.abspath(output_dir)}")
        else:
            print("[Info] matplotlib is not installed; skipped chart generation.")

    return summaries


def main():
    input_csvs = collect_input_csvs(sys.argv[1:])
    results_dir = resolve_default_results_dir()
    output_dir = build_summary_archive_dir(results_dir)

    if not input_csvs:
        print("No CSV files found to analyze.")
        print("You can:")
        print("1. Run: python analyze_results.py")
        print("   This will automatically scan Python/Results/*.csv")
        print("2. Or pass files explicitly:")
        print("   python analyze_results.py Results/exp_global_off.csv Results/exp_global_on.csv")
        return

    print(f"Analysis output directory: {output_dir}")
    analyze_files(input_csvs, output_dir=output_dir)


if __name__ == "__main__":
    main()

