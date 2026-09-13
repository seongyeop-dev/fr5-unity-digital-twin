"""Canonical FR5 MDH forward kinematics solver."""

import numpy as np

from Phase1_Kinematics.Common.transform_utils import (
    extract_position,
    extract_rotation_matrix,
    rot_x,
    rot_z,
    rotation_matrix_to_rpy_deg,
    trans,
)
from Phase1_Kinematics.Python_MDH.fr5_mdh_params import MDH_CONVENTION, get_fr5_mdh_params


def _normalize_joint_list(joint_deg_list):
    if len(joint_deg_list) != 6:
        raise ValueError("joint_deg_list must contain 6 joint values.")
    return [float(value) for value in joint_deg_list]


def calculate_mdh_fk(joint_deg_list, mdh_params=None, case_id="", case_name=""):
    """Calculate canonical FR5 MDH FK from six joint angles in degrees.

    The canonical fields are intended for offline comparison.  Legacy aliases are
    retained at the end of the returned dictionary for the existing Unity TXT
    validation path.
    """
    joint_degrees = _normalize_joint_list(joint_deg_list)
    params = get_fr5_mdh_params() if mdh_params is None else mdh_params
    if len(params) != 6:
        raise ValueError("mdh_params must contain 6 joints.")

    joint_radians = [
        float(np.deg2rad(joint_degrees[index] + float(params[index].get("theta_offset_deg", 0.0))))
        for index in range(6)
    ]

    T = np.eye(4, dtype=float)
    joint_transforms = []
    joint_positions = []
    joint_rotation_matrices = []
    structure_transforms = {"BASE": T.copy()}

    for index, (param, theta) in enumerate(zip(params, joint_radians), start=1):
        alpha = float(param["alpha"])
        a = float(param["a"])
        d = float(param["d"])

        # Official project MDH order: Rx(alpha) -> Tx(a) -> Rz(theta) -> Tz(d).
        T = T @ rot_x(alpha)
        structure_transforms[f"ALPHA{index}"] = T.copy()
        T = T @ trans(a, 0.0, 0.0)
        structure_transforms[f"A{index}"] = T.copy()
        T = T @ rot_z(theta)
        structure_transforms[f"JOINT{index}"] = T.copy()
        T = T @ trans(0.0, 0.0, d)
        structure_transforms[f"D{index}"] = T.copy()

        joint_transforms.append(T.copy())
        joint_positions.append(extract_position(T))
        joint_rotation_matrices.append(extract_rotation_matrix(T))

    structure_transforms["TCP"] = T.copy()
    tcp_position = extract_position(T)
    tcp_rotation = extract_rotation_matrix(T)
    canonical_result = {
        "caseId": str(case_id),
        "caseName": str(case_name),
        "jointDegrees": joint_degrees,
        "jointRadians": joint_radians,
        "T_base_tcp": T.copy(),
        "tcpPositionMeters": tcp_position,
        "tcpRotationMatrix": tcp_rotation,
        "tcpRotationRpyDegrees": rotation_matrix_to_rpy_deg(tcp_rotation),
        "mdhConvention": MDH_CONVENTION,
        "units": {"length": "meter", "angle": "radian", "inputJointAngle": "degree", "rpy": "degree"},
    }

    # Compatibility fields used by groundtruth_single_runner.py and Unity TXT readers.
    canonical_result.update({
        "T": T.copy(),
        "position": tcp_position.copy(),
        "rotation_matrix": tcp_rotation.copy(),
        "joint_transforms": joint_transforms,
        "joint_positions": joint_positions,
        "joint_rotation_matrices": joint_rotation_matrices,
        "structure_transforms": structure_transforms,
        "structure_positions": {name: extract_position(transform) for name, transform in structure_transforms.items()},
    })
    return canonical_result
