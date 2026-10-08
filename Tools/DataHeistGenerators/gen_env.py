"""Environment / props / FX / UI pixel art for The Data Heist (32 px per unit)."""
import math
import os
import random
import sys
import numpy as np
from PIL import Image, ImageFilter
from pixel import Canvas, hexc, mix, shift

OUT = hexc('#120a1a')
PURPLE_D = hexc('#1a1030')
PURPLE = hexc('#2a1748')
PURPLE_L = hexc('#3d2466')
CYAN = hexc('#2ff3ff')
PINK = hexc('#ff3fa4')
ORANGE = hexc('#ff8a2a')
WHITE = hexc('#ffffff')
YELLOW = hexc('#ffd23a')
GREEN = hexc('#7dff6a')
RED = hexc('#ff2d55')

R = random.Random(7)


def save(cv, root, rel):
    p = os.path.join(root, rel)
    os.makedirs(os.path.dirname(p), exist_ok=True)
    (cv.image() if isinstance(cv, Canvas) else cv).save(p)


def glowify(cv, col, radius=2, alpha=110):
    """soft halo behind opaque pixels"""
    im = cv.image()
    a = np.array(im)[:, :, 3]
    halo = Image.fromarray(a).filter(ImageFilter.GaussianBlur(radius))
    h = np.array(halo).astype(float) / 255.0
    base = np.zeros((cv.h, cv.w, 4), dtype=np.uint8)
    base[:, :, 0], base[:, :, 1], base[:, :, 2] = col[0], col[1], col[2]
    base[:, :, 3] = np.clip(h * alpha * 2, 0, alpha).astype(np.uint8)
    out = Image.fromarray(base, 'RGBA')
    out.alpha_composite(im)
    c = Canvas(cv.w, cv.h)
    c.a = np.array(out)
    return c


# ---------------------------------------------------------------------------
# Rooftop tiles
# ---------------------------------------------------------------------------
FACADES = {
    'violet': (hexc('#2b1a4a'), hexc('#1f1236'), hexc('#3c2766'), CYAN),
    'teal': (hexc('#14304a'), hexc('#0d2034'), hexc('#1f4766'), PINK),
    'brick': (hexc('#3a1a2e'), hexc('#2a1222'), hexc('#4e2440'), ORANGE),
}


def roof_top(trim):
    """32x16: ledge cap. Tiled horizontally on top of every rooftop."""
    cv = Canvas(32, 16)
    cv.rect(0, 0, 31, 15, hexc('#3a3352'))
    cv.rect(0, 0, 31, 1, hexc('#6a5f8e'))
    cv.rect(0, 2, 31, 3, hexc('#4b4268'))
    for x in range(0, 32, 8):
        cv.px(x, 2, hexc('#2a2440'))
        cv.rect(x, 4, x, 11, hexc('#2a2440'))
    for x in range(32):
        if R.random() < .15:
            cv.px(x, R.randint(5, 10), hexc('#4b4268'))
    cv.rect(0, 12, 31, 12, trim)
    cv.rect(0, 13, 31, 13, mix(trim, hexc('#000000'), .45))
    cv.rect(0, 14, 31, 15, hexc('#1a142a'))
    return cv


def facade(style, variant):
    base, dark, light, neon = FACADES[style]
    cv = Canvas(32, 32)
    cv.rect(0, 0, 31, 31, base)
    cv.rect(0, 0, 0, 31, dark)
    cv.rect(31, 0, 31, 31, light)
    rr = random.Random(hash((style, variant)) & 0xffff)
    if style == 'brick':
        for y in range(0, 32, 4):
            cv.rect(0, y, 31, y, dark)
            off = 0 if (y // 4) % 2 == 0 else 4
            for x in range(off, 32, 8):
                cv.rect(x, y, x, y + 3, dark)
    # two windows per tile
    for wx in (4, 18):
        lit = rr.random() < 0.55
        wc = rr.choice([hexc('#ffd88a'), hexc('#8af0ff'), hexc('#ff9ad2'), hexc('#c9a8ff')]) if lit else hexc('#120c22')
        cv.rect(wx, 8, wx + 9, 21, hexc('#0c0818'))
        cv.rect(wx + 1, 9, wx + 8, 20, wc)
        if lit:
            cv.rect(wx + 1, 9, wx + 8, 10, mix(wc, WHITE, .4))
            cv.rect(wx + 1, 15, wx + 8, 15, mix(wc, hexc('#000000'), .35))
            if rr.random() < .4:  # silhouette blind
                cv.rect(wx + 1, 16, wx + 8, 20, mix(wc, hexc('#000000'), .25))
        cv.rect(wx + 4, 9, wx + 4, 20, hexc('#0c0818'))
        cv.rect(wx, 22, wx + 9, 22, light)
    if variant == 2:
        cv.rect(0, 27, 31, 28, neon)
        cv.rect(0, 29, 31, 29, mix(neon, base, .6))
    return cv


# ---------------------------------------------------------------------------
# Props / obstacles
# ---------------------------------------------------------------------------
def garbage_can():
    cv = Canvas(32, 32)
    body, dark, light = hexc('#4a5a78'), hexc('#2e3850'), hexc('#7d90b4')
    cv.rect(7, 9, 24, 31, body)
    cv.rect(7, 9, 9, 31, dark)
    cv.rect(21, 9, 22, 31, light)
    for y in (14, 21, 28):
        cv.rect(7, y, 24, y, dark)
    cv.rect(5, 6, 26, 9, hexc('#5c6d92'))
    cv.rect(5, 6, 26, 6, light)
    cv.rect(13, 3, 18, 5, hexc('#5c6d92'))
    # toxic neon ooze & hazard sticker
    cv.rect(11, 16, 17, 19, YELLOW)
    cv.px(14, 17, OUT); cv.px(14, 18, OUT)
    cv.rect(8, 9, 12, 10, GREEN)
    cv.px(9, 11, GREEN); cv.px(9, 12, mix(GREEN, body, .4))
    cv.outline(OUT)
    return glowify(cv, GREEN, 1.2, 50)


def barrier():
    """64x112 tall neon billboard on posts. Panel = rows 0..79 (2.5 units), posts below.
    Collider in engine: panel only (0.75u .. 3.25u above ground) -> must slide."""
    cv = Canvas(64, 112)
    post, post_d = hexc('#3b3456'), hexc('#241e38')
    for x in (3, 57):
        cv.rect(x, 70, x + 3, 111, post_d)
        cv.rect(x, 70, x, 111, post)
        cv.rect(x - 1, 108, x + 4, 111, post)
    cv.rect(2, 1, 61, 81, hexc('#160c2a'))
    cv.rect(3, 2, 60, 80, hexc('#2a1052'))
    for y in range(2, 80, 2):
        cv.rect(3, y, 60, y, hexc('#33145f'))
    for (x0, y0, x1, y1) in [(2, 1, 61, 1), (2, 81, 61, 81), (2, 1, 2, 81), (61, 1, 61, 81)]:
        cv.rect(x0, y0, x1, y1, PINK)
    # hazard stripes along the bottom edge
    for x in range(3, 61):
        cv.rect(x, 74, x, 80, YELLOW if ((x + 0) // 4) % 2 == 0 else hexc('#1a1020'))
    # big downward chevrons = "duck!"
    for cx, cy in ((18, 14), (46, 14), (32, 38)):
        for k in range(8):
            cv.rect(cx - 8 + k, cy + k, cx - 7 + k, cy + k + 1, CYAN)
            cv.rect(cx + 7 - k, cy + k, cx + 8 - k, cy + k + 1, CYAN)
    # corporate logo blob
    cv.rect(8, 58, 55, 66, hexc('#1a0c36'))
    for x in range(10, 54, 3):
        cv.rect(x, 60, x + 1, 64, hexc('#5a2a9a'))
    cv.outline(OUT)
    return glowify(cv, PINK, 2, 70)


def drone(frame, kind='scout'):
    cv = Canvas(32, 32)
    eye = RED if kind == 'scout' else ORANGE
    body, dark, light = hexc('#565f7e'), hexc('#2f3550'), hexc('#a3b0d0')
    # rotors
    span = [9, 4, 9, 2][frame % 4]
    for cx in (7, 25):
        cv.rect(cx - span // 2, 7, cx + span // 2, 7, hexc('#c8d2ee'))
        cv.rect(cx, 8, cx, 11, dark)
    cv.rect(5, 11, 27, 13, dark)
    cv.ellipse(16, 18, 9, 6.5, body)
    cv.ellipse(16, 16, 8, 3.5, light)
    cv.ellipse(16, 17, 8, 4, body)
    cv.rect(8, 21, 24, 23, dark)
    cv.disk(19, 18, 3, hexc('#140a14'))
    cv.disk(19, 18, 2, eye)
    cv.px(20, 17, WHITE)
    if kind == 'gunner':
        cv.rect(10, 24, 13, 27, dark)
        cv.rect(13, 26, 22, 27, hexc('#3a405c'))
        cv.rect(22, 26, 23, 27, ORANGE)
    else:
        cv.line(12, 24, 10, 28, dark)
        cv.line(20, 24, 22, 28, dark)
    # blinking beacon
    if frame % 2 == 0:
        cv.px(16, 10, eye)
    cv.outline(OUT)
    return glowify(cv, eye, 1.5, 60)


def bolt(frame):
    cv = Canvas(16, 8)
    c = ORANGE
    cv.ellipse(8, 4, 6, 2, c)
    cv.ellipse(9, 4, 4, 1.2, mix(c, WHITE, .7))
    if frame:
        cv.px(1, 4, c)
    return glowify(cv, c, 1.5, 120)


def explosion(frame):
    cv = Canvas(32, 32)
    t = frame / 5
    r = 3 + 11 * t
    cols = [WHITE, YELLOW, ORANGE, PINK, hexc('#6a2a8a'), hexc('#3a1a4a')]
    if frame < 4:
        cv.disk(16, 16, r, cols[min(frame + 1, 5)])
        cv.disk(16, 16, r * .65, cols[frame])
    rr = random.Random(frame)
    for k in range(10 + frame * 2):
        a = rr.random() * 6.28
        dd = r * (0.8 + rr.random() * .5)
        cv.px(16 + math.cos(a) * dd, 16 + math.sin(a) * dd, cols[min(frame, 5)])
    if frame >= 3:
        for k in range(6):
            a = rr.random() * 6.28
            dd = r * rr.random()
            cv.disk(16 + math.cos(a) * dd, 16 + math.sin(a) * dd, 2, hexc('#4a3a5a', 200))
    return cv


def data_byte(frame):
    """16x16 spinning hex token."""
    cv = Canvas(16, 16)
    sx = [1.0, 0.75, 0.35, 0.12, 0.35, 0.75][frame]
    pts = []
    for k in range(6):
        a = math.pi / 6 + k * math.pi / 3
        pts.append((7.5 + math.cos(a) * 6.5 * sx, 7.5 + math.sin(a) * 7))
    cv.poly(pts, hexc('#0aa6c8'))
    inner = [(7.5 + (x - 7.5) * .7, 7.5 + (y - 7.5) * .7) for x, y in pts]
    cv.poly(inner, CYAN)
    if sx > .5:
        cv.rect(6, 5, 6, 9, hexc('#08405a'))   # "1"
        cv.rect(8, 5, 9, 5, hexc('#08405a'))   # "0"
        cv.rect(8, 9, 9, 9, hexc('#08405a'))
        cv.rect(8, 5, 8, 9, hexc('#08405a'))
        cv.rect(10, 5, 10, 9, hexc('#08405a'))
    cv.px(7 - 3 * sx, 4, WHITE)
    cv.outline(hexc('#062033'))
    return glowify(cv, CYAN, 1.5, 90)


def data_shard(frame):
    cv = Canvas(32, 48)
    sh = frame * 1
    body = [(16, 4), (25, 18), (21, 40), (11, 40), (7, 18)]
    cv.poly(body, hexc('#b01e8a'))
    cv.poly([(16, 4), (25, 18), (16, 22)], PINK)
    cv.poly([(16, 4), (7, 18), (16, 22)], hexc('#ff8ad8'))
    cv.poly([(16, 22), (21, 40), (11, 40)], hexc('#7a1260'))
    cv.line(16, 22, 16, 40, hexc('#ff8ad8'))
    # data circuit glyph
    for y in range(24 + sh % 4, 38, 4):
        cv.px(14, y, CYAN); cv.px(18, y + 2, CYAN)
    sp = [(12, 10), (20, 14), (14, 30), (18, 26)][frame % 4]
    cv.px(sp[0], sp[1], WHITE); cv.px(sp[0] + 1, sp[1], WHITE); cv.px(sp[0], sp[1] - 1, WHITE)
    cv.outline(hexc('#2a0420'))
    return glowify(cv, PINK, 3, 120)


def repair_kit(frame):
    cv = Canvas(16, 16)
    cv.rect(2, 3, 13, 13, hexc('#e8eef8'))
    cv.rect(2, 3, 13, 4, hexc('#ffffff'))
    cv.rect(2, 12, 13, 13, hexc('#a6b0c8'))
    cv.rect(6, 5, 9, 11, GREEN)
    cv.rect(4, 7, 11, 9, GREEN)
    cv.rect(6, 1, 9, 2, hexc('#a6b0c8'))
    cv.outline(OUT)
    return glowify(cv, GREEN, 1.5, 80 + 40 * frame)


def jump_pad(frame):
    cv = Canvas(32, 16)
    cv.rect(2, 11, 29, 15, hexc('#2c2640'))
    cv.rect(2, 11, 29, 11, hexc('#5a4e80'))
    top = 8 if frame == 0 else 3
    cv.rect(5, top, 26, top + 2, YELLOW)
    cv.rect(5, top, 26, top, mix(YELLOW, WHITE, .5))
    for x in (8, 23):
        cv.line(x, top + 3, x + (3 if frame else 1), 11, hexc('#8a8aa8'))
    for k in range(3):
        cv.px(15 + k, top + 1, ORANGE)
    cv.outline(OUT)
    return glowify(cv, YELLOW, 2, 90)


def checkpoint(on):
    cv = Canvas(16, 64)
    cv.rect(6, 8, 9, 63, hexc('#3b3456'))
    cv.rect(6, 8, 6, 63, hexc('#5a4e80'))
    cv.rect(3, 58, 12, 63, hexc('#2c2640'))
    col = GREEN if on else hexc('#5a5070')
    cv.rect(4, 2, 11, 9, hexc('#241e38'))
    cv.rect(5, 3, 10, 8, col)
    if on:
        cv.rect(6, 4, 7, 5, WHITE)
    for y in range(14, 56, 6):
        cv.rect(7, y, 8, y + 2, col if on else hexc('#2c2640'))
    cv.outline(OUT)
    return glowify(cv, GREEN, 2.5, 120) if on else cv


def laser_emitter(on, flip=False):
    cv = Canvas(16, 16)
    cv.rect(2, 0, 13, 9, hexc('#3b3456'))
    cv.rect(2, 0, 13, 1, hexc('#5a4e80'))
    cv.rect(4, 10, 11, 13, hexc('#241e38'))
    c = RED if on else hexc('#5a3040')
    cv.rect(6, 12, 9, 15, c)
    cv.rect(5, 4, 10, 5, c)
    cv.outline(OUT)
    if flip:
        cv = Canvas.flip(cv)
        cv.a = cv.a[::-1, ::-1].copy()
    return cv


def laser_beam(frame):
    cv = Canvas(16, 32)
    for y in range(32):
        wob = 1 if ((y + frame * 3) % 7) < 2 else 0
        cv.rect(5 - wob, y, 10 + wob, y, (255, 45, 85, 120))
        cv.rect(7, y, 8, y, (255, 200, 210, 255))
    return cv


def extraction_tower(frame):
    """96x160 landing beacon with EXIT sign."""
    cv = Canvas(96, 160)
    steel, steel_d = hexc('#3b3456'), hexc('#241e38')
    # lattice tower
    cv.rect(40, 24, 55, 159, steel_d)
    for y in range(24, 160, 12):
        cv.line(40, y, 55, y + 12, steel)
        cv.line(55, y, 40, y + 12, steel)
    cv.rect(38, 24, 39, 159, steel)
    cv.rect(56, 24, 57, 159, steel)
    # dish / beacon
    cv.rect(30, 16, 65, 23, steel)
    cv.rect(30, 16, 65, 16, hexc('#6a5f8e'))
    on = frame % 2 == 0
    bc = CYAN if on else hexc('#1c8a9c')
    cv.ellipse(48, 9, 8, 7, bc)
    cv.ellipse(48, 8, 4, 3, WHITE if on else CYAN)
    # sign panel
    cv.rect(4, 60, 91, 92, hexc('#160c2a'))
    cv.rect(4, 60, 91, 60, CYAN); cv.rect(4, 92, 91, 92, CYAN)
    cv.rect(4, 60, 4, 92, CYAN); cv.rect(91, 60, 91, 92, CYAN)
    font = {
        'E': ['111', '100', '110', '100', '111'], 'X': ['101', '101', '010', '101', '101'],
        'I': ['111', '010', '010', '010', '111'], 'T': ['111', '010', '010', '010', '010'],
    }
    x0 = 18
    for ch in 'EXIT':
        g = font[ch]
        for gy, row in enumerate(g):
            for gx, bit in enumerate(row):
                if bit == '1':
                    cv.rect(x0 + gx * 4, 66 + gy * 4, x0 + gx * 4 + 3, 66 + gy * 4 + 3, PINK if on else hexc('#c02a80'))
        x0 += 16
    cv.outline(OUT)
    return glowify(cv, CYAN, 3, 110 if on else 60)


def pad_platform():
    """160x16 extraction pad floor marker."""
    cv = Canvas(160, 16)
    cv.rect(0, 4, 159, 15, hexc('#241e38'))
    for x in range(0, 160, 16):
        cv.rect(x + 2, 6, x + 9, 8, CYAN)
    cv.rect(0, 4, 159, 4, CYAN)
    return glowify(cv, CYAN, 2, 90)


# --- decor --------------------------------------------------------------------
def ac_unit():
    cv = Canvas(32, 24)
    cv.rect(2, 4, 29, 23, hexc('#6b7290'))
    cv.rect(2, 4, 29, 5, hexc('#9aa2c2'))
    cv.ellipse(12, 14, 7, 7, hexc('#3b4058'))
    for a in range(0, 360, 60):
        r = math.radians(a)
        cv.line(12, 14, 12 + math.cos(r) * 6, 14 + math.sin(r) * 6, hexc('#8a90aa'))
    for y in range(8, 22, 3):
        cv.rect(22, y, 27, y, hexc('#3b4058'))
    cv.outline(OUT)
    return cv


def antenna():
    cv = Canvas(16, 64)
    cv.rect(7, 6, 8, 63, hexc('#4b4268'))
    for y in (16, 28, 40):
        cv.rect(3, y, 12, y, hexc('#4b4268'))
    cv.disk(7.5, 4, 2, RED)
    cv.outline(OUT)
    return glowify(cv, RED, 2, 90)


def water_tank():
    cv = Canvas(48, 64)
    cv.rect(8, 6, 39, 44, hexc('#4a2e3a'))
    for x in range(8, 40, 5):
        cv.rect(x, 6, x, 44, hexc('#3a2230'))
    cv.poly([(6, 6), (24, -2), (41, 6)], hexc('#3a2230'))
    cv.rect(10, 44, 12, 63, hexc('#2c2640'))
    cv.rect(35, 44, 37, 63, hexc('#2c2640'))
    cv.line(12, 46, 35, 62, hexc('#2c2640'))
    cv.line(35, 46, 12, 62, hexc('#2c2640'))
    cv.outline(OUT)
    return cv


def neon_sign(text_col, kind):
    cv = Canvas(32, 64)
    cv.rect(4, 2, 27, 61, hexc('#140a24'))
    cv.rect(4, 2, 27, 2, text_col); cv.rect(4, 61, 27, 61, text_col)
    cv.rect(4, 2, 4, 61, text_col); cv.rect(27, 2, 27, 61, text_col)
    glyphs = {'ramen': ['0110', '1001', '1111', '1001', '0000', '1111', '0100', '1111'],
              'data': ['1110', '1001', '1110', '0000', '0110', '1001', '1111', '1001']}[kind]
    for gy, row in enumerate(glyphs):
        for gx, b in enumerate(row):
            if b == '1':
                cv.rect(9 + gx * 4, 8 + gy * 6, 11 + gx * 4, 11 + gy * 6, text_col)
    cv.outline(OUT)
    return glowify(cv, text_col, 3, 120)


def vent():
    cv = Canvas(32, 16)
    cv.rect(4, 2, 27, 15, hexc('#4b4268'))
    cv.rect(4, 2, 27, 3, hexc('#6a5f8e'))
    for x in range(7, 26, 4):
        cv.rect(x, 6, x + 1, 13, hexc('#241e38'))
    cv.outline(OUT)
    return cv


# --- FX ----------------------------------------------------------------------
def soft_glow():
    im = Image.new('RGBA', (64, 64), (0, 0, 0, 0))
    a = np.zeros((64, 64, 4), dtype=np.uint8)
    for y in range(64):
        for x in range(64):
            d_ = math.hypot(x - 31.5, y - 31.5) / 32
            v = max(0.0, 1 - d_) ** 2
            a[y, x] = (255, 255, 255, int(v * 255))
    c = Canvas(64, 64)
    c.a = a
    return c


def square(n, col=WHITE):
    c = Canvas(n, n)
    c.rect(0, 0, n - 1, n - 1, col)
    return c


def rain():
    c = Canvas(2, 10)
    for y in range(10):
        c.rect(0, y, 0, y, (170, 200, 255, int(40 + 15 * y)))
    return c


def slash(frame):
    """48x48 white arc (tinted in engine)."""
    cv = Canvas(48, 48)
    a0, a1 = [(-100, -20), (-80, 40), (-30, 70)][frame], None
    a1 = a0[1]
    a0 = a0[0]
    for r in range(14, 22):
        for a in range(a0, a1, 2):
            t = (a - a0) / max(1, (a1 - a0))
            alpha = int(255 * t * (1 - abs(r - 18) / 5))
            if alpha <= 0:
                continue
            rad = math.radians(a)
            cv.px(14 + math.cos(rad) * r, 24 + math.sin(rad) * r, (255, 255, 255, min(255, alpha)))
    return cv


def ring(frame, n=6):
    """64x64 shockwave ring for EMP / pickups."""
    cv = Canvas(64, 64)
    r = 6 + 25 * frame / (n - 1)
    alpha = int(255 * (1 - frame / n))
    for a in range(0, 360, 2):
        rad = math.radians(a)
        cv.px(31.5 + math.cos(rad) * r, 31.5 + math.sin(rad) * r, (255, 255, 255, alpha))
        cv.px(31.5 + math.cos(rad) * (r - 1), 31.5 + math.sin(rad) * (r - 1), (255, 255, 255, alpha // 2))
    return cv


# --- UI ----------------------------------------------------------------------
def heart(full):
    cv = Canvas(16, 16)
    shape = ['0110110', '1111111', '1111111', '0111110', '0011100', '0001000']
    col = PINK if full else hexc('#3a2a4a')
    for y, row in enumerate(shape):
        for x, b in enumerate(row):
            if b == '1':
                cv.rect(1 + x * 2, 2 + y * 2, 2 + x * 2, 3 + y * 2, col)
    if full:
        cv.rect(3, 4, 4, 5, hexc('#ffc0e4'))
    cv.outline(OUT)
    return cv


def panel(col):
    """48x48 9-slice panel, border 12."""
    cv = Canvas(48, 48)
    cv.rect(2, 2, 45, 45, (20, 10, 36, 230))
    for y in range(3, 45, 3):
        cv.rect(3, y, 44, y, (40, 20, 70, 120))
    cv.rect(2, 2, 45, 2, col); cv.rect(2, 45, 45, 45, col)
    cv.rect(2, 2, 2, 45, col); cv.rect(45, 2, 45, 45, col)
    # corner brackets
    for (x, y, dx, dy) in [(0, 0, 1, 1), (47, 0, -1, 1), (0, 47, 1, -1), (47, 47, -1, -1)]:
        for k in range(8):
            cv.px(x + dx * k, y, col)
            cv.px(x, y + dy * k, col)
            cv.px(x + dx * k, y + dy, col)
            cv.px(x + dx, y + dy * k, col)
    return cv


def ability_icon(cid):
    cv = Canvas(32, 32)
    if cid == 'kai':   # double chevron up (thrusters)
        for k, y in enumerate((6, 15)):
            for i in range(8):
                cv.rect(15 - i, y + i, 16 - i, y + i + 1, CYAN)
                cv.rect(15 + i, y + i, 16 + i, y + i + 1, CYAN)
        cv.rect(12, 26, 19, 28, ORANGE)
    elif cid == 'nova':  # dash arrow with speed lines
        cv.poly([(8, 10), (22, 10), (22, 5), (30, 16), (22, 27), (22, 22), (8, 22)], PINK)
        for y in (8, 16, 24):
            cv.rect(1, y, 5, y, WHITE)
    elif cid == 'jax':  # EMP burst
        cv.disk(16, 16, 5, hexc('#38b6ff'))
        for a in range(0, 360, 45):
            r = math.radians(a)
            cv.line(16 + math.cos(r) * 8, 16 + math.sin(r) * 8, 16 + math.cos(r) * 14, 16 + math.sin(r) * 14, YELLOW)
        cv.disk(16, 16, 2, WHITE)
    elif cid == 'lin':  # hourglass / glitch
        cv.poly([(8, 4), (24, 4), (16, 16)], GREEN)
        cv.poly([(16, 16), (24, 28), (8, 28)], GREEN)
        cv.rect(6, 3, 25, 4, WHITE); cv.rect(6, 28, 25, 29, WHITE)
        cv.rect(4, 13, 12, 14, PINK); cv.rect(20, 18, 28, 19, CYAN)
    cv.outline(OUT)
    return cv


def key_icon(label_w=20):
    cv = Canvas(label_w, 16)
    cv.rect(0, 0, label_w - 1, 14, hexc('#5a4e80'))
    cv.rect(1, 1, label_w - 2, 12, hexc('#2c2640'))
    cv.rect(0, 15, label_w - 1, 15, hexc('#160c2a'))
    return cv


# ---------------------------------------------------------------------------
# Parallax
# ---------------------------------------------------------------------------
def skyline(width, height, seed, palette, window_density, neon_cols, max_h, min_h, signs=True):
    rr = random.Random(seed)
    cv = Canvas(width, height)
    x = 0
    while x < width:
        bw = rr.randint(28, 70)
        bh = rr.randint(min_h, max_h)
        top = height - bh
        body = rr.choice(palette)
        cv.rect(x, top, min(width - 1, x + bw), height - 1, body)
        cv.rect(x, top, min(width - 1, x + bw), top, mix(body, WHITE, .12))
        # roof features
        r = rr.random()
        if r < .3:
            cv.rect(x + bw // 2 - 1, top - rr.randint(8, 22), x + bw // 2, top, body)
            cv.px(x + bw // 2, top - 1 - rr.randint(8, 22), RED)
        elif r < .5:
            cv.rect(x + 4, top - 6, x + bw - 4, top, body)
        # windows
        for wy in range(top + 6, height - 4, 7):
            for wx in range(x + 4, x + bw - 3, 6):
                if wx >= width:
                    continue
                if rr.random() < window_density:
                    c = rr.choice([hexc('#ffd88a'), hexc('#8af0ff'), hexc('#ff9ad2'), hexc('#c9a8ff')])
                    c = mix(body, c, rr.uniform(.35, .8))
                    cv.rect(wx, wy, min(width - 1, wx + 2), wy + 3, c)
        if signs and rr.random() < .35:
            col = rr.choice(neon_cols)
            sy = top + rr.randint(8, max(9, bh // 2))
            sx = x + rr.randint(2, max(3, bw - 12))
            vertical = rr.random() < .5
            if vertical:
                cv.rect(sx, sy, sx + 5, min(height - 1, sy + 28), mix(body, col, .25))
                for k in range(sy + 2, min(height - 2, sy + 26), 4):
                    cv.rect(sx + 1, k, sx + 4, k + 2, col)
            else:
                cv.rect(sx, sy, min(width - 1, sx + 20), sy + 7, mix(body, col, .25))
                cv.rect(sx + 1, sy + 2, min(width - 1, sx + 19), sy + 5, col)
        x += bw + rr.randint(0, 6)
    # wrap seam: copy first columns onto end so the tile loops
    return cv


def parallax_layers(root):
    far_pal = [hexc('#2b1b4e'), hexc('#26184a'), hexc('#2e1d56')]
    mid_pal = [hexc('#1d1236'), hexc('#20143c'), hexc('#1a1030')]
    near_pal = [hexc('#120a24'), hexc('#150c2a'), hexc('#100820')]
    far = skyline(640, 260, 11, far_pal, .25, [CYAN, PINK], 220, 90, signs=False)
    mid = skyline(640, 220, 23, mid_pal, .35, [CYAN, PINK, ORANGE], 190, 70)
    near = skyline(640, 180, 37, near_pal, .2, [PINK, ORANGE, CYAN], 150, 40)
    # haze tint over far layer
    for cv, rel in ((far, 'Parallax/city_far.png'), (mid, 'Parallax/city_mid.png'), (near, 'Parallax/city_near.png')):
        make_seamless(cv)
        save(cv, root, rel)
    # sky gradient (tall 1px wide strip stretched by engine) + stars + moon
    sky = Canvas(320, 180)
    top, bot = hexc('#0b0618'), hexc('#5a1f5e')
    for y in range(180):
        t = y / 179
        c = mix(top, bot, t ** 1.4)
        sky.rect(0, y, 319, y, c)
    rr = random.Random(5)
    for k in range(70):
        sky.px(rr.randint(0, 319), rr.randint(0, 110), (255, 255, 255, rr.randint(60, 200)))
    sky.disk(250, 34, 11, hexc('#ffe9f4'))
    sky.disk(255, 31, 10, mix(top, bot, .15))
    save(sky, root, 'Parallax/sky.png')


def make_seamless(cv):
    # blend last 24 columns into the first 24 so horizontal tiling hides the seam
    w = cv.w
    n = 24
    a = cv.a.astype(float)
    for i in range(n):
        t = i / n
        a[:, w - n + i] = a[:, w - n + i] * (1 - t) + a[:, i] * t
    cv.a = a.astype(np.uint8)


# ---------------------------------------------------------------------------
def main(root):
    for style in FACADES:
        for v in range(3):
            save(facade(style, v), root, f'Tiles/facade_{style}_{v}.png')
        save(roof_top(FACADES[style][3]), root, f'Tiles/rooftop_{style}.png')
    save(garbage_can(), root, 'Obstacles/garbage_can.png')
    save(barrier(), root, 'Obstacles/barrier.png')
    for k in range(4):
        save(drone(k, 'scout'), root, f'Enemies/drone_scout_{k:02d}.png')
        save(drone(k, 'gunner'), root, f'Enemies/drone_gunner_{k:02d}.png')
    for k in range(2):
        save(bolt(k), root, f'Enemies/bolt_{k:02d}.png')
    for k in range(6):
        save(explosion(k), root, f'FX/explosion_{k:02d}.png')
        save(data_byte(k), root, f'Pickups/byte_{k:02d}.png')
        save(ring(k), root, f'FX/ring_{k:02d}.png')
    for k in range(4):
        save(data_shard(k), root, f'Pickups/shard_{k:02d}.png')
    for k in range(2):
        save(repair_kit(k), root, f'Pickups/repair_{k:02d}.png')
        save(jump_pad(k), root, f'Obstacles/jumppad_{k:02d}.png')
        save(checkpoint(k == 1), root, f'Obstacles/checkpoint_{"on" if k else "off"}.png')
        save(laser_emitter(k == 1), root, f'Obstacles/laser_emitter_{"on" if k else "off"}.png')
        save(extraction_tower(k), root, f'Obstacles/extraction_{k:02d}.png')
    for k in range(3):
        save(laser_beam(k), root, f'Obstacles/laser_beam_{k:02d}.png')
        save(slash(k), root, f'FX/slash_{k:02d}.png')
    save(pad_platform(), root, 'Obstacles/extraction_pad.png')
    save(ac_unit(), root, 'Decor/ac_unit.png')
    save(antenna(), root, 'Decor/antenna.png')
    save(water_tank(), root, 'Decor/water_tank.png')
    save(neon_sign(PINK, 'ramen'), root, 'Decor/sign_pink.png')
    save(neon_sign(CYAN, 'data'), root, 'Decor/sign_cyan.png')
    save(vent(), root, 'Decor/vent.png')
    save(soft_glow(), root, 'FX/glow.png')
    save(square(4), root, 'FX/pixel.png')
    save(rain(), root, 'FX/rain.png')
    save(heart(True), root, 'UI/heart_full.png')
    save(heart(False), root, 'UI/heart_empty.png')
    save(panel(CYAN), root, 'UI/panel_cyan.png')
    save(panel(PINK), root, 'UI/panel_pink.png')
    for cid in ('kai', 'nova', 'jax', 'lin'):
        save(ability_icon(cid), root, f'UI/ability_{cid}.png')
    save(data_byte(0), root, 'UI/icon_byte.png')
    sh = data_shard(0)
    save(sh, root, 'UI/icon_shard.png')
    parallax_layers(root)


if __name__ == '__main__':
    main(sys.argv[1])
