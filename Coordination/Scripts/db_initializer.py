#!/usr/bin/env python3
"""
SQLite Database Initializer for Unity Test Coordination
Creates and manages the test coordination database schema
"""

import sqlite3
import os
import sys
from datetime import datetime
from pathlib import Path

def get_db_path():
    """Get the database path relative to this script"""
    script_dir = Path(__file__).parent.parent
    return script_dir / "test_coordination.db"

def create_database():
    """Create the test coordination database with all required tables"""
    db_path = get_db_path()
    
    # Create connection
    conn = sqlite3.connect(str(db_path))
    conn.execute("PRAGMA journal_mode=WAL")  # Enable Write-Ahead Logging for better concurrency
    cursor = conn.cursor()
    
    try:
        # Create test_requests table
        cursor.execute("""
            CREATE TABLE IF NOT EXISTS test_requests (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                request_type TEXT NOT NULL CHECK(request_type IN ('all', 'class', 'method', 'category')),
                test_filter TEXT,
                test_platform TEXT NOT NULL CHECK(test_platform IN ('EditMode', 'PlayMode', 'Both')),
                status TEXT NOT NULL DEFAULT 'pending' CHECK(status IN ('pending', 'running', 'completed', 'failed', 'cancelled')),
                priority INTEGER DEFAULT 0,
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                started_at TIMESTAMP,
                completed_at TIMESTAMP,
                result_summary TEXT,
                error_message TEXT,
                total_tests INTEGER DEFAULT 0,
                passed_tests INTEGER DEFAULT 0,
                failed_tests INTEGER DEFAULT 0,
                skipped_tests INTEGER DEFAULT 0,
                duration_seconds REAL DEFAULT 0.0
            )
        """)
        
        # Create test_results table for detailed test results
        cursor.execute("""
            CREATE TABLE IF NOT EXISTS test_results (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                request_id INTEGER NOT NULL,
                test_name TEXT NOT NULL,
                test_class TEXT,
                test_method TEXT,
                result TEXT NOT NULL CHECK(result IN ('Passed', 'Failed', 'Skipped', 'Inconclusive')),
                duration_ms REAL DEFAULT 0.0,
                error_message TEXT,
                stack_trace TEXT,
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (request_id) REFERENCES test_requests(id) ON DELETE CASCADE
            )
        """)
        
        # Create system_status table for monitoring
        cursor.execute("""
            CREATE TABLE IF NOT EXISTS system_status (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                component TEXT NOT NULL CHECK(component IN ('Unity', 'Python', 'Database')),
                status TEXT NOT NULL CHECK(status IN ('online', 'offline', 'error')),
                last_heartbeat TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                message TEXT,
                metadata TEXT
            )
        """)
        
        # Create execution_log table for debugging
        cursor.execute("""
            CREATE TABLE IF NOT EXISTS execution_log (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                request_id INTEGER,
                log_level TEXT NOT NULL CHECK(log_level IN ('DEBUG', 'INFO', 'WARNING', 'ERROR')),
                message TEXT NOT NULL,
                source TEXT,
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (request_id) REFERENCES test_requests(id) ON DELETE CASCADE
            )
        """)
        
        # Create asset_refresh_requests table for asset refresh coordination
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
        
        # Create console_logs table for Unity console output capture
        cursor.execute("""
            CREATE TABLE IF NOT EXISTS console_logs (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                session_id TEXT NOT NULL,
                log_level TEXT NOT NULL CHECK(log_level IN ('Info', 'Warning', 'Error', 'Exception', 'Assert')),
                message TEXT NOT NULL,
                stack_trace TEXT,
                truncated_stack TEXT,
                source_file TEXT,
                source_line INTEGER,
                timestamp TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                frame_count INTEGER,
                is_truncated BOOLEAN DEFAULT 0,
                context TEXT,
                request_id INTEGER,
                FOREIGN KEY (request_id) REFERENCES test_requests(id)
            )
        """)
        
        # Create indexes for better query performance
        cursor.execute("CREATE INDEX IF NOT EXISTS idx_requests_status ON test_requests(status)")
        cursor.execute("CREATE INDEX IF NOT EXISTS idx_requests_created ON test_requests(created_at DESC)")
        cursor.execute("CREATE INDEX IF NOT EXISTS idx_results_request ON test_results(request_id)")
        cursor.execute("CREATE INDEX IF NOT EXISTS idx_log_request ON execution_log(request_id)")
        cursor.execute("CREATE INDEX IF NOT EXISTS idx_status_component ON system_status(component)")
        cursor.execute("CREATE INDEX IF NOT EXISTS idx_refresh_status ON asset_refresh_requests(status)")
        cursor.execute("CREATE INDEX IF NOT EXISTS idx_refresh_created ON asset_refresh_requests(created_at DESC)")
        
        # Console log indexes
        cursor.execute("CREATE INDEX IF NOT EXISTS idx_console_logs_session ON console_logs(session_id, timestamp DESC)")
        cursor.execute("CREATE INDEX IF NOT EXISTS idx_console_logs_level ON console_logs(log_level, timestamp DESC)")
        cursor.execute("CREATE INDEX IF NOT EXISTS idx_console_logs_request ON console_logs(request_id, timestamp DESC)")
        
        # Insert initial system status
        cursor.execute("""
            INSERT INTO system_status (component, status, message)
            VALUES ('Database', 'online', 'Database initialized successfully')
        """)
        
        conn.commit()
        print(f"Database created successfully at: {db_path}")
        print("Tables created:")
        print("  - test_requests")
        print("  - test_results")
        print("  - system_status")
        print("  - execution_log")
        print("  - asset_refresh_requests")
        print("  - console_logs")
        
    except sqlite3.Error as e:
        print(f"Error creating database: {e}")
        conn.rollback()
        return False
    finally:
        conn.close()
    
    return True

def verify_database():
    """Verify the database structure is correct"""
    db_path = get_db_path()
    
    if not db_path.exists():
        print(f"Database does not exist at: {db_path}")
        return False
    
    conn = sqlite3.connect(str(db_path))
    cursor = conn.cursor()
    
    try:
        # Check tables exist
        cursor.execute("""
            SELECT name FROM sqlite_master 
            WHERE type='table' 
            ORDER BY name
        """)
        tables = cursor.fetchall()
        
        expected_tables = {'execution_log', 'system_status', 'test_requests', 'test_results', 'asset_refresh_requests', 'console_logs'}
        actual_tables = {table[0] for table in tables}
        
        if expected_tables.issubset(actual_tables):
            print("Database verification successful!")
            print(f"Found tables: {', '.join(sorted(actual_tables))}")
            return True
        else:
            missing = expected_tables - actual_tables
            print(f"Missing tables: {', '.join(missing)}")
            return False
            
    except sqlite3.Error as e:
        print(f"Error verifying database: {e}")
        return False
    finally:
        conn.close()

def reset_database():
    """Reset the database (drop all tables and recreate)"""
    db_path = get_db_path()
    
    if db_path.exists():
        print(f"Removing existing database at: {db_path}")
        os.remove(db_path)
    
    return create_database()

if __name__ == "__main__":
    if len(sys.argv) > 1 and sys.argv[1] == "reset":
        print("Resetting database...")
        reset_database()
    elif len(sys.argv) > 1 and sys.argv[1] == "verify":
        verify_database()
    else:
        if get_db_path().exists():
            print("Database already exists. Use 'reset' argument to recreate.")
            verify_database()
        else:
            create_database()