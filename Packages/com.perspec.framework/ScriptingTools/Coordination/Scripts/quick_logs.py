#!/usr/bin/env python3
"""
Quick Console Logs CLI Tool
Fast access to Unity console logs from command line
"""

import argparse
import sys
import json
from pathlib import Path
from datetime import datetime
from console_log_reader import ConsoleLogReader, LogLevel

def cmd_latest(args):
    """Show latest logs"""
    reader = ConsoleLogReader()
    
    log_level = None
    if args.level:
        try:
            log_level = LogLevel[args.level.upper()]
        except KeyError:
            print(f"Invalid log level: {args.level}")
            print(f"Valid levels: {', '.join([l.name for l in LogLevel])}")
            return 1
    
    logs = reader.get_latest_logs(
        limit=args.count,
        log_level=log_level,
        minutes_ago=args.minutes
    )
    
    if args.json:
        print(json.dumps(logs, indent=2, default=str))
    else:
        reader.print_logs(logs, show_stack=not args.no_stack)
    
    return 0

def cmd_errors(args):
    """Show error logs"""
    reader = ConsoleLogReader()
    
    errors = reader.get_error_logs(
        limit=args.count,
        include_exceptions=not args.errors_only
    )
    
    if args.json:
        print(json.dumps(errors, indent=2, default=str))
    else:
        reader.print_logs(errors, show_stack=not args.no_stack)
    
    # Return non-zero if errors found (useful for CI)
    return 1 if errors else 0

def cmd_warnings(args):
    """Show warning logs"""
    reader = ConsoleLogReader()
    
    warnings = reader.get_latest_logs(
        limit=args.count,
        log_level=LogLevel.WARNING
    )
    
    if args.json:
        print(json.dumps(warnings, indent=2, default=str))
    else:
        reader.print_logs(warnings, show_stack=not args.no_stack)
    
    return 0

def cmd_summary(args):
    """Show session summary"""
    reader = ConsoleLogReader()
    
    summary = reader.get_session_summary(args.session)
    
    if args.json:
        print(json.dumps(summary, indent=2, default=str))
    else:
        print(f"\n{'='*60}")
        print(f"Session Summary: {summary.get('session_id', 'Current')[:8]}...")
        print(f"{'='*60}")
        print(f"Duration: {summary.get('duration', 'N/A')}")
        print(f"Total Logs: {summary.get('total_logs', 0)}")
        print(f"  - Info: {summary.get('info_count', 0)}")
        print(f"  - Warnings: {summary.get('warning_count', 0)}")
        print(f"  - Errors: {summary.get('error_count', 0)}")
        print(f"  - Exceptions: {summary.get('exception_count', 0)}")
        print(f"  - Asserts: {summary.get('assert_count', 0)}")
        
        # Show health status
        error_count = summary.get('error_count', 0) + summary.get('exception_count', 0)
        if error_count > 0:
            print(f"\n[WARNING] {error_count} errors/exceptions detected!")
        else:
            print(f"\n[OK] No errors detected")
    
    return 0

def cmd_sessions(args):
    """List recent sessions"""
    reader = ConsoleLogReader()
    
    sessions = reader.get_sessions(limit=args.count)
    
    if args.json:
        print(json.dumps(sessions, indent=2, default=str))
    else:
        print(f"\n{'='*80}")
        print(f"{'Session ID':<40} {'Logs':<8} {'Errors':<8} {'Duration':<12} {'Start Time'}")
        print(f"{'='*80}")
        
        for session in sessions:
            session_id = session['session_id'][:36] + "..."
            log_count = session['log_count']
            error_count = session['error_count']
            duration = session['duration']
            start = session['start_time']
            
            # Format start time
            try:
                dt = datetime.fromisoformat(start.replace(' ', 'T'))
                start = dt.strftime('%Y-%m-%d %H:%M:%S')
            except:
                pass
            
            # Status indicator based on errors
            if error_count > 0:
                status = "[ERR]"
            elif session['warning_count'] > 0:
                status = "[WRN]"
            else:
                status = "[OK] "
            
            print(f"{status} {session_id:<37} {log_count:<8} {error_count:<8} {duration:<12} {start}")
    
    return 0

def cmd_clear(args):
    """Clear old logs"""
    reader = ConsoleLogReader()
    
    if args.session:
        # Clear specific session - would need to implement this
        print(f"Clearing session: {args.session}")
        # TODO: Implement clear_session method
    else:
        # Clear old logs
        count = reader.clear_old_logs(days_old=args.days)
        print(f"Cleared {count} logs older than {args.days} days")
    
    return 0

def cmd_export(args):
    """Export logs to file"""
    reader = ConsoleLogReader()
    
    log_level = None
    if args.level:
        try:
            log_level = LogLevel[args.level.upper()]
        except KeyError:
            print(f"Invalid log level: {args.level}")
            return 1
    
    format = 'json' if args.output.endswith('.json') else 'text'
    
    count = reader.export_logs(
        output_file=args.output,
        session_id=args.session,
        log_level=log_level,
        format=format
    )
    
    print(f"Exported {count} logs to {args.output}")
    return 0

def cmd_monitor(args):
    """Monitor logs in real-time"""
    reader = ConsoleLogReader()
    
    log_level = None
    if args.level:
        try:
            log_level = LogLevel[args.level.upper()]
        except KeyError:
            print(f"Invalid log level: {args.level}")
            return 1
    
    try:
        reader.monitor_logs(
            session_id=args.session,
            log_level=log_level,
            refresh_interval=args.interval
        )
    except KeyboardInterrupt:
        return 0
    
    return 0

def main():
    parser = argparse.ArgumentParser(description='Unity Console Logs CLI Tool')
    
    # Global options
    parser.add_argument('--json', action='store_true', 
                       help='Output in JSON format')
    parser.add_argument('--no-stack', action='store_true',
                       help='Hide stack traces')
    
    subparsers = parser.add_subparsers(dest='command', help='Available commands')
    
    # Latest logs command
    latest_parser = subparsers.add_parser('latest', help='Show latest logs')
    latest_parser.add_argument('-n', '--count', type=int, default=20,
                              help='Number of logs to show (default: 20)')
    latest_parser.add_argument('-l', '--level', type=str,
                              help='Filter by log level (info, warning, error, exception, assert)')
    latest_parser.add_argument('-m', '--minutes', type=int,
                              help='Show logs from last N minutes')
    latest_parser.set_defaults(func=cmd_latest)
    
    # Error logs command
    errors_parser = subparsers.add_parser('errors', help='Show error logs')
    errors_parser.add_argument('-n', '--count', type=int, default=10,
                              help='Number of errors to show (default: 10)')
    errors_parser.add_argument('--errors-only', action='store_true',
                              help='Exclude exceptions and asserts')
    errors_parser.set_defaults(func=cmd_errors)
    
    # Warning logs command
    warnings_parser = subparsers.add_parser('warnings', help='Show warning logs')
    warnings_parser.add_argument('-n', '--count', type=int, default=10,
                              help='Number of warnings to show (default: 10)')
    warnings_parser.set_defaults(func=cmd_warnings)
    
    # Summary command
    summary_parser = subparsers.add_parser('summary', help='Show session summary')
    summary_parser.add_argument('-s', '--session', type=str,
                               help='Session ID (default: current)')
    summary_parser.set_defaults(func=cmd_summary)
    
    # Sessions command
    sessions_parser = subparsers.add_parser('sessions', help='List recent sessions')
    sessions_parser.add_argument('-n', '--count', type=int, default=10,
                                help='Number of sessions to show (default: 10)')
    sessions_parser.set_defaults(func=cmd_sessions)
    
    # Clear command
    clear_parser = subparsers.add_parser('clear', help='Clear old logs')
    clear_parser.add_argument('-d', '--days', type=int, default=7,
                             help='Clear logs older than N days (default: 7)')
    clear_parser.add_argument('-s', '--session', type=str,
                             help='Clear specific session')
    clear_parser.set_defaults(func=cmd_clear)
    
    # Export command
    export_parser = subparsers.add_parser('export', help='Export logs to file')
    export_parser.add_argument('output', help='Output file path (.json or .txt)')
    export_parser.add_argument('-s', '--session', type=str,
                              help='Filter by session ID')
    export_parser.add_argument('-l', '--level', type=str,
                              help='Filter by log level')
    export_parser.set_defaults(func=cmd_export)
    
    # Monitor command
    monitor_parser = subparsers.add_parser('monitor', help='Monitor logs in real-time')
    monitor_parser.add_argument('-s', '--session', type=str,
                               help='Filter by session ID')
    monitor_parser.add_argument('-l', '--level', type=str,
                               help='Filter by log level')
    monitor_parser.add_argument('-i', '--interval', type=int, default=2,
                               help='Refresh interval in seconds (default: 2)')
    monitor_parser.set_defaults(func=cmd_monitor)
    
    # Parse arguments
    args = parser.parse_args()
    
    # Execute command
    if hasattr(args, 'func'):
        try:
            return args.func(args)
        except Exception as e:
            print(f"Error: {e}", file=sys.stderr)
            return 1
    else:
        parser.print_help()
        return 0

if __name__ == "__main__":
    sys.exit(main())