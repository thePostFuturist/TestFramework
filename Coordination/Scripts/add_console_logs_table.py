#!/usr/bin/env python3
"""
Add console_logs table to existing database
"""

import sqlite3
from pathlib import Path

def add_console_logs_table():
    """Add console_logs table to existing database"""
    script_dir = Path(__file__).parent.parent
    db_path = script_dir / "test_coordination.db"
    
    if not db_path.exists():
        print(f"Database not found at: {db_path}")
        return False
    
    conn = sqlite3.connect(str(db_path))
    conn.execute("PRAGMA journal_mode=WAL")
    cursor = conn.cursor()
    
    try:
        # Check if table already exists
        cursor.execute("""
            SELECT name FROM sqlite_master 
            WHERE type='table' AND name='console_logs'
        """)
        
        if cursor.fetchone():
            print("console_logs table already exists")
            return True
        
        # Create console_logs table
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
        
        # Create indexes
        cursor.execute("CREATE INDEX IF NOT EXISTS idx_console_logs_session ON console_logs(session_id, timestamp DESC)")
        cursor.execute("CREATE INDEX IF NOT EXISTS idx_console_logs_level ON console_logs(log_level, timestamp DESC)")
        cursor.execute("CREATE INDEX IF NOT EXISTS idx_console_logs_request ON console_logs(request_id, timestamp DESC)")
        
        conn.commit()
        print("Successfully added console_logs table")
        
        # Verify table was created
        cursor.execute("""
            SELECT name FROM sqlite_master 
            WHERE type='table' AND name='console_logs'
        """)
        
        if cursor.fetchone():
            print("Table verified successfully")
            
            # Show table schema
            cursor.execute("PRAGMA table_info(console_logs)")
            columns = cursor.fetchall()
            print("\nTable schema:")
            for col in columns:
                print(f"  {col[1]} {col[2]}")
        
        return True
        
    except sqlite3.Error as e:
        print(f"Error adding console_logs table: {e}")
        conn.rollback()
        return False
    
    finally:
        conn.close()

if __name__ == "__main__":
    if add_console_logs_table():
        print("\n✅ Migration completed successfully")
    else:
        print("\n❌ Migration failed")