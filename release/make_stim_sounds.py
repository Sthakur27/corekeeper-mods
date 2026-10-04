"""Synthesizes StimHits' built-in metallic sounds (original, stdlib only).

    python release/make_stim_sounds.py   # writes StimHits/Sounds/stim_hit.wav + stim_hurt.wav

Bell/bar-like inharmonic partials with exponential decay plus a short noise transient.
"""
import math, os, random, struct, wave

RATE = 44100
OUT = os.path.join(os.path.dirname(__file__), "..", "StimHits", "Sounds")


def tone(f0, partials, length, click, gain, seed):
    rnd = random.Random(seed)
    n = int(RATE * length)
    out = []
    for i in range(n):
        t = i / RATE
        s = 0.0
        for ratio, amp, decay in partials:
            s += amp * math.exp(-t / decay) * math.sin(2 * math.pi * f0 * ratio * t)
        if t < click:  # strike transient
            s += (rnd.random() * 2 - 1) * 0.6 * (1 - t / click) ** 2
        attack = min(1.0, t / 0.0015)
        tail = min(1.0, (length - t) / 0.02)
        out.append(s * attack * tail * gain)
    peak = max(abs(x) for x in out)
    return [x / peak * 0.9 for x in out]


def write(name, samples):
    os.makedirs(OUT, exist_ok=True)
    with wave.open(os.path.join(OUT, name), "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(b"".join(struct.pack("<h", int(max(-1, min(1, x)) * 32767)) for x in samples))


# Hit: bright, short "ting".
write("stim_hit.wav", tone(1760, [(1.0, 1.0, 0.16), (2.76, 0.55, 0.08), (5.40, 0.3, 0.04), (8.93, 0.15, 0.02), (1.004, 0.5, 0.14)], 0.38, 0.004, 1.0, 1))
# Hurt: lower, heavier "clang" with beating partials.
write("stim_hurt.wav", tone(620, [(1.0, 1.0, 0.25), (1.013, 0.8, 0.22), (2.32, 0.6, 0.12), (4.25, 0.4, 0.06), (6.8, 0.25, 0.03)], 0.55, 0.008, 1.0, 2))
print("wrote", os.path.abspath(OUT))
