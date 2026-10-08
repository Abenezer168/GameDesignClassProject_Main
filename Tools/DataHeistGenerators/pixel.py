"""Tiny pixel-art canvas helpers used by the Data Heist asset generators."""
import math
import numpy as np
from PIL import Image


def hexc(h, a=255):
    h = h.lstrip('#')
    return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), a)


def mix(c1, c2, t):
    return tuple(int(round(c1[i] * (1 - t) + c2[i] * t)) for i in range(4))


class Canvas:
    def __init__(self, w, h):
        self.w, self.h = w, h
        self.a = np.zeros((h, w, 4), dtype=np.uint8)

    # --- primitives -------------------------------------------------------
    def px(self, x, y, c):
        x, y = int(round(x)), int(round(y))
        if 0 <= x < self.w and 0 <= y < self.h:
            if len(c) == 4 and c[3] < 255:
                if c[3] == 0:
                    return
                base = self.a[y, x].astype(float)
                t = c[3] / 255.0
                if base[3] == 0:
                    self.a[y, x] = c
                else:
                    self.a[y, x, :3] = (base[:3] * (1 - t) + np.array(c[:3]) * t).astype(np.uint8)
                    self.a[y, x, 3] = max(base[3], c[3])
            else:
                self.a[y, x] = c

    def rect(self, x0, y0, x1, y1, c):
        """inclusive rect"""
        for y in range(int(y0), int(y1) + 1):
            for x in range(int(x0), int(x1) + 1):
                self.px(x, y, c)

    def disk(self, cx, cy, r, c):
        ri = int(math.ceil(r)) + 1
        for y in range(int(cy) - ri, int(cy) + ri + 1):
            for x in range(int(cx) - ri, int(cx) + ri + 1):
                if (x - cx) ** 2 + (y - cy) ** 2 <= r * r + 0.25:
                    self.px(x, y, c)

    def ellipse(self, cx, cy, rx, ry, c):
        for y in range(int(cy - ry) - 1, int(cy + ry) + 2):
            for x in range(int(cx - rx) - 1, int(cx + rx) + 2):
                if ((x - cx) / max(rx, .01)) ** 2 + ((y - cy) / max(ry, .01)) ** 2 <= 1.0:
                    self.px(x, y, c)

    def line(self, x0, y0, x1, y1, c, w=1):
        d = max(abs(x1 - x0), abs(y1 - y0), 1)
        n = int(d * 2) + 1
        for i in range(n + 1):
            t = i / n
            x = x0 + (x1 - x0) * t
            y = y0 + (y1 - y0) * t
            if w <= 1:
                self.px(x, y, c)
            else:
                self.disk(x, y, (w - 1) / 2.0, c)

    def poly(self, pts, c):
        xs = [p[0] for p in pts]
        ys = [p[1] for p in pts]
        for y in range(int(min(ys)), int(max(ys)) + 1):
            for x in range(int(min(xs)), int(max(xs)) + 1):
                if point_in_poly(x + .0, y + .0, pts):
                    self.px(x, y, c)

    def paste(self, other, ox, oy):
        for y in range(other.h):
            for x in range(other.w):
                c = other.a[y, x]
                if c[3]:
                    self.px(ox + x, oy + y, tuple(int(v) for v in c))

    # --- post passes -------------------------------------------------------
    def outline(self, c, diag=False):
        al = self.a[:, :, 3] > 0
        out = np.zeros_like(al)
        shifts = [(0, 1), (0, -1), (1, 0), (-1, 0)]
        if diag:
            shifts += [(1, 1), (1, -1), (-1, 1), (-1, -1)]
        for dy, dx in shifts:
            out |= shift(al, dy, dx)
        out &= ~al
        self.a[out] = c

    def rim(self, c, side=-1, strength=0.55, only=None):
        """lighten pixels whose neighbour on `side` (x dir) is empty"""
        al = self.a[:, :, 3] > 0
        nb = shift(al, 0, -side)  # neighbour value at x+side
        edge = al & ~nb
        ys, xs = np.nonzero(edge)
        for y, x in zip(ys, xs):
            base = tuple(int(v) for v in self.a[y, x])
            if only is not None and base[:3] in only:
                continue
            self.a[y, x] = mix(base, c, strength)[:3] + (base[3],)

    def image(self, scale=1):
        im = Image.fromarray(self.a, 'RGBA')
        if scale != 1:
            im = im.resize((self.w * scale, self.h * scale), Image.NEAREST)
        return im

    def save(self, path, scale=1):
        self.image(scale).save(path)

    def flip(self):
        c = Canvas(self.w, self.h)
        c.a = self.a[:, ::-1].copy()
        return c


def point_in_poly(x, y, pts):
    inside = False
    n = len(pts)
    j = n - 1
    for i in range(n):
        xi, yi = pts[i]
        xj, yj = pts[j]
        if ((yi > y) != (yj > y)) and (x < (xj - xi) * (y - yi) / ((yj - yi) or 1e-9) + xi):
            inside = not inside
        j = i
    return inside


def rot(dx, dy, ang):
    ca, sa = math.cos(ang), math.sin(ang)
    return dx * ca - dy * sa, dx * sa + dy * ca


def shift(a, dy, dx):
    """non-wrapping shift of a 2D bool array"""
    out = np.zeros_like(a)
    h, w = a.shape
    ys, yd = (slice(0, h - dy), slice(dy, h)) if dy >= 0 else (slice(-dy, h), slice(0, h + dy))
    xs, xd = (slice(0, w - dx), slice(dx, w)) if dx >= 0 else (slice(-dx, w), slice(0, w + dx))
    out[yd, xd] = a[ys, xs]
    return out
