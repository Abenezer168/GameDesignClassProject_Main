"""Art for the ending (citizens, the mayor, the free city, the ceremony, the statue).

    python3 gen_ending_art.py <Resources/DataHeist>

People are drawn with gen_characters' skeleton renderer, so they match the runners exactly:
  Ending/npc/<id>_<anim>_NN     citizens c1..c8, kids k1..k2, mayor  (walk, idle, wave, cheer, clap, talk, present)
  Ending/npc/mayor_portrait
  Characters/<runner>/<runner>_{walk,wave,shake}_NN   extra hero poses for the ending
  Ending/statue_<runner>        bronze statue of each runner (win pose)
Props (bottom-centre pivot): shops (closed/open), lamps (red/warm), billboards (oppressive/free),
evidence screens, tram, flying cars, bunting, string lights, stage, podium, award banner, medal,
statue pedestal and its cloth, sidewalk and road tiles.
"""
import math
import os
import random
import sys
from PIL import Image
from pixel import Canvas, hexc, mix
from gen_env import glowify, save, CYAN, PINK, WHITE, RED, YELLOW, GREEN, ORANGE
import gen_characters as gc

GOLD, GOLD_D, GOLD_L = hexc('#e8b33a'), hexc('#a8761c'), hexc('#fff0a0')
NIGHT = hexc('#0b0618')
INK = hexc('#05030b')

# --------------------------------------------------------------------------- 5x7 pixel font
FONT = {
    'A': ['01110', '10001', '10001', '11111', '10001', '10001', '10001'],
    'B': ['11110', '10001', '10001', '11110', '10001', '10001', '11110'],
    'C': ['01111', '10000', '10000', '10000', '10000', '10000', '01111'],
    'D': ['11110', '10001', '10001', '10001', '10001', '10001', '11110'],
    'E': ['11111', '10000', '10000', '11110', '10000', '10000', '11111'],
    'F': ['11111', '10000', '10000', '11110', '10000', '10000', '10000'],
    'G': ['01111', '10000', '10000', '10011', '10001', '10001', '01111'],
    'H': ['10001', '10001', '10001', '11111', '10001', '10001', '10001'],
    'I': ['11111', '00100', '00100', '00100', '00100', '00100', '11111'],
    'K': ['10001', '10010', '10100', '11000', '10100', '10010', '10001'],
    'L': ['10000', '10000', '10000', '10000', '10000', '10000', '11111'],
    'M': ['10001', '11011', '10101', '10101', '10001', '10001', '10001'],
    'N': ['10001', '11001', '10101', '10011', '10001', '10001', '10001'],
    'O': ['01110', '10001', '10001', '10001', '10001', '10001', '01110'],
    'P': ['11110', '10001', '10001', '11110', '10000', '10000', '10000'],
    'R': ['11110', '10001', '10001', '11110', '10100', '10010', '10001'],
    'S': ['01111', '10000', '10000', '01110', '00001', '00001', '11110'],
    'T': ['11111', '00100', '00100', '00100', '00100', '00100', '00100'],
    'U': ['10001', '10001', '10001', '10001', '10001', '10001', '01110'],
    'V': ['10001', '10001', '10001', '10001', '10001', '01010', '00100'],
    'W': ['10001', '10001', '10001', '10101', '10101', '11011', '10001'],
    'Y': ['10001', '10001', '01010', '00100', '00100', '00100', '00100'],
    '/': ['00001', '00010', '00010', '00100', '01000', '01000', '10000'],
    '!': ['00100', '00100', '00100', '00100', '00100', '00000', '00100'],
    '.': ['00000', '00000', '00000', '00000', '00000', '00000', '00100'],
    ',': ['00000', '00000', '00000', '00000', '00000', '00100', '01000'],
    ' ': ['00000'] * 7,
}


def text_width(s, k=1):
    return len(s) * 6 * k - k


def draw_text(cv, s, x, y, col, k=1):
    for ch in s.upper():
        g = FONT.get(ch, FONT[' '])
        for row, bits in enumerate(g):
            for col_i, b in enumerate(bits):
                if b == '1':
                    cv.rect(x + col_i * k, y + row * k, x + col_i * k + k - 1, y + row * k + k - 1, col)
        x += 6 * k


def draw_text_c(cv, s, cx, y, col, k=1):
    draw_text(cv, s, cx - text_width(s, k) // 2, y, col, k)


# --------------------------------------------------------------------------- people
def person(skin, hair, top, pants, shoe, hair_style, build=1.0, trim=None, eye=None, coat=False):
    sd = mix(skin, INK, 0.25)
    return dict(
        skin=skin, skin_d=sd, hair=hair, hair_l=mix(hair, WHITE, 0.3), hair_d=mix(hair, INK, 0.35),
        top=top, top_d=mix(top, INK, 0.35), top_l=mix(top, WHITE, 0.35), trim=trim or mix(top, WHITE, 0.5),
        pants=pants, pants_d=mix(pants, INK, 0.35), shoe=shoe, shoe_d=mix(shoe, INK, 0.35), sole=mix(shoe, WHITE, 0.4),
        glove=skin, eye=eye or hexc('#1a1420'), visor=False, weapon='none', wcol=top,
        streak=mix(hair, WHITE, 0.3), stripe=mix(pants, WHITE, 0.3), under=mix(top, INK, 0.5),
        metal=hexc('#c2cddc'), metal_d=hexc('#7d889c'), joint=top,
        torso_w=10 if build >= 1 else 9, hair_style=hair_style, build=build, coat=coat,
    )


SKINS = [hexc('#7a4a30'), hexc('#d39468'), hexc('#eab48e'), hexc('#5a3624'), hexc('#f2cba8'), hexc('#a8704c')]
NPCS = {
    'c1': person(SKINS[0], hexc('#1b1420'), hexc('#3a6ea8'), hexc('#2c2440'), hexc('#eef0fa'), 'hightop'),
    'c2': person(SKINS[1], hexc('#5a2a14'), hexc('#d6455f'), hexc('#2a2a3a'), hexc('#262632'), 'ponytail', 0.9, trim=YELLOW),
    'c3': person(SKINS[2], hexc('#c9c2b0'), hexc('#4b7a52'), hexc('#4a4d5e'), hexc('#3a2a20'), 'buzz', 1.08),
    'c4': person(SKINS[3], hexc('#14101c'), hexc('#e8a03a'), hexc('#221c30'), hexc('#ff3fa4'), 'bob', 0.92),
    'c5': person(SKINS[4], hexc('#e9c75c'), hexc('#7a5ad6'), hexc('#1e1c2c'), hexc('#eef0fa'), 'ponytail', 0.9, trim=CYAN),
    'c6': person(SKINS[5], hexc('#2a1a14'), hexc('#2a8f8a'), hexc('#3a3346'), hexc('#262632'), 'buzz', 1.0, coat=True),
    'c7': person(SKINS[1], hexc('#8a3a1a'), hexc('#eceefe'), hexc('#3a5a8a'), hexc('#d6455f'), 'bob', 0.9),
    'c8': person(SKINS[0], hexc('#3a2c40'), hexc('#ff7b1c'), hexc('#2c2440'), hexc('#2ff3ff'), 'hightop', 1.05),
    'k1': person(SKINS[2], hexc('#e9c75c'), hexc('#38b6ff'), hexc('#2a2a3a'), hexc('#ff3fa4'), 'bob', 0.8),
    'k2': person(SKINS[3], hexc('#14101c'), hexc('#7dff6a'), hexc('#3a3346'), hexc('#eef0fa'), 'hightop', 0.8),
    'mayor': person(SKINS[2], hexc('#b8b8c8'), hexc('#20284a'), hexc('#20284a'), hexc('#14101c'), 'buzz', 1.06, trim=GOLD),
}
NPCS['mayor']['wcol'] = GOLD


def pose_walk(i, n=6):
    p = 2 * math.pi * i / n
    def leg(ph):
        return 0.42 * math.sin(ph), 0.15 + 0.55 * max(0.0, math.cos(ph))
    return dict(lean=0.04, fl=leg(p), bl=leg(p + math.pi),
                fa=(-0.45 * math.sin(p) + 0.1, 0.5), ba=(0.45 * math.sin(p) + 0.1, 0.5), flutter=0.2 * math.sin(p), hold=True)


def pose_wave(i):
    return dict(lean=0.0, fl=(0.1, 0.05), bl=(-0.08, 0.05), fa=(2.75, 0.2 + 0.7 * i), ba=(-0.1, 0.3), hold=True, flutter=0.3)


def pose_cheer(i):
    return dict(lean=0.0, fl=(0.15, 0.1 + 0.5 * i), bl=(-0.12, 0.1 + 0.5 * i), fa=(2.9, 0.15), ba=(-2.9 + 0.2 * i, 0.15),
                hold=True, flutter=0.5 * i, bob=-1 - i, air=i == 1)


def pose_clap(i):
    return dict(lean=0.02, fl=(0.1, 0.05), bl=(-0.08, 0.05), fa=(1.3, 0.9 - 0.6 * i), ba=(0.9, 1.1 - 0.6 * i), hold=True)


def pose_talk(i):
    return dict(lean=0.02, fl=(0.1, 0.05), bl=(-0.08, 0.05), fa=(0.9 + 0.35 * i, 1.0), ba=(-0.1, 0.3), hold=True, bob=i)


def pose_present(i):
    return dict(lean=0.05, fl=(0.12, 0.05), bl=(-0.08, 0.05), fa=(1.45, 0.25), ba=(1.25, 0.35), hold=True, bob=i)


def pose_shake(i):
    return dict(lean=0.06, fl=(0.15, 0.05), bl=(-0.1, 0.05), fa=(1.5, 0.3 + 0.15 * i), ba=(-0.1, 0.3), hold=True, bob=i)


EXTRA_ANIMS = {
    'walk': (6, pose_walk), 'wave': (2, pose_wave), 'cheer': (2, pose_cheer), 'clap': (2, pose_clap),
    'talk': (2, pose_talk), 'present': (2, pose_present), 'shake': (2, pose_shake),
}


def render(cid, anim, i):
    n, fn = gc.ANIMS[anim]
    return gc.render(cid, anim, i) if anim != 'walk' else gc.render(cid, anim, i)


def bronze(img):
    """recolour a sprite into a cast-bronze statue: luminance mapped onto a bronze ramp"""
    out = img.copy().convert('RGBA')
    px = out.load()
    ramp = [hexc('#2a1a0c'), hexc('#6a4420'), hexc('#a8742c'), hexc('#e0b060'), hexc('#fff0c0')]
    for y in range(out.height):
        for x in range(out.width):
            r, g, b, a = px[x, y]
            if a == 0:
                continue
            l = (0.3 * r + 0.59 * g + 0.11 * b) / 255
            t = min(0.999, l * 1.15) * (len(ramp) - 1)
            c = mix(ramp[int(t)], ramp[int(t) + 1], t - int(t))
            px[x, y] = (c[0], c[1], c[2], a)
    return out


# --------------------------------------------------------------------------- city props
def shop(variant, open_):
    """96x80 storefront: shuttered and dark, or open with lit window, goods and sign"""
    rr = random.Random(10 + variant)
    body = [hexc('#2b1a4a'), hexc('#14304a'), hexc('#3a1a2e')][variant]
    cv = Canvas(96, 80)
    cv.rect(0, 0, 95, 79, body)
    cv.rect(0, 0, 95, 2, mix(body, WHITE, 0.15))
    # sign box
    names = [('RAMEN', PINK), ('TECH', CYAN), ('BOOKS', YELLOW)]
    name, col = names[variant]
    cv.rect(10, 6, 85, 20, INK)
    if open_:
        draw_text_c(cv, name, 48, 10, col)
        cv.rect(10, 6, 85, 6, col); cv.rect(10, 20, 85, 20, col)
    else:
        draw_text_c(cv, name, 48, 10, mix(col, INK, 0.75))
    # window + door
    if open_:
        cv.rect(8, 28, 60, 72, mix(hexc('#ffd88a'), body, 0.25))
        for k in range(5):
            gx = 12 + k * 10
            cv.rect(gx, 56, gx + 6, 66, rr.choice([PINK, CYAN, GREEN, ORANGE]))
        cv.rect(8, 28, 60, 29, WHITE)
        cv.rect(66, 34, 88, 79, mix(hexc('#ffd88a'), body, 0.4))
        cv.rect(66, 34, 88, 35, WHITE)
        # awning
        for x in range(0, 96, 8):
            cv.rect(x, 22, x + 3, 26, col)
            cv.rect(x + 4, 22, x + 7, 26, WHITE)
        return glowify(cv, mix(hexc('#ffd88a'), col, 0.3), 2, 70)
    # shutters
    cv.rect(6, 26, 89, 79, hexc('#3a3352'))
    for y in range(28, 79, 3):
        cv.rect(6, y, 89, y, hexc('#24203a'))
    draw_text_c(cv, 'CLOSED', 48, 44, RED)
    return cv


def lamp(warm):
    cv = Canvas(16, 72)
    cv.rect(7, 8, 8, 71, hexc('#2a2440'))
    cv.rect(3, 4, 12, 9, hexc('#3a3352'))
    c = hexc('#ffd88a') if warm else RED
    cv.rect(4, 9, 11, 11, c)
    return glowify(cv, c, 3, 150 if warm else 110)


def billboard(free):
    cv = Canvas(112, 72)
    cv.rect(52, 52, 59, 71, hexc('#2a2440'))
    cv.rect(0, 0, 111, 52, INK)
    if free:
        cv.rect(2, 2, 109, 50, hexc('#12203a'))
        for (x0, c) in ((4, PINK), (40, CYAN), (76, YELLOW)):
            cv.disk(x0 + 14, 14, 8, mix(c, hexc('#12203a'), 0.5))
        draw_text_c(cv, 'FREE CITY', 56, 18, WHITE, 2)
        draw_text_c(cv, 'THANK YOU RUNNERS', 56, 38, CYAN)
        cv.rect(0, 0, 111, 1, CYAN); cv.rect(0, 51, 111, 52, PINK)
        return glowify(cv, CYAN, 2, 80)
    cv.rect(2, 2, 109, 50, hexc('#2a0a12'))
    draw_text_c(cv, 'OBEY', 56, 8, RED, 3)
    draw_text_c(cv, 'HALCYON SEES ALL', 56, 36, mix(RED, WHITE, 0.4))
    cv.rect(0, 0, 111, 1, RED); cv.rect(0, 51, 111, 52, RED)
    return glowify(cv, RED, 2, 90)


def evidence_screen(frame):
    """128x72 building screen broadcasting the leaked Halcyon files"""
    rr = random.Random(frame * 7 + 3)
    cv = Canvas(128, 72)
    cv.rect(0, 0, 127, 71, INK)
    cv.rect(2, 2, 125, 69, hexc('#0a1830'))
    draw_text(cv, 'HALCYON FILES', 6, 6, CYAN)
    draw_text(cv, 'LEAKED', 86, 6, RED)
    for k in range(7):
        w = rr.randint(30, 70)
        cv.rect(6, 18 + k * 6, 6 + w, 19 + k * 6, mix(hexc('#0a1830'), WHITE, 0.55))
        if rr.random() < 0.35:
            cv.rect(6 + rr.randint(4, max(5, w - 12)), 18 + k * 6, 6 + w, 19 + k * 6, INK)   # redactions
    cv.rect(84, 22, 120, 60, mix(hexc('#0a1830'), WHITE, 0.15))
    cv.disk(102, 38, 9, mix(hexc('#0a1830'), WHITE, 0.4))     # a face, blurred
    cv.rect(94, 48, 110, 58, mix(hexc('#0a1830'), WHITE, 0.3))
    # red stamp slides in on the last frames
    if frame >= 2:
        sx = 70 - (3 - frame) * 10
        for d in range(2):
            cv.rect(sx - 2 + d, 48 + d, sx + 50 - d, 49 + d, RED)
            cv.rect(sx - 2 + d, 62 - d, sx + 50 - d, 63 - d, RED)
        draw_text(cv, 'GUILTY', sx + 6, 52, RED)
    return glowify(cv, CYAN, 2, 70)


def screen_off():
    cv = Canvas(128, 72)
    cv.rect(0, 0, 127, 71, INK)
    cv.rect(2, 2, 125, 69, hexc('#0b0916'))
    cv.line(10, 60, 40, 10, hexc('#1a1633'))
    return cv


def tram():
    cv = Canvas(160, 40)
    cv.rect(4, 6, 155, 33, hexc('#dfe6f5'))
    cv.rect(4, 6, 155, 8, WHITE)
    cv.poly([(140, 6), (159, 18), (159, 33), (140, 33)], hexc('#dfe6f5'))
    for x in range(12, 136, 18):
        cv.rect(x, 12, x + 12, 22, hexc('#ffd88a'))
    cv.rect(4, 26, 155, 28, CYAN)
    cv.rect(10, 34, 150, 37, hexc('#3a3352'))
    return glowify(cv, CYAN, 2, 70)


def car(variant):
    cv = Canvas(40, 14)
    body = [PINK, CYAN][variant]
    cv.ellipse(20, 7, 18, 5, mix(body, INK, 0.4))
    cv.ellipse(22, 5, 9, 3, mix(hexc('#ffd88a'), body, 0.3))
    cv.rect(2, 9, 6, 10, RED)
    cv.rect(34, 8, 38, 9, WHITE)
    return glowify(cv, body, 2, 90)


def bunting():
    cv = Canvas(64, 16)
    for x in range(64):
        y = int(2 + 3 * math.sin(math.pi * x / 63))
        cv.px(x, y, hexc('#6a5f8e'))
    cols = [PINK, CYAN, YELLOW, GREEN]
    for k, x in enumerate(range(2, 62, 10)):
        y = int(2 + 3 * math.sin(math.pi * x / 63))
        cv.poly([(x, y), (x + 7, y), (x + 3.5, y + 8)], cols[k % 4])
    return cv


def string_lights():
    cv = Canvas(64, 16)
    for x in range(64):
        y = int(1 + 5 * math.sin(math.pi * x / 63))
        cv.px(x, y, hexc('#3a3352'))
    cols = [hexc('#ffd88a'), PINK, CYAN, hexc('#ffd88a')]
    for k, x in enumerate(range(4, 62, 8)):
        y = int(1 + 5 * math.sin(math.pi * x / 63)) + 2
        cv.disk(x, y, 1.5, cols[k % 4])
    return glowify(cv, hexc('#ffd88a'), 2, 110)


def stage():
    cv = Canvas(224, 40)
    cv.rect(0, 6, 223, 39, hexc('#2b1a4a'))
    cv.rect(0, 4, 223, 7, GOLD)
    for x in range(0, 224, 16):
        cv.rect(x, 12, x + 7, 39, hexc('#24163e'))
    cv.rect(0, 36, 223, 39, hexc('#14101c'))
    for x in range(4, 224, 24):           # footlights
        cv.disk(x + 8, 9, 1.5, hexc('#ffd88a'))
    return glowify(cv, GOLD, 2, 60)


def podium():
    cv = Canvas(32, 40)
    cv.poly([(4, 8), (27, 8), (24, 39), (7, 39)], hexc('#20284a'))
    cv.rect(2, 4, 29, 8, hexc('#3a4a7a'))
    cv.rect(10, 16, 21, 27, GOLD)
    cv.rect(13, 19, 18, 24, hexc('#20284a'))
    cv.rect(15, 0, 16, 4, hexc('#6a5f8e'))
    return glowify(cv, GOLD, 1.5, 50)


def award_banner():
    cv = Canvas(208, 28)
    cv.rect(0, 0, 207, 27, hexc('#20284a'))
    cv.rect(0, 0, 207, 1, GOLD); cv.rect(0, 26, 207, 27, GOLD)
    draw_text_c(cv, 'GUARDIAN OF THE CITY', 104, 10, GOLD_L)
    for x in (6, 199):
        cv.disk(x, 13, 3, GOLD)
    return glowify(cv, GOLD, 2, 70)


def medal(frame):
    cv = Canvas(32, 48)
    # ribbon
    cv.poly([(9, 0), (15, 0), (17, 22), (12, 22)], CYAN)
    cv.poly([(17, 0), (23, 0), (20, 22), (15, 22)], PINK)
    # medal disc with a star and a moving glint
    cv.disk(16, 33, 12, GOLD_D)
    cv.disk(16, 33, 10, GOLD)
    for k in range(5):
        a = -math.pi / 2 + k * 2 * math.pi / 5
        cv.line(16, 33, 16 + math.cos(a) * 7, 33 + math.sin(a) * 7, GOLD_L, 2)
    gx = 8 + frame * 5
    cv.line(gx, 26, gx + 4, 40, WHITE)
    return glowify(cv, GOLD, 3, 140)


def pedestal():
    """104x56 plinth with an engraved gold plaque"""
    cv = Canvas(104, 56)
    cv.rect(6, 8, 97, 55, hexc('#3a3352'))
    cv.rect(0, 2, 103, 9, hexc('#4b4570'))
    cv.rect(0, 50, 103, 55, hexc('#2a2440'))
    cv.rect(12, 16, 91, 44, GOLD_D)
    cv.rect(14, 18, 89, 42, GOLD)
    draw_text_c(cv, 'GUARDIAN', 52, 21, hexc('#3a2410'))
    draw_text_c(cv, 'OF THE CITY', 52, 31, hexc('#3a2410'))
    return cv


def cloth():
    """ceremonial drape over the statue (112x152)"""
    cv = Canvas(112, 152)
    body, dark, light = hexc('#8a1a3a'), hexc('#5a0a22'), hexc('#c03a5a')
    cv.poly([(40, 0), (72, 0), (110, 151), (2, 151)], body)
    for x0 in (30, 52, 74):
        cv.line(56, 4, x0 - 10, 150, dark, 2)
    cv.line(56, 2, 90, 150, light)
    cv.rect(2, 144, 109, 151, GOLD)
    return cv


def sidewalk():
    cv = Canvas(32, 16)
    cv.rect(0, 0, 31, 15, hexc('#3a3352'))
    cv.rect(0, 0, 31, 1, hexc('#6a5f8e'))
    cv.rect(31, 0, 31, 15, hexc('#2a2440'))
    return cv


def road():
    cv = Canvas(32, 32)
    cv.rect(0, 0, 31, 31, hexc('#1a1626'))
    cv.rect(0, 0, 31, 3, hexc('#2a2440'))
    cv.rect(4, 16, 15, 17, mix(hexc('#ffd88a'), hexc('#1a1626'), 0.4))
    return cv


def main(root):
    for cid, cfg in NPCS.items():
        cfg.setdefault('name', 'Mayor' if cid == 'mayor' else 'Citizen ' + cid)
        cfg.setdefault('title', '')
        gc.CHARS[cid] = cfg
    for runner in ('kai', 'nova', 'jax', 'lin'):
        gc.CHARS[runner].setdefault('hair_d', mix(gc.CHARS[runner]['hair'], INK, 0.35))
    gc.ANIMS.update(EXTRA_ANIMS)
    npc_anims = ['walk', 'idle', 'wave', 'cheer', 'clap', 'talk']
    for cid in NPCS:
        anims = npc_anims + (['present', 'shake'] if cid == 'mayor' else [])
        for anim in anims:
            n = gc.ANIMS[anim][0]
            for i in range(n):
                save(gc.render(cid, anim, i), root, f'Ending/npc/{cid}_{anim}_{i:02d}.png')
    gc.portrait('mayor').save(os.path.join(root, 'Ending/npc/mayor_portrait.png'))
    for runner in ('kai', 'nova', 'jax', 'lin'):
        for anim in ('walk', 'wave', 'shake'):
            for i in range(gc.ANIMS[anim][0]):
                save(gc.render(runner, anim, i), root, f'Characters/{runner}/{runner}_{anim}_{i:02d}.png')
        bronze(gc.render(runner, 'win', 0).image()).save(os.path.join(root, f'Ending/statue_{runner}.png'))
    for v in range(3):
        save(shop(v, False), root, f'Ending/shop_closed_{v}.png')
        save(shop(v, True), root, f'Ending/shop_open_{v}.png')
    save(lamp(False), root, 'Ending/lamp_red.png')
    save(lamp(True), root, 'Ending/lamp_warm.png')
    save(billboard(False), root, 'Ending/ad_obey.png')
    save(billboard(True), root, 'Ending/ad_free.png')
    for k in range(4):
        save(evidence_screen(k), root, f'Ending/evidence_{k:02d}.png')
        save(medal(k), root, f'Ending/medal_{k:02d}.png')
    save(screen_off(), root, 'Ending/screen_off.png')
    save(tram(), root, 'Ending/tram.png')
    for v in range(2):
        save(car(v), root, f'Ending/car_{v}.png')
    save(bunting(), root, 'Ending/bunting.png')
    save(string_lights(), root, 'Ending/lights.png')
    save(stage(), root, 'Ending/stage.png')
    save(podium(), root, 'Ending/podium.png')
    save(award_banner(), root, 'Ending/banner.png')
    save(pedestal(), root, 'Ending/pedestal.png')
    save(cloth(), root, 'Ending/cloth.png')
    save(sidewalk(), root, 'Tiles/rooftop_street.png')
    save(road(), root, 'Ending/road.png')
    print('ending art ->', root)


if __name__ == '__main__':
    main(sys.argv[1])
