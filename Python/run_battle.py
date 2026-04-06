import time

from mlagents_envs.base_env import ActionTuple
from mlagents_envs.environment import UnityEnvironment
from mlagents_envs.exception import UnityCommunicatorStoppedException

from attacker_policy import AttackerPolicy
from defender_policy import DefenderPolicy
from protocol_constants import ACTION_SIZE, OBSERVATION_SIZE
from runtime_policy_config import load_runtime_policy_config


def find_behavior_name(keys, keyword):
    for key in keys:
        if keyword in key:
            return key
    return None


def validate_behavior_spec(spec, behavior_name: str) -> None:
    if len(spec.observation_specs) != 1:
        raise RuntimeError(
            f"{behavior_name} should expose exactly one observation tensor, "
            f"but found {len(spec.observation_specs)}."
        )

    observation_shape = tuple(getattr(spec.observation_specs[0], "shape", ()))
    if observation_shape != (OBSERVATION_SIZE,):
        raise RuntimeError(
            f"{behavior_name} observation size mismatch: expected {OBSERVATION_SIZE}, "
            f"got {observation_shape}."
        )

    continuous_size = getattr(spec.action_spec, "continuous_size", None)
    if continuous_size is not None and continuous_size != ACTION_SIZE:
        raise RuntimeError(
            f"{behavior_name} action size mismatch: expected {ACTION_SIZE}, "
            f"got {continuous_size}."
        )


def main():
    runtime_config = load_runtime_policy_config()
    unity_env_config = runtime_config.unity_env
    tuning = runtime_config.tuning

    print(f"Unity scene config source: {runtime_config.scene_path}")
    if runtime_config.used_defaults:
        print(
            "[Warning] Some Unity scene parameters could not be read. "
            f"Falling back to defaults for: {', '.join(runtime_config.missing_fields)}"
        )
    else:
        print("Unity scene parameters loaded successfully.")

    print(
        "Resolved policy inputs: "
        f"local_sense_radius={unity_env_config.local_sense_radius}, "
        f"defender_patrol_radius={unity_env_config.defender_patrol_radius}, "
        f"attack_radius={unity_env_config.attack_radius}"
    )
    print(
        "Note: these values are read from the saved Unity scene file. "
        "If you changed EnvParams in the Inspector, save SampleScene before running Python."
    )

    env = UnityEnvironment()
    env.reset()

    keys = list(env.behavior_specs.keys())
    print("Behavior keys:", keys)

    attacker_behavior = find_behavior_name(keys, "AttackerBehavior")
    defender_behavior = find_behavior_name(keys, "DefenderBehavior")

    if attacker_behavior is None or defender_behavior is None:
        raise RuntimeError(
            "Could not find the required behaviors. "
            f"Found keys={keys}. Please confirm Unity is using "
            "AttackerBehavior / DefenderBehavior."
        )

    attacker_spec = env.behavior_specs[attacker_behavior]
    defender_spec = env.behavior_specs[defender_behavior]
    validate_behavior_spec(attacker_spec, attacker_behavior)
    validate_behavior_spec(defender_spec, defender_behavior)

    print("Attacker behavior:", attacker_behavior)
    print("Defender behavior:", defender_behavior)
    print("Attacker spec:", attacker_spec)
    print("Defender spec:", defender_spec)
    print(
        f"Expected protocol: observation_size={OBSERVATION_SIZE}, "
        f"continuous_action_size={ACTION_SIZE}"
    )

    attacker_policy = AttackerPolicy(
        detect_radius=unity_env_config.local_sense_radius,
        avoid_distance=unity_env_config.attack_radius,
        pso_particles=tuning.attacker_pso_particles,
        pso_iterations=tuning.attacker_pso_iterations,
        search_radius=tuning.attacker_search_radius,
    )

    defender_policy = DefenderPolicy(
        detect_radius=unity_env_config.local_sense_radius,
        patrol_radius=unity_env_config.defender_patrol_radius,
        patrol_lookahead=tuning.defender_patrol_lookahead,
        local_chase_distance=tuning.defender_local_chase_distance,
    )

    step_count = 0

    try:
        while True:
            attacker_count = 0
            defender_count = 0

            decision_steps, _ = env.get_steps(attacker_behavior)
            if len(decision_steps) > 0:
                obs_batch = decision_steps.obs[0]
                attacker_actions = attacker_policy.act_batch(obs_batch)
                attacker_count = len(attacker_actions)
                env.set_actions(attacker_behavior, ActionTuple(continuous=attacker_actions))

            decision_steps, _ = env.get_steps(defender_behavior)
            if len(decision_steps) > 0:
                obs_batch = decision_steps.obs[0]
                agent_ids = decision_steps.agent_id
                defender_actions = defender_policy.act_batch(agent_ids, obs_batch)
                defender_count = len(defender_actions)
                env.set_actions(defender_behavior, ActionTuple(continuous=defender_actions))

            env.step()
            step_count += 1

            if step_count % 200 == 0:
                print(f"step={step_count}, atk_agents={attacker_count}, def_agents={defender_count}")

            time.sleep(0.02)

    except KeyboardInterrupt:
        print("\nCtrl+C pressed, exiting...")

    except UnityCommunicatorStoppedException:
        print("\nUnity communicator stopped.")

    finally:
        env.close()
        print("Env closed.")


if __name__ == "__main__":
    main()
