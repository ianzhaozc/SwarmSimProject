# Python Workflow

This folder contains the Python-side control and analysis scripts for the Unity swarm simulation.

## Files

- `run_battle.py`
  Connects to Unity through ML-Agents, validates the observation/action protocol, and sends actions for both teams.
- `attacker_policy.py`
  Attacker policy. Uses a PSO-style local search to choose the next target point.
- `defender_policy.py`
  Defender policy. Uses a small state machine with patrol and engage modes.
- `common_utils.py`
  Shared helper functions for parsing observations and angle calculations.
- `protocol_constants.py`
  Shared protocol constants such as observation size, action size, and observation indices.
- `analyze_results.py`
  Reads exported CSV files from `Results/`, prints summaries, writes an archived `summary_results.csv` under `Results/Summaries/<timestamp>/`, and optionally saves charts.
- `Results/`
  Default output directory for battle statistics and analysis artifacts.

## Expected Protocol

The Unity and Python sides must agree on the same protocol.

- Observation size: `14`
- Continuous action size: `3`
- Enemy observation slots: `3`
- Yaw convention: relative to the `Z` axis, in radians

Observation layout:

```text
[self_x, self_z, self_yaw_z_rad,
 target_x, target_z,
 enemy1_dx, enemy1_dz, enemy1_distNorm,
 enemy2_dx, enemy2_dz, enemy2_distNorm,
 enemy3_dx, enemy3_dz, enemy3_distNorm]
```

Action layout:

```text
[target_x, target_z, target_yaw_z_rad]
```

## Dependencies

Install the Python dependencies in your environment first.

```powershell
pip install -r Python/requirements.txt
```

Important:

- `mlagents_envs` must be compatible with the Unity ML-Agents package used by this project.
- The Unity project currently references a local ML-Agents package path in [manifest.json](/d:/UnityProjects/Github/SwarmSimProject/Packages/manifest.json).
- If Unity and Python ML-Agents versions do not match, `run_battle.py` may fail to connect or validate behavior specs.

## Running a Battle

1. Open the Unity project.
2. Open `SampleScene`.
3. Run:

```powershell
python Python/run_battle.py
```

4. After the Python process starts waiting for the Unity Editor connection, enter Play mode in Unity.

If everything is configured correctly, the script will:

- print detected behavior names
- validate the observation/action protocol
- load environment-dependent policy inputs from the saved `Assets/Scenes/SampleScene.unity`
- continuously send actions for attackers and defenders

Important:

- If you changed `EnvParams` in the Unity Inspector, save `SampleScene` before running `run_battle.py`.

## Analyzing Results

To analyze all CSV files under `Python/Results/`:

```powershell
python Python/analyze_results.py
```

To analyze specific files:

```powershell
python Python/analyze_results.py Python/Results/exp_global_off.csv Python/Results/exp_global_on.csv
```

The analyzer will:

- print per-file summaries
- print a comparison table when more than one CSV is provided
- write `Python/Results/Summaries/<timestamp>/summary_results.csv`
- optionally save charts if `matplotlib` is installed

## Typical Debug Checklist

- Unity `GameManager` has `EnvParams`, `ScenarioManager`, `GameRuleManager`, `BattleManager`, and `BattleStatsManager`.
- `BattleStatsManager` is exporting timestamped battle CSV files to `Python/Results/`.
- Attacker and defender prefabs still use `AttackerBehavior` and `DefenderBehavior`.
- Observation size is still `14`.
- Continuous action size is still `3`.
- Start `run_battle.py` first, then enter Unity Play mode so the Editor can connect back.
