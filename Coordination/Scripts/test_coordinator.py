#!/usr/bin/env python3
"""
Unity Test Coordinator - Python Interface
Provides functions to submit test requests and monitor results
"""

import sqlite3
import json
import time
from datetime import datetime
from pathlib import Path
from typing import Optional, Dict, List, Tuple
from enum import Enum

class TestPlatform(Enum):
    EDIT_MODE = "EditMode"
    PLAY_MODE = "PlayMode"
    BOTH = "Both"

class TestRequestType(Enum):
    ALL = "all"
    CLASS = "class"
    METHOD = "method"
    CATEGORY = "category"

class TestStatus(Enum):
    PENDING = "pending"
    RUNNING = "running"
    COMPLETED = "completed"
    FAILED = "failed"
    CANCELLED = "cancelled"

def get_db_path():
    """Get the database path"""
    script_dir = Path(__file__).parent.parent
    return script_dir / "test_coordination.db"

class TestCoordinator:
    def __init__(self):
        self.db_path = get_db_path()
        self._ensure_database_exists()
    
    def _ensure_database_exists(self):
        """Ensure the database exists"""
        if not self.db_path.exists():
            raise FileNotFoundError(f"Database not found at {self.db_path}. Run db_initializer.py first.")
    
    def _get_connection(self) -> sqlite3.Connection:
        """Get a database connection with proper settings"""
        conn = sqlite3.connect(str(self.db_path))
        conn.row_factory = sqlite3.Row  # Enable column access by name
        conn.execute("PRAGMA journal_mode=WAL")
        return conn
    
    def submit_test_request(self, 
                           request_type: TestRequestType,
                           platform: TestPlatform,
                           test_filter: Optional[str] = None,
                           priority: int = 0) -> int:
        """
        Submit a new test request to the queue
        
        Args:
            request_type: Type of test to run (all, class, method, category)
            platform: Test platform (EditMode, PlayMode, Both)
            test_filter: Optional filter string (class name, method name, or category)
            priority: Priority level (higher numbers run first)
            
        Returns:
            Request ID of the submitted test
        """
        conn = self._get_connection()
        try:
            cursor = conn.cursor()
            cursor.execute("""
                INSERT INTO test_requests (request_type, test_filter, test_platform, priority)
                VALUES (?, ?, ?, ?)
            """, (request_type.value, test_filter, platform.value, priority))
            
            request_id = cursor.lastrowid
            
            # Log the submission
            cursor.execute("""
                INSERT INTO execution_log (request_id, log_level, source, message)
                VALUES (?, 'INFO', 'Python', ?)
            """, (request_id, f"Test request submitted: {request_type.value} on {platform.value}"))
            
            conn.commit()
            
            print(f"[SUCCESS] Test request submitted with ID: {request_id}")
            print(f"   Type: {request_type.value}")
            print(f"   Platform: {platform.value}")
            if test_filter:
                print(f"   Filter: {test_filter}")
            
            return request_id
            
        except sqlite3.Error as e:
            print(f"[ERROR] Error submitting test request: {e}")
            conn.rollback()
            raise
        finally:
            conn.close()
    
    def get_request_status(self, request_id: int) -> Optional[Dict]:
        """
        Get the current status of a test request
        
        Args:
            request_id: ID of the test request
            
        Returns:
            Dictionary with request details or None if not found
        """
        conn = self._get_connection()
        try:
            cursor = conn.cursor()
            cursor.execute("""
                SELECT * FROM test_requests WHERE id = ?
            """, (request_id,))
            
            row = cursor.fetchone()
            if row:
                return dict(row)
            return None
            
        finally:
            conn.close()
    
    def wait_for_completion(self, request_id: int, timeout: int = 300, poll_interval: float = 1.0) -> Dict:
        """
        Wait for a test request to complete
        
        Args:
            request_id: ID of the test request
            timeout: Maximum seconds to wait
            poll_interval: Seconds between status checks
            
        Returns:
            Final status dictionary
        """
        start_time = time.time()
        last_status = None
        
        while time.time() - start_time < timeout:
            status = self.get_request_status(request_id)
            
            if not status:
                raise ValueError(f"Request {request_id} not found")
            
            # Print status updates
            if status['status'] != last_status:
                print(f"[STATUS] {status['status']}")
                last_status = status['status']
            
            if status['status'] in ['completed', 'failed', 'cancelled']:
                return status
            
            time.sleep(poll_interval)
        
        raise TimeoutError(f"Request {request_id} did not complete within {timeout} seconds")
    
    def get_test_results(self, request_id: int) -> List[Dict]:
        """
        Get detailed test results for a request
        
        Args:
            request_id: ID of the test request
            
        Returns:
            List of test result dictionaries
        """
        conn = self._get_connection()
        try:
            cursor = conn.cursor()
            cursor.execute("""
                SELECT * FROM test_results 
                WHERE request_id = ?
                ORDER BY test_name
            """, (request_id,))
            
            return [dict(row) for row in cursor.fetchall()]
            
        finally:
            conn.close()
    
    def cancel_request(self, request_id: int) -> bool:
        """
        Cancel a pending or running test request
        
        Args:
            request_id: ID of the test request
            
        Returns:
            True if cancelled successfully
        """
        conn = self._get_connection()
        try:
            cursor = conn.cursor()
            cursor.execute("""
                UPDATE test_requests 
                SET status = 'cancelled', 
                    completed_at = CURRENT_TIMESTAMP,
                    error_message = 'Cancelled by user'
                WHERE id = ? AND status IN ('pending', 'running')
            """, (request_id,))
            
            if cursor.rowcount > 0:
                conn.commit()
                print(f"[CANCELLED] Request {request_id} cancelled")
                return True
            else:
                print(f"[WARNING] Request {request_id} cannot be cancelled (not pending/running)")
                return False
                
        except sqlite3.Error as e:
            print(f"[ERROR] Error cancelling request: {e}")
            conn.rollback()
            return False
        finally:
            conn.close()
    
    def get_pending_requests(self) -> List[Dict]:
        """Get all pending test requests"""
        conn = self._get_connection()
        try:
            cursor = conn.cursor()
            cursor.execute("""
                SELECT * FROM test_requests 
                WHERE status = 'pending'
                ORDER BY priority DESC, created_at ASC
            """)
            
            return [dict(row) for row in cursor.fetchall()]
            
        finally:
            conn.close()
    
    def get_execution_log(self, request_id: Optional[int] = None, limit: int = 100) -> List[Dict]:
        """
        Get execution log entries
        
        Args:
            request_id: Optional request ID to filter by
            limit: Maximum number of entries to return
            
        Returns:
            List of log entry dictionaries
        """
        conn = self._get_connection()
        try:
            cursor = conn.cursor()
            
            if request_id:
                cursor.execute("""
                    SELECT * FROM execution_log 
                    WHERE request_id = ?
                    ORDER BY created_at DESC
                    LIMIT ?
                """, (request_id, limit))
            else:
                cursor.execute("""
                    SELECT * FROM execution_log 
                    ORDER BY created_at DESC
                    LIMIT ?
                """, (limit,))
            
            return [dict(row) for row in cursor.fetchall()]
            
        finally:
            conn.close()
    
    def update_system_heartbeat(self, component: str = "Python"):
        """Update system heartbeat for monitoring"""
        conn = self._get_connection()
        try:
            cursor = conn.cursor()
            cursor.execute("""
                INSERT INTO system_status (component, status, last_heartbeat, message)
                VALUES (?, 'online', CURRENT_TIMESTAMP, 'Active')
                ON CONFLICT(component) DO UPDATE SET
                    status = 'online',
                    last_heartbeat = CURRENT_TIMESTAMP,
                    message = 'Active'
            """)
            conn.commit()
        except sqlite3.Error:
            # Ignore errors for heartbeat
            pass
        finally:
            conn.close()
    
    def print_summary(self, request_id: int):
        """Print a nice summary of test results"""
        status = self.get_request_status(request_id)
        if not status:
            print(f"[ERROR] Request {request_id} not found")
            return
        
        print("\n" + "="*60)
        print(f"Test Request #{request_id} Summary")
        print("="*60)
        print(f"Status: {status['status']}")
        print(f"Platform: {status['test_platform']}")
        print(f"Type: {status['request_type']}")
        
        if status['test_filter']:
            print(f"Filter: {status['test_filter']}")
        
        if status['status'] == 'completed':
            print(f"\nResults:")
            print(f"  Total: {status['total_tests']}")
            print(f"  Passed: {status['passed_tests']}")
            print(f"  Failed: {status['failed_tests']}")
            print(f"  Skipped: {status['skipped_tests']}")
            print(f"  Duration: {status['duration_seconds']:.2f} seconds")
            
            # Show failed tests if any
            if status['failed_tests'] > 0:
                results = self.get_test_results(request_id)
                failed = [r for r in results if r['result'] == 'Failed']
                if failed:
                    print("\nFailed Tests:")
                    for test in failed:
                        print(f"  [FAILED] {test['test_name']}")
                        if test['error_message']:
                            print(f"     {test['error_message']}")
        
        elif status['status'] == 'failed':
            print(f"\n[ERROR] {status['error_message']}")
        
        print("="*60 + "\n")


# Convenience functions for quick operations
def run_all_tests(platform: TestPlatform = TestPlatform.BOTH) -> int:
    """Run all tests on specified platform"""
    coordinator = TestCoordinator()
    return coordinator.submit_test_request(TestRequestType.ALL, platform)

def run_test_class(class_name: str, platform: TestPlatform = TestPlatform.EDIT_MODE) -> int:
    """Run tests for a specific class"""
    coordinator = TestCoordinator()
    return coordinator.submit_test_request(TestRequestType.CLASS, platform, class_name)

def run_test_method(method_name: str, platform: TestPlatform = TestPlatform.EDIT_MODE) -> int:
    """Run a specific test method"""
    coordinator = TestCoordinator()
    return coordinator.submit_test_request(TestRequestType.METHOD, platform, method_name)

def run_test_category(category: str, platform: TestPlatform = TestPlatform.BOTH) -> int:
    """Run tests by category"""
    coordinator = TestCoordinator()
    return coordinator.submit_test_request(TestRequestType.CATEGORY, platform, category)

if __name__ == "__main__":
    # Example usage
    coordinator = TestCoordinator()
    
    # Submit a test request
    request_id = coordinator.submit_test_request(
        TestRequestType.ALL,
        TestPlatform.EDIT_MODE,
        priority=1
    )
    
    # Wait for completion (with timeout)
    try:
        final_status = coordinator.wait_for_completion(request_id, timeout=60)
        coordinator.print_summary(request_id)
    except TimeoutError as e:
        print(f"[TIMEOUT] {e}")
        coordinator.cancel_request(request_id)