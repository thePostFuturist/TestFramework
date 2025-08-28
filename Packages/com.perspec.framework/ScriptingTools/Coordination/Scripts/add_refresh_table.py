#!/usr/bin/env python3
"""
Add asset_refresh_requests table to existing database
"""

import sqlite3
from pathlib import Path

def get_project_root():
    """Find Unity project root by looking for Assets folder"""
    current = Path.cwd()
    while current != current.parent:
        if (current / "Assets").exists():
            return current
        current = current.parent
    return Path.cwd()

def get_db_path():
    """Get database path in PerSpec folder"""
    project_root = get_project_root()
    perspec_dir = project_root / "PerSpec"
    perspec_dir.mkdir(exist_ok=True)
    return perspec_dir / "test_coordination.db"

def add_refresh_table():
    """Add the asset_refresh_requests table to existing database"""
    db_path = get_db_path()
    
    conn = sqlite3.connect(str(db_path))
    conn.execute("PRAGMA journal_mode=WAL")
    cursor = conn.cursor()
    
    try:
        # Check if table already exists
        cursor.execute("""
            SELECT name FROM sqlite_master 
            WHERE type='table' AND name='asset_refresh_requests'
        """)
        
        if cursor.fetchone():
            print("asset_refresh_requests table already exists")
            return True
        
        # Create asset_refresh_requests table
        cursor.execute("""
            CREATE TABLE IF NOT EXISTS asset_refresh_requests (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                refresh_type TEXT NOT NULL DEFAULT 'full' CHECK(refresh_type IN ('full', 'selective')),
                paths TEXT,
                import_options TEXT DEFAULT 'default' CHECK(import_options IN ('default', 'synchronous', 'force_update')),
                status TEXT NOT NULL DEFAULT 'pending' CHECK(status IN ('pending', 'running', 'completed', 'failed', 'cancelled')),
                priority INTEGER DEFAULT 0,
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                started_at TIMESTAMP,
                completed_at TIMESTAMP,
                duration_seconds REAL DEFAULT 0.0,
                result_message TEXT,
                error_message TEXT
            )
        """)
        
        # Create indexes
        cursor.execute("CREATE INDEX IF NOT EXISTS idx_refresh_status ON asset_refresh_requests(status)")
        cursor.execute("CREATE INDEX IF NOT EXISTS idx_refresh_created ON asset_refresh_requests(created_at DESC)")
        
        conn.commit()
        print("Successfully added asset_refresh_requests table")
        return True
        
    except sqlite3.Error as e:
        print(f"Error adding table: {e}")
        conn.rollback()
        return False
    finally:
        conn.close()

if __name__ == "__main__":
    add_refresh_table()