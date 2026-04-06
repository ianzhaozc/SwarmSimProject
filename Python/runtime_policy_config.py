import os
import re
from dataclasses import dataclass
from typing import List, Optional, Tuple


SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
PROJECT_ROOT = os.path.dirname(SCRIPT_DIR)
DEFAULT_SCENE_PATH = os.path.join(PROJECT_ROOT, "Assets", "Scenes", "SampleScene.unity")


@dataclass(frozen=True)
class UnityEnvConfig:
    local_sense_radius: float = 150.0
    defender_patrol_radius: float = 150.0
    attack_radius: float = 60.0


@dataclass(frozen=True)
class PolicyTuning:
    attacker_pso_particles: int = 16
    attacker_pso_iterations: int = 8
    attacker_search_radius: float = 60.0
    defender_patrol_lookahead: float = 20.0
    defender_local_chase_distance: float = 25.0


@dataclass(frozen=True)
class RuntimePolicyConfig:
    unity_env: UnityEnvConfig
    tuning: PolicyTuning
    scene_path: str
    used_defaults: bool
    missing_fields: Tuple[str, ...]


def _extract_scalar(scene_text: str, field_name: str) -> Optional[str]:
    pattern = re.compile(rf"^\s*{re.escape(field_name)}:\s*(.+?)\s*$", re.MULTILINE)
    match = pattern.search(scene_text)
    if not match:
        return None
    return match.group(1).strip()


def _extract_float(scene_text: str, field_name: str) -> Optional[float]:
    value = _extract_scalar(scene_text, field_name)
    if value is None:
        return None

    try:
        return float(value)
    except ValueError:
        return None


def load_runtime_policy_config(scene_path: str = DEFAULT_SCENE_PATH) -> RuntimePolicyConfig:
    default_unity_env = UnityEnvConfig()
    tuning = PolicyTuning()

    if not os.path.exists(scene_path):
        return RuntimePolicyConfig(
            unity_env=default_unity_env,
            tuning=tuning,
            scene_path=scene_path,
            used_defaults=True,
            missing_fields=("scene_file_missing",),
        )

    with open(scene_path, "r", encoding="utf-8-sig") as handle:
        scene_text = handle.read()

    local_sense_radius = _extract_float(scene_text, "localSenseRadius")
    defender_patrol_radius = _extract_float(scene_text, "defenderPatrolRadius")
    attack_radius = _extract_float(scene_text, "attackRadius")

    missing_fields: List[str] = []
    if local_sense_radius is None:
        missing_fields.append("localSenseRadius")
    if defender_patrol_radius is None:
        missing_fields.append("defenderPatrolRadius")
    if attack_radius is None:
        missing_fields.append("attackRadius")

    unity_env = UnityEnvConfig(
        local_sense_radius=(
            local_sense_radius
            if local_sense_radius is not None
            else default_unity_env.local_sense_radius
        ),
        defender_patrol_radius=(
            defender_patrol_radius
            if defender_patrol_radius is not None
            else default_unity_env.defender_patrol_radius
        ),
        attack_radius=attack_radius if attack_radius is not None else default_unity_env.attack_radius,
    )

    return RuntimePolicyConfig(
        unity_env=unity_env,
        tuning=tuning,
        scene_path=scene_path,
        used_defaults=len(missing_fields) > 0,
        missing_fields=tuple(missing_fields),
    )
