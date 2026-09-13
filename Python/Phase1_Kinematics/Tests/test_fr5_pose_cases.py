import json
import os
import sys
import tempfile
import unittest

PYTHON_ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
if PYTHON_ROOT not in sys.path:
    sys.path.insert(0, PYTHON_ROOT)

from Phase1_Kinematics.GroundTruth.fr5_pose_case_runner import default_case_file, run_pose_cases


class Fr5PoseCaseTests(unittest.TestCase):
    def test_official_pose_cases_export_json_csv_and_txt(self):
        with tempfile.TemporaryDirectory() as output_dir:
            results, output_paths = run_pose_cases(default_case_file(), output_dir)

            self.assertEqual([result["caseId"] for result in results], [
                "ZERO", "SDK_HOME_CANDIDATE", "ROS2_DEMO_MOVEJ", "SMALL_SAFE_TEST",
            ])
            for path in output_paths.values():
                self.assertTrue(os.path.isfile(path))

            with open(output_paths["json"], "r", encoding="utf-8") as file:
                exported = json.load(file)
            self.assertEqual(len(exported["results"]), 4)
            self.assertEqual(exported["results"][3]["caseId"], "SMALL_SAFE_TEST")


if __name__ == "__main__":
    unittest.main()
