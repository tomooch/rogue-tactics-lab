#!/usr/bin/env python3
"""Finite sequential Unity checks. Run through the detached verification runner."""
import argparse
import json
from pathlib import Path
import subprocess

EDITOR = '/Applications/Unity/Hub/Editor/6000.3.25f1/Unity.app/Contents/MacOS/Unity'

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('mode', choices=['setup', 'tests'])
    parser.add_argument('--project', type=Path, required=True)
    parser.add_argument('--evidence', type=Path, required=True)
    args = parser.parse_args()
    args.evidence.mkdir(parents=True, exist_ok=True)
    head = subprocess.check_output(['git', '-C', str(args.project.parent), 'rev-parse', 'HEAD'], text=True).strip()
    runs = [('setup', ['-quit', '-executeMethod', 'ShiroSpikeSetup.Configure'])] if args.mode == 'setup' else [
        ('EditMode', ['-runTests', '-testPlatform', 'EditMode']),
        ('PlayMode', ['-runTests', '-testPlatform', 'PlayMode'])]
    results = []
    for label, extra in runs:
        command = [EDITOR, '-batchmode', '-projectPath', str(args.project), *extra,
                   '-logFile', str(args.evidence / f'{label}.log')]
        if label != 'setup':
            command += ['-testResults', str(args.evidence / f'{label}.xml')]
        print(f'UNITY_START {label} source={head}', flush=True)
        run = subprocess.run(command, timeout=1800)
        results.append({'phase': label, 'source_head': head, 'command': command, 'exit_code': run.returncode})
        (args.evidence / f'{args.mode}-commands.json').write_text(json.dumps(results, indent=2) + '\n')
        print(f'UNITY_END {label} exit={run.returncode}', flush=True)
        if run.returncode:
            raise SystemExit(run.returncode)

if __name__ == '__main__':
    main()
