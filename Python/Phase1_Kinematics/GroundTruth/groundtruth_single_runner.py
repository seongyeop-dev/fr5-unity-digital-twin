import os
import sys
import numpy as np

CURRENT_FILE = os.path.abspath(__file__)

PYTHON_ROOT = os.path.dirname(
    os.path.dirname(
        os.path.dirname(CURRENT_FILE)
    )
)

PROJECT_ROOT = os.path.dirname(PYTHON_ROOT)

if PYTHON_ROOT not in sys.path:
    sys.path.append(PYTHON_ROOT)

from Phase1_Kinematics.Python_MDH.fr5_fk_solver import calculate_mdh_fk
from Phase1_Kinematics.GroundTruth.groundtruth_output_writer import (
    ensure_output_directory,
    clear_output_file,
    append_groundtruth_readable,
)


STREAMING_ASSETS_DIR = os.path.join(
    PROJECT_ROOT,
    "Unity",
    "FAIRINO_FR5_DigitalTwin",
    "Assets",
    "StreamingAssets"
)
STREAMING_ASSETS_DIR = os.path.abspath(STREAMING_ASSETS_DIR)

# Preserve the development layout and also support the public Unity repository.
PUBLIC_STREAMING_ASSETS_DIR = os.path.join(PROJECT_ROOT, "Assets", "StreamingAssets")
if not os.path.isdir(STREAMING_ASSETS_DIR) and os.path.isdir(PUBLIC_STREAMING_ASSETS_DIR):
    STREAMING_ASSETS_DIR = PUBLIC_STREAMING_ASSETS_DIR


def build_live_paths():
    input_dir = os.path.join(STREAMING_ASSETS_DIR, "Input")
    output_dir = os.path.join(STREAMING_ASSETS_DIR, "Output")

    joint_input_path = os.path.join(input_dir, "current_joint.txt")
    readable_path = os.path.join(output_dir, "python_groundtruth_readable.txt")

    return joint_input_path, output_dir, readable_path


def read_current_joint_file(file_path: str):
    if not os.path.exists(file_path):
        raise FileNotFoundError(f"current_joint.txt not found: {file_path}")

    values = []

    with open(file_path, "r", encoding="utf-8") as file:
        for raw_line in file:
            line = raw_line.strip()
            if not line:
                continue

            # Accept both Unity's six-line format and a single CSV joint row.
            for token in line.split(","):
                token = token.strip()
                if not token:
                    continue
                try:
                    values.append(float(token))
                except ValueError as ex:
                    raise ValueError(
                        f"current_joint.txt contains a non-numeric joint value: {token!r}"
                    ) from ex

    if len(values) != 6:
        raise ValueError(
            "current_joint.txt must contain exactly 6 joint values "
            f"(six lines or one comma-separated line). Found: {len(values)}"
        )

    return values


def print_vector(name: str, vec: np.ndarray) -> None:
    print(f"{name} X = {vec[0]:.6f}")
    print(f"{name} Y = {vec[1]:.6f}")
    print(f"{name} Z = {vec[2]:.6f}")


def run_live_single_export() -> None:
    joint_input_path, output_dir, readable_path = build_live_paths()

    joints = read_current_joint_file(joint_input_path)
    fk_result = calculate_mdh_fk(joints, case_id="LIVE", case_name="UnityLive")

    tcp_position = np.asarray(fk_result["position"], dtype=float)
    rotation_matrix = np.asarray(fk_result["rotation_matrix"], dtype=float)

    ensure_output_directory(output_dir)
    clear_output_file(readable_path)

    append_groundtruth_readable(
        readable_path,
        "LIVE",
        "UnityLive",
        joints,
        tcp_position,
        rotation_matrix,
    )

    print("========== Python Ground Truth Live Single Export ==========")
    print(f"Joints : {joints}")
    print_vector("TCP Position", tcp_position)
    print(">> Saved Live Ground Truth Output")


if __name__ == "__main__":
    run_live_single_export()
