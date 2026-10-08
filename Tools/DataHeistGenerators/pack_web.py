"""Packs the generated PNGs into one atlas + writes build/assets.js for the browser build."""
import base64
import glob
import io
import json
import os
import sys
from PIL import Image

res, out = sys.argv[1], sys.argv[2]
files = sorted(glob.glob(os.path.join(res, '**', '*.png'), recursive=True))
skip_dirs = ('Parallax',)
items = []
for f in files:
    rel = os.path.relpath(f, res).replace(os.sep, '/')
    if rel.split('/')[0] in skip_dirs:
        continue
    name = os.path.splitext(os.path.basename(f))[0]
    items.append((name, Image.open(f).convert('RGBA')))

# shelf packing, tallest first
items.sort(key=lambda t: (-t[1].height, -t[1].width))
W = 1024
x = y = shelf = 0
pos = {}
for name, im in items:
    if x + im.width > W:
        x = 0; y += shelf + 1; shelf = 0
    pos[name] = (x, y, im.width, im.height)
    x += im.width + 1
    shelf = max(shelf, im.height)
H = y + shelf
atlas = Image.new('RGBA', (W, H), (0, 0, 0, 0))
for name, im in items:
    px, py, _, _ = pos[name]
    atlas.paste(im, (px, py))


def uri(im):
    b = io.BytesIO(); im.save(b, 'PNG', optimize=True)
    return 'data:image/png;base64,' + base64.b64encode(b.getvalue()).decode()


parallax = {}
for f in sorted(glob.glob(os.path.join(res, 'Parallax', '*.png'))):
    parallax[os.path.splitext(os.path.basename(f))[0]] = uri(Image.open(f))

audio = {}
for f in sorted(glob.glob(os.path.join(out, 'audio', '*.mp3'))):
    with open(f, 'rb') as fh:
        audio[os.path.splitext(os.path.basename(f))[0]] = 'data:audio/mpeg;base64,' + base64.b64encode(fh.read()).decode()

with open(os.path.join(res, 'Levels', 'level01.json')) as fh:
    level = json.load(fh)

with open(os.path.join(out, 'assets.js'), 'w') as fh:
    fh.write('window.DH_ASSETS=')
    json.dump(dict(atlas=uri(atlas), frames=pos, parallax=parallax, audio=audio, level=level), fh, separators=(',', ':'))
    fh.write(';\n')
print('atlas', W, H, 'sprites', len(pos), 'assets.js', os.path.getsize(os.path.join(out, 'assets.js')) // 1024, 'KB')
