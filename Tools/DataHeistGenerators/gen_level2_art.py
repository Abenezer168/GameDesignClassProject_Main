"""Level 2 "Corporate District" art: glass-tower rooftops and the three new hazards.

    python3 gen_level2_art.py <Resources/DataHeist>

Tiles/facade_glass_{0,1,2}, Tiles/rooftop_glass  - curtain-wall office towers
Enemies/sniper_window_{00,01}                    - sniper nest in a penthouse window (01 = muzzle flash)
Enemies/sniper_bolt_{00,01}                      - red tracer round
Obstacles/shock_off, Obstacles/shock_on_{00..02} - electrified floor grate (tiled along x)
Obstacles/sign_hang, Obstacles/sign_fallen       - Halcyon billboard, hanging and wrecked
"""
import random
import sys
from pixel import Canvas, hexc, mix
from gen_env import FACADES, facade, roof_top, glowify, save, CYAN, PINK, WHITE, RED, PURPLE_D, OUT

GLASS = (hexc('#17324f'), hexc('#0d2136'), hexc('#2f6690'), CYAN)


def glass_facade(variant):
    """curtain wall: tall reflective panes and mullions instead of punched windows"""
    base, dark, light, neon = GLASS
    cv = Canvas(32, 32)
    cv.rect(0, 0, 31, 31, base)
    rr = random.Random(40 + variant)
    for x0 in (1, 11, 21):
        lit = rr.random() < 0.45
        pane = mix(base, hexc('#8af0ff'), 0.35) if lit else mix(base, hexc('#05101c'), 0.4)
        cv.rect(x0, 1, x0 + 8, 30, pane)
        # sky reflection streak
        cv.line(x0 + 1, 28, x0 + 7, 4, mix(pane, WHITE, 0.25))
        if lit and rr.random() < 0.5:
            cv.rect(x0 + 1, 14, x0 + 7, 15, mix(pane, WHITE, 0.4))
    for x in (0, 10, 20, 30):
        cv.rect(x, 0, x, 31, dark)
    cv.rect(0, 0, 31, 0, light)
    cv.rect(31, 0, 31, 31, light)
    if variant == 2:
        cv.rect(0, 26, 31, 27, neon)
        cv.rect(0, 28, 31, 28, mix(neon, base, 0.6))
    return cv


def sniper_window(flash):
    """48x64 penthouse wall chunk; the window opening sits at chest height (rows 24-46)"""
    cv = Canvas(48, 64)
    wall, edge = hexc('#1a1430'), hexc('#3c2766')
    cv.rect(4, 6, 47, 63, wall)
    cv.rect(4, 6, 47, 7, edge)
    cv.rect(4, 6, 5, 63, edge)
    # neon corporate trim
    cv.rect(6, 10, 45, 10, mix(wall, PINK, 0.8))
    # window opening (dark room behind)
    cv.rect(8, 22, 40, 46, hexc('#05030b'))
    cv.rect(8, 22, 40, 22, hexc('#5a4a8a'))
    cv.rect(8, 46, 40, 47, hexc('#5a4a8a'))
    # sniper silhouette: head, shoulder, rifle pointing left out of the window
    cv.disk(30, 31, 4, hexc('#120e1e'))
    cv.rect(24, 34, 38, 46, hexc('#120e1e'))
    cv.rect(2, 36, 28, 37, hexc('#6a5f8e'))          # barrel
    cv.rect(16, 34, 22, 35, hexc('#2a2440'))         # scope
    cv.px(16, 34, RED); cv.px(17, 34, RED)           # scope glint
    cv.rect(28, 30, 30, 31, RED)                     # visor
    if flash:
        for (x, y, c) in ((0, 36, WHITE), (1, 35, hexc('#ffd23a')), (1, 37, hexc('#ffd23a')), (0, 34, RED), (0, 38, RED)):
            cv.px(x, y, c)
        cv.rect(0, 36, 3, 37, WHITE)
    return glowify(cv, RED, 1.5, 70)


def sniper_bolt(frame):
    cv = Canvas(24, 6)
    cv.rect(2, 2, 21, 3, RED)
    cv.rect(10 + frame * 2, 2, 21, 3, mix(RED, WHITE, 0.7))
    cv.px(22, 2, WHITE); cv.px(22, 3, WHITE)
    return glowify(cv, RED, 1.5, 140)


def shock_tile(on, frame=0):
    """32x8 floor grate; when live, white-cyan arcs jump between the rails"""
    cv = Canvas(32, 8)
    cv.rect(0, 2, 31, 7, hexc('#2a2440'))
    cv.rect(0, 2, 31, 2, hexc('#5a4a8a'))
    for x in range(1, 32, 4):
        cv.rect(x, 4, x + 1, 6, hexc('#120e1e'))
    rail = mix(hexc('#ffb36a'), hexc('#2a2440'), 0.3) if not on else CYAN
    cv.rect(0, 1, 31, 1, rail)
    if on:
        rr = random.Random(frame * 13 + 1)
        y = 1
        for x in range(0, 32):
            y = max(0, min(3, y + rr.choice((-1, 0, 1))))
            cv.px(x, y, WHITE if rr.random() < 0.4 else CYAN)
        for _ in range(3):
            x = rr.randrange(32)
            cv.px(x, 0, WHITE)
        return glowify(cv, CYAN, 1.5, 140)
    return cv


def halcyon_sign(fallen):
    """64x40 billboard with the Halcyon H mark; the wreck is bent, cracked and sparking"""
    cv = Canvas(64, 40)
    frame, panel = hexc('#3c2766'), hexc('#160c2a')
    cv.rect(2, 4, 61, 35, frame)
    cv.rect(4, 6, 59, 33, panel)
    cv.rect(4, 6, 59, 7, PINK)
    cv.rect(4, 32, 59, 33, PINK)
    # H in a diamond
    cx, cy = 18, 20
    for k in range(9):
        for (dx, dy) in ((-k, -9 + k), (k, -9 + k), (-k, 9 - k), (k, 9 - k)):
            cv.px(cx + dx, cy + dy, CYAN)
    cv.rect(cx - 3, cy - 4, cx - 2, cy + 4, CYAN)
    cv.rect(cx + 2, cy - 4, cx + 3, cy + 4, CYAN)
    cv.rect(cx - 2, cy, cx + 2, cy, CYAN)
    # "text" bars
    for i, w in enumerate((30, 22, 26)):
        cv.rect(30, 13 + i * 6, 30 + w, 14 + i * 6, mix(PINK, WHITE, 0.3) if i == 0 else mix(CYAN, panel, 0.3))
    if not fallen:
        for x in (10, 52):                              # hanging brackets
            cv.rect(x, 0, x + 1, 4, hexc('#5a4a8a'))
        return glowify(cv, PINK, 2, 90)
    # wreck: shift the right half down, crack and sparks
    wreck = Canvas(64, 40)
    for y in range(40):
        for x in range(64):
            c = cv.a[y, x]
            if c[3] == 0:
                continue
            ny = y + (x - 30) // 8 if x > 30 else y
            if 0 <= ny < 40:
                dim = (c * [0.6, 0.6, 0.65, 1]).astype('uint8') if (x + y) % 7 == 0 else c
                wreck.a[ny, x] = dim
    wreck.line(28, 6, 36, 36, hexc('#05030b'), 2)
    for (x, y) in ((33, 8), (35, 12), (31, 20), (37, 28)):
        wreck.px(x, y, WHITE)
        wreck.px(x + 1, y - 1, hexc('#ffd23a'))
    # trim to the lower 24 rows so the wreck sits on the roof
    out = Canvas(64, 24)
    out.a = wreck.a[16:40].copy()
    return glowify(out, PINK, 1.5, 70)


def main(root):
    FACADES['glass'] = GLASS
    for v in range(3):
        save(glass_facade(v), root, f'Tiles/facade_glass_{v}.png')
    save(roof_top(CYAN), root, 'Tiles/rooftop_glass.png')
    for k in range(2):
        save(sniper_window(k == 1), root, f'Enemies/sniper_window_{k:02d}.png')
        save(sniper_bolt(k), root, f'Enemies/sniper_bolt_{k:02d}.png')
    save(shock_tile(False), root, 'Obstacles/shock_off.png')
    for k in range(3):
        save(shock_tile(True, k), root, f'Obstacles/shock_on_{k:02d}.png')
    save(halcyon_sign(False), root, 'Obstacles/sign_hang.png')
    save(halcyon_sign(True), root, 'Obstacles/sign_fallen.png')
    print('level 2 art ->', root)


if __name__ == '__main__':
    main(sys.argv[1])
