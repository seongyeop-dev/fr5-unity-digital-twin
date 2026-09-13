import csv
import json
import os
from typing import Dict, List
import numpy as np


def ensure_output_directory(output_dir: str) -> None:
    os.makedirs(output_dir, exist_ok=True)


def clear_output_file(file_path: str) -> None:
    with open(file_path, "w", encoding="utf-8") as file:
        file.write("")


def append_groundtruth_readable(
    file_path: str,
    case_id: str,
    case_name: str,
    joints: List[float],
    tcp_position: np.ndarray,
    rotation_matrix: np.ndarray,
) -> None:
    with open(file_path, "a", encoding="utf-8") as file:
        file.write("[TestCase]\n")
        file.write(f"ID={case_id}\n")
        file.write(f"Name={case_name}\n")

        file.write("\n[Input]\n")
        for i in range(6):
            file.write(f"J{i+1}={joints[i]:.6f}\n")

        file.write("\n[TCP]\n")
        file.write(f"TCP_Position_X={tcp_position[0]:.6f}\n")
        file.write(f"TCP_Position_Y={tcp_position[1]:.6f}\n")
        file.write(f"TCP_Position_Z={tcp_position[2]:.6f}\n")

        file.write("\n[RotationMatrix]\n")
        for row in rotation_matrix:
            file.write(" ".join([f"{value:.6f}" for value in row]) + "\n")

        file.write("\n-----------------------------------\n\n")


def append_groundtruth_raw(
    file_path: str,
    case_id: str,
    case_name: str,
    joints: List[float],
    fk_result: Dict[str, object],
) -> None:
    T = np.asarray(fk_result["T"], dtype=float)
    tcp_position = np.asarray(fk_result["position"], dtype=float)
    rotation_matrix = np.asarray(fk_result["rotation_matrix"], dtype=float)
    joint_positions = fk_result["joint_positions"]
    structure_positions = fk_result.get("structure_positions", {})

    ordered_names = [
        "BASE",
        "JOINT1",
        "D1",
        "ALPHA1",
        "JOINT2",
        "A2",
        "JOINT3",
        "A3",
        "JOINT4",
        "D4",
        "ALPHA4",
        "JOINT5",
        "D5",
        "ALPHA5",
        "JOINT6",
        "D6",
        "TCP",
    ]

    with open(file_path, "a", encoding="utf-8") as file:
        file.write("[TestCase]\n")
        file.write(f"ID={case_id}\n")
        file.write(f"Name={case_name}\n")

        file.write("\n[Input]\n")
        for i in range(6):
            file.write(f"J{i+1}={joints[i]:.6f}\n")

        file.write("\n[TCP]\n")
        file.write(f"TCP_Position_X={tcp_position[0]:.6f}\n")
        file.write(f"TCP_Position_Y={tcp_position[1]:.6f}\n")
        file.write(f"TCP_Position_Z={tcp_position[2]:.6f}\n")

        file.write("\n[JointPositions]\n")
        for index, position in enumerate(joint_positions, start=1):
            pos = np.asarray(position, dtype=float)
            file.write(
                f"J{index}_Position={pos[0]:.6f},{pos[1]:.6f},{pos[2]:.6f}\n"
            )

        file.write("\n[StructurePositions]\n")
        for name in ordered_names:
            if name not in structure_positions:
                continue
            pos = np.asarray(structure_positions[name], dtype=float)
            file.write(f"{name}_Position={pos[0]:.6f},{pos[1]:.6f},{pos[2]:.6f}\n")

        file.write("\n[RotationMatrix]\n")
        for row in rotation_matrix:
            file.write(" ".join([f"{value:.6f}" for value in row]) + "\n")

        file.write("\n[Transform]\n")
        for row in T:
            file.write(" ".join([f"{value:.6f}" for value in row]) + "\n")

        file.write("\n===================================\n\n")


def _json_value(value):
    if isinstance(value, np.ndarray):
        return value.tolist()
    if isinstance(value, np.generic):
        return value.item()
    if isinstance(value, dict):
        return {key: _json_value(item) for key, item in value.items()}
    if isinstance(value, (list, tuple)):
        return [_json_value(item) for item in value]
    return value


def write_canonical_outputs(results, output_dir: str, file_prefix: str = "fr5_mdh_results"):
    """Write canonical pose-case results as separate JSON, CSV, and TXT files."""
    ensure_output_directory(output_dir)
    json_path = os.path.join(output_dir, file_prefix + ".json")
    csv_path = os.path.join(output_dir, file_prefix + ".csv")
    txt_path = os.path.join(output_dir, file_prefix + ".txt")

    canonical_keys = (
        "caseId", "caseName", "jointDegrees", "jointRadians", "T_base_tcp",
        "tcpPositionMeters", "tcpRotationMatrix", "tcpRotationRpyDegrees",
        "mdhConvention", "units",
    )
    canonical_results = [
        {key: _json_value(result[key]) for key in canonical_keys}
        for result in results
    ]

    with open(json_path, "w", encoding="utf-8") as file:
        json.dump({"results": canonical_results}, file, ensure_ascii=False, indent=2)
        file.write("\n")

    with open(csv_path, "w", newline="", encoding="utf-8") as file:
        writer = csv.DictWriter(
            file,
            fieldnames=(
                ["caseId", "caseName"]
                + [f"jointDegree{i}" for i in range(1, 7)]
                + [f"jointRadian{i}" for i in range(1, 7)]
                + ["tcpX_m", "tcpY_m", "tcpZ_m", "roll_deg", "pitch_deg", "yaw_deg", "mdhConvention"]
            ),
        )
        writer.writeheader()
        for result in canonical_results:
            row = {"caseId": result["caseId"], "caseName": result["caseName"], "mdhConvention": result["mdhConvention"]}
            row.update({f"jointDegree{i + 1}": result["jointDegrees"][i] for i in range(6)})
            row.update({f"jointRadian{i + 1}": result["jointRadians"][i] for i in range(6)})
            row.update({"tcpX_m": result["tcpPositionMeters"][0], "tcpY_m": result["tcpPositionMeters"][1], "tcpZ_m": result["tcpPositionMeters"][2]})
            row.update({"roll_deg": result["tcpRotationRpyDegrees"][0], "pitch_deg": result["tcpRotationRpyDegrees"][1], "yaw_deg": result["tcpRotationRpyDegrees"][2]})
            writer.writerow(row)

    with open(txt_path, "w", encoding="utf-8") as file:
        for result in canonical_results:
            file.write(f"[PoseCase] {result['caseId']} | {result['caseName']}\n")
            file.write(f"Joint degrees: {result['jointDegrees']}\n")
            file.write(f"Joint radians: {result['jointRadians']}\n")
            file.write("T_base_tcp:\n")
            for row in result["T_base_tcp"]:
                file.write("  " + " ".join(f"{value:.9f}" for value in row) + "\n")
            file.write(f"TCP position (m): {result['tcpPositionMeters']}\n")
            file.write(f"TCP RPY (deg): {result['tcpRotationRpyDegrees']}\n")
            file.write(f"MDH: {result['mdhConvention']}\n\n")

    return {"json": json_path, "csv": csv_path, "txt": txt_path}
