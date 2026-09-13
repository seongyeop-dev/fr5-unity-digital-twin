import math
import os
import sys
import unittest

import numpy as np

PYTHON_ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
if PYTHON_ROOT not in sys.path:
    sys.path.insert(0, PYTHON_ROOT)

from Phase1_Kinematics.Python_MDH.fr5_fk_solver import calculate_mdh_fk
from Phase1_Kinematics.Python_MDH.fr5_mdh_params import MDH_CONVENTION


class Fr5MdhFkTests(unittest.TestCase):
    def test_zero_pose_has_canonical_fields_and_expected_transform(self):
        result = calculate_mdh_fk([0, 0, 0, 0, 0, 0], case_id="ZERO", case_name="ZERO")

        for key in (
            "caseId", "caseName", "jointDegrees", "jointRadians", "T_base_tcp",
            "tcpPositionMeters", "tcpRotationMatrix", "tcpRotationRpyDegrees",
            "mdhConvention", "units",
        ):
            self.assertIn(key, result)

        self.assertEqual(result["mdhConvention"], MDH_CONVENTION)
        self.assertEqual(result["caseId"], "ZERO")
        self.assertTrue(np.allclose(result["jointRadians"], np.zeros(6)))
        self.assertEqual(result["T_base_tcp"].shape, (4, 4))
        self.assertTrue(np.all(np.isfinite(result["T_base_tcp"])))

        # Independent zero-pose position implied by the official MDH order.
        self.assertTrue(np.allclose(result["tcpPositionMeters"], [-0.820, -0.354, -0.102], atol=1e-12))

    def test_degree_input_converts_to_radians(self):
        result = calculate_mdh_fk([180, -90, 90, -180, 45, 30])
        self.assertTrue(np.allclose(
            result["jointRadians"],
            [math.pi, -math.pi / 2.0, math.pi / 2.0, -math.pi, math.pi / 4.0, math.pi / 6.0],
        ))


if __name__ == "__main__":
    unittest.main()
