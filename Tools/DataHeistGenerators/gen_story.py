"""Story-intro art for The Data Heist (32 px per unit, same palette as gen_env.py).

Writes into <Resources/DataHeist>/Story:
  tower.png        Halcyon Corp megatower (4 x 14 units)
  vault.png        the shard vault interior, a full 640x360 backdrop
  searchlight.png  soft light cone, tinted in engine
  holo_frame.png   hologram message frame (text is drawn in engine)
"""
import math
import os
import random
import sys
import numpy as np
from PIL import Image
from pixel import Canvas, hexc, mix
from gen_env import glowify, save, CYAN, PINK, WHITE, RED, YELLOW, GREEN, PURPLE, PURPLE_L, PURPLE_D

NIGHT = hexc('#0b0618')
INK = hexc('#e8e2ff')


# ---------------------------------------------------------------------------
# Halcyon tower
# ---------------------------------------------------------------------------
def tower():
    W, H = 128, 448
    cv = Canvas(W, H)
    rr = random.Random(21)
    cx = W // 2
    body_d, body, edge = hexc('#140a26'), hexc('#1d1238'), hexc('#3c2766')

    def section(y0, y1, half, chamfer):
        pts = [(cx - half + chamfer, y0), (cx + half - chamfer, y0), (cx + half, y0 + chamfer),
               (cx + half, y1), (cx - half, y1), (cx - half, y0 + chamfer)]
        cv.poly(pts, body)
        # darker right half for a lit-from-left read
        cv.poly([(cx, y0), (cx + half - chamfer, y0), (cx + half, y0 + chamfer), (cx + half, y1), (cx, y1)], body_d)
        cv.line(cx - half + chamfer, y0, cx + half - chamfer, y0, edge)
        cv.line(cx - half, y0 + chamfer, cx - half + chamfer, y0, edge)
        # neon edge strips
        for x in (cx - half + 1, cx + half - 2):
            cv.rect(x, y0 + chamfer + 2, x, y1 - 2, mix(body, PINK, .75))
        # windows
        for wy in range(y0 + chamfer + 6, y1 - 4, 6):
            for wx in range(cx - half + 5, cx + half - 6, 5):
                r = rr.random()
                if r < .30:
                    c = rr.choice([CYAN, INK, hexc('#c9a8ff'), hexc('#ffd88a')])
                    c = mix(body, c, rr.uniform(.3, .55) if r > .06 else .95)
                    cv.rect(wx, wy, wx + 2, wy + 2, c)

    def ring(front):
        # holographic ring around the crown; the back half passes behind it
        for i in range(240):
            a = i / 240 * math.tau
            if (math.sin(a) > 0) != front:
                continue
            cv.px(cx - 0.5 + math.cos(a) * 46, 90 + math.sin(a) * 9, CYAN if front else mix(body, CYAN, .45))

    section(250, H - 1, 56, 8)     # podium
    section(112, 250, 40, 10)      # shaft
    ring(front=False)
    section(66, 112, 26, 8)        # crown
    ring(front=True)
    # spire with cross bars and the red beacon
    cv.rect(cx - 2, 8, cx + 1, 66, body)
    cv.rect(cx - 2, 8, cx - 2, 66, edge)
    for y in (24, 40, 54):
        cv.rect(cx - 7, y, cx + 6, y + 1, body)
    cv.disk(cx - 0.5, 5, 3, RED)
    cv.px(cx - 1, 4, WHITE)
    # corporate mark: an H inside a diamond, high on the shaft
    my = 140
    cv.poly([(cx, my - 16), (cx + 16, my), (cx, my + 16), (cx - 16, my)], hexc('#0d2034'))
    for k in range(16):
        cv.px(cx - k, my - 16 + k, CYAN); cv.px(cx + k, my - 16 + k, CYAN)
        cv.px(cx - k, my + 16 - k, CYAN); cv.px(cx + k, my + 16 - k, CYAN)
    cv.rect(cx - 6, my - 6, cx - 4, my + 6, CYAN)
    cv.rect(cx + 3, my - 6, cx + 5, my + 6, CYAN)
    cv.rect(cx - 4, my - 1, cx + 3, my + 1, CYAN)
    # lobby glow at street level
    cv.rect(cx - 30, H - 22, cx + 29, H - 2, mix(body, CYAN, .35))
    cv.rect(cx - 30, H - 22, cx + 29, H - 21, CYAN)
    return glowify(cv, CYAN, 2, 60)


# ---------------------------------------------------------------------------
# Vault interior (640 x 360)
# ---------------------------------------------------------------------------
def vault():
    W, H = 640, 360
    a = np.zeros((H, W, 4), dtype=np.float64)
    top, bot = np.array(hexc('#0b0618')[:3], float), np.array(hexc('#1a1030')[:3], float)
    for y in range(H):
        a[y, :, :3] = top + (bot - top) * (y / H)
    a[:, :, 3] = 255
    horizon = 236
    vx, vy = W / 2, 150

    # floor with perspective grid
    floor = np.array(hexc('#120a1a')[:3], float)
    a[horizon:, :, :3] = floor
    grid = np.array(hexc('#2a1748')[:3], float)
    for k in range(-14, 15):
        xb = vx + k * 60
        for y in range(horizon, H):
            t = (y - vy) / (H - vy)
            x = int(round(vx + (xb - vx) * t))
            if 0 <= x < W:
                a[y, x, :3] = grid
    d = 6.0
    y = horizon + 2
    while y < H:
        a[int(y), :, :3] = grid
        d *= 1.35
        y += d

    # server racks along the back wall, leaving the centre open for the pedestal
    rr = random.Random(5)
    rack, rack_line = np.array(hexc('#1a1030')[:3], float), np.array(hexc('#3d2466')[:3], float)
    leds = [hexc('#2ff3ff'), hexc('#7dff6a'), hexc('#ff3fa4'), hexc('#ffd23a')]
    x = 8
    while x < W - 40:
        if 230 < x + 36 and x < 410:
            x = 410
            continue
        h = rr.randint(150, 196)
        y0 = horizon - h
        a[y0:horizon, x:x + 36, :3] = rack
        a[y0:horizon, x, :3] = rack_line
        a[y0:horizon, x + 35, :3] = rack_line
        a[y0, x:x + 36, :3] = rack_line
        for ly in range(y0 + 6, horizon - 4, 6):
            a[ly + 3, x + 2:x + 34, :3] = rack * 0.8
            for lx in range(x + 4, x + 32, 4):
                if rr.random() < .32:
                    c = np.array(rr.choice(leds)[:3], float)
                    a[ly, lx:lx + 2, :3] = c * rr.uniform(.45, 1)
        x += 40

    # red security laser grid across the room
    red = np.array(hexc('#ff2d55')[:3], float)
    for ly in (178, 206):
        a[ly, :, :3] = a[ly, :, :3] * 0.4 + red * 0.6
        for lx in range(0, W, 12):
            a[ly - 1:ly + 2, lx, :3] = red

    # light cone from the ceiling onto the pedestal
    cyan = np.array(hexc('#2ff3ff')[:3], float)
    for y in range(0, 300):
        half = 18 + y * 0.28
        for x in range(int(vx - half), int(vx + half) + 1):
            edge = 1 - abs(x - vx) / half
            k = 0.16 * edge * (0.55 + 0.45 * y / 300)
            a[y, x, :3] = a[y, x, :3] * (1 - k) + cyan * k

    # pedestal
    cv = Canvas(W, H)
    cv.a = np.clip(a, 0, 255).astype(np.uint8)
    pc = int(vx)
    cv.poly([(pc - 44, 252), (pc + 44, 252), (pc + 58, 312), (pc - 58, 312)], hexc('#2a1748'))
    cv.poly([(pc, 252), (pc + 44, 252), (pc + 58, 312), (pc, 312)], hexc('#1d1238'))
    cv.ellipse(pc, 252, 46, 7, hexc('#3d2466'))
    cv.ellipse(pc, 252, 38, 5, mix(hexc('#3d2466'), CYAN, .55))
    cv.ellipse(pc, 252, 30, 3, mix(hexc('#3d2466'), CYAN, .85))
    for yy in range(262, 310, 8):
        cv.line(pc - 44 - (yy - 252) * 0.23, yy, pc + 44 + (yy - 252) * 0.23, yy, hexc('#3d2466'))
    for xx in (pc - 30, pc, pc + 30):
        cv.rect(xx - 1, 268, xx + 1, 298, mix(hexc('#2a1748'), CYAN, .5))
    return cv


# ---------------------------------------------------------------------------
# Searchlight cone + hologram frame
# ---------------------------------------------------------------------------
def searchlight():
    W, H = 64, 384
    cv = Canvas(W, H)
    for y in range(H):
        half = 2 + (W / 2 - 2) * (y / H)
        fade = (1 - y / H) ** 0.8
        for x in range(W):
            e = 1 - abs(x - W / 2 + 0.5) / half
            if e <= 0:
                continue
            cv.a[y, x] = (255, 255, 255, int(255 * min(1, e * 1.6) * fade * 0.55))
    return cv


def holo_frame():
    W, H = 112, 64
    cv = Canvas(W, H)
    fill = hexc('#ff3fa4', 46)
    cv.rect(2, 2, W - 3, H - 3, fill)
    for y in range(3, H - 3, 3):
        cv.rect(3, y, W - 4, y, hexc('#ff3fa4', 70))
    for (x0, y0, dx, dy) in ((1, 1, 1, 1), (W - 2, 1, -1, 1), (1, H - 2, 1, -1), (W - 2, H - 2, -1, -1)):
        for k in range(10):
            cv.px(x0 + dx * k, y0, PINK); cv.px(x0, y0 + dy * k, PINK)
    cv.rect(1, 1, W - 2, 1, hexc('#ff3fa4', 150)); cv.rect(1, H - 2, W - 2, H - 2, hexc('#ff3fa4', 150))
    cv.rect(1, 1, 1, H - 2, hexc('#ff3fa4', 150)); cv.rect(W - 2, 1, W - 2, H - 2, hexc('#ff3fa4', 150))
    return glowify(cv, PINK, 2, 70)


def main(root):
    out = os.path.join(root, 'Story')
    os.makedirs(out, exist_ok=True)
    save(tower(), out, 'tower.png')
    save(vault(), out, 'vault.png')
    save(searchlight(), out, 'searchlight.png')
    save(holo_frame(), out, 'holo_frame.png')
    print('story art ->', out)


if __name__ == '__main__':
    main(sys.argv[1])
