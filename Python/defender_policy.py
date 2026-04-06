import math

import numpy as np

from common_utils import extract_enemies_from_obs, nearest_enemy, yaw_from_z_axis
from protocol_constants import (
    ENEMY_OBSERVATION_SLOTS,
    SELF_X_INDEX,
    SELF_YAW_INDEX,
    SELF_Z_INDEX,
    TARGET_X_INDEX,
    TARGET_Z_INDEX,
)


class DefenderPolicy:
    def __init__(
        self,
        detect_radius=150.0,
        patrol_radius=150.0,
        patrol_lookahead=20.0,
        local_chase_distance=25.0,
    ):
        self.detect_radius = detect_radius
        self.patrol_radius = patrol_radius
        self.patrol_lookahead = patrol_lookahead
        self.local_chase_distance = local_chase_distance
        self.state = {}

    def act_batch(self, agent_ids, obs_batch: np.ndarray) -> np.ndarray:
        actions = []
        for agent_id, obs in zip(agent_ids, obs_batch):
            actions.append(self.act_single(int(agent_id), obs))
        return np.array(actions, dtype=np.float32)

    def act_single(self, agent_id: int, obs: np.ndarray) -> np.ndarray:
        self_x, self_z = obs[SELF_X_INDEX], obs[SELF_Z_INDEX]
        _self_yaw_rad = obs[SELF_YAW_INDEX]
        center_x, center_z = obs[TARGET_X_INDEX], obs[TARGET_Z_INDEX]

        enemies = extract_enemies_from_obs(
            obs,
            self_x,
            self_z,
            self.detect_radius,
            k=ENEMY_OBSERVATION_SLOTS,
        )
        has_enemy = len(enemies) > 0

        if agent_id not in self.state:
            self.state[agent_id] = "Patrol"

        if self.state[agent_id] == "Patrol":
            if has_enemy:
                self.state[agent_id] = "Engage"
            else:
                px, pz = self._patrol_point(self_x, self_z, center_x, center_z)
                yaw_z_rad = yaw_from_z_axis(self_x, self_z, px, pz)
                return np.array([px, pz, yaw_z_rad], dtype=np.float32)

        if self.state[agent_id] == "Engage":
            if not has_enemy:
                self.state[agent_id] = "Patrol"
                px, pz = self._patrol_point(self_x, self_z, center_x, center_z)
                yaw_z_rad = yaw_from_z_axis(self_x, self_z, px, pz)
                return np.array([px, pz, yaw_z_rad], dtype=np.float32)

            enemy = nearest_enemy(enemies)
            ex, ez, _ = enemy
            px, pz = self._local_intercept_point(self_x, self_z, ex, ez)
            yaw_z_rad = yaw_from_z_axis(self_x, self_z, px, pz)
            return np.array([px, pz, yaw_z_rad], dtype=np.float32)

        px, pz = self._patrol_point(self_x, self_z, center_x, center_z)
        yaw_z_rad = yaw_from_z_axis(self_x, self_z, px, pz)
        return np.array([px, pz, yaw_z_rad], dtype=np.float32)

    def _patrol_point(self, self_x: float, self_z: float, center_x: float, center_z: float):
        rx = self_x - center_x
        rz = self_z - center_z
        radius = math.hypot(rx, rz)

        if radius < 1e-6:
            rx, rz = self.patrol_radius, 0.0
            radius = self.patrol_radius

        rx = rx / radius * self.patrol_radius
        rz = rz / radius * self.patrol_radius

        tx = -rz
        tz = rx
        tangent_norm = math.hypot(tx, tz)
        tx /= tangent_norm
        tz /= tangent_norm

        look_x = center_x + rx + tx * self.patrol_lookahead
        look_z = center_z + rz + tz * self.patrol_lookahead

        cx = look_x - center_x
        cz = look_z - center_z
        center_norm = math.hypot(cx, cz)
        if center_norm < 1e-6:
            return self_x, self_z

        cx = cx / center_norm * self.patrol_radius
        cz = cz / center_norm * self.patrol_radius
        return center_x + cx, center_z + cz

    def _local_intercept_point(self, self_x: float, self_z: float, enemy_x: float, enemy_z: float):
        dx = enemy_x - self_x
        dz = enemy_z - self_z
        distance = math.hypot(dx, dz)
        if distance < 1e-6:
            return self_x, self_z

        step = min(self.local_chase_distance, distance)
        dx /= distance
        dz /= distance
        return self_x + dx * step, self_z + dz * step
