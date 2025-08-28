#!/usr/bin/env python3
"""
Quick Test Runner - Simple interface for common test operations
"""

import sys
import argparse
from test_coordinator import TestCoordinator, TestPlatform, TestRequestType

def main():
    parser = argparse.ArgumentParser(description='Quick Unity test runner')
    parser.add_argument('action', choices=['all', 'class', 'method', 'category', 'status', 'cancel'],
                       help='Action to perform')
    parser.add_argument('target', nargs='?', help='Target (class/method/category name or request ID)')
    parser.add_argument('-p', '--platform', choices=['edit', 'play', 'both'], default='edit',
                       help='Test platform (default: edit)')
    parser.add_argument('--priority', type=int, default=0,
                       help='Priority level (higher runs first)')
    parser.add_argument('--wait', action='store_true',
                       help='Wait for test completion')
    parser.add_argument('--timeout', type=int, default=300,
                       help='Timeout in seconds (default: 300)')
    
    args = parser.parse_args()
    
    # Map platform strings to enum
    platform_map = {
        'edit': TestPlatform.EDIT_MODE,
        'play': TestPlatform.PLAY_MODE,
        'both': TestPlatform.BOTH
    }
    platform = platform_map[args.platform]
    
    coordinator = TestCoordinator()
    
    try:
        if args.action == 'status':
            if not args.target:
                # Show all pending requests
                requests = coordinator.get_pending_requests()
                if requests:
                    print("Pending test requests:")
                    for req in requests:
                        print(f"  #{req['id']}: {req['request_type']} on {req['test_platform']} "
                              f"(priority: {req['priority']})")
                else:
                    print("No pending test requests")
            else:
                # Show specific request status
                request_id = int(args.target)
                status = coordinator.get_request_status(request_id)
                if status:
                    coordinator.print_summary(request_id)
                else:
                    print(f"Request {request_id} not found")
        
        elif args.action == 'cancel':
            if not args.target:
                print("Error: Request ID required for cancel")
                sys.exit(1)
            request_id = int(args.target)
            coordinator.cancel_request(request_id)
        
        else:
            # Submit test request
            request_type_map = {
                'all': TestRequestType.ALL,
                'class': TestRequestType.CLASS,
                'method': TestRequestType.METHOD,
                'category': TestRequestType.CATEGORY
            }
            request_type = request_type_map[args.action]
            
            # For 'all' tests, target is optional
            test_filter = args.target if args.action != 'all' else None
            
            if args.action != 'all' and not test_filter:
                print(f"Error: {args.action} requires a target")
                sys.exit(1)
            
            # Submit the request
            request_id = coordinator.submit_test_request(
                request_type,
                platform,
                test_filter,
                args.priority
            )
            
            # Wait if requested
            if args.wait:
                print(f"Waiting for completion (timeout: {args.timeout}s)...")
                final_status = coordinator.wait_for_completion(request_id, args.timeout)
                coordinator.print_summary(request_id)
            else:
                print(f"Use 'python quick_test.py status {request_id}' to check progress")
    
    except KeyboardInterrupt:
        print("\nOperation cancelled")
        sys.exit(1)
    except Exception as e:
        print(f"Error: {e}")
        sys.exit(1)

if __name__ == "__main__":
    main()