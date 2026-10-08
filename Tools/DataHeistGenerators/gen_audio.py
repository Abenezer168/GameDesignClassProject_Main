"""Original synthwave soundtrack + SFX for The Data Heist, synthesised with numpy.
Outputs 16-bit WAV files (Unity imports them directly; ffmpeg makes web copies)."""
import math
import os
import sys
import wave
import numpy as np
from scipy.signal import butter, lfilter

SR = 44100
rng = np.random.default_rng(42)


def write(path, x, stereo=False):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    x = np.asarray(x, dtype=np.float64)
    peak = np.max(np.abs(x)) or 1
    x = x / peak * 0.89
    if stereo:
        data = (x.T * 32767).astype(np.int16).tobytes() if x.ndim == 2 else None
        ch = 2
        frames = np.empty((x.shape[1] * 2,), dtype=np.int16)
        frames[0::2] = (x[0] * 32767).astype(np.int16)
        frames[1::2] = (x[1] * 32767).astype(np.int16)
        data = frames.tobytes()
    else:
        ch = 1
        data = (x * 32767).astype(np.int16).tobytes()
    with wave.open(path, 'wb') as w:
        w.setnchannels(ch)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(data)


def t_(dur):
    return np.arange(int(SR * dur)) / SR


def env_adsr(n, a=0.005, d=0.05, s=0.6, r=0.1, sus_time=None):
    a_n, d_n, r_n = int(a * SR), int(d * SR), int(r * SR)
    if sus_time is None:
        s_n = max(0, n - a_n - d_n - r_n)
    else:
        s_n = int(sus_time * SR)
    e = np.concatenate([
        np.linspace(0, 1, max(a_n, 1)),
        np.linspace(1, s, max(d_n, 1)),
        np.full(s_n, s),
        np.linspace(s, 0, max(r_n, 1)),
    ])
    if len(e) < n:
        e = np.concatenate([e, np.zeros(n - len(e))])
    return e[:n]


def lp(x, cutoff, order=2):
    b, a = butter(order, min(cutoff / (SR / 2), 0.99), 'low')
    return lfilter(b, a, x)


def hp(x, cutoff, order=2):
    b, a = butter(order, min(cutoff / (SR / 2), 0.99), 'high')
    return lfilter(b, a, x)


def bp(x, lo, hi, order=2):
    b, a = butter(order, [lo / (SR / 2), min(hi / (SR / 2), 0.99)], 'band')
    return lfilter(b, a, x)


def saw(f, t, phase=0.0):
    return 2 * ((f * t + phase) % 1.0) - 1


def square(f, t, duty=0.5):
    return np.where((f * t) % 1.0 < duty, 1.0, -1.0)


def tri(f, t):
    return 2 * np.abs(saw(f, t)) - 1


def sweep(f0, f1, dur, kind='sine', curve=1.0):
    t = t_(dur)
    k = (t / dur) ** curve
    f = f0 + (f1 - f0) * k
    ph = 2 * np.pi * np.cumsum(f) / SR
    if kind == 'sine':
        return np.sin(ph)
    if kind == 'square':
        return np.sign(np.sin(ph))
    if kind == 'saw':
        return 2 * ((ph / (2 * np.pi)) % 1) - 1
    if kind == 'tri':
        return 2 * np.abs(2 * ((ph / (2 * np.pi)) % 1) - 1) - 1


def place(buf, sig, start):
    s = int(round(start * SR))
    n = min(len(sig), len(buf) - s)
    if n > 0:
        buf[s:s + n] += sig[:n]


def noise(dur):
    return rng.uniform(-1, 1, int(SR * dur))


def midi(n):
    return 440.0 * 2 ** ((n - 69) / 12)


def delay(x, time, fb=0.35, mix=0.35):
    d = int(time * SR)
    y = np.copy(x)
    for k in range(1, 6):
        g = mix * fb ** (k - 1)
        if d * k >= len(x):
            break
        y[d * k:] += x[:-d * k] * g
    return y


# ---------------------------------------------------------------------------
# SFX
# ---------------------------------------------------------------------------
def sfx_all(out):
    S = {}
    n = sweep(220, 660, 0.16, 'square', 0.6) * env_adsr(int(.16 * SR), .002, .05, .5, .08)
    S['jump'] = lp(n, 3500) * 0.6
    n = sweep(300, 1100, 0.22, 'saw', .5) * env_adsr(int(.22 * SR), .002, .08, .4, .1)
    S['air_jump'] = lp(n, 4000) * .5 + lp(noise(.22), 2500) * env_adsr(int(.22 * SR), .001, .05, .2, .1) * .5
    S['slide'] = bp(noise(.35), 600, 3000) * env_adsr(int(.35 * SR), .01, .1, .5, .2) * .8
    S['land'] = lp(noise(.12), 500) * env_adsr(int(.12 * SR), .001, .04, .2, .06) + sweep(140, 60, .12) * env_adsr(int(.12 * SR), .001, .05, .2, .05) * .6
    S['attack'] = bp(noise(.18), 1500, 9000) * np.linspace(1, 0, int(.18 * SR)) ** 2 + sweep(900, 300, .18, 'saw') * np.linspace(1, 0, int(.18 * SR)) ** 3 * .3
    boom = lp(noise(.6), 900) * np.exp(-t_(.6) * 7)
    S['explode'] = boom + sweep(160, 40, .6) * np.exp(-t_(.6) * 6) * .9 + bp(noise(.6), 2000, 8000) * np.exp(-t_(.6) * 20) * .4
    S['hurt'] = sweep(600, 120, .3, 'square', .7) * env_adsr(int(.3 * SR), .001, .1, .5, .1) * .5 + lp(noise(.3), 1800) * np.exp(-t_(.3) * 12) * .6
    b = t_(.14)
    S['byte'] = (square(midi(88), b, .25) * (b < .05) + square(midi(95), b, .25) * (b >= .05)) * env_adsr(len(b), .001, .03, .6, .05) * .35
    shard = np.zeros(int(1.4 * SR))
    for k, nn in enumerate([76, 79, 83, 88, 91, 95]):
        tt = t_(1.4 - k * .08)
        s = (np.sin(2 * np.pi * midi(nn) * tt) + .3 * np.sin(2 * np.pi * midi(nn) * 2 * tt)) * np.exp(-tt * 3)
        place(shard, s, k * .08)
    S['shard'] = delay(shard, .18, .4, .4)
    rep = np.zeros(int(.5 * SR))
    for k, nn in enumerate([72, 76, 79, 84]):
        tt = t_(.2)
        s = tri(midi(nn), tt) * np.exp(-tt * 12)
        place(rep, s, k * .07)
    S['repair'] = rep * .6
    cp = np.zeros(int(.7 * SR))
    for k, nn in enumerate([69, 76, 81]):
        tt = t_(.4)
        s = square(midi(nn), tt, .3) * np.exp(-tt * 6)
        place(cp, s, k * .1)
    S['checkpoint'] = lp(cp, 5000) * .5
    S['pad'] = sweep(150, 900, .35, 'sine', .4) * env_adsr(int(.35 * SR), .002, .1, .6, .2) + sweep(300, 1800, .35, 'tri', .4) * .3 * np.exp(-t_(.35) * 6)
    S['dash'] = bp(noise(.4), 400, 6000) * np.concatenate([np.linspace(.2, 1, int(.08 * SR)), np.exp(-t_(.32) * 6)]) + sweep(200, 1400, .4, 'saw', .3) * np.exp(-t_(.4) * 5) * .4
    emp = sweep(1200, 50, 1.0, 'saw', .4) * np.exp(-t_(1.0) * 3) * .6 + lp(noise(1.0), 1200) * np.exp(-t_(1.0) * 4)
    S['emp'] = emp + np.sin(2 * np.pi * 55 * t_(1.0)) * np.exp(-t_(1.0) * 3) * .8
    g = sweep(900, 120, 1.0, 'square', .5) * .3 * np.exp(-t_(1.0) * 1.5)
    g *= (np.sin(2 * np.pi * 18 * t_(1.0)) > 0) * .6 + .4
    S['glitch'] = lp(g, 3000) + bp(noise(1.0), 3000, 9000) * np.exp(-t_(1.0) * 4) * .3
    S['deflect'] = sweep(1800, 2600, .12, 'tri') * np.exp(-t_(.12) * 25) + sweep(2600, 600, .12, 'square') * np.exp(-t_(.12) * 30) * .3
    S['shoot'] = sweep(1400, 300, .2, 'square', .5) * np.exp(-t_(.2) * 14) * .5
    over = np.zeros(int(1.6 * SR))
    for k, nn in enumerate([64, 60, 57, 52]):
        tt = t_(.5 if k < 3 else .9)
        s = saw(midi(nn), tt) * np.exp(-tt * (5 if k < 3 else 2))
        place(over, s, k * .22)
    S['game_over'] = lp(over, 2200)
    win = np.zeros(int(2.2 * SR))
    seq = [(69, 0), (72, .12), (76, .24), (81, .36), (79, .6), (81, .72), (84, .84)]
    for nn, st in seq:
        tt = t_(.9 if nn == 84 else .3)
        s = (square(midi(nn), tt, .25) * .5 + saw(midi(nn) * 1.003, tt) * .5) * np.exp(-tt * (2.5 if nn == 84 else 8))
        place(win, s, st)
    S['level_complete'] = delay(lp(win, 5000), .15, .35, .3)
    S['ui_move'] = square(midi(84), t_(.05), .25) * np.exp(-t_(.05) * 60) * .5
    sel = np.concatenate([square(midi(79), t_(.05), .25), square(midi(86), t_(.09), .25)])
    S['ui_select'] = sel * env_adsr(len(sel), .001, .05, .6, .04) * .5
    S['ui_back'] = sweep(700, 300, .12, 'square') * np.exp(-t_(.12) * 20) * .5
    S['denied'] = (square(110, t_(.18)) * .5) * env_adsr(int(.18 * SR), .001, .02, .8, .05)
    S['count'] = np.sin(2 * np.pi * 880 * t_(.18)) * np.exp(-t_(.18) * 14)
    S['go'] = (np.sin(2 * np.pi * 1320 * t_(.5)) + .5 * square(660, t_(.5), .25)) * np.exp(-t_(.5) * 5)
    al = t_(1.2)
    S['alarm'] = square(1, al) * 0 + square(np.where((al * 2.5) % 1 < .5, 740, 560), al, .5) * .35 * np.minimum(1, (1.2 - al) * 6)
    S['unlock'] = delay(sweep(400, 1600, .5, 'tri', .5) * np.exp(-t_(.5) * 3), .1, .4, .4)
    S['step'] = lp(noise(.04), 1400) * np.exp(-t_(.04) * 90) * .5
    for k, v in S.items():
        write(os.path.join(out, 'SFX', f'sfx_{k}.wav'), v)
    return list(S)


# ---------------------------------------------------------------------------
# MUSIC: "Neon Rooftops" - A minor, 124 BPM, 32 bars, seamless loop
# ---------------------------------------------------------------------------
def music(out, name='music_level01', bpm=124, bars=32, menu=False):
    beat = 60 / bpm
    bar = beat * 4
    total = bars * bar
    N = int(total * SR)
    L = np.zeros(N); Rr = np.zeros(N)
    prog = [(57, [57, 60, 64]), (53, [53, 57, 60]), (48, [48, 52, 55]), (55, [55, 59, 62])]  # Am F C G

    def add(buf, sig, start):
        s = int(start * SR)
        e = min(N, s + len(sig))
        if s < N:
            buf[s:e] += sig[:e - s]

    def add_wrap(buf, sig, start):
        s = int(start * SR) % N
        n = len(sig)
        first = min(n, N - s)
        buf[s:s + first] += sig[:first]
        if n > first:
            buf[:n - first] += sig[first:]

    kick_t = t_(.35)
    kick = np.sin(2 * np.pi * np.cumsum(45 + 110 * np.exp(-kick_t * 30)) / SR) * np.exp(-kick_t * 9)
    kick += lp(noise(.35), 2000) * np.exp(-kick_t * 80) * .25
    sn_t = t_(.4)
    snare = bp(noise(.4), 900, 7000) * np.exp(-sn_t * 9) * .7 + np.sin(2 * np.pi * 190 * sn_t) * np.exp(-sn_t * 20) * .5
    snare = delay(snare, .07, .5, .4)  # gated-ish room
    hh_t = t_(.06)
    hat = hp(noise(.06), 7000) * np.exp(-hh_t * 70) * .2
    ohat = hp(noise(.25), 6000) * np.exp(-t_(.25) * 14) * .25

    duck = np.ones(N)
    sections = []
    for b in range(bars):
        sec = b // 8   # 0 intro, 1 groove, 2 lead, 3 lead+full
        if menu:
            sec = min(sec, 1) if b < 16 else 1
        sections.append(sec)
        root, chord = prog[(b // 2) % 4] if not menu else prog[b % 4]
        t0 = b * bar
        # drums
        if sec >= 1 or (sec == 0 and b >= 4):
            for k in range(4):
                add_wrap(L, kick * .9, t0 + k * beat); add_wrap(Rr, kick * .9, t0 + k * beat)
                ds = int((t0 + k * beat) * SR)
                dl = int(.22 * SR)
                if ds < N:
                    duck[ds:ds + dl] = np.minimum(duck[ds:ds + dl], np.linspace(.35, 1, min(dl, N - ds)))
        if sec >= 1:
            for k in (1, 3):
                add_wrap(L, snare * .55, t0 + k * beat); add_wrap(Rr, snare * .55, t0 + k * beat)
            for k in range(16 if sec >= 2 else 8):
                step = beat / (4 if sec >= 2 else 2)
                hv = hat * (1.0 if k % 2 == 0 else .6)
                add(L, hv * .8, t0 + k * step); add(Rr, hv, t0 + k * step + .004)
            add(L, ohat, t0 + 3.5 * beat); add(Rr, ohat, t0 + 3.5 * beat)
        # bass: 8ths with octave jumps
        for k in range(8):
            if sec == 0 and b < 4:
                break
            nn = root - 24 + (12 if k % 2 == 1 else 0)
            dur = beat / 2 * .9
            tt = t_(dur)
            f = midi(nn)
            sig = (saw(f, tt) + saw(f * 1.006, tt)) * .5
            fe = np.exp(-tt * 18)
            sig = (lp(sig, 1300) * fe + lp(sig, 380) * (1 - fe)) * env_adsr(len(tt), .004, .05, .7, .03)
            add(L, sig * .55, t0 + k * beat / 2); add(Rr, sig * .55, t0 + k * beat / 2)
        # pad (per bar)
        tt = t_(bar)
        pad = np.zeros(len(tt)); padR = np.zeros(len(tt))
        for nn in chord:
            f = midi(nn)
            pad += saw(f * .998, tt) + saw(f * 1.004, tt, .3)
            padR += saw(f * 1.002, tt, .5) + saw(f * .996, tt, .7)
        pe = env_adsr(len(tt), .25, .2, .8, .25)
        cut = 900 if sec < 3 else 1500
        add(L, lp(pad, cut) * pe * .09, t0); add(Rr, lp(padR, cut) * pe * .09, t0)
        # arp 16ths
        if sec >= 0:
            arp_notes = chord + [chord[0] + 12, chord[1] + 12, chord[2], chord[1]]
            for k in range(16):
                nn = arp_notes[k % len(arp_notes)] + 12
                d = beat / 4
                tt = t_(d * .8)
                sig = square(midi(nn), tt, .25) * np.exp(-tt * 14)
                sig = lp(sig, 2400 if sec else 1600)
                g = .12 if sec else .16
                if k % 2 == 0:
                    add(L, sig * g, t0 + k * d)
                    add(Rr, sig * g * .55, t0 + k * d)
                else:
                    add(Rr, sig * g, t0 + k * d)
                    add(L, sig * g * .55, t0 + k * d)
    # lead melody in sections 2-3 (8-bar phrase over 2-bar chords)
    melody = [  # (beat offset within 8 bars, midi, beats)
        (0, 76, 1.5), (1.5, 74, .5), (2, 72, 1), (3, 71, 1), (4, 72, 2), (6, 69, 2),
        (8, 72, 1.5), (9.5, 74, .5), (10, 76, 1), (11, 77, 1), (12, 76, 3), (15, 74, 1),
        (16, 79, 1.5), (17.5, 76, .5), (18, 74, 1), (19, 72, 1), (20, 74, 2), (22, 71, 2),
        (24, 72, 1), (25, 74, 1), (26, 76, 2), (28, 81, 2.5), (30.5, 79, .5), (31, 76, 1),
    ]
    lead = np.zeros(N)
    if not menu:
        for rep in (2, 3):
            base = rep * 8 * bar
            for off, nn, ln in melody:
                d = ln * beat
                tt = t_(d)
                f = midi(nn) * (1 + 0.006 * np.sin(2 * np.pi * 5.5 * tt) * np.minimum(1, tt * 3))
                ph = np.cumsum(f) / SR
                sig = (2 * (ph % 1) - 1) * .6 + np.sign(np.sin(2 * np.pi * ph * 1.003)) * .3
                sig = lp(sig, 2600) * env_adsr(len(tt), .02, .1, .75, .08)
                s = int((base + off * beat) * SR)
                lead[s:s + len(sig)] += sig[:max(0, N - s)]
        lead = delay(lead, beat * .75, .4, .35)
    L += lead * .22; Rr += np.concatenate([np.zeros(int(.012 * SR)), lead[:-int(.012 * SR)]]) * .22
    # sidechain pump on everything except drums is approximated on the whole mix
    L = L * (0.55 + 0.45 * duck); Rr = Rr * (0.55 + 0.45 * duck)
    mix_ = np.vstack([L, Rr])
    mix_ = np.tanh(mix_ * 1.4) / np.tanh(1.4)
    write(os.path.join(out, 'Music', f'{name}.wav'), mix_, stereo=True)


if __name__ == '__main__':
    out = sys.argv[1]
    names = sfx_all(out)
    print('sfx', len(names))
    music(out)
    music(out, 'music_menu', bpm=100, bars=16, menu=True)
    print('music done')
