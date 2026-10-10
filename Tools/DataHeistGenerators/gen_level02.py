"""Authoring script for Level 2 - "Corporate District" (a copy of gen_level.py's toolkit).

Writes level02.json for the Unity build. Units are world units (1 unit = 32 px), y = 0 is the
default rooftop height. New hazards, each taught on its own before they are combined:
  shock  - electrified floor strip; live 0.9 s of every 1.5 s once the runner is 14 units away
  sign   - Halcyon billboard; shakes at 10.5 units, falls, and lands as a low wreck to jump or slash
  sniper - penthouse window; red laser sight for 0.9 s, then a fast round at chest height
Validate with Data Heist > Validate Level 2 (Playtest Bot) after every change.
"""
import json
import random
import sys

PHYS = dict(
    runSpeed=7.5, gravity=55.0, jumpVelocity=15.5, jumpCutMultiplier=0.45,
    coyoteTime=0.10, jumpBuffer=0.12, slideMin=0.45, slideMax=1.1,
    padVelocity=23.0, stompBounce=13.0, maxFall=26.0,
    standHeight=1.1, slideHeight=0.5, width=0.55,
)

roofs, platforms, ents, prompts, zones = [], [], [], [], []


def roof(x0, x1, y=0, style='glass'):
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


# --- Level 2 hazards ---------------------------------------------------------
def shock(x0, w):
    """live floor strip from x0, w units long; jump it (or cross while it is off)"""
    return e('shock', x0, w=w)


def sign(x, hang=4.2):
    """hanging billboard centred on x; drops as the runner closes in"""
    return e('sign', x, hang)


def sniper(x):
    """sniper nest at x on the same roof the runner is on (fires along chest height)"""
    return e('sniper', x)


# ----------------------------------------------------------------------------
# SECTION 0 - Lobby run: the glass towers, a familiar warm-up
# ----------------------------------------------------------------------------
roof(-12, 70)
prompt(8, '!', 'CORPORATE DISTRICT', "Halcyon's home turf. Their security is smarter here.")
byte_line(4, 14)
e('can', 24)
byte_arc(24 - 2.1)
e('can', 36); e('can', 36.9)
byte_arc(36.45 - 2.1, n=8)
byte_line(46, 60)

# ----------------------------------------------------------------------------
# SECTION 1 - LIVE WIRES (electrified floors)
# ----------------------------------------------------------------------------
roof(73, 175)
byte_arc(70 - 0.6)
prompt(82, 'SPACE', 'LIVE WIRES', 'Electrified floors pulse on and off. Jump them.')
shock(94, 2.5)
byte_arc(94 - 1.0)
shock(112, 3.0)
byte_arc(112 - 0.8)
byte_line(120, 126)
shock(132, 2.0)
e('can', 140)
byte_arc(140 - 2.1)
shock(152, 2.5)
byte_arc(152 - 1.0)
e('repair', 166, 0.7)

# ----------------------------------------------------------------------------
# SECTION 2 - FALLING SIGNS (step up one floor)
# ----------------------------------------------------------------------------
roof(177, 282, 1, 'violet')
byte_arc(175 - 0.7, y0=0)
prompt(186, 'S', 'FALLING SIGNS', 'Billboards drop as you get close. Jump the wreck or slash it.')
sign(202)
byte_line(196, 200)
sign(222)
byte_line(226, 232)
sign(244)
e('drone', 256, 1.0, kind='scout', move='bob')
byte_line(259, 264)
e('checkpoint', 270)

# ----------------------------------------------------------------------------
# SECTION 3 - SNIPERS (drop back down)
# ----------------------------------------------------------------------------
roof(285, 402)
byte_arc(282 - 0.5, y0=1)
prompt(292, 'S', 'SNIPER', 'A red laser marks the shot. Slide under it, jump it or slash the round.')
sniper(318)
byte_line(296, 304, 0.35, 0.75)
byte_line(322, 330)
sniper(352)
e('can', 360)
byte_arc(360 - 2.1)
shock(368, 2.5)
byte_arc(368 - 1.0)
sniper(392)

# ----------------------------------------------------------------------------
# SECTION 4 - MIXED: catwalk over the plaza, then everything together
# ----------------------------------------------------------------------------
roof(405, 524)
byte_arc(402 - 0.5)
e('pad', 412)
platforms.append(dict(x0=414.5, x1=442, y=4.2))
byte_line(418, 426, 4.8, 1.0)
ents.append(dict(t='drone', x=431.0, y=5.2, kind='scout', move='bob'))
byte_line(434, 440, 4.8, 1.0)
byte_arc(442 - 0.2, n=9, y0=4.2, base_dy=0.6)
shock(456, 3.0)
byte_arc(456 - 0.8)
sniper(482)
sign(498)
e('gunner', 512, 1.6, kind='gunner', move='bob')
e('repair', 504, 0.7)
e('checkpoint', 518)

# ----------------------------------------------------------------------------
# SECTION 5 - LOCKDOWN: alarm zone finale
# ----------------------------------------------------------------------------
roof(527, 700)
byte_arc(524 - 0.5)
zones.append(dict(x0=530, x1=630, speed=1.12, alarm=True))
prompt(531, '!', 'LOCKDOWN', 'Halcyon locked the district down. Run!')
sniper(556)
shock(566, 2.5)
byte_arc(566 - 1.0)
sign(584)
e('laser', 596, 0.0, h=0.65, mode='low')
byte_arc(596 - 2.2)
sniper(622)
byte_line(606, 612)

# ----------------------------------------------------------------------------
# EXTRACTION
# ----------------------------------------------------------------------------
e('shard', 632, 1.0)
e('exit', 644)

# ----------------------------------------------------------------------------
# Decor (seeded, purely cosmetic)
# ----------------------------------------------------------------------------
decor = []
rr = random.Random(2077)
for r in roofs:
    x = r['x0'] + 2
    while x < r['x1'] - 2:
        kind = rr.choice(['ac_unit', 'antenna', 'vent', 'sign_cyan', 'water_tank', 'vent', 'ac_unit', 'sign_pink'])
        decor.append(dict(k=kind, x=round(x, 2), y=r['y'], layer=-1))
        x += rr.uniform(6, 12)

level = dict(
    id='level02', name='CORPORATE DISTRICT', subtitle='Sector 9 - Shard 2 of 5',
    physics=PHYS, killY=-9.0, spawnX=0.0, exitX=644.0,
    roofs=roofs, platforms=platforms, entities=ents, prompts=prompts,
    zones=zones, decor=decor,
    parTime=90.0,
)

if __name__ == '__main__':
    out = sys.argv[1]
    with open(out, 'w') as f:
        json.dump(level, f, indent=1)
    nb = sum(1 for x in ents if x['t'] == 'byte')
    print('bytes', nb, 'entities', len(ents), 'length', level['exitX'])
