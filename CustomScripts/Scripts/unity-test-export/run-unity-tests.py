#!/usr/bin/env python3
"""
=============================================================================
Unity Test Runner with XML Export - Python Cross-Platform Version
=============================================================================
This script runs Unity tests and exports results to XML format
Works on Windows, macOS, and Linux

Usage: python run-unity-tests.py [options]
Examples:
    python run-unity-tests.py
    python run-unity-tests.py --platform PlayMode
    python run-unity-tests.py --filter "TestNamespace.TestClass"
    python run-unity-tests.py --categories "Integration,Critical"
=============================================================================
"""

import os
import sys
import subprocess
import platform
import argparse
import xml.etree.ElementTree as ET
from datetime import datetime
from pathlib import Path
import json
import time

class UnityTestRunner:
    """Unity Test Runner with XML Export"""
    
    # Default Unity paths for different platforms
    UNITY_PATHS = {
        'Windows': [
            r'C:\Program Files\Unity\Hub\Editor\{version}\Editor\Unity.exe',
            r'C:\Program Files\Unity\{version}\Editor\Unity.exe',
        ],
        'Darwin': [  # macOS
            '/Applications/Unity/Hub/Editor/{version}/Unity.app/Contents/MacOS/Unity',
            '/Applications/Unity {version}/Unity.app/Contents/MacOS/Unity',
        ],
        'Linux': [
            '/opt/Unity/Hub/Editor/{version}/Editor/Unity',
            '/opt/Unity-{version}/Editor/Unity',
        ]
    }
    
    def __init__(self, project_path=None, unity_version='6000.0.47f1'):
        """Initialize the test runner"""
        self.system = platform.system()
        self.project_path = project_path or os.path.abspath('.')
        self.unity_version = unity_version
        self.unity_path = None
        self.timestamp = datetime.now().strftime('%Y%m%d_%H%M%S')
        
        # Setup paths
        self.test_results_dir = Path(self.project_path) / 'TestResults'
        self.test_results_dir.mkdir(exist_ok=True)
        
    def find_unity(self):
        """Find Unity installation"""
        # First check if Unity is in PATH
        unity_cmd = 'Unity.exe' if self.system == 'Windows' else 'Unity'
        if self._check_command_exists(unity_cmd):
            self.unity_path = unity_cmd
            return True
        
        # Check common installation paths
        paths_to_check = self.UNITY_PATHS.get(self.system, [])
        
        # Try exact version first
        for path_template in paths_to_check:
            path = path_template.format(version=self.unity_version)
            if os.path.exists(path):
                self.unity_path = path
                return True
        
        # Try to find any Unity installation
        if self.system == 'Windows':
            unity_hub = r'C:\Program Files\Unity\Hub\Editor'
            if os.path.exists(unity_hub):
                versions = [d for d in os.listdir(unity_hub) if os.path.isdir(os.path.join(unity_hub, d))]
                if versions:
                    # Prefer 6000.x versions
                    unity_6000 = [v for v in versions if v.startswith('6000.')]
                    if unity_6000:
                        self.unity_version = unity_6000[0]
                    else:
                        self.unity_version = versions[0]
                    
                    self.unity_path = os.path.join(unity_hub, self.unity_version, 'Editor', 'Unity.exe')
                    if os.path.exists(self.unity_path):
                        print(f"[INFO] Using Unity version: {self.unity_version}")
                        return True
        
        return False
    
    def _check_command_exists(self, command):
        """Check if a command exists in PATH"""
        try:
            subprocess.run([command, '-version'], 
                         stdout=subprocess.DEVNULL, 
                         stderr=subprocess.DEVNULL)
            return True
        except (subprocess.CalledProcessError, FileNotFoundError):
            return False
    
    def run_tests(self, test_platform='EditMode', test_filter=None, 
                  test_categories=None, verbose=False):
        """Run Unity tests with specified parameters"""
        
        if not self.find_unity():
            print(f"[ERROR] Unity not found. Please install Unity {self.unity_version}")
            return 1
        
        # Setup file paths
        xml_file = self.test_results_dir / f'TestResults_{test_platform}_{self.timestamp}.xml'
        log_file = self.test_results_dir / f'unity_{test_platform}_{self.timestamp}.log'
        summary_file = self.test_results_dir / f'TestResults_{test_platform}_{self.timestamp}.summary.txt'
        
        # Build command
        cmd = [
            self.unity_path,
            '-batchmode',
            '-quit',
            '-projectPath', self.project_path,
            '-runTests',
            '-testPlatform', test_platform,
            '-testResultFile', str(xml_file),
            '-logFile', str(log_file)
        ]
        
        # Add optional parameters
        if test_filter:
            cmd.extend(['-testFilter', test_filter])
        if test_categories:
            cmd.extend(['-testCategories', test_categories])
        
        # Display configuration
        self._print_header()
        print(f"Unity Version:  {self.unity_version}")
        print(f"Project Path:   {self.project_path}")
        print(f"Test Platform:  {test_platform}")
        if test_filter:
            print(f"Test Filter:    {test_filter}")
        if test_categories:
            print(f"Test Categories: {test_categories}")
        print(f"\nOutput Files:")
        print(f"  XML Results:  {xml_file}")
        print(f"  Unity Log:    {log_file}")
        print(f"  Summary:      {summary_file}")
        self._print_line()
        
        # Run tests
        print(f"\n[{datetime.now().strftime('%H:%M:%S')}] Starting Unity tests...")
        if verbose:
            print(f"Command: {' '.join(cmd)}")
        
        start_time = time.time()
        
        try:
            # Run Unity with real-time output
            process = subprocess.Popen(
                cmd,
                stdout=subprocess.PIPE,
                stderr=subprocess.STDOUT,
                universal_newlines=True,
                bufsize=1
            )
            
            # Stream output if verbose
            if verbose:
                for line in process.stdout:
                    print(f"  {line}", end='')
            
            # Wait for completion
            return_code = process.wait()
            duration = time.time() - start_time
            
            print(f"\n[{datetime.now().strftime('%H:%M:%S')}] Tests completed in {duration:.2f} seconds")
            self._print_line()
            
            # Process results
            if return_code == 0:
                print("[SUCCESS] All tests passed!")
            elif return_code == 1:
                print("[FAILURE] Some tests failed!")
            elif return_code == 2:
                print("[ERROR] Compilation errors detected!")
            else:
                print(f"[ERROR] Unknown error code: {return_code}")
            
            # Parse and display XML results
            if xml_file.exists():
                self._parse_xml_results(xml_file)
            
            # Generate summary
            self._generate_summary(summary_file, test_platform, test_filter, 
                                 test_categories, duration, xml_file, return_code)
            
            return return_code
            
        except subprocess.CalledProcessError as e:
            print(f"[ERROR] Unity command failed: {e}")
            return e.returncode
        except Exception as e:
            print(f"[ERROR] Unexpected error: {e}")
            return 1
    
    def _parse_xml_results(self, xml_file):
        """Parse and display XML test results"""
        try:
            tree = ET.parse(xml_file)
            root = tree.getroot()
            
            # Get test run attributes
            total = root.get('testcasecount', '0')
            passed = root.get('passed', '0')
            failed = root.get('failed', '0')
            skipped = root.get('skipped', '0')
            duration = root.get('duration', '0')
            
            print("\nTest Results Summary:")
            print(f"  Total Tests:  {total}")
            print(f"  Passed:       {passed} {'✓' if int(passed) > 0 else ''}")
            print(f"  Failed:       {failed} {'✗' if int(failed) > 0 else ''}")
            print(f"  Skipped:      {skipped}")
            print(f"  Duration:     {float(duration):.3f}s")
            
            # Show failed tests
            if int(failed) > 0:
                print("\nFailed Tests:")
                for test_case in root.findall(".//test-case[@result='Failed']"):
                    name = test_case.get('fullname', test_case.get('name', 'Unknown'))
                    print(f"  ✗ {name}")
                    
                    # Show failure message if available
                    failure = test_case.find('failure')
                    if failure is not None:
                        message = failure.find('message')
                        if message is not None and message.text:
                            print(f"    {message.text.strip()[:100]}...")
            
        except ET.ParseError as e:
            print(f"[WARNING] Could not parse XML results: {e}")
        except Exception as e:
            print(f"[WARNING] Error reading XML results: {e}")
    
    def _generate_summary(self, summary_file, test_platform, test_filter, 
                         test_categories, duration, xml_file, return_code):
        """Generate a summary file"""
        try:
            with open(summary_file, 'w') as f:
                f.write("Unity Test Results Summary\n")
                f.write("=" * 50 + "\n")
                f.write(f"Generated: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}\n\n")
                
                f.write("Configuration:\n")
                f.write(f"  Platform: {test_platform}\n")
                f.write(f"  Unity Version: {self.unity_version}\n")
                f.write(f"  Project: {self.project_path}\n")
                if test_filter:
                    f.write(f"  Filter: {test_filter}\n")
                if test_categories:
                    f.write(f"  Categories: {test_categories}\n")
                f.write(f"  Duration: {duration:.2f} seconds\n\n")
                
                f.write("Results:\n")
                f.write(f"  Exit Code: {return_code}\n")
                f.write(f"  Status: {'SUCCESS' if return_code == 0 else 'FAILURE'}\n\n")
                
                f.write("Output Files:\n")
                f.write(f"  XML: {xml_file}\n")
                
            print(f"\nSummary saved to: {summary_file}")
            
        except Exception as e:
            print(f"[WARNING] Could not create summary: {e}")
    
    def _print_header(self):
        """Print header"""
        print("=" * 70)
        print("Unity Test Runner with XML Export (Python)")
        print("=" * 70)
    
    def _print_line(self):
        """Print separator line"""
        print("-" * 70)


def main():
    """Main entry point"""
    parser = argparse.ArgumentParser(
        description='Run Unity tests and export results to XML',
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Examples:
  python run-unity-tests.py
  python run-unity-tests.py --platform PlayMode
  python run-unity-tests.py --filter "MyNamespace.MyTestClass"
  python run-unity-tests.py --categories "Unit,Integration"
  python run-unity-tests.py --unity-version 2022.3.18f1
        """
    )
    
    parser.add_argument(
        '--platform',
        choices=['EditMode', 'PlayMode'],
        default='EditMode',
        help='Test platform (default: EditMode)'
    )
    
    parser.add_argument(
        '--filter',
        help='Filter tests by name'
    )
    
    parser.add_argument(
        '--categories',
        help='Comma-separated test categories'
    )
    
    parser.add_argument(
        '--project',
        default=r'D:\Dev\TestFramework',
        help='Path to Unity project'
    )
    
    parser.add_argument(
        '--unity-version',
        default='6000.0.23f1',
        help='Unity version to use'
    )
    
    parser.add_argument(
        '--verbose', '-v',
        action='store_true',
        help='Enable verbose output'
    )
    
    args = parser.parse_args()
    
    # Create and run test runner
    runner = UnityTestRunner(
        project_path=args.project,
        unity_version=args.unity_version
    )
    
    exit_code = runner.run_tests(
        test_platform=args.platform,
        test_filter=args.filter,
        test_categories=args.categories,
        verbose=args.verbose
    )
    
    sys.exit(exit_code)


if __name__ == '__main__':
    main()