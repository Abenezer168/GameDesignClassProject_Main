"""HD key art for The Data Heist (1920x1080).

A painted-style night city rendered procedurally (2x supersampled for clean edges): sky and fog,
three layers of skyline with atmospheric depth, searchlights, the Halcyon megatower, drones,
a rain-soaked reflective rooftop, bloom, chromatic aberration and grain. The four runners are
the game's own pixel sprites at large integer scale ("HD-2D"), rim-lit and reflected.

    python3 gen_keyart.py <Resources/DataHeist>      ->  Story/keyart.png

The logo is drawn in engine on top, so the upper middle stays calm.
"""
import math
import os
import random
import sys
import numpy as np
from PIL import Image, ImageDraw
from scipy.ndimage import gaussian_filter, map_coordinates

W, H = 1920, 1080
S = 2                      # supersampling for the vector layers
SW, SH = W * S, H * S
HORIZON = 760              # where the far skyline meets the fog (final px)
ROOF = 885                 # top edge of the foreground rooftop (final px)


def hexf(h):
    h = h.lstrip('#')
    return np.array([int(h[i:i + 2], 16) / 255 for i in (0, 2, 4)], np.float32)


CYAN, PINK, RED = hexf('2ff3ff'), hexf('ff3fa4'), hexf('ff2d55')
AMBER, VIOLET, GREEN = hexf('ffd23a'), hexf('9a5cff'), hexf('7dff6a')


# --------------------------------------------------------------------------- helpers
def blank(w=SW, h=SH):
    return np.zeros((h, w, 3), np.float32)


def over(base, rgb, alpha):
    """alpha-composite a colour (or image) over base with a per-pixel alpha map"""
    a = alpha[..., None]
    base *= (1 - a)
    base += rgb * a


def mask_of(draw_fn, w=SW, h=SH):
    m = Image.new('L', (w, h), 0)
    draw_fn(ImageDraw.Draw(m))
    return np.asarray(m, np.float32) / 255


def glow(mask, colour, sigma, strength):
    return gaussian_filter(mask, sigma)[..., None] * colour * strength


def fbm(h, w, seed, scales=(220, 90, 35), weights=(0.6, 0.3, 0.1)):
    rng = np.random.default_rng(seed)
    out = np.zeros((h, w), np.float32)
    for sc, wt in zip(scales, weights):
        small = rng.random((max(2, h // sc + 2), max(2, w // sc + 2))).astype(np.float32)
        img = Image.fromarray((small * 255).astype(np.uint8)).resize((w, h), Image.BICUBIC)
        out += np.asarray(img, np.float32) / 255 * wt
    return out


def load_sprite(root, rel):
    return Image.open(os.path.join(root, rel)).convert('RGBA')


# --------------------------------------------------------------------------- layers
def sky(img):
    y = np.linspace(0, 1, SH, dtype=np.float32)[:, None]
    stops = [(0.0, hexf('02010a')), (0.35, hexf('0e0522')), (0.58, hexf('260a3e')),
             (HORIZON / H, hexf('6a1c58')), (1.0, hexf('14061c'))]
    for (y0, c0), (y1, c1) in zip(stops, stops[1:]):
        t = np.clip((y - y0) / (y1 - y0), 0, 1)
        sel = ((y >= y0) & (y <= y1)).astype(np.float32)
        img += (c0 * (1 - t) + c1 * t)[:, None, :] * sel[..., None] * np.ones((1, SW, 1), np.float32)
    # stars
    rng = np.random.default_rng(3)
    for _ in range(260):
        x, yy = rng.integers(0, SW), rng.integers(0, int(SH * 0.45))
        b = rng.uniform(0.3, 1.0)
        img[yy:yy + 2, x:x + 2] = np.maximum(img[yy:yy + 2, x:x + 2], b * 0.8)
    # moon (crescent) with halo
    mx, my, r = 1610 * S, 190 * S, 62 * S
    full = mask_of(lambda d: d.ellipse([mx - r, my - r, mx + r, my + r], fill=255))
    bite = mask_of(lambda d: d.ellipse([mx - r + 34 * S, my - r - 18 * S, mx + r + 34 * S, my + r - 18 * S], fill=255))
    crescent = np.clip(full - bite, 0, 1)
    img += glow(full, hexf('ffb3d9'), 60 * S, 0.35)
    over(img, hexf('ffe6f2'), crescent * 0.95)
    # drifting fog bands lit by the city
    fog = fbm(SH, SW, 11) * np.clip(1 - np.abs(np.linspace(-1, 1, SH)[:, None] - 0.25) * 1.6, 0, 1)
    img += fog[..., None] * hexf('ff5fae') * 0.10


def skyline(img, seed, base_y, h_range, w_range, body, haze, window_p, neon_p, antenna_p):
    """one depth layer of buildings; returns the mask so nearer layers can reflect/occlude"""
    rng = random.Random(seed)
    layer = blank()
    alpha = np.zeros((SH, SW), np.float32)
    lights = np.zeros((SH, SW), np.float32)
    neon_mask = {}
    edge_tops = []
    x = -40 * S
    by = base_y * S
    while x < SW:
        bw = rng.randint(*w_range) * S
        bh = rng.randint(*h_range) * S
        top = by - bh
        x0, x1 = x, min(SW, x + bw)
        alpha[max(0, top):by, max(0, x0):x1] = 1
        edge_tops.append((max(0, top), max(0, x0), x1))
        # vertical gradient on the facade, slightly lit from below by the city
        col = np.linspace(0, 1, max(1, by - top), dtype=np.float32)[:, None, None]
        face = body * (0.75 + 0.35 * col) + haze * 0.25 * col
        layer[max(0, top):by, max(0, x0):x1] = face[-(by - max(0, top)):]
        # windows
        wx_step, wy_step = 7 * S, 9 * S
        for wy in range(top + 8 * S, by - 6 * S, wy_step):
            for wx in range(x0 + 5 * S, x1 - 6 * S, wx_step):
                if rng.random() < window_p and wy > 0:
                    lights[wy:wy + 4 * S, wx:wx + 3 * S] = rng.uniform(0.35, 1.0)
        # roof antenna with a red light
        if rng.random() < antenna_p and top > 0:
            ax = x0 + bw // 2
            ah = rng.randint(20, 60) * S
            alpha[max(0, top - ah):top, ax:ax + 2 * S] = 1
            layer[max(0, top - ah):top, ax:ax + 2 * S] = body
            neon_mask.setdefault('red', []).append((ax, top - ah))
        # neon sign
        if rng.random() < neon_p:
            c = rng.choice(['pink', 'cyan', 'violet'])
            sx = x0 + rng.randint(4, max(5, bw // S - 20)) * S
            sy = top + rng.randint(10, max(11, (bh // S) // 2)) * S
            if rng.random() < 0.5:
                neon_mask.setdefault(c, []).append(('v', sx, sy, 7 * S, rng.randint(40, 110) * S))
            else:
                neon_mask.setdefault(c, []).append(('h', sx, sy, rng.randint(40, 90) * S, 9 * S))
        x += bw + rng.randint(-6, 10) * S
    window_cols = [hexf('ffd88a'), hexf('8af0ff'), hexf('ff9ad2'), hexf('c9a8ff')]
    tint = np.stack([lights] * 3, -1) * window_cols[seed % 4] * 0.6 + np.stack([lights] * 3, -1) * 0.25
    layer = layer + tint * alpha[..., None]
    over(img, layer, alpha)
    # atmospheric haze toward the horizon, and a thin lit cornice on every roofline
    over(img, haze, alpha * 0.08)
    for top, x0, x1 in edge_tops:
        img[top:top + 2 * S, x0:x1] += haze * 0.9
    # neon on top, with bloom later
    em = blank()
    for key, items in neon_mask.items():
        colour = {'pink': PINK, 'cyan': CYAN, 'violet': VIOLET, 'red': RED}[key]
        m = np.zeros((SH, SW), np.float32)
        for it in items:
            if key == 'red':
                ax, ay = it
                m[max(0, ay - 3 * S):ay + 3 * S, max(0, ax - 2 * S):ax + 4 * S] = 1
            else:
                kind, sx, sy, sw, sh = it
                m[max(0, sy):sy + sh, max(0, sx):sx + sw] = 1
                # stripes inside the sign
                if kind == 'v':
                    for k in range(sy + 4 * S, sy + sh - 4 * S, 10 * S):
                        m[k:k + 4 * S, sx + 2 * S:sx + sw - 2 * S] = 0.35
        em += m[..., None] * colour
    img += em * 0.9
    return alpha


def searchlights(img):
    beams = [(260, 15, 0.13, hexf('ffffff')), (700, -12, 0.15, CYAN), (1050, 22, 0.10, hexf('ffffff')), (1820, -18, 0.14, PINK)]
    yy, xx = np.mgrid[0:SH, 0:SW].astype(np.float32)
    for bx, ang, k, c in beams:
        ox, oy = bx * S, HORIZON * S
        a = math.radians(ang)
        dx, dy = math.sin(a), -math.cos(a)
        rx, ry = xx - ox, yy - oy
        along = rx * dx + ry * dy
        across = np.abs(rx * dy - ry * dx)
        width = 8 * S + along * 0.075
        m = np.clip(1 - across / np.maximum(width, 1), 0, 1) * (along > 0) * np.clip(1 - along / (1100 * S), 0, 1)
        img += m[..., None] * c * k


def halcyon_tower(img):
    cx = 1330 * S
    sections = [  # (top, bottom, half width, chamfer)
        (720, 1080, 190, 22), (330, 720, 135, 26), (190, 330, 92, 20),
    ]
    body_l, body_r = hexf('241650'), hexf('120a2c')
    alpha = np.zeros((SH, SW), np.float32)
    shape = blank()
    em = np.zeros((SH, SW, 3), np.float32)
    rng = random.Random(9)
    for top, bot, hw, ch in sections:
        t, b, w, c = top * S, bot * S, hw * S, ch * S
        poly = [(cx - w + c, t), (cx + w - c, t), (cx + w, t + c), (cx + w, b), (cx - w, b), (cx - w, t + c)]
        m = mask_of(lambda d: d.polygon(poly, fill=255))
        xs = np.linspace(-1, 1, SW, dtype=np.float32)[None, :]
        rel = np.clip((np.arange(SW)[None, :] - (cx - w)) / (2 * w), 0, 1)
        face = body_l[None, None, :] * (1 - rel[..., None]) + body_r[None, None, :] * rel[..., None]
        over(shape, np.broadcast_to(face, (SH, SW, 3)), m)
        alpha = np.maximum(alpha, m)
        # neon edge strips + ledge line
        for ex in (cx - w + 4 * S, cx + w - 7 * S):
            em[t + c:b, ex:ex + 3 * S] += PINK * 1.0
        em[t:t + 2 * S, cx - w + c:cx + w - c] += CYAN * 0.8
        # window grid
        for wy in range(t + c + 14 * S, b - 8 * S, 11 * S):
            for wx in range(cx - w + 16 * S, cx + w - 18 * S, 9 * S):
                r = rng.random()
                if r < 0.33:
                    colr = rng.choice([CYAN, hexf('e8e2ff'), hexf('c9a8ff'), hexf('ffd88a')])
                    em[wy:wy + 5 * S, wx:wx + 4 * S] += colr * (0.9 if r < 0.05 else rng.uniform(0.25, 0.5))
    # spire
    spire = mask_of(lambda d: d.polygon([(cx - 6 * S, 190 * S), (cx + 6 * S, 190 * S), (cx + 2 * S, 62 * S), (cx - 2 * S, 62 * S)], fill=255))
    over(shape, body_l, spire)
    alpha = np.maximum(alpha, spire)
    for yb in (110, 140, 168):
        em[yb * S:yb * S + 2 * S, cx - 16 * S:cx + 16 * S] += hexf('3c2766')
    over(img, shape, alpha)
    # side rim light from the cyan city glow
    rim = gaussian_filter(alpha, 3 * S) - gaussian_filter(alpha, 1 * S)
    img += np.clip(rim, 0, 1)[..., None] * CYAN * 0.8
    img += em * alpha[..., None]
    # holographic rings (front halves over the tower, back halves behind are hidden by it)
    for ry, rw, rh in ((262, 175, 26), (560, 230, 30)):
        ring_back = mask_of(lambda d: d.arc([cx - rw * S, (ry - rh) * S, cx + rw * S, (ry + rh) * S], 180, 360, fill=255, width=3 * S))
        ring_front = mask_of(lambda d: d.arc([cx - rw * S, (ry - rh) * S, cx + rw * S, (ry + rh) * S], 0, 180, fill=255, width=3 * S))
        img += ring_back[..., None] * CYAN * 0.35 * (1 - alpha[..., None])
        img += ring_front[..., None] * CYAN * 1.2
    # corporate mark: neon H in a diamond
    my = 430 * S
    dia = mask_of(lambda d: d.polygon([(cx, my - 62 * S), (cx + 62 * S, my), (cx, my + 62 * S), (cx - 62 * S, my)], outline=255, width=5 * S))
    hmark = mask_of(lambda d: [d.rectangle([cx - 24 * S, my - 26 * S, cx - 14 * S, my + 26 * S], fill=255),
                               d.rectangle([cx + 14 * S, my - 26 * S, cx + 24 * S, my + 26 * S], fill=255),
                               d.rectangle([cx - 14 * S, my - 5 * S, cx + 14 * S, my + 5 * S], fill=255)])
    img += (dia + hmark)[..., None] * CYAN * 1.3
    # red beacon with a horizontal lens streak
    by = 60 * S
    beacon = mask_of(lambda d: d.ellipse([cx - 7 * S, by - 7 * S, cx + 7 * S, by + 7 * S], fill=255))
    img += glow(beacon, RED, 30 * S, 3.0) + beacon[..., None] * hexf('ffd0d8') * 1.5
    streak = np.zeros((SH, SW), np.float32)
    streak[by - 1 * S:by + 1 * S, cx - 260 * S:cx + 260 * S] = np.linspace(0, 1, 520 * S)[None, :] ** 0 * \
        (1 - np.abs(np.linspace(-1, 1, 520 * S)))[None, :] ** 2
    img += gaussian_filter(streak, 2 * S)[..., None] * RED * 2.0
    # fog pooling around the podium
    fogy = np.clip((np.arange(SH)[:, None] - 640 * S) / (200 * S), 0, 1)
    img += (fogy * np.exp(-((np.arange(SW)[None, :] - cx) / (500 * S)) ** 2))[..., None] * hexf('ff5fae') * 0.18
    return alpha


def drone(img, x, y, scale, beam_angle):
    x, y = x * S, y * S
    k = scale * S
    body = mask_of(lambda d: d.ellipse([x - 26 * k, y - 12 * k, x + 26 * k, y + 14 * k], fill=255))
    arms = mask_of(lambda d: [d.rectangle([x - 44 * k, y - 16 * k, x + 44 * k, y - 12 * k], fill=255),
                              d.rectangle([x - 46 * k, y - 22 * k, x - 26 * k, y - 19 * k], fill=255),
                              d.rectangle([x + 26 * k, y - 22 * k, x + 46 * k, y - 19 * k], fill=255),
                              d.rectangle([x - 14 * k, y + 12 * k, x - 10 * k, y + 22 * k], fill=255),
                              d.rectangle([x + 10 * k, y + 12 * k, x + 14 * k, y + 22 * k], fill=255)])
    # scanning cone first (behind)
    yy, xx = np.mgrid[0:SH, 0:SW].astype(np.float32)
    a = math.radians(beam_angle)
    dx, dy = math.sin(a), math.cos(a)
    rx, ry = xx - x, yy - (y + 8 * k)
    along = rx * dx + ry * dy
    across = np.abs(rx * dy - ry * dx)
    cone = np.clip(1 - across / np.maximum(3 * k + along * 0.28, 1), 0, 1) * (along > 0) * np.clip(1 - along / (520 * k), 0, 1)
    img += cone[..., None] * RED * 0.22
    shape = np.clip(body + arms, 0, 1)
    over(img, hexf('2a3050'), shape)
    # top-lit metal: bright rim on the upper edge, cyan bounce underneath
    rim = (shape - np.roll(shape, 3 * S, axis=0)).clip(0, 1)
    img += rim[..., None] * hexf('b8c0e8') * 0.9
    under = (shape - np.roll(shape, -2 * S, axis=0)).clip(0, 1)
    img += under[..., None] * CYAN * 0.5
    eye = mask_of(lambda d: d.ellipse([x - 7 * k, y - 3 * k, x + 7 * k, y + 11 * k], fill=255))
    img += glow(eye, RED, 10 * k, 2.2) + eye[..., None] * hexf('ffd0d8')


def data_byte(img, x, y, r):
    x, y, r = x * S, y * S, r * S
    pts = [(x + r * math.cos(math.radians(60 * i + 30)), y + r * math.sin(math.radians(60 * i + 30))) for i in range(6)]
    fill = mask_of(lambda d: d.polygon(pts, fill=255))
    edge = mask_of(lambda d: d.polygon(pts, outline=255, width=max(2, int(r * 0.16))))
    over(img, hexf('0b3a52'), fill * 0.9)
    img += glow(fill, CYAN, r * 0.8, 0.9) + edge[..., None] * CYAN * 1.4
    core = mask_of(lambda d: d.rectangle([x - r * 0.18, y - r * 0.45, x + r * 0.18, y + r * 0.45], fill=255))
    img += core[..., None] * CYAN * 0.9


def rooftop(img, scene_above):
    """wet foreground roof: dark slab, glowing ledge, rippled reflection of the city above"""
    top = ROOF * S
    h = SH - top
    refl = scene_above[top - h:top][::-1].copy()
    # ripple: horizontal displacement noise that grows toward the viewer
    yy, xx = np.mgrid[0:h, 0:SW].astype(np.float32)
    n = fbm(h, SW, 21, scales=(60, 20, 8), weights=(0.5, 0.35, 0.15)) - 0.5
    disp = n * (6 + yy / h * 26) * S
    refl = np.stack([map_coordinates(refl[..., c], [yy, np.clip(xx + disp, 0, SW - 1)], order=1) for c in range(3)], -1)
    refl = gaussian_filter(refl, (2 * S, 1 * S, 0))
    slab = np.linspace(0, 1, h, dtype=np.float32)[:, None, None]
    base = hexf('140b24') * (1 - slab * 0.4)
    region = base + refl * (0.55 - slab * 0.25)
    img[top:] = np.broadcast_to(region, img[top:].shape)
    # ledge: lit cap and neon strip
    img[top:top + 10 * S] = hexf('3a3352') * 1.2
    img[top + 10 * S:top + 13 * S] += CYAN * 1.3
    # puddle glints
    rng = random.Random(4)
    for _ in range(60):
        px, py = rng.randrange(0, SW), rng.randrange(top + 30 * S, SH)
        w = rng.randint(30, 120) * S
        img[py:py + 1 * S, px:px + w] += hexf('ff9ad2') * rng.uniform(0.15, 0.4)


def paste_sprite(img_u8, sprite, cx, bottom, k, rim, rim_strength=1.0, reflect=True, trail=0):
    """pixel sprite at integer scale k (nearest), with coloured rim light and a wet-roof reflection"""
    sp = sprite.resize((sprite.width * k, sprite.height * k), Image.NEAREST)
    x0, y0 = int(cx - sp.width / 2), int(bottom - sp.height)
    a = np.asarray(sp, np.float32)[..., 3] / 255
    pad = 40
    big = np.zeros((sp.height + 2 * pad, sp.width + 2 * pad), np.float32)
    big[pad:-pad, pad:-pad] = a
    halo = np.clip(gaussian_filter(big, 7) * 2.2, 0, 1) * 0.55 * rim_strength
    canvas = img_u8
    # motion trail (ghost copies behind, tinted with the rim colour)
    for i in range(trail, 0, -1):
        g = np.asarray(sp, np.float32).copy()
        g[..., :3] = g[..., :3] * 0.2 + np.array(rim) * 255 * 0.8
        g[..., 3] *= 0.10 * (trail - i + 1) / trail
        canvas.alpha_composite(Image.fromarray(g.clip(0, 255).astype(np.uint8)), (x0 - i * k * 9, y0 + i * k * 3))
    h_rgba = np.zeros(big.shape + (4,), np.float32)
    h_rgba[..., :3] = np.array(rim) * 255
    h_rgba[..., 3] = halo * 255
    canvas.alpha_composite(Image.fromarray(h_rgba.astype(np.uint8)), (x0 - pad, y0 - pad))
    canvas.alpha_composite(sp, (x0, y0))
    if reflect and bottom >= ROOF - 10:
        r = np.asarray(sp.transpose(Image.FLIP_TOP_BOTTOM), np.float32).copy()
        fade = np.linspace(0.32, 0, r.shape[0])[:, None]
        r[..., 3] *= fade
        canvas.alpha_composite(Image.fromarray(r.astype(np.uint8)), (x0, int(bottom) + 2))


def slash_and_burst(img_u8, x, y, scale=1.0):
    lay = np.zeros((H, W, 3), np.float32)
    arc = mask_of(lambda d: d.arc([(x - 150) * S, (y - 130) * S, (x + 70) * S, (y + 90) * S], 250, 360, fill=255, width=14 * S), SW, SH)
    arc = np.asarray(Image.fromarray((arc * 255).astype(np.uint8)).resize((W, H), Image.LANCZOS), np.float32) / 255
    lay += gaussian_filter(arc, 10)[..., None] * hexf('ffffff') * 1.4 + arc[..., None] * hexf('ffffff')
    # explosion burst
    bx, by = x + 70, y - 40
    yy, xx = np.mgrid[0:H, 0:W].astype(np.float32)
    d = np.sqrt((xx - bx) ** 2 + (yy - by) ** 2)
    lay += np.exp(-(d / 26) ** 2)[..., None] * hexf('fff0b0') * 0.9 + np.exp(-(d / 70) ** 2)[..., None] * AMBER * 0.55 \
        + np.exp(-(d / 150) ** 2)[..., None] * hexf('ff4a2a') * 0.35
    rng = random.Random(8)
    for _ in range(40):
        ang, ln = rng.uniform(0, math.tau), rng.uniform(40, 150)
        x1, y1 = bx + math.cos(ang) * ln, by + math.sin(ang) * ln
        sm = Image.new('L', (W, H), 0)
        ImageDraw.Draw(sm).line([(bx + math.cos(ang) * 30, by + math.sin(ang) * 30), (x1, y1)], fill=255, width=2)
        lay += np.asarray(sm, np.float32)[..., None] / 255 * AMBER * 0.6
    base = np.asarray(img_u8, np.float32)[..., :3] / 255
    out = np.clip(base + lay, 0, 1)
    img_u8.paste(Image.fromarray((out * 255).astype(np.uint8)).convert('RGBA'))


def rain(img_u8, seed, n, length, alpha, width=1):
    lay = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(lay)
    rng = random.Random(seed)
    for _ in range(n):
        x, y = rng.uniform(-100, W), rng.uniform(-100, H)
        ln = rng.uniform(*length)
        d.line([(x, y), (x - ln * 0.18, y + ln)], fill=(200, 210, 255, int(255 * rng.uniform(*alpha))), width=width)
    img_u8.alpha_composite(lay)


def post(img):
    a = np.asarray(img, np.float32)[..., :3] / 255
    lum = a.max(-1)
    bright = a * np.clip((lum - 0.55) / 0.45, 0, 1)[..., None]
    bloom = gaussian_filter(bright, (8, 8, 0)) * 0.55 + gaussian_filter(bright, (40, 40, 0)) * 0.6
    a = a + bloom
    # chromatic aberration growing toward the edges
    xs = (np.arange(W) - W / 2) / (W / 2)
    shift = (xs * 3).astype(np.float32)
    yy, xx = np.mgrid[0:H, 0:W].astype(np.float32)
    a[..., 0] = map_coordinates(a[..., 0], [yy, np.clip(xx - shift[None, :], 0, W - 1)], order=1)
    a[..., 2] = map_coordinates(a[..., 2], [yy, np.clip(xx + shift[None, :], 0, W - 1)], order=1)
    # vignette, top band for the logo, bottom band for PRESS START
    d = np.sqrt(((xx - W / 2) / (W / 2)) ** 2 + ((yy - H / 2) / (H / 2)) ** 2)
    shade = np.clip((d - 0.7) / 0.7, 0, 1) * 0.55
    shade += np.clip(1 - yy / 260, 0, 1) * 0.5 + np.clip((yy - 930) / 150, 0, 1) * 0.45
    a = a * (1 - np.clip(shade, 0, 0.85)[..., None]) + hexf('080412') * np.clip(shade, 0, 0.85)[..., None]
    # grade: deep blacks, a little extra contrast and saturation, grain
    a = np.clip(a, 0, 1) ** 1.12
    lum = a.mean(-1, keepdims=True)
    a = lum + (a - lum) * 1.2
    a += np.random.default_rng(1).normal(0, 0.012, a.shape).astype(np.float32)
    return Image.fromarray((np.clip(a, 0, 1) * 255).astype(np.uint8))


def keyart(root):
    img = blank()
    sky(img)
    searchlights(img)
    skyline(img, 1, HORIZON, (140, 320), (60, 150), hexf('2a1650'), hexf('7a3a8a'), 0.18, 0.12, 0.25)
    halcyon_tower(img)
    skyline(img, 2, ROOF, (80, 250), (110, 240), hexf('1a0d30'), hexf('5a2a6a'), 0.22, 0.55, 0.4)
    drone(img, 300, 250, 2.0, -12)
    drone(img, 1075, 300, 1.4, 10)
    drone(img, 1740, 420, 1.1, -6)
    rooftop(img, img.copy())
    # data bytes arc tracing Kai's jump
    for i in range(8):
        t = i / 7
        data_byte(img, 300 + t * 420, ROOF - 60 - math.sin(t * math.pi * 0.9) * 360, 22 - t * 3)

    # downsample the vector scene (2x supersampling = smooth edges)
    small = Image.fromarray((np.clip(img, 0, 1) * 255).astype(np.uint8)).resize((W, H), Image.LANCZOS).convert('RGBA')

    # the crew, as crisp large-scale pixel art (HD-2D), rim-lit and reflected in the wet roof
    sp = lambda rel: load_sprite(root, rel)
    paste_sprite(small, sp('Characters/nova/nova_run_02.png'), 330, ROOF + 6, 6, PINK)
    paste_sprite(small, sp('Characters/lin/lin_ability_00.png'), 1650, ROOF + 6, 6, GREEN)
    paste_sprite(small, sp('Characters/jax/jax_attack_01.png'), 1060, ROOF + 6, 6, hexf('38b6ff'))
    paste_sprite(small, sp('Characters/kai/kai_jump_00.png'), 780, 640, 8, CYAN, 1.3, reflect=False, trail=2)
    slash_and_burst(small, 1200, 690)

    rain(small, 5, 900, (18, 40), (0.05, 0.14))
    rain(small, 6, 70, (60, 120), (0.08, 0.20), width=2)
    return post(small)


if __name__ == '__main__':
    root = sys.argv[1]
    out = os.path.join(root, 'Story', 'keyart.png')
    keyart(root).save(out, optimize=True)
    print('key art ->', out)
