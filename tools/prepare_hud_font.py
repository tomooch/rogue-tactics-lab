"""Instantiate the existing OFL Japanese variable font for Unity's static font path.

Development-only dependency: fonttools==4.60.1. No runtime dependency or network I/O.
Pass the original variable TTF as --source; retain its OFL.txt with the output.
The source is available in Git at 36d5a288:unity/Assets/RogueTactics/Art/MobileG1/NotoSansJP.ttf.
"""
import argparse
import hashlib
import json
from pathlib import Path


def main():
    from fontTools.ttLib import TTFont
    from fontTools.varLib.instancer import instantiateVariableFont

    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--source', required=True, type=Path)
    parser.add_argument('--output', required=True, type=Path)
    args = parser.parse_args()
    if args.source.resolve() == args.output.resolve():
        parser.error('Source and output must be different paths')
    font = TTFont(args.source)
    axes = {axis.axisTag: axis for axis in font['fvar'].axes}
    if set(axes) != {'wght'} or not axes['wght'].minValue <= 400 <= axes['wght'].maxValue:
        parser.error('Expected the existing single-weight-axis Japanese font')
    glyph_count = len(font.getGlyphOrder())
    instance = instantiateVariableFont(font, {'wght': 400}, inplace=False)
    assert 'fvar' not in instance
    assert instance['OS/2'].usWeightClass == 400
    assert len(instance.getGlyphOrder()) == glyph_count
    assert instance.getBestCmap() == font.getBestCmap()
    instance.save(args.output)
    print(json.dumps({'weight': 400, 'glyphs': glyph_count,
                      'source_sha256': hashlib.sha256(args.source.read_bytes()).hexdigest(),
                      'output_sha256': hashlib.sha256(args.output.read_bytes()).hexdigest()}))


if __name__ == '__main__':
    main()
