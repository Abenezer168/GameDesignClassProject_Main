"""Key art for The Data Heist: a 640x360 poster composed from the game's own sprites.

Run after the other generators (it reads their output):
    python3 gen_keyart.py <Resources/DataHeist>
Writes Story/keyart.png. The logo is drawn in engine on top, so the upper middle stays calm.
"""
import math
import os
import random
import sys
import numpy as np
from PIL import Image, ImageFilter

W, H = 640, 360


def load(root, rel):
    return Image.open(os.path.join(root, rel)).convert('RGBA')


def scaled(im, k):
    return im.resize((im.width * k, im.height * k), Image.NEAREST) if k != 1 else im


def tinted(im, rgb, alpha=1.0):
    a = np.array(im).astype(float)
    a[:, :, 0] = a[:, :, 0] * rgb[0]
    a[:, :, 1] = a[:, :, 1] * rgb[1]
    a[:, :, 2] = a[:, :, 2] * rgb[2]
    a[:, :, 3] = a[:, :, 3] * alpha
    return Image.fromarray(np.clip(a, 0, 255).astype(np.uint8))


def put(canvas, im, cx, bottom, k=1, tint=None, alpha=1.0):
    """paste a sprite by its bottom-centre (sprites use bottom-centre pivots in engine too)"""
    im = scaled(im, k)
    if tint or alpha < 1:
        im = tinted(im, tint or (1, 1, 1), alpha)
    canvas.alpha_composite(im, (int(round(cx - im.width / 2)), int(round(bottom - im.height))))


def glow(canvas, cx, cy, radius, rgb, strength):
    yy, xx = np.mgrid[0:H, 0:W]
    d = np.sqrt((xx - cx) ** 2 + (yy - cy) ** 2) / radius
    a = np.clip(1 - d, 0, 1) ** 2 * strength
    layer = np.zeros((H, W, 4), np.uint8)
    layer[:, :, 0], layer[:, :, 1], layer[:, :, 2] = rgb
    layer[:, :, 3] = (a * 255).astype(np.uint8)
    canvas.alpha_composite(Image.fromarray(layer))


def beam(canvas, root, x, y, angle, rgb, alpha, length_scale=1.0):
    b = load(root, 'Story/searchlight.png')
    if length_scale != 1.0:
        b = b.resize((b.width, int(b.height * length_scale)), Image.NEAREST)
    b = tinted(b, (rgb[0] / 255, rgb[1] / 255, rgb[2] / 255), alpha)
    # searchlight art points down from its apex (top centre); rotate about that apex
    pad = Image.new('RGBA', (b.width, b.height * 2), (0, 0, 0, 0))
    pad.alpha_composite(b, (0, b.height))
    r = pad.rotate(angle, resample=Image.BILINEAR, expand=True)
    canvas.alpha_composite(r, (int(x - r.width / 2), int(y - r.height / 2)))


def tile_row(canvas, tile, y, k):
    t = scaled(tile, k)
    for x in range(0, W, t.width):
        canvas.alpha_composite(t, (x, y))


def keyart(root):
    rr = random.Random(11)
    img = Image.new('RGBA', (W, H), (11, 6, 24, 255))

    # sky + horizon glow
    img.alpha_composite(load(root, 'Parallax/sky.png').resize((W, H), Image.NEAREST))
    glow(img, 470, 250, 330, (255, 63, 164), 0.22)

    # searchlights sweeping up from the city
    beam(img, root, 150, 330, 200, (255, 255, 255), 0.18, 1.1)
    beam(img, root, 360, 330, 168, (47, 243, 255), 0.20, 1.1)
    beam(img, root, 600, 330, 190, (255, 255, 255), 0.14, 1.1)

    # far city, then the Halcyon tower rising out of it, then the nearer layers
    far = tinted(load(root, 'Parallax/city_far.png'), (0.8, 0.75, 0.95))
    img.alpha_composite(far, (0, 120))
    tower = load(root, 'Story/tower.png')
    glow(img, 510, 120, 120, (47, 243, 255), 0.18)
    img.alpha_composite(tower, (510 - tower.width // 2, 18))
    glow(img, 510, 22, 16, (255, 45, 85), 0.9)
    img.alpha_composite(load(root, 'Parallax/city_mid.png'), (0, 168))
    img.alpha_composite(load(root, 'Parallax/city_near.png'), (-120, 214))

    # security drones with red scanning beams
    drone = load(root, 'Enemies/drone_scout_00.png')
    for (dx, dy, ang) in ((66, 112, -14), (430, 104, 10)):
        beam(img, root, dx, dy + 6, ang, (255, 45, 85), 0.32, 0.5)
        put(img, drone, dx, dy + 32, 2)

    # foreground rooftop
    roof_y = 300
    tile_row(img, load(root, 'Tiles/facade_violet_0.png'), roof_y + 16, 2)
    tile_row(img, load(root, 'Tiles/facade_violet_0.png'), roof_y + 80, 2)
    tile_row(img, load(root, 'Tiles/rooftop_violet.png'), roof_y - 16, 2)

    # data byte arc tracing Kai's jump
    byte = load(root, 'Pickups/byte_00.png')
    for i in range(7):
        t = i / 6
        bx = 150 + t * 120
        by = roof_y - 40 - math.sin(t * math.pi * 0.85) * 118
        put(img, byte, bx, by, 2, alpha=0.95)

    # the crew: rim glows first, then sprites
    crew = [
        # (sprite, centre x, bottom y, scale, glow colour)
        ('Characters/nova/nova_run_02.png', 96, roof_y + 2, 2, (255, 63, 164)),
        ('Characters/jax/jax_attack_01.png', 392, roof_y + 2, 2, (56, 182, 255)),
        ('Characters/lin/lin_ability_00.png', 560, roof_y + 2, 2, (125, 255, 106)),
        ('Characters/kai/kai_jump_00.png', 278, 252, 3, (47, 243, 255)),
    ]
    for rel, cx, bottom, k, col in crew:
        glow(img, cx, bottom - 24 * k, 30 * k, col, 0.35)
    for rel, cx, bottom, k, col in crew:
        put(img, load(root, rel), cx, bottom, k)

    # Jax slashing a drone out of the air
    put(img, load(root, 'FX/slash_01.png'), 440, roof_y - 34, 2)
    put(img, load(root, 'FX/explosion_02.png'), 472, roof_y - 40, 2)

    # rain
    a = np.array(img)
    for _ in range(420):
        x, y = rr.randrange(W), rr.randrange(H)
        for k in range(rr.randint(5, 10)):
            px, py = x - k // 4, y + k
            if 0 <= px < W and 0 <= py < H:
                a[py, px, :3] = (a[py, px, :3] * 0.6 + np.array([190, 200, 255]) * 0.4).astype(np.uint8)
    img = Image.fromarray(a)

    # readability: darker top band for the logo, darker floor for PRESS START, soft vignette
    shade = np.zeros((H, W, 4), np.uint8)
    yy, xx = np.mgrid[0:H, 0:W]
    top = np.clip(1 - yy / 120, 0, 1) * 0.55
    bottom = np.clip((yy - 300) / 60, 0, 1) * 0.6
    d = np.sqrt(((xx - W / 2) / (W / 2)) ** 2 + ((yy - H / 2) / (H / 2)) ** 2)
    vig = np.clip((d - 0.75) / 0.6, 0, 1) * 0.6
    shade[:, :, 3] = (np.clip(top + bottom + vig, 0, 0.85) * 255).astype(np.uint8)
    shade[:, :, 0], shade[:, :, 1], shade[:, :, 2] = 8, 4, 18
    img.alpha_composite(Image.fromarray(shade))
    return img


if __name__ == '__main__':
    root = sys.argv[1]
    out = os.path.join(root, 'Story', 'keyart.png')
    keyart(root).save(out)
    print('key art ->', out)
