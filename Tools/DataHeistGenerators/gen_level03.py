"""Authoring script for Level 3 - "The Underground Data Vault" (gen_level.py's toolkit).

Writes level03.json. Units are world units (1 unit = 32 px), y = 0 is the default floor height.
Level 3 hazards (all triggered by the runner's distance, so retries play out the same):
  turret        - floor turret; red warning line 0.7 s, then a bolt at chest height (or ankles: mode 'low')
  laser cycling - mode low_cycle / high_cycle: off, flicker warning, on (period 2 s)
  shock 'warn'  - live floor with a 0.4 s amber warning before every live phase
  press         - hydraulic crusher slamming to 0.6 above the floor: slide under it or pass while it is up
  machine       - falling server crate (the Level 2 sign logic) with a red floor marker
  patrol drones - gunner drones sweeping back and forth (move='patrol')
Validate with Data Heist > Validate Level 3 (Playtest Bot) after every change.
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


def roof(x0, x1, y=0, style='vault'):
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


# --- hazards -----------------------------------------------------------------
def shock(x0, w):
    """live floor strip with an amber warning before every live phase"""
    return e('shock', x0, w=w, mode='warn')


def turret(x, mode='chest'):
    return e('turret', x, mode=mode)


def laser(x, mode):
    """mode: low_cycle (jump it when live) or high_cycle (slide under it when live)"""
    if mode.startswith('low'):
        return e('laser', x, 0.0, h=0.65, mode=mode)
    return e('laser', x, 0.75, h=3.0, mode=mode)


def press(x):
    return e('press', x)


def machine(x, hang=4.2):
    return e('sign', x, hang, kind='machine')


def patrol_drone(x, dy=1.6):
    return e('gunner', x, dy, kind='gunner', move='patrol')


def sign(x, hang=4.2):
    """hanging billboard centred on x; drops as the runner closes in"""
    return e('sign', x, hang)


def sniper(x):
    """sniper nest at x on the same roof the runner is on (fires along chest height)"""
    return e('sniper', x)


# ----------------------------------------------------------------------------
# SECTION 0 - The descent: into the server hall
# ----------------------------------------------------------------------------
roof(-12, 84)
prompt(8, '!', 'THE UNDERGROUND VAULT', 'Halcyon hides its secrets down here. Find the data. Get out.')
byte_line(4, 16)
e('barrier', 28)
byte_line(25, 31, 0.35, 0.75)
shock(42, 2.5)
byte_arc(42 - 1.0)
machine(62)
byte_line(52, 58)
byte_line(70, 78)

# ----------------------------------------------------------------------------
# SECTION 1 - SECURITY TURRETS
# ----------------------------------------------------------------------------
roof(87, 196)
byte_arc(84 - 0.6)
prompt(92, 'S', 'SECURITY TURRETS', 'A red line marks the shot. High shots: slide. Low shots: jump. Or slash them.')
turret(116)
byte_line(100, 106, 0.35, 0.75)
turret(146, 'low')
byte_line(130, 136)
patrol_drone(172)
turret(188)
byte_line(176, 182, 0.35, 0.75)

# ----------------------------------------------------------------------------
# SECTION 2 - LASER GRID
# ----------------------------------------------------------------------------
roof(199, 300)
byte_arc(196 - 0.6)
prompt(204, 'SPACE', 'LASER GRID', 'Lasers flicker before they switch on. Low beams: jump. High beams: slide.')
laser(218, 'low_cycle')
byte_arc(218 - 2.2)
laser(236, 'high_cycle')
byte_line(233, 239, 0.35, 0.75)
laser(256, 'low_cycle')
laser(270, 'high_cycle')
patrol_drone(284, 1.0)
e('checkpoint', 294)

# ----------------------------------------------------------------------------
# SECTION 3 - MACHINE HALL: crushers and falling machinery
# ----------------------------------------------------------------------------
roof(303, 412)
byte_arc(300 - 0.6)
prompt(308, 'S', 'CRUSHERS', 'Presses slam down to knee height. Slide under them, or pass while they are up.')
press(324)
byte_line(320, 328, 0.3, 0.75)
press(344)
machine(362)
shock(374, 2.5)
byte_arc(374 - 1.0)
press(392)
byte_line(398, 406)

# ---- (Phase 2: data fragments, vault door, boss arena; Phase 3: escape) ----

# ----------------------------------------------------------------------------
# Decor (seeded, purely cosmetic)
# ----------------------------------------------------------------------------
decor = []
rr = random.Random(3031)
for r in roofs:
    x = r['x0'] + 2
    while x < r['x1'] - 2:
        kind = rr.choice(['server', 'server', 'screen', 'camera', 'beacon', 'vent', 'server', 'screen'])
        decor.append(dict(k=kind, x=round(x, 2), y=r['y'], layer=-1))
        x += rr.uniform(5, 10)

EXIT = 412.0
level = dict(
    id='level03', name='UNDERGROUND VAULT', subtitle='Halcyon Archive - Shard 3 of 5',
    theme='vault',
    physics=PHYS, killY=-9.0, spawnX=0.0, exitX=EXIT,
    roofs=roofs, platforms=platforms, entities=ents, prompts=prompts,
    zones=zones, decor=decor,
    parTime=95.0,
)

if __name__ == '__main__':
    out = sys.argv[1]
    with open(out, 'w') as f:
        json.dump(level, f, indent=1)
    nb = sum(1 for x in ents if x['t'] == 'byte')
    print('bytes', nb, 'entities', len(ents), 'length', level['exitX'])
