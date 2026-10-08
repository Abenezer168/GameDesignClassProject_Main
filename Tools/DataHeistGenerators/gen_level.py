"""Authoring script for Level 1 - "Neon Rooftops".

Writes level01.json that BOTH the Unity build (LevelBuilder.cs) and the
browser prototype read, so the two stay in sync. Units are world units
(1 unit = 32 px). y = 0 is the default rooftop height.
"""
import json
import math
import random
import sys

PHYS = dict(
    runSpeed=7.5, gravity=55.0, jumpVelocity=15.5, jumpCutMultiplier=0.45,
    coyoteTime=0.10, jumpBuffer=0.12, slideMin=0.45, slideMax=1.1,
    padVelocity=23.0, stompBounce=13.0, maxFall=26.0,
    standHeight=1.1, slideHeight=0.5, width=0.55,
)

roofs, platforms, ents, prompts, zones = [], [], [], [], []


def roof(x0, x1, y=0, style='violet'):
    roofs.append(dict(x0=x0, x1=x1, y=y, style=style))


def ground(x):
    for r in roofs:
        if r['x0'] <= x <= r['x1']:
            return r['y']
    for p in platforms:
        if p['x0'] <= x <= p['x1']:
            return p['y']
    return 0


def e(t, x, dy=0.0, **kw):
    d = dict(t=t, x=round(x, 3), y=round(ground(x) + dy, 3))
    d.update(kw)
    ents.append(d)
    return d


def byte_line(x0, x1, dy=0.6, step=1.0):
    n = int(round((x1 - x0) / step))
    for i in range(n + 1):
        e('byte', x0 + i * step, dy)


def byte_arc(x_takeoff, n=7, speed=PHYS['runSpeed'], v=PHYS['jumpVelocity'], g=PHYS['gravity'], base_dy=0.6, y0=None):
    """bytes along the jump parabola that starts at x_takeoff -> teaches timing"""
    T = 2 * v / g
    gy = ground(x_takeoff) if y0 is None else y0
    for i in range(1, n + 1):
        t = T * i / (n + 1)
        x = x_takeoff + speed * t
        y = gy + v * t - 0.5 * g * t * t + base_dy
        ents.append(dict(t='byte', x=round(x, 3), y=round(y, 3)))


def prompt(x, key, title, body, ability=False):
    prompts.append(dict(x=x, key=key, title=title, body=body, ability=ability))


# ----------------------------------------------------------------------------
# SECTION 0 - Wake-up run & JUMP
# ----------------------------------------------------------------------------
roof(-12, 60, 0, 'violet')
byte_line(4, 12)
prompt(14, 'SPACE', 'JUMP', 'Tap to hop. Hold for a higher jump.')
e('can', 22)
byte_arc(22 - 2.1)
e('can', 31)
byte_arc(31 - 2.1)
e('can', 40); e('can', 40.9)
byte_arc(40.45 - 2.1, n=8)
byte_line(46, 54)

# ----------------------------------------------------------------------------
# SECTION 1 - Gaps & step-ups
# ----------------------------------------------------------------------------
prompt(52, 'SPACE', 'MIND THE GAP', 'Jump between rooftops. Falling ends the run.')
roof(62.5, 108, 0, 'teal')
byte_arc(60 - 0.6)
e('can', 74)
byte_arc(74 - 2.1)
byte_line(82, 90, 0.6, 1)
e('can', 96); e('can', 99)
roof(110, 150, 1, 'brick')            # step up
byte_arc(108 - 0.7, y0=0)
e('can', 124, 0)
byte_arc(124 - 2.1)
e('repair', 136, 0.7)
roof(153, 215, 0, 'violet')           # drop down
byte_arc(150 - 0.5, y0=1)

# ----------------------------------------------------------------------------
# SECTION 2 - SLIDE
# ----------------------------------------------------------------------------
prompt(158, 'S', 'SLIDE', 'Slide under billboards. Hold to slide longer.')
e('barrier', 168)
byte_line(165, 171, 0.35, 0.75)
e('barrier', 180)
byte_line(177, 183, 0.35, 0.75)
e('barrier', 191)
e('can', 197)
byte_arc(197 - 2.1)
e('pad', 205)
byte_arc(205, n=11, v=PHYS['padVelocity'], base_dy=0.5)
roof(217.5, 292, 0, 'teal')
byte_arc(215 - 0.6)

# ----------------------------------------------------------------------------
# SECTION 3 - ATTACK (meet the drones)
# ----------------------------------------------------------------------------
prompt(222, 'LMB', 'ATTACK', 'Click to slash Security Drones. Land on one to stomp it.')
e('drone', 233, 1.0, kind='scout', move='bob')
byte_line(236, 240)
e('drone', 245, 1.0, kind='scout', move='bob')
e('drone', 255, 1.0, kind='scout', move='bob')
e('drone', 258, 1.0, kind='scout', move='bob')
e('drone', 268, 0.45, kind='scout', move='still')
byte_line(270, 276)
e('checkpoint', 280)
prompt(284, 'W', 'ABILITY', '', ability=True)

# ----------------------------------------------------------------------------
# SECTION 4 - Mixed sequences
# ----------------------------------------------------------------------------
roof(295, 350, 0, 'violet')
byte_arc(292 - 0.6)
e('can', 302)
e('barrier', 311)
byte_line(308, 314, 0.35, 0.75)
e('drone', 320, 1.0, kind='scout', move='bob')
e('can', 327)
byte_arc(327 - 2.1)
e('drone', 336, 0.45, kind='scout', move='still')
byte_line(339, 346)
# 3.5 gap, drop down 1
roof(353.5, 400, -1, 'brick')
byte_arc(350 - 0.4, n=8, y0=0)
e('gunner', 372, 1.6, kind='gunner', move='bob')
byte_line(358, 364)
e('can', 378)
e('barrier', 386)
byte_line(383, 389, 0.35, 0.75)
e('repair', 394, 0.7)
# step back up across a 2.5 gap
roof(402.5, 470, 0, 'teal')
byte_arc(400 - 0.5, y0=-1)
e('pad', 408)
# jump pad launches onto a neon catwalk high above the street
platforms.append(dict(x0=410.5, x1=441, y=4.2))
byte_line(414, 421, 4.8, 1.0)
ents.append(dict(t='drone', x=425.0, y=5.2, kind='scout', move='bob'))
ents.append(dict(t='can', x=431.0, y=4.2))
byte_arc(431 - 2.1, y0=4.2)
byte_line(434, 440, 4.8, 1.0)
byte_arc(441 - 0.2, n=9, y0=4.2, base_dy=0.6)
e('can', 450)
byte_arc(444 - 2.1)
e('checkpoint', 452)

# ----------------------------------------------------------------------------
# SECTION 5 - SECURITY SWEEP finale
# ----------------------------------------------------------------------------
zones.append(dict(x0=456, x1=560, speed=1.12, alarm=True))
prompt(456, '!', 'SECURITY SWEEP', 'Corporate security is onto you. Run!')
e('laser', 463, 0.0, h=0.65, mode='low')
byte_arc(463 - 2.2)
roof(473, 540, 1, 'violet')
byte_arc(470 - 0.6, y0=0)
e('laser', 481, 0.75, h=3.0, mode='high')
byte_line(478, 484, 1.35, 0.75)
e('drone', 489, 1.0, kind='scout', move='bob')
e('can', 496)
byte_arc(496 - 2.1)
e('gunner', 511, 1.7, kind='gunner', move='bob')
e('barrier', 516)
byte_line(513, 519, 0.35, 0.75)
e('drone', 523, 0.45, kind='scout', move='still')
e('laser', 530, 0.0, h=0.65, mode='low')
byte_arc(530 - 2.2)
e('drone', 536, 1.0, kind='scout', move='bob')
roof(542.5, 640, 0, 'teal')
byte_arc(540 - 0.6, y0=1)
byte_line(548, 560)

# ----------------------------------------------------------------------------
# EXTRACTION
# ----------------------------------------------------------------------------
e('shard', 568, 1.0)
e('exit', 580)

# ----------------------------------------------------------------------------
# Decor (seeded, purely cosmetic)
# ----------------------------------------------------------------------------
decor = []
rr = random.Random(1337)
blocked = sorted(x['x'] for x in ents if x['t'] not in ('byte',))
for r in roofs:
    x = r['x0'] + 2
    while x < r['x1'] - 2:
        kind = rr.choice(['ac_unit', 'antenna', 'water_tank', 'vent', 'sign_pink', 'sign_cyan', 'vent', 'ac_unit'])
        decor.append(dict(k=kind, x=round(x, 2), y=r['y'], layer=-1))
        x += rr.uniform(5, 11)

level = dict(
    id='level01', name='NEON ROOFTOPS', subtitle='Sector 7 - Shard 1 of 5',
    physics=PHYS, killY=-9.0, spawnX=0.0, exitX=580.0,
    roofs=roofs, platforms=platforms, entities=ents, prompts=prompts,
    zones=zones, decor=decor,
    parTime=82.0,
)

if __name__ == '__main__':
    out = sys.argv[1]
    with open(out, 'w') as f:
        json.dump(level, f, indent=1)
    nb = sum(1 for x in ents if x['t'] == 'byte')
    print('bytes', nb, 'entities', len(ents), 'length', level['exitX'])
