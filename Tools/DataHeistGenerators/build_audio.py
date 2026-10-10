"""Builds the game's audio from CC0 sources: Kenney sound packs and OpenGameArt music.

    pip install numpy scipy soundfile pyloudnorm
    python3 build_audio.py <Resources/DataHeist>

Downloads the pinned sources into .audio_cache/ (gitignored), then, for every file the game
plays (same names the code uses), mixes its recipe: layers with offsets and gains, an optional
slice, low-pass or pitch. Every effect is trimmed, faded and loudness-normalised, so the in-code
volume multipliers are the mix. Music is loudness-matched to one target and looped cleanly.
sfx_alarm and sfx_level_complete stay as synthesised by gen_audio.py (nothing in the packs fits).

All sources are CC0 (public domain). Credits are kept anyway, in CREDITS below and in the game.
"""
import io
import os
import sys
import urllib.request
import zipfile
import numpy as np
import soundfile as sf
import pyloudnorm as pyln
from scipy.signal import butter, resample_poly, sosfilt

SR = 44100
HERE = os.path.dirname(os.path.abspath(__file__))
CACHE = os.path.join(HERE, '.audio_cache')

PACKS = {   # Kenney, CC0 - https://kenney.nl
    'scifi': 'https://kenney.nl/media/pages/assets/sci-fi-sounds/6b296f9ecf-1677589334/kenney_sci-fi-sounds.zip',
    'impact': 'https://kenney.nl/media/pages/assets/impact-sounds/87b4ddecda-1677589768/kenney_impact-sounds.zip',
    'ui': 'https://kenney.nl/media/pages/assets/interface-sounds/fa43c1dd4d-1677589452/kenney_interface-sounds.zip',
    'digital': 'https://kenney.nl/media/pages/assets/digital-audio/216eac4753-1677590265/kenney_digital-audio.zip',
}

MUSIC = {   # OpenGameArt, CC0. (file name in game, url, seconds to skip, author, title)
    'music_menu': ('https://opengameart.org/sites/default/files/midnight_drive.ogg', 0.0, 'congusbongus', 'Midnight Drive'),
    'music_story': ('https://opengameart.org/sites/default/files/Cyberpunk%20Moonlight%20Sonata%20v2.mp3', 0.0, 'joth', 'Cyberpunk Moonlight Sonata'),
    'music_level01': ('https://opengameart.org/sites/default/files/cyber_runner.ogg', 0.0, 'ansimuz', 'Cyber Runner'),
    # the first ~29 s are a quiet intro; a level should start on the beat
    'music_level02': ('https://opengameart.org/sites/default/files/cybermarch.wav', 'auto', 'iamoneabe', 'Cyber March'),
    # Level 3: tense infiltration, the Guardian fight, then the lockdown escape
    'music_level03': ('https://opengameart.org/sites/default/files/darkcity.wav', 0.0, 'cinameng', 'Dark City'),
    'music_boss': ('https://opengameart.org/sites/default/files/boss_fight_2.ogg', 0.0, 'ansimuz', 'Boss Fight 2'),
    'music_escape': ('https://opengameart.org/sites/default/files/die_hardv1.ogg', 0.0, 'centurionofwar', 'Die Hard Battle'),
}

CREDITS = [
    'Sound effects: Kenney (kenney.nl) - Sci-Fi Sounds, Impact Sounds, Interface Sounds, Digital Audio (CC0)',
] + [f'Music: "{t}" by {a} (OpenGameArt.org, CC0)' for (_, _, a, t) in MUSIC.values()]

# name -> list of layers: (pack, file stem, gain, offset seconds, options)
#   options: slice=(start, end) seconds, lowpass=Hz, pitch=ratio
L = lambda pack, stem, gain=1.0, at=0.0, **o: (pack, stem, gain, at, o)
SFX = {
    # movement
    'jump':        [L('digital', 'phaseJump3'), L('impact', 'footstep_concrete_003', 0.6)],
    'air_jump':    [L('digital', 'phaserUp6'), L('scifi', 'thrusterFire_002', 0.5, 0.0, slice=(0.17, 0.45))],
    'land':        [L('impact', 'footstep_concrete_000'), L('impact', 'impactSoft_medium_000', 0.7)],
    'slide':       [L('ui', 'scratch_005', 0.9, lowpass=4200), L('impact', 'footstep_concrete_004', 0.5)],
    'step':        [L('impact', 'footstep_concrete_001', 0.8)],
    'dash':        [L('scifi', 'forceField_002'), L('scifi', 'thrusterFire_000', 0.35, 0.0, slice=(0.17, 0.5))],
    # combat
    'attack':      [L('scifi', 'laserSmall_004'), L('ui', 'scratch_001', 0.3, lowpass=6000)],
    'hurt':        [L('impact', 'impactPunch_heavy_000'), L('ui', 'glitch_002', 0.5, 0.02), L('ui', 'error_005', 0.25, 0.05)],
    'explode':     [L('scifi', 'explosionCrunch_000'), L('scifi', 'lowFrequency_explosion_001', 0.8)],
    'shoot':       [L('scifi', 'laserRetro_004', 0.9)],
    'deflect':     [L('impact', 'impactMetal_light_000'), L('scifi', 'laserRetro_002', 0.3, 0.02, pitch=1.4)],
    'emp':         [L('scifi', 'lowFrequency_explosion_000'), L('scifi', 'forceField_000', 0.8), L('digital', 'zapThreeToneDown', 0.4, 0.1)],
    'glitch':      [L('ui', 'glitch_001'), L('ui', 'glitch_003', 1.0, 0.06), L('ui', 'glitch_002', 1.0, 0.11),
                    L('ui', 'glitch_004', 1.0, 0.17), L('digital', 'zapThreeToneDown', 0.45, 0.2, slice=(0.0, 0.6))],
    # Level 2 hazards
    'sniper_aim':  [L('digital', 'phaserUp2', 0.8, pitch=0.55), L('scifi', 'computerNoise_000', 0.35, 0.0, slice=(0.0, 0.6))],
    'sniper_shot': [L('scifi', 'laserLarge_000'), L('impact', 'impactMetal_heavy_000', 0.6)],
    'shock':       [L('digital', 'zap1'), L('ui', 'glitch_004', 0.6, 0.03)],
    'sign_creak':  [L('impact', 'impactMetal_light_002', 0.8, pitch=0.7), L('impact', 'impactPlank_medium_000', 0.5, 0.08)],
    'sign_crash':  [L('impact', 'impactMetal_heavy_002'), L('impact', 'impactGlass_heavy_000', 0.8, 0.02),
                    L('impact', 'impactPlate_heavy_000', 0.6, 0.05)],
    # Level 3 hazards
    'turret_aim':  [L('digital', 'phaserUp4', 0.7, pitch=0.7), L('ui', 'tick_004', 0.6, 0.05)],
    'turret_shot': [L('scifi', 'laserSmall_001'), L('impact', 'impactMetal_light_001', 0.5)],
    'laser_on':    [L('scifi', 'forceField_003', 0.8, slice=(0.0, 0.3))],
    'press_warn':  [L('scifi', 'engineCircular_000', 0.7, 0.0, slice=(0.0, 0.35)), L('ui', 'error_008', 0.4)],
    'press_slam':  [L('impact', 'impactPlate_heavy_002'), L('scifi', 'lowFrequency_explosion_001', 0.7), L('impact', 'impactMetal_heavy_001', 0.6)],
    # pickups and level objects
    'byte':        [L('digital', 'pepSound3', 0.9)],
    'repair':      [L('digital', 'powerUp11')],
    'shard':       [L('digital', 'zapThreeToneUp'), L('digital', 'powerUp12', 0.5, 0.25)],
    'pad':         [L('digital', 'phaserUp1')],
    'checkpoint':  [L('ui', 'confirmation_002'), L('digital', 'powerUp2', 0.5, 0.05)],
    'unlock':      [L('digital', 'powerUp12'), L('ui', 'confirmation_003', 0.6, 0.15)],
    # run flow
    'count':       [L('digital', 'tone1')],
    'go':          [L('digital', 'highUp'), L('ui', 'confirmation_003', 0.5)],
    'game_over':   [L('digital', 'lowThreeTone'), L('scifi', 'lowFrequency_explosion_000', 0.5)],
    'denied':      [L('ui', 'error_006')],
    # interface
    'ui_move':     [L('ui', 'click_005')],
    'ui_select':   [L('ui', 'select_004')],
    'ui_back':     [L('ui', 'back_002')],
}

# relative loudness per kind of sound (dBFS RMS of the loudest 50 ms); the code's per-call
# volumes then balance moments against each other
TARGET = {'ui_move': -18, 'ui_select': -15, 'ui_back': -15, 'count': -14, 'byte': -14, 'step': -18}
# synthesised by gen_audio.py and kept; only brought into the same loudness balance (idempotent)
KEEP = {'alarm': -14.0, 'level_complete': -11.0}
DEFAULT_TARGET = -11.0
MUSIC_LUFS = -16.0


# --------------------------------------------------------------------------- sources
def fetch(url, dest):
    if os.path.exists(dest):
        return dest
    os.makedirs(os.path.dirname(dest), exist_ok=True)
    print('  downloading', url)
    req = urllib.request.Request(url, headers={'User-Agent': 'DataHeist-build-audio'})
    with urllib.request.urlopen(req, timeout=300) as r, open(dest, 'wb') as f:
        f.write(r.read())
    return dest


def pack_dir(pack):
    d = os.path.join(CACHE, pack)
    if not os.path.isdir(d):
        data = open(fetch(PACKS[pack], d + '.zip'), 'rb').read()
        with zipfile.ZipFile(io.BytesIO(data)) as z:
            z.extractall(d)       # zipfile strips absolute paths and '..'
    return d


_index = {}


def find(pack, stem):
    if pack not in _index:
        idx = {}
        for dp, _, files in os.walk(pack_dir(pack)):
            for f in files:
                if f.endswith('.ogg'):
                    idx[f[:-4]] = os.path.join(dp, f)
        _index[pack] = idx
    if stem not in _index[pack]:
        raise KeyError(f'{stem} not found in {pack} pack')
    return _index[pack][stem]


def read_mono(path):
    x, sr = sf.read(path, always_2d=True, dtype='float32')
    x = x.mean(1)
    if sr != SR:
        g = np.gcd(sr, SR)
        x = resample_poly(x, SR // g, sr // g).astype(np.float32)
    return x


# --------------------------------------------------------------------------- processing
def lowpass(x, hz):
    return sosfilt(butter(4, hz, 'low', fs=SR, output='sos'), x).astype(np.float32)


def pitch(x, ratio):
    # resample to shift pitch and length together, like Unity's AudioSource.pitch
    n = max(1, int(round(len(x) / ratio)))
    return np.interp(np.linspace(0, len(x) - 1, n), np.arange(len(x)), x).astype(np.float32)


def fades(x, fin=0.003, fout=0.02):
    a, b = int(fin * SR), int(fout * SR)
    x = x.copy()
    if a: x[:a] *= np.linspace(0, 1, a)
    if b and len(x) > b: x[-b:] *= np.linspace(1, 0, b) ** 2
    return x


def trim(x, db=-50):
    thr = 10 ** (db / 20) * (np.max(np.abs(x)) + 1e-9)
    idx = np.where(np.abs(x) > thr)[0]
    return x[max(0, idx[0] - int(0.002 * SR)): idx[-1] + int(0.01 * SR)] if len(idx) else x


def mix(layers):
    parts = []
    for pack, stem, gain, at, o in layers:
        x = read_mono(find(pack, stem))
        if 'slice' in o:
            s, e = o['slice']
            x = fades(x[int(s * SR):int(e * SR)], 0.005, 0.06)
        if 'lowpass' in o: x = lowpass(x, o['lowpass'])
        if 'pitch' in o: x = pitch(x, o['pitch'])
        parts.append((int(at * SR), x * gain))
    n = max(off + len(x) for off, x in parts)
    out = np.zeros(n, np.float32)
    for off, x in parts:
        out[off:off + len(x)] += x
    return out


def normalise_sfx(x, target_db):
    x = trim(x)
    win = int(0.05 * SR)
    if len(x) > win:
        rms = np.sqrt(np.convolve(x ** 2, np.ones(win) / win, 'valid')).max()
    else:
        rms = np.sqrt(np.mean(x ** 2))
    x = x * (10 ** (target_db / 20) / (rms + 1e-9))
    peak = np.max(np.abs(x))
    if peak > 0.89:                              # soft ceiling at -1 dBFS
        x = np.tanh(x / peak * 1.4) / np.tanh(1.4) * 0.89
    return fades(x)


def build_sfx(out_dir):
    os.makedirs(out_dir, exist_ok=True)
    for name, layers in SFX.items():
        x = normalise_sfx(mix(layers), TARGET.get(name, DEFAULT_TARGET))
        sf.write(os.path.join(out_dir, f'sfx_{name}.wav'), x, SR, subtype='PCM_16')
        print(f'  sfx_{name}.wav  {len(x) / SR * 1000:5.0f} ms')
    for name, target in KEEP.items():
        path = os.path.join(out_dir, f'sfx_{name}.wav')
        if os.path.exists(path):
            x = normalise_sfx(read_mono(path), target)
            sf.write(path, x, SR, subtype='PCM_16')
            print(f'  sfx_{name}.wav  kept (synthesised), levelled to {target} dB')


def kick_in(x, sr):
    """where the full beat starts: the first 2 s that stay within 6 dB of the track's typical
    level (isolated intro hits don't qualify), refined to 10 ms"""
    blk = sr // 2
    db = np.array([20 * np.log10(np.sqrt(np.mean(x[i:i + blk] ** 2)) + 1e-9) for i in range(0, len(x) - blk, blk)])
    thr = np.median(db) - 6
    for i in range(len(db) - 4):
        if np.all(db[i:i + 4] > thr):
            fine = sr // 100
            lo = max(0, (i - 1) * blk)
            for j in range(lo, (i + 1) * blk, fine):
                if 20 * np.log10(np.sqrt(np.mean(x[j:j + fine] ** 2)) + 1e-9) > thr:
                    return max(0, j - fine)
            return i * blk
    return 0


def build_music(out_dir):
    os.makedirs(out_dir, exist_ok=True)
    meter = pyln.Meter(SR)
    for name, (url, skip, author, title) in MUSIC.items():
        src = fetch(url, os.path.join(CACHE, 'music', os.path.basename(url).replace('%20', '_')))
        x, sr = sf.read(src, always_2d=True, dtype='float32')
        if sr != SR:
            g = np.gcd(sr, SR)
            x = resample_poly(x, SR // g, sr // g, axis=0).astype(np.float32)
        start = kick_in(x.mean(1), SR) if skip == 'auto' else int(skip * SR)
        x = x[start:]
        loud = meter.integrated_loudness(x)
        x = x * 10 ** ((MUSIC_LUFS - loud) / 20)
        x = np.clip(x, -0.98, 0.98)
        n = int(0.015 * SR)                     # click-free loop seam
        x[:n] *= np.linspace(0, 1, n)[:, None]
        x[-n:] *= np.linspace(1, 0, n)[:, None]
        # write in blocks: libsndfile's Vorbis encoder can crash on one huge write
        with sf.SoundFile(os.path.join(out_dir, f'{name}.ogg'), 'w', SR, x.shape[1], format='OGG', subtype='VORBIS') as f:
            for i in range(0, len(x), SR):
                f.write(np.ascontiguousarray(x[i:i + SR]))
        print(f'  {name}.ogg  "{title}" by {author}  {len(x) / SR:5.1f} s  (skipped {start / SR:.1f} s)  {MUSIC_LUFS} LUFS')


if __name__ == '__main__':
    res = sys.argv[1]
    print('sound effects:')
    build_sfx(os.path.join(res, 'Audio', 'SFX'))
    print('music:')
    build_music(os.path.join(res, 'Audio', 'Music'))
    with open(os.path.join(res, 'Audio', 'CREDITS.txt'), 'w') as f:
        f.write('\n'.join(CREDITS) + '\n')
    print('\n'.join(CREDITS))
