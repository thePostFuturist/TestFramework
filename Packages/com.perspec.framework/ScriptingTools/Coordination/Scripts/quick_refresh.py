#!/usr/bin/env python3
"""
Quick Asset Refresh - Simple CLI interface for Unity asset refresh operations
"""

import sys
import argparse
import json
from asset_refresh_coordinator import AssetRefreshCoordinator, RefreshType, ImportOptions

def main():
    parser = argparse.ArgumentParser(description='Quick Unity asset refresh')
    parser.add_argument('action', choices=['full', 'paths', 'status', 'cancel', 'list'],
                       help='Action to perform')
    parser.add_argument('target', nargs='*', 
                       help='Paths for selective refresh or request ID for status/cancel')
    parser.add_argument('-o', '--options', choices=['default', 'synchronous', 'force_update'],
                       default='default', help='Import options (default: default)')
    parser.add_argument('--priority', type=int, default=0,
                       help='Priority level (higher runs first)')
    parser.add_argument('--wait', action='store_true',
                       help='Wait for refresh completion')
    parser.add_argument('--timeout', type=int, default=60,
                       help='Timeout in seconds (default: 60)')
    parser.add_argument('--focus', action='store_true',
                       help='Focus Unity window after submitting request')
    
    args = parser.parse_args()
    
    # Map options strings to enum
    options_map = {
        'default': ImportOptions.DEFAULT,
        'synchronous': ImportOptions.SYNCHRONOUS,
        'force_update': ImportOptions.FORCE_UPDATE
    }
    import_options = options_map[args.options]
    
    coordinator = AssetRefreshCoordinator()
    
    try:
        if args.action == 'status':
            if not args.target:
                # Show all pending requests
                requests = coordinator.get_pending_requests()
                if requests:
                    print("Pending asset refresh requests:")
                    for req in requests:
                        print(f"  #{req['id']}: {req['refresh_type']} "
                              f"(priority: {req['priority']})")
                        if req.get('paths'):
                            print(f"    Paths: {', '.join(req['paths'])}")
                else:
                    print("No pending asset refresh requests")
            else:
                # Show specific request status
                request_id = int(args.target[0])
                status = coordinator.get_request_status(request_id)
                if status:
                    coordinator.print_summary(request_id)
                else:
                    print(f"Request {request_id} not found")
        
        elif args.action == 'cancel':
            if not args.target:
                print("Error: Request ID required for cancel")
                sys.exit(1)
            request_id = int(args.target[0])
            success = coordinator.cancel_request(request_id)
            if not success:
                sys.exit(1)
        
        elif args.action == 'list':
            # List all requests
            requests = coordinator.get_pending_requests()
            if requests:
                print("Pending asset refresh requests:")
                for req in requests:
                    print(f"  #{req['id']}: {req['refresh_type']} - {req['status']}")
                    if req.get('paths'):
                        print(f"    Paths: {', '.join(req['paths'])}")
                    print(f"    Created: {req['created_at']}")
            else:
                print("No pending asset refresh requests")
        
        elif args.action == 'full':
            # Submit full refresh request
            request_id = coordinator.submit_refresh_request(
                RefreshType.FULL,
                import_options=import_options,
                priority=args.priority
            )
            
            # Focus Unity if requested
            if args.focus:
                try:
                    import unity_focus
                    print("Focusing Unity window...")
                    unity_focus.focus_after_delay(1)
                except Exception as e:
                    print(f"Could not focus Unity: {e}")
            
            # Wait if requested
            if args.wait:
                print(f"Waiting for completion (timeout: {args.timeout}s)...")
                final_status = coordinator.wait_for_completion(request_id, args.timeout)
                coordinator.print_summary(request_id)
            else:
                print(f"Use 'python quick_refresh.py status {request_id}' to check progress")
        
        elif args.action == 'paths':
            if not args.target:
                print("Error: At least one path required for selective refresh")
                sys.exit(1)
            
            # Submit selective refresh request
            request_id = coordinator.submit_refresh_request(
                RefreshType.SELECTIVE,
                paths=args.target,
                import_options=import_options,
                priority=args.priority
            )
            
            # Focus Unity if requested
            if args.focus:
                try:
                    import unity_focus
                    print("Focusing Unity window...")
                    unity_focus.focus_after_delay(1)
                except Exception as e:
                    print(f"Could not focus Unity: {e}")
            
            # Wait if requested
            if args.wait:
                print(f"Waiting for completion (timeout: {args.timeout}s)...")
                final_status = coordinator.wait_for_completion(request_id, args.timeout)
                coordinator.print_summary(request_id)
            else:
                print(f"Use 'python quick_refresh.py status {request_id}' to check progress")
    
    except KeyboardInterrupt:
        print("\nOperation cancelled")
        sys.exit(1)
    except Exception as e:
        print(f"Error: {e}")
        sys.exit(1)

if __name__ == "__main__":
    main()