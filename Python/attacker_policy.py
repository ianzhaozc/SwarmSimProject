import math
import random

import numpy as np

from common_utils import extract_enemies_from_obs, yaw_from_z_axis
from protocol_constants import (
    ENEMY_OBSERVATION_SLOTS,
    SELF_X_INDEX,
    SELF_YAW_INDEX,
    SELF_Z_INDEX,
    TARGET_X_INDEX,
    TARGET_Z_INDEX,
)


class AttackerPolicy:
    def __init__(
        self,
        detect_radius=150.0,
        avoid_distance=60.0,
        pso_particles=16,
        pso_iterations=8,
        search_radius=60.0,
    ):
        self.detect_radius = detect_radius
        self.avoid_distance = avoid_distance
        self.pso_particles = pso_particles
        self.pso_iterations = pso_iterations
        self.search_radius = search_radius

    def act_batch(self, obs_batch: np.ndarray) -> np.ndarray:
        return np.array([self.act_single(obs) for obs in obs_batch], dtype=np.float32)

    def act_single(self, obs: np.ndarray) -> np.ndarray:
        self_x, self_z = obs[SELF_X_INDEX], obs[SELF_Z_INDEX]
        _self_yaw_rad = obs[SELF_YAW_INDEX]
        target_x, target_z = obs[TARGET_X_INDEX], obs[TARGET_Z_INDEX]

        enemies = extract_enemies_from_obs(
            obs,
            self_x,
            self_z,
            self.detect_radius,
            k=ENEMY_OBSERVATION_SLOTS,
        )

        best_x, best_z = self._pso_search(self_x, self_z, target_x, target_z, enemies)
        yaw_z_rad = yaw_from_z_axis(self_x, self_z, best_x, best_z)

        return np.array([best_x, best_z, yaw_z_rad], dtype=np.float32)

    def _fitness(self, x: float, z: float, target_x: float, target_z: float, enemies):
        d_target = math.hypot(x - target_x, z - target_z)

        enemy_penalty = 0.0
        close_penalty = 0.0
        for ex, ez, _ in enemies:
            d = math.hypot(x - ex, z - ez)
            d = max(d, 1e-3)
            enemy_penalty += 1.0 / d
            if d < self.avoid_distance:
                close_penalty += (self.avoid_distance - d)

        return -1.0 * d_target - 35.0 * enemy_penalty - 0.8 * close_penalty

    def _pso_search(self, self_x: float, self_z: float, target_x: float, target_z: float, enemies):
        particles = []
        velocities = []
        pbest = []
        pbest_score = []

        for _ in range(self.pso_particles):
            ang = random.uniform(0.0, 2.0 * math.pi)
            rad = random.uniform(0.0, self.search_radius)
            px = self_x + math.cos(ang) * rad
            pz = self_z + math.sin(ang) * rad

            particles.append([px, pz])
            velocities.append([0.0, 0.0])

            score = self._fitness(px, pz, target_x, target_z, enemies)
            pbest.append([px, pz])
            pbest_score.append(score)

        gbest_idx = int(np.argmax(pbest_score))
        gbest = pbest[gbest_idx].copy()
        gbest_score = pbest_score[gbest_idx]

        w = 0.55
        c1 = 1.25
        c2 = 1.25

        for _ in range(self.pso_iterations):
            for i in range(self.pso_particles):
                r1 = random.random()
                r2 = random.random()

                velocities[i][0] = (
                    w * velocities[i][0]
                    + c1 * r1 * (pbest[i][0] - particles[i][0])
                    + c2 * r2 * (gbest[0] - particles[i][0])
                )
                velocities[i][1] = (
                    w * velocities[i][1]
                    + c1 * r1 * (pbest[i][1] - particles[i][1])
                    + c2 * r2 * (gbest[1] - particles[i][1])
                )

                particles[i][0] += velocities[i][0]
                particles[i][1] += velocities[i][1]

                score = self._fitness(
                    particles[i][0],
                    particles[i][1],
                    target_x,
                    target_z,
                    enemies,
                )

                if score > pbest_score[i]:
                    pbest[i] = particles[i].copy()
                    pbest_score[i] = score

                if score > gbest_score:
                    gbest = particles[i].copy()
                    gbest_score = score

        return gbest[0], gbest[1]
