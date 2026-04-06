import math
from typing import List, Tuple

import numpy as np

from protocol_constants import (
    ENEMY_FEATURE_SIZE,
    ENEMY_OBSERVATION_SLOTS,
    OBSERVATION_HEADER_SIZE,
)


def flat_distance(x1: float, z1: float, x2: float, z2: float) -> float:
    return math.hypot(x2 - x1, z2 - z1)


def yaw_from_z_axis(x0: float, z0: float, x1: float, z1: float) -> float:
    dx = x1 - x0
    dz = z1 - z0
    if abs(dx) < 1e-6 and abs(dz) < 1e-6:
        return 0.0
    return math.atan2(dx, dz)


def extract_enemies_from_obs(
    obs: np.ndarray,
    self_x: float,
    self_z: float,
    detect_radius: float,
    k: int = ENEMY_OBSERVATION_SLOTS,
) -> List[Tuple[float, float, float]]:
    """Extract enemy observations from the shared vector observation."""
    enemies = []
    for i in range(k):
        base_index = OBSERVATION_HEADER_SIZE + i * ENEMY_FEATURE_SIZE
        dx = obs[base_index]
        dz = obs[base_index + 1]
        dist_norm = obs[base_index + 2]

        if abs(dx) < 1e-6 and abs(dz) < 1e-6 and abs(dist_norm) < 1e-6:
            continue

        ex = self_x + dx
        ez = self_z + dz
        dist = float(dist_norm) * detect_radius
        enemies.append((ex, ez, dist))
    return enemies


def nearest_enemy(enemies: List[Tuple[float, float, float]]):
    if len(enemies) == 0:
        return None
    return min(enemies, key=lambda e: e[2])
