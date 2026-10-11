"""Renders an annotated overview map of Level 1 (for the README / GDD) + a character line-up sheet."""
import json
import os
import sys
from PIL import Image, ImageDraw, ImageFont

RES = sys.argv[1]
OUT = sys.argv[2]
FONT = os.path.join(RES, 'Fonts/PressStart2P-Regular.ttf')
L = json.load(open(os.path.join(RES, 'Levels/level01.json')))
PPU = 32
ROWS = 4
SEG = 150  # units per row
TOP, BOT = 7.5, -4.0
H = int((TOP - BOT) * PPU)


def sp(path):
    return Image.open(os.path.join(RES, path + '.png')).convert('RGBA')


def ground(x):
    for r in L['roofs']:
        if r['x0'] <= x <= r['x1']:
            return r['y']
    return None


rows = []
for k in range(ROWS):
    x0 = -2 + k * SEG
    x1 = x0 + SEG
    W = int(SEG * PPU)
    im = Image.new('RGBA', (W, H), (16, 9, 32, 255))
    dr = ImageDraw.Draw(im)
    # sky gradient
    for y in range(H):
        t = y / H
        dr.line([(0, y), (W, y)], fill=(int(11 + 60 * t), int(6 + 20 * t), int(24 + 60 * t), 255))
    def X(wx): return int((wx - x0) * PPU)
    def Y(wy): return int((TOP - wy) * PPU)
    # zones
    for z in L['zones']:
        if z['x1'] > x0 and z['x0'] < x1:
            dr.rectangle([X(z['x0']), 0, X(z['x1']), H], fill=(255, 45, 85, 40))
    for r in L['roofs']:
        if r['x1'] < x0 or r['x0'] > x1:
            continue
        top = sp(f"Tiles/rooftop_{r['style']}")
        fac = sp(f"Tiles/facade_{r['style']}_0")
        for wx in range(int(r['x0']), int(r['x1'])):
            for yy in range(Y(r['y']) + 14, H, 32):
                im.alpha_composite(fac, (X(wx), yy))
            im.alpha_composite(top, (X(wx), Y(r['y']) - 2))
    for p in L['platforms']:
        dr.rectangle([X(p['x0']), Y(p['y']), X(p['x1']), Y(p['y']) + 6], fill=(59, 52, 86, 255))
        dr.rectangle([X(p['x0']), Y(p['y']) + 6, X(p['x1']), Y(p['y']) + 8], fill=(255, 63, 164, 255))
    sprites = {
        'can': ('Obstacles/garbage_can', 'b'), 'barrier': ('Obstacles/barrier', 'b'), 'pad': ('Obstacles/jumppad_00', 'b'),
        'drone': ('Enemies/drone_scout_00', 'c'), 'gunner': ('Enemies/drone_gunner_00', 'c'),
        'byte': ('Pickups/byte_00', 'c'), 'repair': ('Pickups/repair_00', 'c'), 'shard': ('Pickups/shard_00', 'c'),
        'checkpoint': ('Obstacles/checkpoint_on', 'b'),
    }
    for e in L['entities']:
        if not (x0 - 2 < e['x'] < x1 + 2):
            continue
        if e['t'] == 'laser':
            dr.rectangle([X(e['x']) - 2, Y(e['y'] + e['h']), X(e['x']) + 2, Y(e['y'])], fill=(255, 45, 85, 255))
            continue
        if e['t'] == 'exit':
            g = ground(e['x'])
            t = sp('Obstacles/extraction_00')
            im.alpha_composite(t, (X(e['x'] + 3.5) - t.width // 2, Y(g) - t.height))
            continue
        s = sprites.get(e['t'])
        if not s:
            continue
        img = sp(s[0])
        if s[1] == 'b':
            im.alpha_composite(img, (X(e['x']) - img.width // 2, Y(e['y']) - img.height))
        else:
            im.alpha_composite(img, (X(e['x']) - img.width // 2, Y(e['y']) - img.height // 2))
    f = ImageFont.truetype(FONT, 16)
    fs = ImageFont.truetype(FONT, 10)
    for pr in L['prompts']:
        if x0 <= pr['x'] < x1:
            label = pr['title'] if not pr['ability'] else 'ABILITY (W)'
            dr.rectangle([X(pr['x']) - 4, 8, X(pr['x']) + 4 + int(f.getlength(label)), 34], fill=(18, 10, 36, 230))
            dr.text((X(pr['x']), 12), label, font=f, fill=(255, 210, 58, 255))
    for wx in range(int(x0 // 10 * 10), int(x1), 10):
        if wx < x0:
            continue
        dr.line([(X(wx), H - 14), (X(wx), H)], fill=(255, 255, 255, 90))
        dr.text((X(wx) + 3, H - 13), f'{wx}m', font=fs, fill=(255, 255, 255, 140))
    rows.append(im)

scale = 0.5
rw, rh = int(rows[0].width * scale), int(H * scale)
sheet = Image.new('RGBA', (rw, (rh + 8) * ROWS), (10, 6, 20, 255))
for i, r in enumerate(rows):
    sheet.alpha_composite(r.resize((rw, rh), Image.NEAREST), (0, i * (rh + 8)))
os.makedirs(OUT, exist_ok=True)
sheet.convert('RGB').save(os.path.join(OUT, 'level01_map.png'))

# character line-up sheet
names = {'kai': ('KAI OKAFOR', 'The Courier', '#2ff3ff'), 'nova': ('NOVA REYES', 'The Runner', '#ff3fa4'),
         'jax': ('JAX MERCER', 'The Breaker', '#38b6ff'), 'lin': ('LIN SATO', 'The Ghost', '#7dff6a')}
anims = ['idle_00', 'run_01', 'run_05', 'jump_00', 'fall_01', 'slide_00', 'attack_01', 'ability_00', 'hurt_00', 'win_01']
S = 3
cell = 48 * S
lu = Image.new('RGBA', (220 + cell * len(anims), 40 + cell * 4), (22, 12, 42, 255))
d = ImageDraw.Draw(lu)
f = ImageFont.truetype(FONT, 14)
fs = ImageFont.truetype(FONT, 9)
for j, a in enumerate(anims):
    d.text((220 + j * cell + 8, 12), a.rsplit('_', 1)[0].upper(), font=fs, fill=(138, 127, 176, 255))
for i, (cid, (nm, tl, col)) in enumerate(names.items()):
    y = 40 + i * cell
    d.text((14, y + cell // 2 - 18), nm, font=f, fill=col)
    d.text((14, y + cell // 2 + 6), tl.upper(), font=fs, fill=(232, 226, 255, 255))
    for j, a in enumerate(anims):
        fr = sp(f'Characters/{cid}/{cid}_{a}').resize((cell, cell), Image.NEAREST)
        lu.alpha_composite(fr, (220 + j * cell, y))
lu.convert('RGB').save(os.path.join(OUT, 'characters_lineup.png'))
print('ok', sheet.size, lu.size)
