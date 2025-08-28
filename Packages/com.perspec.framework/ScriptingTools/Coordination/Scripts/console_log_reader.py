#!/usr/bin/env python3
"""
Console Log Reader for Unity Console Output
Retrieves and formats Unity console logs from SQLite database
"""

import sqlite3
import os
from datetime import datetime, timedelta
from pathlib import Path
from typing import List, Dict, Optional, Tuple
from enum import Enum
import json

# Try to import termcolor, but make it optional
try:
    from termcolor import colored
except ImportError:
    # Fallback if termcolor is not installed
    def colored(text, color=None, attrs=None):
        return text

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
    return str(perspec_dir / "test_coordination.db")

class LogLevel(Enum):
    INFO = "Info"
    WARNING = "Warning"
    ERROR = "Error"
    EXCEPTION = "Exception"
    ASSERT = "Assert"

class ConsoleLogReader:
    def __init__(self, db_path: str = None):
        if db_path is None:
            db_path = get_db_path()
        
        self.db_path = db_path
        self._ensure_database_exists()
    
    def _ensure_database_exists(self):
        """Ensure the database exists"""
        if not os.path.exists(self.db_path):
            raise FileNotFoundError(f"Database not found at: {self.db_path}")
    
    def _get_connection(self):
        """Get database connection with WAL mode"""
        conn = sqlite3.connect(str(self.db_path))
        conn.execute("PRAGMA journal_mode=WAL")
        conn.row_factory = sqlite3.Row
        return conn
    
    def get_latest_logs(self, 
                       limit: int = 50,
                       log_level: LogLevel = None,
                       session_id: str = None,
                       minutes_ago: int = None) -> List[Dict]:
        """
        Get latest console logs
        
        Args:
            limit: Maximum number of logs to return
            log_level: Filter by log level
            session_id: Filter by session ID
            minutes_ago: Get logs from last N minutes
        
        Returns:
            List of log entries
        """
        conn = self._get_connection()
        cursor = conn.cursor()
        
        try:
            query = "SELECT * FROM console_logs WHERE 1=1"
            params = []
            
            if session_id:
                query += " AND session_id = ?"
                params.append(session_id)
            
            if log_level:
                query += " AND log_level = ?"
                params.append(log_level.value)
            
            if minutes_ago:
                cutoff = datetime.now() - timedelta(minutes=minutes_ago)
                query += " AND timestamp > ?"
                params.append(cutoff.isoformat())
            
            query += " ORDER BY timestamp DESC LIMIT ?"
            params.append(limit)
            
            cursor.execute(query, params)
            rows = cursor.fetchall()
            
            return [dict(row) for row in rows]
        
        finally:
            conn.close()
    
    def get_error_logs(self, limit: int = 20, include_exceptions: bool = True) -> List[Dict]:
        """Get error and optionally exception logs"""
        conn = self._get_connection()
        cursor = conn.cursor()
        
        try:
            levels = ["'Error'"]
            if include_exceptions:
                levels.extend(["'Exception'", "'Assert'"])
            
            query = f"""
                SELECT * FROM console_logs 
                WHERE log_level IN ({','.join(levels)})
                ORDER BY timestamp DESC 
                LIMIT ?
            """
            
            cursor.execute(query, (limit,))
            rows = cursor.fetchall()
            
            return [dict(row) for row in rows]
        
        finally:
            conn.close()
    
    def get_session_summary(self, session_id: str = None) -> Dict:
        """Get summary statistics for a session or current session"""
        conn = self._get_connection()
        cursor = conn.cursor()
        
        try:
            # If no session specified, get the most recent one
            if not session_id:
                cursor.execute("""
                    SELECT DISTINCT session_id 
                    FROM console_logs 
                    ORDER BY timestamp DESC 
                    LIMIT 1
                """)
                row = cursor.fetchone()
                if row:
                    session_id = row['session_id']
                else:
                    return {"error": "No sessions found"}
            
            # Get summary stats
            cursor.execute("""
                SELECT 
                    COUNT(*) as total_logs,
                    COUNT(CASE WHEN log_level = 'Info' THEN 1 END) as info_count,
                    COUNT(CASE WHEN log_level = 'Warning' THEN 1 END) as warning_count,
                    COUNT(CASE WHEN log_level = 'Error' THEN 1 END) as error_count,
                    COUNT(CASE WHEN log_level = 'Exception' THEN 1 END) as exception_count,
                    COUNT(CASE WHEN log_level = 'Assert' THEN 1 END) as assert_count,
                    MIN(timestamp) as first_log,
                    MAX(timestamp) as last_log
                FROM console_logs
                WHERE session_id = ?
            """, (session_id,))
            
            row = cursor.fetchone()
            
            return {
                "session_id": session_id,
                "total_logs": row['total_logs'],
                "info_count": row['info_count'],
                "warning_count": row['warning_count'],
                "error_count": row['error_count'],
                "exception_count": row['exception_count'],
                "assert_count": row['assert_count'],
                "first_log": row['first_log'],
                "last_log": row['last_log'],
                "duration": self._calculate_duration(row['first_log'], row['last_log'])
            }
        
        finally:
            conn.close()
    
    def _calculate_duration(self, first: str, last: str) -> str:
        """Calculate duration between timestamps"""
        if not first or not last:
            return "N/A"
        
        try:
            start = datetime.fromisoformat(first.replace(' ', 'T'))
            end = datetime.fromisoformat(last.replace(' ', 'T'))
            duration = end - start
            
            hours, remainder = divmod(duration.total_seconds(), 3600)
            minutes, seconds = divmod(remainder, 60)
            
            if hours > 0:
                return f"{int(hours)}h {int(minutes)}m {int(seconds)}s"
            elif minutes > 0:
                return f"{int(minutes)}m {int(seconds)}s"
            else:
                return f"{seconds:.2f}s"
        except:
            return "N/A"
    
    def format_log_entry(self, log: Dict, show_stack: bool = True, colored_output: bool = True) -> str:
        """Format a single log entry for display"""
        lines = []
        
        # Timestamp and level
        timestamp = log.get('timestamp', 'N/A')
        if timestamp != 'N/A':
            try:
                dt = datetime.fromisoformat(timestamp.replace(' ', 'T'))
                timestamp = dt.strftime('%H:%M:%S.%f')[:-3]
            except:
                pass
        
        level = log.get('log_level', 'Unknown')
        message = log.get('message', '')
        
        # Color based on level
        if colored_output:
            level_colors = {
                'Info': 'cyan',
                'Warning': 'yellow',
                'Error': 'red',
                'Exception': 'magenta',
                'Assert': 'red'
            }
            color = level_colors.get(level, 'white')
            
            header = f"[{timestamp}] [{colored(level.upper(), color, attrs=['bold'])}]"
        else:
            header = f"[{timestamp}] [{level.upper()}]"
        
        lines.append(f"{header} {message}")
        
        # Source location
        source_file = log.get('source_file')
        source_line = log.get('source_line')
        if source_file:
            location = f"  at {source_file}"
            if source_line:
                location += f":{source_line}"
            lines.append(location)
        
        # Stack trace
        if show_stack:
            truncated_stack = log.get('truncated_stack')
            if truncated_stack:
                lines.append("  Stack trace:")
                for stack_line in truncated_stack.split('\n'):
                    if stack_line.strip():
                        lines.append(f"    {stack_line}")
        
        # Truncation info
        if log.get('is_truncated'):
            frame_count = log.get('frame_count', 0)
            lines.append(f"  (Total frames: {frame_count}, truncated for clarity)")
        
        return '\n'.join(lines)
    
    def print_logs(self, 
                   logs: List[Dict], 
                   show_stack: bool = True, 
                   colored_output: bool = True):
        """Print formatted logs to console"""
        if not logs:
            print("No logs found.")
            return
        
        for i, log in enumerate(logs):
            if i > 0:
                print("-" * 80)
            print(self.format_log_entry(log, show_stack, colored_output))
    
    def export_logs(self, 
                   output_file: str,
                   session_id: str = None,
                   log_level: LogLevel = None,
                   format: str = 'json') -> int:
        """
        Export logs to file
        
        Args:
            output_file: Output file path
            session_id: Filter by session
            log_level: Filter by level
            format: 'json' or 'text'
        
        Returns:
            Number of logs exported
        """
        logs = self.get_latest_logs(
            limit=10000,  # High limit for export
            log_level=log_level,
            session_id=session_id
        )
        
        if format == 'json':
            with open(output_file, 'w') as f:
                json.dump(logs, f, indent=2, default=str)
        else:  # text format
            with open(output_file, 'w') as f:
                for log in logs:
                    f.write(self.format_log_entry(log, show_stack=True, colored_output=False))
                    f.write('\n' + '=' * 80 + '\n')
        
        print(f"Exported {len(logs)} logs to {output_file}")
        return len(logs)
    
    def clear_old_logs(self, days_old: int = 7) -> int:
        """Clear logs older than specified days"""
        conn = self._get_connection()
        cursor = conn.cursor()
        
        try:
            cutoff = datetime.now() - timedelta(days=days_old)
            
            cursor.execute("""
                SELECT COUNT(*) as count 
                FROM console_logs 
                WHERE timestamp < ?
            """, (cutoff.isoformat(),))
            
            count = cursor.fetchone()['count']
            
            if count > 0:
                cursor.execute("""
                    DELETE FROM console_logs 
                    WHERE timestamp < ?
                """, (cutoff.isoformat(),))
                
                conn.commit()
                print(f"Deleted {count} logs older than {days_old} days")
            else:
                print(f"No logs older than {days_old} days found")
            
            return count
        
        finally:
            conn.close()
    
    def get_sessions(self, limit: int = 10) -> List[Dict]:
        """Get list of recent sessions"""
        conn = self._get_connection()
        cursor = conn.cursor()
        
        try:
            cursor.execute("""
                SELECT 
                    session_id,
                    COUNT(*) as log_count,
                    MIN(timestamp) as start_time,
                    MAX(timestamp) as end_time,
                    COUNT(CASE WHEN log_level = 'Error' THEN 1 END) as error_count,
                    COUNT(CASE WHEN log_level = 'Warning' THEN 1 END) as warning_count
                FROM console_logs
                GROUP BY session_id
                ORDER BY MAX(timestamp) DESC
                LIMIT ?
            """, (limit,))
            
            sessions = []
            for row in cursor.fetchall():
                session = dict(row)
                session['duration'] = self._calculate_duration(
                    session['start_time'], 
                    session['end_time']
                )
                sessions.append(session)
            
            return sessions
        
        finally:
            conn.close()
    
    def monitor_logs(self, 
                    session_id: str = None, 
                    log_level: LogLevel = None,
                    refresh_interval: int = 2):
        """
        Monitor logs in real-time (requires manual refresh)
        
        Args:
            session_id: Filter by session
            log_level: Filter by level
            refresh_interval: Seconds between refreshes
        """
        import time
        import os
        
        last_id = 0
        
        print(f"Monitoring logs... (Ctrl+C to stop)")
        print(f"Filters: session={session_id}, level={log_level}")
        print("-" * 80)
        
        try:
            while True:
                conn = self._get_connection()
                cursor = conn.cursor()
                
                query = "SELECT * FROM console_logs WHERE id > ?"
                params = [last_id]
                
                if session_id:
                    query += " AND session_id = ?"
                    params.append(session_id)
                
                if log_level:
                    query += " AND log_level = ?"
                    params.append(log_level.value)
                
                query += " ORDER BY id ASC"
                
                cursor.execute(query, params)
                new_logs = cursor.fetchall()
                
                for log in new_logs:
                    print(self.format_log_entry(dict(log)))
                    last_id = log['id']
                
                conn.close()
                time.sleep(refresh_interval)
        
        except KeyboardInterrupt:
            print("\nMonitoring stopped.")

def main():
    """Example usage"""
    reader = ConsoleLogReader()
    
    # Get latest error logs
    errors = reader.get_error_logs(limit=5)
    print("\n=== Latest Errors ===")
    reader.print_logs(errors)
    
    # Get session summary
    summary = reader.get_session_summary()
    print("\n=== Session Summary ===")
    print(json.dumps(summary, indent=2))

if __name__ == "__main__":
    main()