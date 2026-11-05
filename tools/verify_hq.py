"""Owner-local Unity verification. Private screenshots remain under ignored Unity Logs.
No uploads, package installs, credentials or signing operations. Run on a frozen source commit.
"""
import argparse
import hashlib
import json
import pathlib
import subprocess
import sys
import xml.etree.ElementTree as ET


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--editor', required=True, help='Exact pinned Unity executable')
    parser.add_argument('--output', required=True, help='Owner-local evidence directory')
    parser.add_argument('--configure', action='store_true', help='Regenerate scene with existing private skins')
    parser.add_argument('--filter', help='Optional Unity test filter; omitted means full Edit/Play')
    args = parser.parse_args()
    root = pathlib.Path(__file__).resolve().parents[1]
    output = pathlib.Path(args.output).resolve()
    output.mkdir(parents=True, exist_ok=True)
    project = root / 'unity'
    head = subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=root, text=True).strip()
    delta = subprocess.check_output(['git', 'diff', '--binary', 'HEAD', '--', 'unity'], cwd=root)
    files = list((project / 'Assets').rglob('*.cs'))
    files += list((project / 'Assets').rglob('*.shader'))
    # Include newly authored geometry too: a tracked diff omits untracked prototypes.
    files += list((project / 'Assets/RogueTactics/Art/HqRuins').glob('*.obj'))
    files += list((project / 'Assets/RogueTactics/Art/HqRuins').glob('*.json'))
    manifest = {str(p.relative_to(root)): hashlib.sha256(p.read_bytes()).hexdigest() for p in sorted(files)}
    summary = {'head': head, 'unity_diff_sha256': hashlib.sha256(delta).hexdigest(),
               'source_files': manifest, 'editor': args.editor, 'filter': args.filter,
               'results': {}, 'physical_device': False, 'human_acceptance': False}
    steps = []
    if args.configure:
        steps.append(('configure', ['-quit', '-executeMethod', 'RogueTactics.Editor.MobileG1Setup.Configure']))
    for platform in ['EditMode', 'PlayMode']:
        options = ['-runTests', '-testPlatform', platform, '-testResults', str(output / (platform + '.xml'))]
        if args.filter:
            options += ['-testFilter', args.filter]
        steps.append((platform, options))
    for name, options in steps:
        command = [args.editor, '-batchmode', '-projectPath', str(project), '-logFile', str(output / (name + '.log'))] + options
        result = subprocess.run(command)
        record = {'exit': result.returncode, 'argv': command}
        if name != 'configure' and (output / (name + '.xml')).exists():
            record['tests'] = ET.parse(output / (name + '.xml')).getroot().attrib
        summary['results'][name] = record
        scene = project / 'Assets/RogueTactics/Scenes/MobileG1.unity'
        summary['configured_scene_sha256'] = hashlib.sha256(scene.read_bytes()).hexdigest()
        (output / 'source-and-results.json').write_text(json.dumps(summary, indent=2) + '\n')
        print(name + '_COMPLETE', result.returncode, flush=True)
        if result.returncode or name != 'configure' and record.get('tests', {}).get('result') != 'Passed':
            return result.returncode or 1
    return 0


if __name__ == '__main__':
    sys.exit(main())
