#!/usr/bin/env python3
"""
Asset Refresh Coordinator - Python interface for Unity asset refresh coordination
"""

import sqlite3
import json
import time
from datetime import datetime
from pathlib import Path
from enum import Enum
from typing import Optional, List, Dict, Any

class RefreshType(Enum):
    FULL = "full"
    SELECTIVE = "selective"

class ImportOptions(Enum):
    DEFAULT = "default"
    SYNCHRONOUS = "synchronous"
    FORCE_UPDATE = "force_update"

class RefreshStatus(Enum):
    PENDING = "pending"
    RUNNING = "running"
    COMPLETED = "completed"
    FAILED = "failed"
    CANCELLED = "cancelled"

class AssetRefreshCoordinator:
    def __init__(self, db_path: Optional[Path] = None):
        """Initialize the asset refresh coordinator"""
        if db_path is None:
            db_path = Path(__file__).parent.parent / "test_coordination.db"
        
        self.db_path = str(db_path)
        self._verify_database()
    
    def _verify_database(self):
        """Verify database exists and has required tables"""
        conn = sqlite3.connect(self.db_path)
        cursor = conn.cursor()
        
        try:
            cursor.execute("""
                SELECT name FROM sqlite_master 
                WHERE type='table' AND name='asset_refresh_requests'
            """)
            if not cursor.fetchone():
                raise RuntimeError("asset_refresh_requests table not found. Run db_initializer.py first.")
        finally:
            conn.close()
    
    def submit_refresh_request(self, 
                              refresh_type: RefreshType = RefreshType.FULL,
                              paths: Optional[List[str]] = None,
                              import_options: ImportOptions = ImportOptions.DEFAULT,
                              priority: int = 0) -> int:
        """Submit an asset refresh request to the database"""
        conn = sqlite3.connect(self.db_path)
        cursor = conn.cursor()
        
        try:
            # Convert paths list to JSON string if provided
            paths_json = json.dumps(paths) if paths else None
            
            cursor.execute("""
                INSERT INTO asset_refresh_requests 
                (refresh_type, paths, import_options, status, priority, created_at)
                VALUES (?, ?, ?, 'pending', ?, ?)
            """, (
                refresh_type.value,
                paths_json,
                import_options.value,
                priority,
                datetime.now().isoformat()
            ))
            
            request_id = cursor.lastrowid
            conn.commit()
            
            print(f"[SUCCESS] Submitted asset refresh request #{request_id}")
            print(f"  Type: {refresh_type.value}")
            if paths:
                print(f"  Paths: {', '.join(paths)}")
            print(f"  Options: {import_options.value}")
            
            return request_id
            
        except sqlite3.Error as e:
            print(f"[ERROR] Failed to submit refresh request: {e}")
            raise
        finally:
            conn.close()
    
    def get_request_status(self, request_id: int) -> Optional[Dict[str, Any]]:
        """Get the status of a specific refresh request"""
        conn = sqlite3.connect(self.db_path)
        conn.row_factory = sqlite3.Row
        cursor = conn.cursor()
        
        try:
            cursor.execute("""
                SELECT * FROM asset_refresh_requests WHERE id = ?
            """, (request_id,))
            
            row = cursor.fetchone()
            if row:
                result = dict(row)
                # Parse paths JSON if present
                if result.get('paths'):
                    result['paths'] = json.loads(result['paths'])
                return result
            return None
            
        finally:
            conn.close()
    
    def wait_for_completion(self, request_id: int, timeout: int = 60) -> str:
        """Wait for a refresh request to complete"""
        start_time = time.time()
        last_status = None
        
        print(f"Waiting for refresh request #{request_id} to complete...")
        
        while time.time() - start_time < timeout:
            status_data = self.get_request_status(request_id)
            
            if not status_data:
                print(f"[ERROR] Request #{request_id} not found")
                return "not_found"
            
            current_status = status_data['status']
            
            if current_status != last_status:
                print(f"  Status: {current_status}")
                last_status = current_status
            
            if current_status in ['completed', 'failed', 'cancelled']:
                return current_status
            
            time.sleep(0.5)
        
        print(f"[WARNING] Timeout waiting for request #{request_id}")
        return "timeout"
    
    def cancel_request(self, request_id: int) -> bool:
        """Cancel a pending or running refresh request"""
        conn = sqlite3.connect(self.db_path)
        cursor = conn.cursor()
        
        try:
            cursor.execute("""
                UPDATE asset_refresh_requests 
                SET status = 'cancelled', completed_at = ?
                WHERE id = ? AND status IN ('pending', 'running')
            """, (datetime.now().isoformat(), request_id))
            
            if cursor.rowcount > 0:
                conn.commit()
                print(f"[SUCCESS] Cancelled refresh request #{request_id}")
                return True
            else:
                print(f"[WARNING] Request #{request_id} not found or already completed")
                return False
                
        except sqlite3.Error as e:
            print(f"[ERROR] Failed to cancel request: {e}")
            return False
        finally:
            conn.close()
    
    def get_pending_requests(self) -> List[Dict[str, Any]]:
        """Get all pending refresh requests"""
        conn = sqlite3.connect(self.db_path)
        conn.row_factory = sqlite3.Row
        cursor = conn.cursor()
        
        try:
            cursor.execute("""
                SELECT * FROM asset_refresh_requests 
                WHERE status = 'pending'
                ORDER BY priority DESC, created_at ASC
            """)
            
            results = []
            for row in cursor.fetchall():
                result = dict(row)
                if result.get('paths'):
                    result['paths'] = json.loads(result['paths'])
                results.append(result)
            
            return results
            
        finally:
            conn.close()
    
    def print_summary(self, request_id: int):
        """Print a summary of the refresh request"""
        status_data = self.get_request_status(request_id)
        
        if not status_data:
            print(f"Request #{request_id} not found")
            return
        
        print(f"\n{'='*60}")
        print(f"Asset Refresh Request #{request_id} Summary")
        print(f"{'='*60}")
        print(f"Type: {status_data['refresh_type']}")
        print(f"Status: {status_data['status']}")
        print(f"Import Options: {status_data['import_options']}")
        
        if status_data.get('paths'):
            print(f"Paths: {', '.join(status_data['paths'])}")
        
        if status_data.get('created_at'):
            print(f"Created: {status_data['created_at']}")
        
        if status_data.get('started_at'):
            print(f"Started: {status_data['started_at']}")
        
        if status_data.get('completed_at'):
            print(f"Completed: {status_data['completed_at']}")
        
        if status_data.get('duration_seconds'):
            print(f"Duration: {status_data['duration_seconds']:.2f} seconds")
        
        if status_data.get('result_message'):
            print(f"Result: {status_data['result_message']}")
        
        if status_data.get('error_message'):
            print(f"Error: {status_data['error_message']}")
        
        print(f"{'='*60}\n")

# Convenience functions
def refresh_all_assets(import_options: ImportOptions = ImportOptions.DEFAULT,
                       wait: bool = False,
                       timeout: int = 60) -> int:
    """Quick function to refresh all assets"""
    coordinator = AssetRefreshCoordinator()
    request_id = coordinator.submit_refresh_request(
        RefreshType.FULL,
        import_options=import_options
    )
    
    if wait:
        final_status = coordinator.wait_for_completion(request_id, timeout)
        coordinator.print_summary(request_id)
    
    return request_id

def refresh_specific_paths(paths: List[str],
                          import_options: ImportOptions = ImportOptions.DEFAULT,
                          wait: bool = False,
                          timeout: int = 60) -> int:
    """Quick function to refresh specific paths"""
    coordinator = AssetRefreshCoordinator()
    request_id = coordinator.submit_refresh_request(
        RefreshType.SELECTIVE,
        paths=paths,
        import_options=import_options
    )
    
    if wait:
        final_status = coordinator.wait_for_completion(request_id, timeout)
        coordinator.print_summary(request_id)
    
    return request_id

if __name__ == "__main__":
    # Example usage
    coordinator = AssetRefreshCoordinator()
    
    # Submit a full refresh
    request_id = coordinator.submit_refresh_request(RefreshType.FULL)
    
    # Wait for completion
    status = coordinator.wait_for_completion(request_id)
    
    # Print summary
    coordinator.print_summary(request_id)