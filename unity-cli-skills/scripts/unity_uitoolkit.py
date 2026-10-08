#!/usr/bin/env python3
"""Render proposals from JSON; publish them with unity_authoring.py separately."""
import argparse
import json
import sys
from pathlib import Path
from unity_cli.uitoolkit import transform

def execute(request):
    if request.get('operation') != 'batch':
        return {'Ok': True, 'Result': transform(request)}
    items = request.get('items')
    if not isinstance(items, list) or not items:
        raise ValueError('Batch requires a nonempty items array')
    results = []
    for index, item in enumerate(items):
        try:
            if not isinstance(item, dict):
                raise ValueError('Each item must be an object')
            results.append({'index': index, 'Ok': True, 'Result': transform(item)})
        except (ValueError, KeyError, TypeError) as error:
            results.append({'index': index, 'Ok': False, 'Error': str(error)})
    succeeded = sum((item['Ok'] for item in results))
    return {'Ok': succeeded == len(items), 'Result': {'requested': len(items), 'succeeded': succeeded, 'failed': len(items) - succeeded, 'results': results}}

def main():
    parser = argparse.ArgumentParser(description='Render UI Toolkit content proposals with stdlib only. No project or Unity mutation. Operations: xml-inspect/add/modify/clone/remove, uss-upsert/remove/variables, create-uxml/uss, template, editor-window, runtime-ui, batch.')
    parser.add_argument('--request', type=Path, required=True, help='UTF-8 JSON request with operation and explicit source/data')
    parser.add_argument('--result', type=Path, help='Result JSON path; stdout by default')
    args = parser.parse_args()
    try:
        request = json.loads(args.request.read_text(encoding='utf-8'))
        if not isinstance(request, dict):
            raise ValueError('Request must be an object')
        result = execute(request)
    except (ValueError, KeyError, TypeError, OSError) as error:
        result = {'Ok': False, 'Error': {'Code': 'CONTENT_REFUSED', 'Message': str(error)}}
    output = json.dumps(result, indent=2, ensure_ascii=False) + '\n'
    if args.result:
        args.result.write_text(output, encoding='utf-8')
    else:
        sys.stdout.write(output)
    return 0 if result['Ok'] else 2
if __name__ == '__main__':
    raise SystemExit(main())
