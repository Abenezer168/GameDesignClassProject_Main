"""Level 3 "The Underground Data Vault" art (32 px per unit, the game's palette).

    python3 gen_level3_art.py <Resources/DataHeist>

Environment  Tiles/facade_vault_{0,1,2}, Tiles/rooftop_vault, Parallax/vault_{sky,far,mid,near,ceiling}
Decor        Decor/server_{00,01}, Decor/screen_{00..03}, Decor/camera_{00,01}, Decor/beacon_{00..03}
Enemies      Enemies/turret_{00,01,02}, Enemies/guardian_{idle_00,idle_01,charge_00,slam_00,dead_00},
             Enemies/boss_orb_{00,01}, Enemies/shockwave_{00,01}
Obstacles    Obstacles/crusher_head, Obstacles/crusher_shaft, Obstacles/machine_hang, Obstacles/machine_fallen,
             Obstacles/vault_door_closed, Obstacles/vault_door_open
Pickups      Pickups/fragment_{00..03}, Pickups/evidence_{00..03}, UI/icon_fragment
"""
import math
import random
import sys
from pixel import Canvas, hexc, mix
from gen_env import FACADES, roof_top, glowify, save, CYAN, PINK, WHITE, RED, YELLOW, GREEN, ORANGE

STEEL, STEEL_D, STEEL_L = hexc('#1d1b30'), hexc('#121022'), hexc('#3a3656')
INK = hexc('#05030b')
BLUE = hexc('#4a8dff')
VIOLET = hexc('#9a5cff')


# --------------------------------------------------------------------------- tiles
def vault_facade(variant):
    cv = Canvas(32, 32)
    cv.rect(0, 0, 31, 31, STEEL)
    cv.rect(0, 0, 31, 0, STEEL_L)
    cv.rect(0, 15, 31, 16, STEEL_D)
    cv.rect(15, 0, 16, 31, STEEL_D)
    for (x, y) in ((2, 2), (29, 2), (2, 29), (29, 29), (2, 13), (29, 13)):
        cv.px(x, y, STEEL_L)
    rr = random.Random(70 + variant)
    if variant == 0:      # vent slots
        for y in range(19, 29, 3):
            cv.rect(4, y, 12, y, INK)
    if variant in (1, 2): # rack LEDs
        for y in range(4, 12, 3):
            for x in range(19, 29, 3):
                if rr.random() < 0.6:
                    cv.px(x, y, rr.choice([CYAN, BLUE, GREEN, VIOLET]))
    if variant == 2:      # red warning stripe
        for x in range(0, 32, 4):
            cv.rect(x, 26, x + 1, 28, RED)
            cv.rect(x + 2, 26, x + 3, 28, hexc('#2a0a12'))
    return cv


def vault_floor():
    """32x16 grate floor with a yellow-black hazard edge"""
    cv = Canvas(32, 16)
    cv.rect(0, 0, 31, 15, hexc('#2e2b44'))
    for x in range(0, 32, 8):
        cv.rect(x, 0, x + 3, 2, YELLOW)
        cv.rect(x + 4, 0, x + 7, 2, INK)
    cv.rect(0, 3, 31, 3, STEEL_L)
    for x in range(1, 32, 4):
        cv.rect(x, 6, x + 1, 13, hexc('#18162a'))
    return cv


# --------------------------------------------------------------------------- parallax
def vault_sky():
    """the server hall's back wall (it fills the view behind every other layer, so no sky)"""
    cv = Canvas(320, 180)
    for y in range(180):
        cv.rect(0, y, 319, y, mix(hexc('#06040e'), hexc('#120e26'), y / 179))
    rr = random.Random(77)
    for x in range(0, 320, 20):                       # structural ribs
        cv.rect(x, 0, x + 2, 179, hexc('#0e0b1e'))
        cv.rect(x + 3, 0, x + 3, 179, hexc('#1a1633'))
    for y in (34, 96):                                # ducts running the length of the hall
        cv.rect(0, y, 319, y + 9, hexc('#151229'))
        cv.rect(0, y, 319, y, hexc('#2a2545'))
        cv.rect(0, y + 9, 319, y + 9, hexc('#0a0816'))
        for x in range(10, 320, 40):
            cv.rect(x, y + 2, x + 3, y + 7, hexc('#0a0816'))
    for _ in range(26):                               # dim indicator lights on the wall panels
        x, y = rr.randrange(4, 316), rr.randrange(8, 170)
        cv.px(x, y, mix(hexc('#120e26'), rr.choice([CYAN, VIOLET, RED, BLUE]), 0.55))
    return cv


def server_hall(width, height, seed, body, led_p, scale):
    rr = random.Random(seed)
    cv = Canvas(width, height)
    x = 0
    while x < width:
        w = rr.randint(18, 30) * scale // 2
        h = rr.randint(int(height * 0.45), int(height * 0.85))
        top = height - h
        cv.rect(x, top, min(width - 1, x + w), height - 1, body)
        cv.rect(x, top, min(width - 1, x + w), top, mix(body, WHITE, 0.08))
        for ly in range(top + 4, height - 3, 4):
            for lx in range(x + 2, x + w - 2, 3):
                if lx < width and rr.random() < led_p:
                    cv.px(lx, ly, mix(body, rr.choice([CYAN, BLUE, VIOLET, GREEN]), rr.uniform(0.4, 0.9)))
        x += w + rr.randint(2, 8)
    return cv


def vault_mid():
    cv = Canvas(640, 220)
    rr = random.Random(9)
    body = hexc('#161328')
    # big pipes
    for y in (40, 54):
        cv.rect(0, y, 639, y + 7, body)
        cv.rect(0, y, 639, y, mix(body, WHITE, 0.12))
    for x in range(0, 640, 80):
        cv.rect(x + 30, 0, x + 37, 219, body)
        cv.rect(x + 30, 0, x + 30, 219, mix(body, WHITE, 0.1))
    # wall screens with code lines
    for x in (60, 260, 470):
        cv.rect(x, 90, x + 70, 140, hexc('#0a1830'))
        cv.rect(x, 90, x + 70, 90, CYAN)
        for k in range(8):
            w = rr.randint(10, 60)
            cv.rect(x + 4, 96 + k * 5, x + 4 + w, 97 + k * 5, mix(hexc('#0a1830'), CYAN, rr.uniform(0.35, 0.8)))
    # catwalk railing
    cv.rect(0, 170, 639, 171, body)
    for x in range(0, 640, 12):
        cv.rect(x, 160, x + 1, 170, body)
    cv.rect(0, 160, 639, 160, mix(body, VIOLET, 0.5))
    # warning lamps
    for x in range(40, 640, 150):
        cv.disk(x, 76, 3, RED)
    return glowify(cv, CYAN, 1.5, 40)


def vault_near():
    cv = Canvas(640, 180)
    rr = random.Random(12)
    body = hexc('#0b0916')
    x = 0
    while x < 640:
        w, h = rr.randint(40, 90), rr.randint(50, 120)
        top = 180 - h
        cv.rect(x, top, min(639, x + w), 179, body)
        cv.rect(x, top, min(639, x + w), top, mix(body, VIOLET, 0.5))
        if rr.random() < 0.5:  # generator vents with red glow
            cv.rect(x + 6, top + 12, x + w - 6, top + 16, mix(body, RED, 0.6))
        x += w + rr.randint(10, 40)
    return cv


def vault_ceiling():
    """64 px band hung from the top of the screen: pipes, sagging cables, lamps"""
    cv = Canvas(640, 64)
    rr = random.Random(5)
    cv.rect(0, 0, 639, 18, hexc('#0b0916'))
    cv.rect(0, 18, 639, 19, STEEL_L)
    for y, c in ((22, hexc('#1d1b30')), (30, hexc('#161328'))):
        cv.rect(0, y, 639, y + 5, c)
        cv.rect(0, y, 639, y, mix(c, WHITE, 0.1))
    for _ in range(8):
        x0 = rr.randint(0, 600)
        span = rr.randint(40, 90)
        for k in range(span):
            sag = math.sin(k / span * math.pi) * rr.uniform(8, 18)
            cv.px(x0 + k, 36 + sag, hexc('#2a2440'))
    for x in range(50, 640, 130):
        cv.rect(x, 36, x + 1, 46, STEEL_L)
        cv.rect(x - 5, 46, x + 6, 49, STEEL_L)
        cv.rect(x - 3, 50, x + 4, 50, hexc('#bfe8ff'))
    return glowify(cv, hexc('#bfe8ff'), 2, 50)


# --------------------------------------------------------------------------- decor
def server_rack(frame):
    cv = Canvas(32, 64)
    cv.rect(2, 4, 29, 63, hexc('#141226'))
    cv.rect(2, 4, 29, 5, STEEL_L)
    cv.rect(2, 4, 3, 63, STEEL_L)
    rr = random.Random(3 + frame)
    for y in range(9, 60, 5):
        cv.rect(5, y, 26, y + 3, hexc('#0b0916'))
        for x in range(7, 25, 3):
            if rr.random() < 0.55:
                cv.px(x, y + 1, rr.choice([CYAN, GREEN, BLUE, VIOLET]))
    return glowify(cv, BLUE, 1.5, 50)


def screen(frame):
    cv = Canvas(48, 40)
    cv.rect(22, 30, 25, 39, STEEL_L)
    cv.rect(14, 38, 33, 39, STEEL_L)
    cv.rect(2, 2, 45, 30, STEEL_D)
    cv.rect(4, 4, 43, 28, hexc('#06182a'))
    rr = random.Random(100)
    lines = [rr.randint(6, 34) for _ in range(12)]
    for k in range(6):
        w = lines[(k + frame) % len(lines)]
        cv.rect(6, 6 + k * 4, 6 + w, 7 + k * 4, mix(hexc('#06182a'), CYAN if k % 3 else GREEN, 0.75))
    cv.rect(6 + (frame * 9) % 30, 26, 8 + (frame * 9) % 30, 26, WHITE)
    return glowify(cv, CYAN, 2, 80)


def camera(frame):
    cv = Canvas(24, 32)
    cv.rect(11, 12, 12, 31, STEEL_L)
    cv.poly([(2, 4), (18, 2), (20, 10), (4, 12)], hexc('#2a2440'))
    cv.rect(0, 6, 3, 9, STEEL_D)
    if frame == 0:
        cv.disk(4, 7, 1.5, RED)
    return glowify(cv, RED, 1.5, 70) if frame == 0 else cv


def beacon(frame):
    cv = Canvas(16, 24)
    cv.rect(7, 12, 8, 23, STEEL_L)
    cv.rect(3, 6, 12, 12, hexc('#4a0a18'))
    a = frame / 4 * math.tau
    bx = 7.5 + math.cos(a) * 4
    cv.rect(int(bx) - 1, 7, int(bx) + 1, 11, RED)
    cv.rect(4, 5, 11, 5, STEEL_L)
    return glowify(cv, RED, 2.5, 120 if frame in (0, 1) else 50)


# --------------------------------------------------------------------------- enemies
def turret(state):
    """0 idle, 1 aiming (red eye, warning), 2 firing (muzzle flash)"""
    cv = Canvas(32, 32)
    cv.rect(6, 24, 25, 31, STEEL_D)
    cv.rect(6, 24, 25, 24, STEEL_L)
    cv.rect(13, 18, 18, 24, STEEL)
    cv.ellipse(16, 15, 9, 6, hexc('#2a2440'))
    cv.ellipse(16, 13, 8, 3, STEEL_L)
    cv.rect(0, 14, 10, 16, hexc('#5a4a8a'))           # barrel, pointing left
    eye = RED if state else hexc('#5a1020')
    cv.disk(18, 15, 2, eye)
    if state == 2:
        cv.rect(0, 13, 1, 17, WHITE)
        cv.px(0, 12, YELLOW); cv.px(0, 18, YELLOW)
    return glowify(cv, RED, 1.5, 90 if state else 30)


def guardian(pose):
    """128x128 security robot. poses: idle0, idle1, charge (core blazing), slam (fist down), dead (cracked, dark)"""
    cv = Canvas(128, 128)
    armor, armor_d, armor_l = hexc('#2b2747'), hexc('#18152b'), hexc('#4b4570')
    dead = pose == 'dead'
    bob = 1 if pose == 'idle1' else 0
    # legs
    for lx in (40, 72):
        cv.rect(lx, 92, lx + 15, 125, armor_d)
        cv.rect(lx, 92, lx + 15, 93, armor_l)
        cv.rect(lx - 4, 120, lx + 19, 127, armor)
    # torso
    cv.poly([(30, 40 + bob), (98, 40 + bob), (106, 94), (22, 94)], armor)
    cv.poly([(64, 40 + bob), (98, 40 + bob), (106, 94), (64, 94)], armor_d)
    cv.rect(30, 40 + bob, 98, 42 + bob, armor_l)
    # core
    core = hexc('#3a0a14') if dead else (WHITE if pose == 'charge' else RED)
    cv.disk(64, 66 + bob, 11, hexc('#120a1a'))
    cv.disk(64, 66 + bob, 8, core)
    if not dead:
        cv.disk(64, 66 + bob, 4, mix(core, WHITE, 0.6))
    # head
    cv.rect(48, 20 + bob, 80, 40 + bob, armor)
    cv.rect(48, 20 + bob, 80, 21 + bob, armor_l)
    visor = hexc('#3a0a14') if dead else RED
    cv.rect(52, 28 + bob, 76, 31 + bob, visor)
    # shoulder cannons
    for (sx, d) in ((14, -1), (100, 1)):
        cv.rect(sx, 34 + bob, sx + 16, 50 + bob, armor)
        cv.rect(sx, 34 + bob, sx + 16, 35 + bob, armor_l)
    cv.rect(0, 40 + bob, 16, 44 + bob, hexc('#5a4a8a'))     # left cannon barrel (aims at the runner)
    # arms
    if pose == 'slam':
        cv.rect(8, 60, 22, 110, armor)
        cv.rect(2, 108, 26, 124, armor_l)               # fist on the ground
    else:
        cv.rect(10, 52 + bob, 22, 86 + bob, armor)
        cv.rect(6, 84 + bob, 24, 96 + bob, armor_l)
    cv.rect(106, 52 + bob, 118, 86 + bob, armor)
    cv.rect(104, 84 + bob, 122, 96 + bob, armor_l)
    # neon trims
    trim = hexc('#3c2766') if dead else PINK
    cv.rect(30, 94, 98, 95, trim)
    cv.rect(36, 50 + bob, 40, 80 + bob, mix(armor, trim, 0.6))
    cv.rect(88, 50 + bob, 92, 80 + bob, mix(armor, trim, 0.6))
    if dead:
        cv.line(50, 44, 70, 90, INK, 2)
        cv.line(80, 46, 62, 70, INK, 2)
        return cv
    return glowify(cv, RED if pose != 'charge' else WHITE, 3, 90)


def boss_orb(frame):
    cv = Canvas(16, 16)
    cv.disk(8, 8, 6, VIOLET)
    cv.disk(8, 8, 3.5 + frame, mix(VIOLET, WHITE, 0.6))
    return glowify(cv, VIOLET, 2, 160)


def shockwave(frame):
    cv = Canvas(32, 20)
    for x in range(32):
        h = int(14 * math.sin(math.pi * x / 31) ** 1.5) + frame
        cv.rect(x, 19 - h, x, 19, mix(CYAN, VIOLET, x / 31))
        cv.px(x, 19 - h, WHITE)
    return glowify(cv, CYAN, 2, 150)


# --------------------------------------------------------------------------- obstacles
def crusher_head():
    """48x32 press plate, hazard stripes along the bottom edge"""
    cv = Canvas(48, 32)
    cv.rect(0, 0, 47, 27, STEEL)
    cv.rect(0, 0, 47, 1, STEEL_L)
    cv.rect(4, 6, 43, 20, STEEL_D)
    for x in range(0, 48, 8):
        cv.rect(x, 26, x + 3, 31, YELLOW)
        cv.rect(x + 4, 26, x + 7, 31, INK)
    cv.rect(20, 10, 27, 16, RED)
    return glowify(cv, RED, 1, 40)


def crusher_shaft():
    cv = Canvas(16, 32)
    cv.rect(2, 0, 13, 31, STEEL_D)
    cv.rect(2, 0, 3, 31, STEEL_L)
    for y in range(0, 32, 8):
        cv.rect(2, y, 13, y, STEEL)
    return cv


def machine(fallen):
    """hanging server crate on a hook, and its wreck"""
    cv = Canvas(48, 40)
    cv.rect(4, 8, 43, 39, hexc('#1f1c33'))
    cv.rect(4, 8, 43, 9, STEEL_L)
    for y in range(13, 37, 6):
        cv.rect(8, y, 39, y + 3, hexc('#0b0916'))
        for x in range(10, 38, 4):
            cv.px(x, y + 1, CYAN if (x + y) % 3 else GREEN)
    cv.rect(10, 2, 37, 3, STEEL_L)
    for x in (12, 35):
        cv.line(x, 3, 24, 0, STEEL_L)
    if not fallen:
        return glowify(cv, BLUE, 1.5, 60)
    out = Canvas(48, 24)
    for y in range(24):
        for x in range(48):
            c = cv.a[16 + y, x]
            if c[3]:
                out.a[min(23, y + (x // 12 if x > 24 else 0)), x] = c
    out.line(14, 2, 30, 23, INK, 2)
    for (x, y) in ((22, 6), (26, 12), (18, 16)):
        out.px(x, y, WHITE); out.px(x + 1, y - 1, YELLOW)
    return glowify(out, ORANGE, 1.5, 70)


def vault_door(open_):
    cv = Canvas(48, 96)
    cv.rect(0, 0, 47, 95, STEEL_D)
    cv.rect(0, 0, 47, 3, STEEL_L)
    cv.rect(0, 0, 3, 95, STEEL_L)
    cv.rect(44, 0, 47, 95, STEEL_L)
    if open_:
        cv.rect(8, 6, 39, 95, hexc('#0a1830'))
        for y in range(10, 95, 6):
            cv.rect(10, y, 37, y, mix(hexc('#0a1830'), CYAN, 0.25))
        cv.rect(8, 6, 39, 7, GREEN)
        return glowify(cv, GREEN, 2, 90)
    cv.rect(6, 6, 41, 95, STEEL)
    cv.rect(23, 6, 24, 95, INK)
    for y in range(12, 95, 14):
        cv.rect(8, y, 21, y + 1, STEEL_L)
        cv.rect(26, y, 39, y + 1, STEEL_L)
    for i in range(3):                                   # three fragment locks
        cv.disk(24, 32 + i * 16, 4, hexc('#2a0a12'))
        cv.disk(24, 32 + i * 16, 2, RED)
    return glowify(cv, RED, 2, 70)


def fragment(frame):
    """encrypted data fragment: a gold chip whose cyan circuit pulses"""
    cv = Canvas(20, 20)
    w = [8, 6, 3, 6][frame]            # fake spin: the chip narrows and widens
    cv.rect(10 - w, 3, 9 + w, 16, hexc('#b8860b'))
    cv.rect(10 - w, 3, 9 + w, 4, YELLOW)
    if w > 3:
        cv.rect(10 - w + 2, 7, 9 + w - 2, 12, hexc('#1a1300'))
        cv.rect(10 - w + 3, 9, 9 + w - 3, 9, CYAN)
        cv.px(10, 11, CYAN)
    for y in (6, 10, 14):
        cv.px(10 - w - 1, y, YELLOW); cv.px(9 + w + 1, y, YELLOW)
    return glowify(cv, YELLOW, 2.5, 130)


def evidence(frame):
    cv = Canvas(24, 24)
    cv.rect(4, 6, 19, 19, hexc('#120a1a'))
    cv.rect(4, 6, 19, 7, PINK)
    cv.rect(7, 10, 16, 15, mix(PINK, CYAN, frame / 3))
    cv.rect(10, 2, 13, 6, STEEL_L)
    return glowify(cv, mix(PINK, CYAN, frame / 3), 3, 150)


def main(root):
    FACADES['vault'] = (STEEL, STEEL_D, STEEL_L, CYAN)
    for v in range(3):
        save(vault_facade(v), root, f'Tiles/facade_vault_{v}.png')
    save(vault_floor(), root, 'Tiles/rooftop_vault.png')
    save(vault_sky(), root, 'Parallax/vault_sky.png')
    save(server_hall(640, 260, 31, hexc('#1a1633'), 0.35, 2), root, 'Parallax/vault_far.png')
    save(vault_mid(), root, 'Parallax/vault_mid.png')
    save(vault_near(), root, 'Parallax/vault_near.png')
    save(vault_ceiling(), root, 'Parallax/vault_ceiling.png')
    for k in range(2):
        save(server_rack(k), root, f'Decor/server_{k:02d}.png')
        save(camera(k), root, f'Decor/camera_{k:02d}.png')
        save(boss_orb(k), root, f'Enemies/boss_orb_{k:02d}.png')
        save(shockwave(k), root, f'Enemies/shockwave_{k:02d}.png')
    for k in range(4):
        save(screen(k), root, f'Decor/screen_{k:02d}.png')
        save(beacon(k), root, f'Decor/beacon_{k:02d}.png')
        save(fragment(k), root, f'Pickups/fragment_{k:02d}.png')
        save(evidence(k), root, f'Pickups/evidence_{k:02d}.png')
    for k in range(3):
        save(turret(k), root, f'Enemies/turret_{k:02d}.png')
    for name, pose in (('idle_00', 'idle0'), ('idle_01', 'idle1'), ('charge_00', 'charge'), ('slam_00', 'slam'), ('dead_00', 'dead')):
        save(guardian(pose), root, f'Enemies/guardian_{name}.png')
    save(crusher_head(), root, 'Obstacles/crusher_head.png')
    save(crusher_shaft(), root, 'Obstacles/crusher_shaft.png')
    save(machine(False), root, 'Obstacles/machine_hang.png')
    save(machine(True), root, 'Obstacles/machine_fallen.png')
    save(vault_door(False), root, 'Obstacles/vault_door_closed.png')
    save(vault_door(True), root, 'Obstacles/vault_door_open.png')
    save(fragment(0), root, 'UI/icon_fragment.png')
    print('level 3 art ->', root)


if __name__ == '__main__':
    main(sys.argv[1])
