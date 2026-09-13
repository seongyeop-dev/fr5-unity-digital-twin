"""Homogeneous-transform helpers for the FR5 math ground-truth path."""

import math
import numpy as np


def rot_x(theta_rad: float) -> np.ndarray:
    c, s = np.cos(theta_rad), np.sin(theta_rad)
    return np.array([[1.0, 0.0, 0.0, 0.0], [0.0, c, -s, 0.0], [0.0, s, c, 0.0], [0.0, 0.0, 0.0, 1.0]], dtype=float)


def rot_y(theta_rad: float) -> np.ndarray:
    c, s = np.cos(theta_rad), np.sin(theta_rad)
    return np.array([[c, 0.0, s, 0.0], [0.0, 1.0, 0.0, 0.0], [-s, 0.0, c, 0.0], [0.0, 0.0, 0.0, 1.0]], dtype=float)


def rot_z(theta_rad: float) -> np.ndarray:
    c, s = np.cos(theta_rad), np.sin(theta_rad)
    return np.array([[c, -s, 0.0, 0.0], [s, c, 0.0, 0.0], [0.0, 0.0, 1.0, 0.0], [0.0, 0.0, 0.0, 1.0]], dtype=float)


def rot_axis(axis: str, theta_rad: float) -> np.ndarray:
    normalized_axis = axis.strip().upper()
    if normalized_axis == "X":
        return rot_x(theta_rad)
    if normalized_axis == "Y":
        return rot_y(theta_rad)
    if normalized_axis == "Z":
        return rot_z(theta_rad)
    raise ValueError(f"Unsupported axis: {axis}")


def trans(x: float, y: float, z: float) -> np.ndarray:
    return np.array([[1.0, 0.0, 0.0, x], [0.0, 1.0, 0.0, y], [0.0, 0.0, 1.0, z], [0.0, 0.0, 0.0, 1.0]], dtype=float)


def extract_position(T: np.ndarray) -> np.ndarray:
    return np.asarray(T, dtype=float)[0:3, 3].copy()


def extract_rotation_matrix(T: np.ndarray) -> np.ndarray:
    return np.asarray(T, dtype=float)[0:3, 0:3].copy()


def rotation_matrix_to_rpy_rad(rotation_matrix: np.ndarray) -> np.ndarray:
    """Return stable [roll, pitch, yaw] for R = Rz(yaw) @ Ry(pitch) @ Rx(roll)."""
    R = np.asarray(rotation_matrix, dtype=float)
    if R.shape != (3, 3):
        raise ValueError("rotation_matrix must be 3x3.")

    cos_pitch = math.hypot(float(R[0, 0]), float(R[1, 0]))
    if cos_pitch > 1e-9:
        roll = math.atan2(float(R[2, 1]), float(R[2, 2]))
        pitch = math.atan2(-float(R[2, 0]), cos_pitch)
        yaw = math.atan2(float(R[1, 0]), float(R[0, 0]))
    else:
        # Gimbal lock: choose yaw=0 and retain the observable roll component.
        roll = math.atan2(-float(R[1, 2]), float(R[1, 1]))
        pitch = math.atan2(-float(R[2, 0]), cos_pitch)
        yaw = 0.0

    return np.array([roll, pitch, yaw], dtype=float)


def rotation_matrix_to_rpy_deg(rotation_matrix: np.ndarray) -> np.ndarray:
    return np.rad2deg(rotation_matrix_to_rpy_rad(rotation_matrix))
