import os
import sys
import tempfile
import unittest

PYTHON_ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
if PYTHON_ROOT not in sys.path:
    sys.path.insert(0, PYTHON_ROOT)

from Phase1_Kinematics.GroundTruth.groundtruth_single_runner import read_current_joint_file


class GroundtruthSingleRunnerInputTests(unittest.TestCase):
    def read_values(self, content):
        with tempfile.NamedTemporaryFile("w", encoding="utf-8", delete=False) as file:
            file.write(content)
            file_path = file.name
        try:
            return read_current_joint_file(file_path)
        finally:
            os.unlink(file_path)

    def test_six_line_input(self):
        self.assertEqual(self.read_values("0\n1\n2\n3\n4\n5\n"), [0.0, 1.0, 2.0, 3.0, 4.0, 5.0])

    def test_comma_separated_input(self):
        self.assertEqual(self.read_values("0,1,2,3,4,5\n"), [0.0, 1.0, 2.0, 3.0, 4.0, 5.0])

    def test_spaced_csv_and_blank_lines(self):
        self.assertEqual(self.read_values("\n 0, 1, 2, 3, 4, 5 \n\n"), [0.0, 1.0, 2.0, 3.0, 4.0, 5.0])

    def test_invalid_joint_count(self):
        with self.assertRaisesRegex(ValueError, "exactly 6 joint values"):
            self.read_values("0, 1, 2\n")


if __name__ == "__main__":
    unittest.main()
