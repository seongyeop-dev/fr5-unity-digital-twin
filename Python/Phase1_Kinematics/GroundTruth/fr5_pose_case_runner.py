"""Run the official FR5 MDH pose cases and export canonical JSON, CSV, and TXT."""

import argparse
import json
import os
import sys

CURRENT_FILE = os.path.abspath(__file__)
PYTHON_ROOT = os.path.dirname(os.path.dirname(os.path.dirname(CURRENT_FILE)))

if PYTHON_ROOT not in sys.path:
    sys.path.insert(0, PYTHON_ROOT)

from Phase1_Kinematics.GroundTruth.groundtruth_output_writer import write_canonical_outputs
from Phase1_Kinematics.Python_MDH.fr5_fk_solver import calculate_mdh_fk
from Phase1_Kinematics.Python_MDH.fr5_mdh_params import MDH_CONVENTION


def default_case_file():
    return os.path.join(PYTHON_ROOT, "Phase1_Kinematics", "Python_MDH", "fr5_pose_cases.json")


def default_output_dir():
    return os.path.join(os.path.dirname(CURRENT_FILE), "output")


def load_pose_cases(case_file):
    with open(case_file, "r", encoding="utf-8") as file:
        data = json.load(file)

    if data.get("mdhConvention") != MDH_CONVENTION:
        raise ValueError("Pose case MDH convention does not match the canonical solver.")
    cases = data.get("cases", [])
    if not cases:
        raise ValueError("No pose cases found.")
    for case in cases:
        if not case.get("caseId") or not case.get("caseName"):
            raise ValueError("Each pose case needs caseId and caseName.")
        if len(case.get("jointDegrees", [])) != 6:
            raise ValueError(f"Pose case {case.get('caseId', '<unknown>')} needs six joint degrees.")
    return cases


def run_pose_cases(case_file=None, output_dir=None):
    cases = load_pose_cases(case_file or default_case_file())
    results = [
        calculate_mdh_fk(case["jointDegrees"], case_id=case["caseId"], case_name=case["caseName"])
        for case in cases
    ]
    output_paths = write_canonical_outputs(results, output_dir or default_output_dir())
    return results, output_paths


def main():
    parser = argparse.ArgumentParser(description="Run official FR5 MDH ground-truth pose cases.")
    parser.add_argument("--case-file", default=default_case_file())
    parser.add_argument("--output-dir", default=default_output_dir())
    args = parser.parse_args()

    results, output_paths = run_pose_cases(args.case_file, args.output_dir)
    print(f"Generated {len(results)} FR5 MDH pose cases.")
    for result in results:
        position = result["tcpPositionMeters"]
        rpy = result["tcpRotationRpyDegrees"]
        print(f"{result['caseId']}: TCP=({position[0]:.6f}, {position[1]:.6f}, {position[2]:.6f}) m | RPY=({rpy[0]:.3f}, {rpy[1]:.3f}, {rpy[2]:.3f}) deg")
    for kind, path in output_paths.items():
        print(f"{kind.upper()}: {path}")


if __name__ == "__main__":
    main()
