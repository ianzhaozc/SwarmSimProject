# SwarmSimProject

Unity-based swarm confrontation simulation with Python-side decision control and result analysis.

## Overview

This project is split into three cooperating parts:

- Unity simulation
  Runs the battlefield, spawns attacker and defender UAVs, applies movement and combat rules, and exports per-episode statistics.
- Python control
  Connects to Unity through ML-Agents, reads observations, computes actions, and sends them back to Unity.
- Python analysis
  Reads exported CSV files and produces summaries and optional plots.

In short:

```text
Unity scene -> ML-Agents communication -> Python policies -> Unity execution -> CSV export -> Python analysis
```

For a detailed Chinese walkthrough for first-time readers, see:

[PROJECT_GUIDE_CN.md](/d:/UnityProjects/Github/SwarmSimProject/PROJECT_GUIDE_CN.md)

## Project Structure

```text
Assets/
  Scenes/          Unity scenes
  Scripts/         Core simulation logic
  Prefabs/         Attacker and defender UAV prefabs
  Materials/       Runtime materials

Python/
  run_battle.py        Python battle loop
  attacker_policy.py   Attacker policy
  defender_policy.py   Defender policy
  common_utils.py      Shared observation helpers
  protocol_constants.py Shared protocol definition
  analyze_results.py   Result analysis
  Results/             CSV and chart output
```

## Main Unity Components

The main scene object is `GameManager` in `SampleScene`.

Recommended components on `GameManager`:

- `EnvParams`
- `ScenarioManager`
- `GameRuleManager`
- `BattleManager`
- `BattleStatsManager`

Responsibilities:

- `EnvParams`
  Stores battlefield, sensing, combat, and team configuration.
- `ScenarioManager`
  Spawns and resets all UAVs.
- `GameRuleManager`
  Handles win conditions, time limits, and multi-episode runs.
- `BattleManager`
  Tracks registered units and supports alive/enemy/friendly queries.
- `BattleStatsManager`
  Tracks episode statistics and exports CSV results to `Python/Results/`.

## Python Workflow

See the Python-specific guide here:

[Python/README.md](/d:/UnityProjects/Github/SwarmSimProject/Python/README.md)

## Protocol Contract

Unity and Python must agree on the same observation/action format.

- Observation size: `14`
- Continuous action size: `3`
- Enemy slots: `3`
- Yaw convention: relative to the `Z` axis, in radians

Observation:

```text
[self_x, self_z, self_yaw_z_rad,
 target_x, target_z,
 enemy1_dx, enemy1_dz, enemy1_distNorm,
 enemy2_dx, enemy2_dz, enemy2_distNorm,
 enemy3_dx, enemy3_dz, enemy3_distNorm]
```

Action:

```text
[target_x, target_z, target_yaw_z_rad]
```

The Python-side shared definition lives in [protocol_constants.py](/d:/UnityProjects/Github/SwarmSimProject/Python/protocol_constants.py).

## Quick Start

1. Open the project in Unity.
2. Open `Assets/Scenes/SampleScene.unity`.
3. Confirm `GameManager` has the expected manager components and references.
4. In your Python environment, install dependencies:

```powershell
pip install -r Python/requirements.txt
```

5. Run the Python controller:

```powershell
python Python/run_battle.py
```

6. After the Python process starts waiting for the Unity Editor connection, enter Play mode in Unity.
7. After one or more episodes complete, analyze results:

```powershell
python Python/analyze_results.py
```

## Result Output

Battle CSV files are written to:

```text
Python/Results/
```

Typical outputs:

- `battle_stats_YYYYMMDD_HHMMSS.csv`
- `Summaries/YYYYMMDD_HHMMSS/summary_results.csv`
- charts saved alongside each archived summary if `matplotlib` is installed

Each Unity Play session now creates a separate battle CSV automatically, so later runs will not overwrite or mix into the previous run's raw output file.

## Important Notes

- The Unity project references ML-Agents through a local path in [manifest.json](/d:/UnityProjects/Github/SwarmSimProject/Packages/manifest.json).
- The Python `mlagents_envs` package must be compatible with that Unity ML-Agents version.
- If Unity behavior names, observation size, or action size change, update both Unity and Python together.

## Current Architecture

This project is best described as:

- component-based Unity simulation
- external Python policy control
- layered result analysis pipeline

It is not a traditional MVC app. The design is closer to:

```text
Simulation layer -> Decision layer -> Analysis layer
```

## Typical Debug Checklist

- `GameManager` is fully wired in Unity.
- Attacker prefab uses `AttackerBehavior`.
- Defender prefab uses `DefenderBehavior`.
- Observation size is still `14`.
- Continuous action size is still `3`.
- Start `Python/run_battle.py` first, then enter Unity Play mode.
- `BattleStatsManager` is exporting CSV files into `Python/Results/`.
