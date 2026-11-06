"""Local-only canonical raster extraction probe; never uploads source pixels.

Model: SkyTNT/anime-segmentation (Apache-2.0), isnetis.onnx at HF revision
493cb60893f47441b26ec4fb9a306bce9e342982. Preprocess follows official inference.py.
This is a masking diagnostic, not editable rig or animation acceptance.
"""
import argparse
import hashlib
import json
from pathlib import Path

import cv2
import numpy as np
import onnxruntime as ort
from PIL import Image, ImageDraw

EXPECTED = '59e6186b126d1c7c5ac05e577340af79bb5eddb04d0aaff270398686c465d317'


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def main():
    p = argparse.ArgumentParser()
    p.add_argument('--source', required=True)
    p.add_argument('--model', required=True)
    p.add_argument('--output', required=True)
    args = p.parse_args()
    assert sha(args.source) == EXPECTED, 'Canonical source mismatch'
    out = Path(args.output)
    out.mkdir(parents=True, exist_ok=True)
    rgb = np.array(Image.open(args.source).convert('RGB'))
    h0, w0 = rgb.shape[:2]
    options = ort.SessionOptions()
    options.intra_op_num_threads = 4
    session = ort.InferenceSession(args.model, sess_options=options,
                                   providers=['CPUExecutionProvider'])
    inp = session.get_inputs()[0]
    size = inp.shape[-1] if isinstance(inp.shape[-1], int) else 1024
    h, w = (size, int(size*w0/h0)) if h0 > w0 else (int(size*h0/w0), size)
    ph, pw = size-h, size-w
    padded = np.zeros((size, size, 3), np.float32)
    padded[ph//2:ph//2+h, pw//2:pw//2+w] = cv2.resize(rgb.astype(np.float32)/255, (w, h))
    prediction = session.run(None, {inp.name: padded.transpose(2, 0, 1)[None]})[0]
    mask = np.squeeze(prediction)
    assert mask.ndim == 2, prediction.shape
    mask = cv2.resize(mask[ph//2:ph//2+h, pw//2:pw//2+w], (w0, h0))
    alpha = np.round(np.clip(mask, 0, 1)*255).astype(np.uint8)
    Image.fromarray(alpha).save(out/'raw_alpha.png')
    rgba = np.dstack((rgb, alpha))
    Image.fromarray(rgba).save(out/'source_rgb_raw_alpha.png')
    # Side-by-side backgrounds expose garden contamination and missing fine edges.
    panel = Image.new('RGB', (w0*3, h0+64), 'white')
    panel.paste(Image.fromarray(rgb), (0, 64))
    fg = Image.fromarray(rgba)
    for i, bg in enumerate([(240,240,240,255), (28,30,40,255)], 1):
        tile = Image.new('RGBA', fg.size, bg)
        tile.alpha_composite(fg)
        panel.paste(tile.convert('RGB'), (w0*i, 64))
    ImageDraw.Draw(panel).text((12, 16), 'ORIGINAL | LOCAL RAW MASK / LIGHT | LOCAL RAW MASK / DARK — NOT ART PASS', fill='black')
    panel.save(out/'extraction_review.png')
    ys, xs = np.nonzero(alpha > 0)
    result = {
        'source_sha256': sha(args.source), 'source_unchanged_after': sha(args.source) == EXPECTED,
        'model_sha256': sha(args.model), 'model_revision': '493cb60893f47441b26ec4fb9a306bce9e342982',
        'model_license': 'Apache-2.0', 'provider': session.get_providers(),
        'input_shape': inp.shape, 'output_shape': list(prediction.shape),
        'source_rgb_unchanged': bool(np.array_equal(rgba[:,:,:3], rgb)),
        'nonzero_alpha_bbox': [int(xs.min()),int(ys.min()),int(xs.max())+1,int(ys.max())+1],
        'alpha_soft_pixels': int(((alpha>0)&(alpha<255)).sum()),
        'status': 'MASKING_DIAGNOSTIC_REQUIRES_VISUAL_REVIEW',
        'hidden_paint_created': False, 'rig_created': False, 'unity_run': False,
        'external_source_upload': False,
    }
    (out/'extraction_metrics.json').write_text(json.dumps(result, indent=2)+'\n')
    print(json.dumps(result), flush=True)


if __name__ == '__main__':
    main()
