"""Canonical FR5 Modified DH parameter table.

Units are metres and radians.  Each row uses the project MDH convention:
RotX(alpha) -> TransX(a) -> RotZ(theta) -> TransZ(d).
"""

import numpy as np


MDH_CONVENTION = "RotX(alpha) -> TransX(a) -> RotZ(theta) -> TransZ(d)"


def get_fr5_mdh_params():
    """Return the authoritative six-axis FR5 MDH parameter table."""
    return [
        {"joint": 1, "alpha": np.pi / 2.0, "a": 0.0, "d": 0.152, "theta_offset_deg": 0.0},
        {"joint": 2, "alpha": 0.0, "a": -0.425, "d": 0.0, "theta_offset_deg": 0.0},
        {"joint": 3, "alpha": 0.0, "a": -0.395, "d": 0.0, "theta_offset_deg": 0.0},
        {"joint": 4, "alpha": np.pi / 2.0, "a": 0.0, "d": 0.102, "theta_offset_deg": 0.0},
        {"joint": 5, "alpha": -np.pi / 2.0, "a": 0.0, "d": 0.102, "theta_offset_deg": 0.0},
        {"joint": 6, "alpha": 0.0, "a": 0.0, "d": 0.100, "theta_offset_deg": 0.0},
    ]
