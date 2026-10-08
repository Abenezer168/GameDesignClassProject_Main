"""Procedural pixel-art generator for the four Data Heist runners.

Every frame is posed from a tiny 2D skeleton (hip, knees, feet, shoulder,
elbows, hands, head) and then dressed per character. Output: one PNG per
frame (48x48, feet on the bottom rows) plus a portrait per character.
"""
import json
import math
import os
import sys
from pixel import Canvas, hexc, mix, shift

W = H = 48
GROUND = 44          # lowest pixel row of the feet (outline goes on 47)
OUTLINE = hexc('#120a1a')
RIM_BACK = hexc('#ff4fd8')
RIM_FRONT = hexc('#7af6ff')

THIGH, SHIN, TORSO, UARM, FARM = 7, 7, 10, 5, 5


def d(a):
    """unit vector for an angle measured from 'straight down', +ve = forward (right)"""
    return math.sin(a), math.cos(a)


def up(a):
    """unit vector for an angle measured from 'straight up', +ve = forward"""
    return math.sin(a), -math.cos(a)


# ---------------------------------------------------------------------------
# Characters
# ---------------------------------------------------------------------------
CHARS = {
    'kai': dict(
        name='Kai Okafor', title='The Courier',
        skin=hexc('#7a4a30'), skin_d=hexc('#5a3220'),
        hair=hexc('#1b1420'), hair_l=hexc('#3a2c40'),
        top=hexc('#18c6ea'), top_d=hexc('#0d6f9e'), top_l=hexc('#9ff4ff'),
        trim=hexc('#ff8a2a'),
        pants=hexc('#2c2440'), pants_d=hexc('#1d172c'),
        shoe=hexc('#eef0fa'), shoe_d=hexc('#9aa0bc'), sole=hexc('#2ff3ff'),
        glove=hexc('#1b1420'),
        eye=hexc('#ff8a2a'), visor=True,
        weapon='baton', wcol=hexc('#2ff3ff'),
        torso_w=10, hair_style='hightop', build=1.0,
    ),
    'nova': dict(
        name='Nova Reyes', title='The Runner',
        skin=hexc('#d39468'), skin_d=hexc('#a86c48'),
        hair=hexc('#ff3fa4'), hair_l=hexc('#ff9ad2'), hair_d=hexc('#a8206e'),
        top=hexc('#eceefe'), top_d=hexc('#a9acd0'), top_l=hexc('#ffffff'),
        trim=hexc('#ff3fa4'),
        pants=hexc('#221c30'), pants_d=hexc('#15111f'), stripe=hexc('#ff3fa4'),
        shoe=hexc('#ff3fa4'), shoe_d=hexc('#a8206e'), sole=hexc('#ffffff'),
        glove=hexc('#d39468'),
        eye=hexc('#2a1430'), visor=False,
        weapon='katana', wcol=hexc('#ff4fd8'),
        torso_w=9, hair_style='ponytail', build=0.9,
    ),
    'jax': dict(
        name='Jax Mercer', title='The Breaker',
        skin=hexc('#eab48e'), skin_d=hexc('#bf8662'),
        hair=hexc('#e9c75c'), hair_l=hexc('#fff0a0'), hair_d=hexc('#a8862c'),
        top=hexc('#ff7b1c'), top_d=hexc('#b24f0a'), top_l=hexc('#ffc07a'),
        under=hexc('#2c2b3a'),
        trim=hexc('#ffd23a'),
        pants=hexc('#4a4d5e'), pants_d=hexc('#33354a'),
        shoe=hexc('#262632'), shoe_d=hexc('#16161e'), sole=hexc('#ff7b1c'),
        glove=hexc('#262632'),
        metal=hexc('#c2cddc'), metal_d=hexc('#7d889c'), joint=hexc('#38b6ff'),
        eye=hexc('#1a1420'), visor=False,
        weapon='fist', wcol=hexc('#38b6ff'),
        torso_w=12, hair_style='buzz', build=1.12,
    ),
    'lin': dict(
        name='Lin Sato', title='The Ghost',
        skin=hexc('#f2cba8'), skin_d=hexc('#cf9f7c'),
        hair=hexc('#1a1626'), hair_l=hexc('#3a3450'), streak=hexc('#2ff3c8'),
        top=hexc('#8a46ee'), top_d=hexc('#55279e'), top_l=hexc('#c59cff'),
        trim=hexc('#2ff3c8'),
        pants=hexc('#1e1c2c'), pants_d=hexc('#13121c'),
        shoe=hexc('#2a2438'), shoe_d=hexc('#191524'), sole=hexc('#2ff3c8'),
        glove=hexc('#1e1c2c'),
        eye=hexc('#7dff6a'), visor=True,
        weapon='holo', wcol=hexc('#7dff6a'),
        torso_w=9, hair_style='bob', build=0.92, coat=True,
    ),
}


# ---------------------------------------------------------------------------
# Poses
# ---------------------------------------------------------------------------
def pose_run(i, n=8):
    p = 2 * math.pi * i / n
    def leg(ph):
        th = 0.85 * math.sin(ph)
        k = 0.2 + 1.7 * max(0.0, math.cos(ph)) ** 1.1
        return th, k
    fl, bl = leg(p), leg(p + math.pi)
    fa = -1.1 * math.sin(p) + 0.3
    ba = 1.1 * math.sin(p) + 0.3
    return dict(lean=0.32, fl=fl, bl=bl,
                fa=(fa, 1.6), ba=(ba, 1.6), phase=p, flutter=math.sin(p * 2))


def pose_idle(i):
    b = [0, 0, 1, 1][i % 4]
    return dict(lean=0.03, fl=(0.12, 0.08), bl=(-0.1, 0.05), bob=b,
                fa=(0.15, 0.35), ba=(-0.1, 0.3), flutter=[0, .3, .6, .3][i % 4], hold=True)


def pose_jump(i):
    if i == 0:
        return dict(lean=0.15, fl=(1.05, 1.75), bl=(-0.25, 0.75), fa=(2.3, 0.5), ba=(-0.9, 0.6),
                    air=True, flutter=-0.6)
    return dict(lean=0.1, fl=(0.75, 1.2), bl=(-0.1, 1.0), fa=(1.8, 0.4), ba=(-1.2, 0.5),
                air=True, flutter=-0.2)


def pose_fall(i):
    if i == 0:
        return dict(lean=0.05, fl=(0.45, 0.35), bl=(-0.35, 0.9), fa=(1.4, 0.3), ba=(-1.6, 0.3),
                    air=True, flutter=0.8)
    return dict(lean=0.0, fl=(0.35, 0.25), bl=(-0.3, 0.7), fa=(1.2, 0.2), ba=(-1.8, 0.2),
                air=True, flutter=1.0)


def pose_slide(i):
    return dict(lean=-1.05 - 0.03 * i, fl=(1.5, 0.0), bl=(1.05, 1.6),
                fa=(0.6 + 0.1 * i, 0.9), ba=(-0.4, 0.2), slide=True, flutter=1.0)


def pose_attack(i):
    arms = [(-2.5, 0.9), (1.7, 0.1), (0.9, 0.1), (0.4, 0.6)]
    lean = [0.05, 0.35, 0.3, 0.2]
    run = pose_run([1, 2, 3, 4][i])
    run.update(lean=lean[i], fa=arms[i], weapon=i, ba=(-0.6, 1.2))
    return run


def pose_hurt(i):
    return dict(lean=-0.35 - 0.1 * i, fl=(0.5, 0.6), bl=(-0.1, 0.8),
                fa=(2.6, 0.4), ba=(-2.2, 0.5), air=True, flutter=1.0)


def pose_ability(i):
    return dict(lean=0.1, fl=(0.5, 1.0), bl=(-0.3, 0.5), fa=(1.6 + 0.2 * i, 0.0), ba=(-1.5, 0.0),
                air=False, flutter=0.5, glow=True)


def pose_win(i):
    return dict(lean=0.0, fl=(0.1, 0.05), bl=(-0.08, 0.05), fa=(3.05, 0.1 + 0.25 * i),
                ba=(-0.3, 0.6), hold=True, flutter=0.4 * i, bob=i)


ANIMS = {
    'idle': (4, pose_idle),
    'run': (8, pose_run),
    'jump': (2, pose_jump),
    'fall': (2, pose_fall),
    'slide': (2, pose_slide),
    'attack': (4, pose_attack),
    'hurt': (2, pose_hurt),
    'ability': (2, pose_ability),
    'win': (2, pose_win),
}


# ---------------------------------------------------------------------------
# Skeleton solve
# ---------------------------------------------------------------------------
def solve(pose, cfg):
    s = cfg['build']
    th, sh = THIGH, SHIN
    def leg_pts(hip, la):
        ta, k = la
        kx, ky = d(ta)
        knee = (hip[0] + kx * th, hip[1] + ky * th)
        sa = ta - k
        sx, sy = d(sa)
        foot = (knee[0] + sx * sh, knee[1] + sy * sh)
        return knee, foot, sa
    probe = (0.0, 0.0)
    _, ff, _ = leg_pts(probe, pose['fl'])
    _, bf, _ = leg_pts(probe, pose['bl'])
    low = max(ff[1], bf[1])
    if pose.get('slide'):
        hip_y = GROUND - 2.5
    elif pose.get('air'):
        hip_y = GROUND - low - 3
    else:
        hip_y = GROUND - low
    hip_y += pose.get('bob', 0)
    hip_x = 22 + (4 if pose.get('slide') else 0)
    hip = (hip_x, hip_y)
    fk, ff, fsa = leg_pts(hip, pose['fl'])
    bk, bf, bsa = leg_pts(hip, pose['bl'])
    tx, ty = up(pose['lean'])
    neck = (hip[0] + tx * TORSO * (1.0 if s <= 1 else 1.0), hip[1] + ty * TORSO)
    shoulder = (hip[0] + tx * (TORSO - 2), hip[1] + ty * (TORSO - 2))
    head = (neck[0] + tx * 5.2, neck[1] + ty * 5.2)
    def arm(a):
        ua, bend = a
        ux, uy = d(ua)
        el = (shoulder[0] + ux * UARM, shoulder[1] + uy * UARM)
        fa = ua + bend
        fx, fy = d(fa)
        hand = (el[0] + fx * FARM, el[1] + fy * FARM)
        return el, hand, fa
    fel, fh, ffa = arm(pose['fa'])
    bel, bh, bfa = arm(pose['ba'])
    return dict(hip=hip, fk=fk, ff=ff, fsa=fsa, bk=bk, bf=bf, bsa=bsa, neck=neck,
                shoulder=shoulder, head=head, fel=fel, fh=fh, ffa=ffa, bel=bel, bh=bh, bfa=bfa)


# ---------------------------------------------------------------------------
# Drawing
# ---------------------------------------------------------------------------
def shade(c, f=0.72):
    return (int(c[0] * f), int(c[1] * f), int(c[2] * f), c[3])


def draw_leg(cv, hip, knee, foot, sa, cfg, back):
    pc = cfg['pants_d'] if back else cfg['pants']
    sw = 6 if cfg['build'] > 1.05 else (5 if cfg['build'] >= 1.0 else 5)
    cv.line(hip[0], hip[1], knee[0], knee[1], pc, w=sw)
    cv.line(knee[0], knee[1], foot[0], foot[1], pc, w=sw - 1)
    if 'stripe' in cfg and not back:
        cv.line(hip[0] + 1, hip[1], knee[0] + 1, knee[1], cfg['stripe'], w=1)
        cv.line(knee[0] + 1, knee[1], foot[0] + 1, foot[1] - 2, cfg['stripe'], w=1)
    # boot: lower 3px of shin + a foot pointing forward perpendicular-ish to shin
    shoe = cfg['shoe_d'] if back else cfg['shoe']
    bx, by = math.sin(sa), math.cos(sa)
    top = (foot[0] - bx * 3, foot[1] - by * 3)
    big = cfg['build'] > 1.05
    cv.line(top[0], top[1], foot[0], foot[1], shoe, w=5 if big else 4)
    # toe direction = shin rotated +90deg (forward)
    fx, fy = math.cos(sa), -math.sin(sa)
    toe = (foot[0] + fx * (4 if big else 3.5), foot[1] + fy * (4 if big else 3.5))
    cv.line(foot[0], foot[1], toe[0], toe[1], shoe, w=4 if big else 3)
    # glowing sole
    sole = cfg['sole'] if not back else shade(cfg['sole'], .6)
    cv.px(foot[0] - bx * 0 + bx * 1, foot[1] + by * 1, sole)
    cv.px(toe[0] + bx, toe[1] + by, sole)


def draw_arm(cv, sh, el, hand, cfg, back, pose, front_metal=False):
    sleeve = cfg['top_d'] if back else cfg['top']
    if cfg['name'].startswith('Jax'):
        sleeve = cfg['under'] if back else cfg['metal']
        if back:
            sleeve = shade(cfg['skin'], .8)
    w = 5 if cfg['build'] > 1.05 else 4
    if not back:
        ol = shade(sleeve, .45)
        cv.line(sh[0], sh[1] + 1, el[0], el[1], ol, w=w + 1)
        cv.line(el[0], el[1], hand[0], hand[1], ol, w=w)
    cv.line(sh[0], sh[1], el[0], el[1], sleeve, w=w)
    fore = sleeve
    if cfg['name'].startswith('Jax') and not back:
        fore = cfg['metal']
    cv.line(el[0], el[1], hand[0], hand[1], fore, w=w - 1)
    if cfg['name'].startswith('Jax') and not back:
        cv.px(el[0], el[1], cfg['joint'])
        cv.line(sh[0], sh[1], el[0], el[1], cfg['metal_d'], w=1)
    # cuff
    cuffc = cfg['trim'] if not back else shade(cfg['trim'], .7)
    cx = el[0] + (hand[0] - el[0]) * 0.7
    cy = el[1] + (hand[1] - el[1]) * 0.7
    cv.px(cx, cy, cuffc)
    hc = cfg['glove'] if cfg['glove'] != cfg['skin'] else cfg['skin']
    if back:
        hc = shade(hc, .8)
    if cfg['name'].startswith('Jax') and not back:
        hc = cfg['metal']
    cv.disk(hand[0], hand[1], 2.0 if cfg['build'] > 1.05 else 1.5, hc)


def draw_torso(cv, P, cfg, pose):
    hip, neck = P['hip'], P['neck']
    w = cfg['torso_w']
    # main body
    cv.line(hip[0], hip[1] - 1, neck[0], neck[1] + 1, cfg['top'], w=w)
    lean = pose['lean']
    tx, ty = up(lean)
    nx, ny = ty * -1, tx  # perpendicular pointing "back"? (rotate)
    # back half shade: offset line toward the back
    bxo, byo = -math.cos(lean), -math.sin(lean)  # back direction
    for k in range(1, w // 2 + 1):
        if k >= w // 2 - 1:
            cv.line(hip[0] + bxo * k, hip[1] - 1 + byo * k, neck[0] + bxo * k, neck[1] + 1 + byo * k,
                    cfg['top_d'], w=1)
    # front highlight
    f = w // 2 - 1
    cv.line(hip[0] - bxo * f, hip[1] - 2 - byo * f, neck[0] - bxo * f, neck[1] + 2 - byo * f, cfg['top_l'], w=1)
    # belt
    cv.line(hip[0] + bxo * (w / 2 - .5), hip[1] + byo * (w / 2 - .5),
            hip[0] - bxo * (w / 2 - .5), hip[1] - byo * (w / 2 - .5), cfg['pants_d'], w=2)
    name = cfg['name']
    if name.startswith('Kai'):
        # orange chest stripe + collar
        mid = (hip[0] + (neck[0] - hip[0]) * .55, hip[1] + (neck[1] - hip[1]) * .55)
        cv.line(mid[0] + bxo * 3, mid[1] + byo * 3, mid[0] - bxo * 3, mid[1] - byo * 3, cfg['trim'], w=1)
        cv.line(neck[0] + bxo * 3, neck[1] + 1 + byo * 3, neck[0] - bxo * 2, neck[1] + 1 - byo * 2, cfg['trim'], w=2)
    if name.startswith('Nova'):
        # crop jacket: show midriff skin band near hip + pink zip
        low = (hip[0] + (neck[0] - hip[0]) * .2, hip[1] + (neck[1] - hip[1]) * .2)
        cv.line(low[0] + bxo * 3, low[1] + byo * 3, low[0] - bxo * 3, low[1] - byo * 3, cfg['pants'], w=2)
        cv.line(hip[0] - bxo * 3, hip[1] - 2 - byo * 3, neck[0] - bxo * 3, neck[1] + 2 - byo * 3, cfg['trim'], w=1)
        cv.line(neck[0] + bxo * 3, neck[1] + 1 + byo * 3, neck[0] - bxo * 2, neck[1] + 1 - byo * 2, cfg['trim'], w=2)
    if name.startswith('Jax'):
        # armored plates + yellow hazard strip
        mid = (hip[0] + (neck[0] - hip[0]) * .5, hip[1] + (neck[1] - hip[1]) * .5)
        cv.line(mid[0] + bxo * 4, mid[1] + byo * 4, mid[0] - bxo * 4, mid[1] - byo * 4, cfg['top_d'], w=1)
        hi = (hip[0] + (neck[0] - hip[0]) * .8, hip[1] + (neck[1] - hip[1]) * .8)
        cv.line(hi[0] + bxo * 4, hi[1] + byo * 4, hi[0] - bxo * 4, hi[1] - byo * 4, cfg['trim'], w=1)
        cv.line(neck[0] + bxo * 4, neck[1] + 2 + byo * 4, neck[0] - bxo * 3, neck[1] + 2 - byo * 3, cfg['under'], w=2)
    if name.startswith('Lin'):
        cv.line(hip[0] - bxo * 2, hip[1] - 1 - byo * 2, neck[0] - bxo * 2, neck[1] + 2 - byo * 2, cfg['trim'], w=1)


def draw_coat_tail(cv, P, cfg, pose):
    """Lin's long coat flaps behind the hips."""
    hip = P['hip']
    lean = pose['lean']
    fl = pose.get('flutter', 0)
    bxo, byo = -math.cos(lean), -math.sin(lean)
    tx, ty = up(lean)
    base1 = (hip[0] + bxo * 3 + tx * 2, hip[1] + byo * 3 + ty * 2)
    base2 = (hip[0] - bxo * 2, hip[1] - byo * 2)
    if pose.get('slide'):
        tip = (hip[0] - 9, hip[1] - 1)
        tip2 = (hip[0] - 7, hip[1] + 2)
    else:
        swing = 2.5 + 1.5 * fl
        tip = (hip[0] - 5 - swing, hip[1] + 9 - abs(fl) * 2)
        tip2 = (hip[0] - 1 - swing * .4, hip[1] + 10)
    cv.poly([base1, tip, tip2, base2], cfg['top_d'])
    cv.line(tip[0], tip[1], tip2[0], tip2[1], cfg['trim'], w=1)


def draw_head(cv, P, cfg, pose, phase_flutter):
    hx, hy = P['head']
    hx, hy = round(hx), round(hy)
    st = cfg['hair_style']
    if st == 'ponytail':
        f = phase_flutter
        base = (hx - 5, hy - 2)
        mid = (hx - 9, hy + 1 + f * 1.2)
        tip = (hx - 12, hy + 6 + f * 2)
        if pose.get('slide'):
            mid, tip = (hx - 7, hy + 4), (hx - 10, hy + 8)
        cv.line(base[0], base[1], mid[0], mid[1], cfg['hair'], w=4)
        cv.line(mid[0], mid[1], tip[0], tip[1], cfg['hair'], w=3)
        cv.px(tip[0] - 1, tip[1] + 1, cfg['hair_d'])
        cv.line(base[0], base[1] - 1.5, mid[0], mid[1] - 1.5, cfg['hair_l'], w=1)
        cv.line(mid[0], mid[1] + 1.5, tip[0], tip[1] + 1, cfg['hair_d'], w=1)
        cv.rect(base[0], base[1] - 1, base[0] + 1, base[1] + 1, cfg['trim'])
    if st == 'bob':
        cv.ellipse(hx - 1, hy + 0.5, 6, 6.2, cfg['hair'])
    # neck
    cv.line(P['neck'][0], P['neck'][1] + 1, hx - 1, hy + 4, cfg['skin_d'], w=4)
    # skull
    cv.ellipse(hx, hy, 5.0, 5.4, cfg['skin'])
    cv.rect(hx - 4, hy + 2, hx - 3, hy + 4, cfg['skin_d'])
    if st == 'hightop':
        cv.ellipse(hx - 1, hy - 4.5, 5.2, 4.2, cfg['hair'])
        cv.rect(hx - 6, hy - 4, hx - 3, hy + 2, cfg['hair'])
        cv.rect(hx + 2, hy - 5, hx + 4, hy - 4, cfg['hair'])
        for (x, y) in [(-4, -7), (-2, -8), (0, -8), (2, -7), (-5, -5), (-1, -6), (1, -5)]:
            cv.px(hx + x, hy + y, cfg['hair_l'])
        for (x, y) in [(-3, -9), (-1, -9), (1, -9)]:
            cv.px(hx + x, hy + y, cfg['hair'])
        cv.line(hx - 3, hy + 4, hx + 3, hy + 5, cfg['hair'], w=1)   # chin strap beard
        cv.px(hx + 4, hy + 4, cfg['hair'])
    elif st == 'ponytail':
        cv.ellipse(hx - 0.5, hy - 3, 5.6, 3.6, cfg['hair'])
        cv.rect(hx - 6, hy - 3, hx - 3, hy + 2, cfg['hair'])
        cv.line(hx - 4, hy - 6, hx + 2, hy - 6, cfg['hair_l'])
        cv.rect(hx + 3, hy - 3, hx + 5, hy - 2, cfg['hair'])   # bangs
        cv.px(hx + 5, hy - 1, cfg['hair_d'])
        cv.px(hx - 3, hy + 3, cfg['trim'])                      # earring
    elif st == 'buzz':
        cv.ellipse(hx - 0.5, hy - 3.4, 5.3, 3.0, cfg['hair'])
        cv.rect(hx - 6, hy - 3, hx - 4, hy, cfg['hair'])
        cv.line(hx - 3, hy - 6, hx + 2, hy - 6, cfg['hair_l'])
        cv.line(hx - 3, hy + 3, hx + 4, hy + 4, cfg['hair_d'], w=2)   # beard
        cv.rect(hx + 4, hy + 1, hx + 5, hy + 3, cfg['hair_d'])
        cv.px(hx - 2, hy + 1, cfg['hair_d'])
        cv.line(hx + 1, hy - 1, hx + 3, hy - 3, hexc('#c0485a'))       # scar
    elif st == 'bob':
        cv.ellipse(hx - 0.5, hy - 3, 5.8, 3.8, cfg['hair'])
        cv.rect(hx - 7, hy - 2, hx - 2, hy + 4, cfg['hair'])
        cv.rect(hx + 2, hy - 4, hx + 5, hy - 2, cfg['hair'])
        cv.line(hx - 5, hy - 5, hx - 1, hy - 6, cfg['streak'], w=1)
        cv.line(hx - 6, hy - 1, hx - 6, hy + 3, cfg['streak'], w=1)
        cv.px(hx - 2, hy - 7, cfg['hair_l'])
    ey = hy - 1
    if cfg['visor']:
        cv.rect(hx - 1, ey, hx + 5, ey + 1, cfg['eye'])
        cv.px(hx + 4, ey, mix(cfg['eye'], hexc('#ffffff'), .7))
        cv.px(hx - 2, ey, shade(cfg['eye'], .6))
    else:
        cv.rect(hx + 3, ey, hx + 3, ey + 1, cfg['eye'])
        cv.px(hx + 4, ey, hexc('#ffffff'))
        brow = cfg.get('hair_d', shade(cfg['hair'], .8))
        cv.line(hx + 2, ey - 2, hx + 4, ey - 2, brow)
    cv.px(hx + 5, hy + 1, cfg['skin_d'])   # nose
    cv.px(hx + 4, hy + 3, shade(cfg['skin'], .8))   # mouth
    cv.rect(hx - 2, hy, hx - 1, hy + 1, cfg['skin_d'])   # ear


def draw_weapon(cv, P, cfg, pose):
    stage = pose.get('weapon')
    if stage is None:
        return
    hand = P['fh']
    a = P['ffa']
    dx, dy = d(a)
    wc = cfg['wcol']
    core = mix(wc, hexc('#ffffff'), .65)
    kind = cfg['weapon']
    if kind == 'fist':
        r = 2.6 if stage == 1 else 2.0
        cv.disk(hand[0], hand[1], r + 1, (wc[0], wc[1], wc[2], 150))
        cv.disk(hand[0], hand[1], r, cfg['metal'])
        cv.px(hand[0], hand[1], core)
        if stage in (1, 2):
            for k in range(3):
                cv.px(hand[0] + dx * (4 + k * 2), hand[1] + dy * (4 + k * 2) + (k % 2) - .5, wc)
        return
    length = {'baton': 9, 'katana': 13, 'holo': 11}[kind]
    if stage == 0:
        length -= 2
    tip = (hand[0] + dx * length, hand[1] + dy * length)
    hilt = (hand[0] - dx * 2, hand[1] - dy * 2)
    cv.line(hilt[0], hilt[1], hand[0], hand[1], hexc('#3a3448'), w=2)
    cv.line(hand[0], hand[1], tip[0], tip[1], (wc[0], wc[1], wc[2], 140), w=3 if kind != 'katana' else 2)
    cv.line(hand[0], hand[1], tip[0], tip[1], core, w=1)


def render(cid, anim, i):
    cfg = CHARS[cid]
    n, fn = ANIMS[anim]
    pose = fn(i) if anim != 'run' else fn(i, n)
    P = solve(pose, cfg)
    cv = Canvas(W, H)
    fl = pose.get('flutter', 0)
    # back layers
    draw_arm(cv, P['shoulder'], P['bel'], P['bh'], cfg, True, pose)
    draw_leg(cv, P['hip'], P['bk'], P['bf'], P['bsa'], cfg, True)
    if cfg.get('coat'):
        draw_coat_tail(cv, P, cfg, pose)
    draw_torso(cv, P, cfg, pose)
    draw_leg(cv, P['hip'], P['fk'], P['ff'], P['fsa'], cfg, False)
    draw_head(cv, P, cfg, pose, fl)
    draw_weapon(cv, P, cfg, pose) if pose.get('weapon') == 0 else None
    draw_arm(cv, P['shoulder'], P['fel'], P['fh'], cfg, False, pose)
    if pose.get('weapon') not in (None, 0):
        draw_weapon(cv, P, cfg, pose)
    # Jax shoulder pad
    if cid == 'jax':
        s = P['shoulder']
        cv.ellipse(s[0], s[1] + 0.5, 2.6, 2.0, cfg['top'])
        cv.px(s[0], s[1] - 1, cfg['top_l'])
    # Lin hood bump behind neck
    if cid == 'lin':
        n_ = P['neck']
        cv.ellipse(n_[0] - 3, n_[1] - 1, 2.5, 2, cfg['top_d'])
    cv.rim(RIM_BACK, side=-1, strength=0.38)
    cv.rim(RIM_FRONT, side=1, strength=0.18)
    cv.outline(OUTLINE)
    if pose.get('glow'):
        add_glow(cv, cfg['wcol'])
    return cv


def add_glow(cv, col):
    import numpy as np
    al = cv.a[:, :, 3] > 0
    ring = np.zeros_like(al)
    for dy in (-1, 0, 1):
        for dx in (-1, 0, 1):
            ring |= shift(al, dy, dx)
    ring &= ~al
    cv.a[ring] = (col[0], col[1], col[2], 170)


def portrait(cid):
    """96x96 bust: idle frame upper body scaled x3 over a framed backdrop."""
    from PIL import Image, ImageDraw
    cfg = CHARS[cid]
    fr = render(cid, 'idle', 0).image()
    hx, hy = solve(pose_idle(0), cfg)['head']
    box = (int(hx) - 12, int(hy) - 11, int(hx) + 12, int(hy) + 13)
    crop = fr.crop(box).resize((72, 72), Image.NEAREST)
    bg = Image.new('RGBA', (80, 80), (0, 0, 0, 0))
    dr = ImageDraw.Draw(bg)
    wc = cfg['wcol']
    for y in range(80):
        t = y / 79
        c = mix(hexc('#1a1030'), hexc('#3a1a56'), t)
        dr.line([(0, y), (79, y)], fill=c)
    for y in range(0, 80, 4):
        dr.line([(0, y), (79, y)], fill=(255, 255, 255, 10))
    dr.ellipse([14, 18, 66, 70], fill=(wc[0], wc[1], wc[2], 70))
    bg.alpha_composite(crop, (4, 8))
    dr.rectangle([0, 0, 79, 79], outline=wc[:3] + (255,), width=2)
    return bg


def main(out_root, preview_dir):
    os.makedirs(preview_dir, exist_ok=True)
    meta = {}
    from PIL import Image
    for cid in CHARS:
        cdir = os.path.join(out_root, cid)
        os.makedirs(cdir, exist_ok=True)
        meta[cid] = {}
        strips = []
        for anim, (n, _) in ANIMS.items():
            frames = []
            for i in range(n):
                cv = render(cid, anim, i)
                p = os.path.join(cdir, f'{cid}_{anim}_{i:02d}.png')
                cv.save(p)
                frames.append(cv.image())
            meta[cid][anim] = n
            strip = Image.new('RGBA', (W * 8, H), (0, 0, 0, 0))
            for i, f in enumerate(frames):
                strip.alpha_composite(f, (i * W, 0))
            strips.append(strip)
        portrait(cid).save(os.path.join(cdir, f'{cid}_portrait.png'))
        sheet = Image.new('RGBA', (W * 8, H * len(strips)), (40, 24, 60, 255))
        for k, s in enumerate(strips):
            sheet.alpha_composite(s, (0, k * H))
        sheet.resize((sheet.width * 3, sheet.height * 3), Image.NEAREST).save(
            os.path.join(preview_dir, f'sheet_{cid}.png'))
    # line-up preview
    lu = Image.new('RGBA', (W * 4 * 4 + 80 * 4, 96 * 2 + 20), (26, 16, 44, 255))
    for k, cid in enumerate(CHARS):
        lu.alpha_composite(render(cid, 'idle', 0).image(4), (k * W * 4, 0))
    lu.save(os.path.join(preview_dir, 'lineup.png'))
    pl = Image.new('RGBA', (80 * 4 + 30, 80), (10, 6, 20, 255))
    for k, cid in enumerate(CHARS):
        pl.alpha_composite(portrait(cid), (k * 90, 0))
    pl.resize((pl.width * 2, pl.height * 2), Image.NEAREST).save(os.path.join(preview_dir, 'portraits.png'))
    return meta


if __name__ == '__main__':
    out = sys.argv[1]
    prev = sys.argv[2]
    m = main(out, prev)
    print(json.dumps(m))
