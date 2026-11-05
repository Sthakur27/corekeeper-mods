"""Enchanting balance calculator: worst-case and typical enchant combos across all slots.

Analysis only. Mirrors how the game folds stats (docs/balance-model.md, decompiled sources):
  damage   = base x (1 + D/1000) x crit x (1 + C/100)              EntityUtility.cs:2132
  crit     = 1 + min(cc,100)/100 x (0.5 + cd/100)                  EntityUtility.cs:2031-2032, cap :403
  rate     = hits/s x (1 + AS/1000)                                MeleeAttackStateSystem.cs:384
  max HP   = flatHP x (1 + HP%/100)                                CharacterStatsWindow.cs:276-282
  taken    = x (1000 + DT)/1000 (DT upper cap 900, no lower cap)  EntityUtility.cs:2133-2152, Summarize :400
  boss dmg = x (100 - BW)/100, BW cap 90                           Summarize :401
  dodge    = DodgeChance %, cap 90                                 Summarize :399
  move     = 1 + MS/1000, only a LOWER cap (-900)                  Summarize :398
  regen    = (HoT/10 + maxHP x pct/1000) x (1 + RegenEff/100)      HealFromConditionsSystem.cs:173-175
  Spellsurge: on crit, +value x 10 raw magic damage for 6 s        EntityUtility.cs:2049-2057
Model B (Relative) enchants are simulated exactly like EnchantEffectSystem: one entry per write condition,
raw = p x (1000 + bucket - own entry), iterated to the fixed point.

Run:  python balance_calc.py            (all tables, markdown)
      python balance_calc.py --brief    (summary lines only)
"""
import itertools
import math
import sys

# --------------------------------------------------------------------------------------------------
# Enchant catalogue (Scripts/Enchants.cs + planned accessory pool). stat = which model stat it feeds.
# unit: raw units per displayed value (10 for tenths conditions).
# --------------------------------------------------------------------------------------------------
E = {}


def d(key, stat, vals, slots, mode="flat", unit=1, cond=None):
    E[key] = dict(key=key, stat=stat, vals=vals, slots=set(slots), mode=mode, unit=unit, cond=cond or stat)


MELEE, RANGED, MAGIC, ARMOR, PANTS, ACC, OFF = "melee", "ranged", "magic", "armor", "pants", "acc", "off"
# weapons
d("sharpness", "dmg", [4, 8, 12], [MELEE], "rel", cond="PhysMelee")
d("power", "dmg", [4, 8, 12], [RANGED], "rel", cond="PhysRange")
d("arcane", "dmg", [4, 8, 12], [MAGIC], "rel", cond="Magic")
d("swiftblade", "as", [3, 6, 10], [MELEE], "rel", cond="MeleeAS")
d("quickdraw", "as", [3, 6, 10], [RANGED, MAGIC], "rel", cond="RangeAS")
d("keen", "crit", [3, 6, 9], [MELEE, RANGED, MAGIC])
d("bossbane", "bossC", [5, 10, 15], [MELEE, RANGED, MAGIC])
d("lifesteal", "loh", [50, 100, 150], [MELEE], "itemlevel")
d("spellsurge", "surge", [10, 20, 30], [MAGIC])
d("fireaspect", "util", [1, 2, 3], [MELEE])
d("knockback", "util", [1, 2, 3], [MELEE])
d("venom", "util", [1, 2, 3], [MELEE])
d("stunning", "util", [3, 6, 9], [MELEE])
# armor (helm/chest/pants)
d("resilience", "dt", [-3, -5, -8], [ARMOR, OFF], unit=10)
d("vitality", "hp", [3, 6, 10], [ARMOR, OFF])
d("evasion", "dodge", [2, 4, 6], [ARMOR])
d("regeneration", "regen", [10, 20, 30], [ARMOR])
d("bosswarden", "bw", [3, 6, 9], [ARMOR])
d("thorns", "util", [1, 2, 3], [ARMOR])
d("swiftness", "move", [3, 6, 9], [PANTS, OFF], unit=10)
# planned accessory pool (ring1, ring2, necklace, offhand), Max-stacked per enchant
d("might", "dmg", [1, 2, 3], [ACC, OFF], "rel", cond="AllDamage")
d("precision", "crit", [1, 2, 3], [ACC, OFF])
d("ferocity", "critdmg", [2, 4, 6], [ACC, OFF])
d("haste", "as", [1, 2, 3], [ACC, OFF], "rel", cond="AttackSpeed")
d("fleetfoot", "move", [1, 2, 3], [ACC, OFF], unit=10, cond="move")
d("hardiness", "hp", [1, 2, 3], [ACC, OFF])
d("greenthumb", "util", [5, 10, 15], [ACC, OFF])
d("angler", "util", [1, 2, 3], [ACC, OFF])
d("gourmet", "util", [1, 2, 3], [ACC, OFF])

ARMOR_SLOTS = ["helm", "chest", "pants"]
ACC_SLOTS = ["ring1", "ring2", "necklace", "offhand"]

# --------------------------------------------------------------------------------------------------
# Vanilla baselines. Curves from balance-model.md 1.4; late values anchored on Sid's measured L20 build
# (+47% damage bucket). Everything else is a reasonable, mid-roll vanilla kit (gear + skills + food).
# --------------------------------------------------------------------------------------------------
STAGES = {
    "early (L5)": dict(L=5, D=100, AS=50, cc=5, cd=0, C=0, flatHP=300, hpPct=0, DT=0, dodge=3, move=0,
                       BW=0, hot=5, hotPct=0, loh=0, hits=2.0),
    "mid (L12)": dict(L=12, D=300, AS=200, cc=12, cd=10, C=10, flatHP=600, hpPct=15, DT=-100, dodge=8,
                      move=50, BW=8, hot=20, hotPct=3, loh=3, hits=2.0),
    "late (L20)": dict(L=20, D=470, AS=400, cc=20, cd=25, C=20, flatHP=1000, hpPct=30, DT=-190, dodge=12,
                       move=100, BW=19, hot=40, hotPct=5, loh=6, hits=2.0),
}

THRESH = dict(dps=0.60, ehp=0.60, move=0.25)

# --------------------------------------------------------------------------------------------------
# Stacking rules
# --------------------------------------------------------------------------------------------------
CURRENT = dict(
    name="current + planned accessory Max",
    armor_stack={},            # per-key override for armor: "max" (default sum)
    acc_max=True,              # accessories: per enchant only the highest copy
    offhand_joins_armor=True,  # offhand Vitality/Resilience/Swiftness Sum with armor copies
    caps={"dt": (-250, None)},  # our-total caps by stat (raw units)
    vals={},                   # per-key value overrides
)


def rules(**kw):
    r = {k: (dict(v) if isinstance(v, dict) else v) for k, v in CURRENT.items()}
    r.update(kw)
    return r


PROPOSED = rules(
    name="proposed",
    # Offhand Vitality/Resilience/Swiftness keep summing with armor: the per-condition caps below bind first, so a
    # special offhand rule changes nothing (checked: identical results with offhand_joins_armor=False).
    offhand_joins_armor=True,
    caps={                       # our own total per stat, raw units (vanilla gear/food come on top)
        "dt": (-120, None),      # Resilience total >= -12% (was -25%)
        "hp": (None, 15),        # Vitality + Hardiness <= +15% max HP
        "dodge": (None, 8),      # Evasion <= 8
        "move": (None, 120),     # Swiftness + Fleetfoot <= 12%
        "regen": (None, 60),     # Regeneration <= +60%
        "bw": (None, 9),         # Bosswarden <= 9 (one piece at III)
    },
    vals={"spellsurge": [5, 10, 15], "lifesteal": [50, 75, 100]},
)

ALT_MAX = rules(
    name="alternative: Max across armor for Resilience/Vitality/Evasion/Bosswarden",
    armor_stack={"resilience": "max", "evasion": "max", "bosswarden": "max", "vitality": "max"},
    offhand_joins_armor=False,
)

# --------------------------------------------------------------------------------------------------
# Model
# --------------------------------------------------------------------------------------------------


def value(key, lvl, r):
    vals = r["vals"].get(key, E[key]["vals"])
    return vals[lvl - 1]


def fold(build, r, stage):
    """build: {slot: [(key, lvl), ...]} -> our totals per stat (raw units) + relative permille per write cond."""
    flat, rel = {}, {}
    acc_best = {}            # key -> best raw among accessories
    armor_vals = {}          # key -> list of raw values from armor pieces
    off_vals = {}

    def raw_of(key, lvl):
        e = E[key]
        if e["mode"] == "itemlevel":   # Lifesteal: % of vanilla max(1, 0.5L)
            return max(1, round(value(key, lvl, r) / 100 * max(1, 0.5 * stage["L"])))
        return value(key, lvl, r) * e["unit"]

    for slot, ens in build.items():
        for key, lvl in ens:
            e = E[key]
            if e["mode"] == "rel":
                continue
            v = raw_of(key, lvl)
            if slot in ARMOR_SLOTS:
                armor_vals.setdefault(key, []).append(v)
            elif slot == "offhand" and ARMOR in e["slots"] or (slot == "offhand" and key == "swiftness"):
                off_vals.setdefault(key, []).append(v)
            elif slot in ACC_SLOTS:
                acc_best[key] = max(acc_best.get(key, v), v, key=abs) if r["acc_max"] else acc_best.get(key, 0) + v
            else:  # held weapon
                flat[e["stat"]] = flat.get(e["stat"], 0) + v
    for key, vs in armor_vals.items():
        mode = r["armor_stack"].get(key, "sum")
        tot = sum(vs) if mode == "sum" else max(vs, key=abs)
        armor_vals[key] = tot
    for key, vs in off_vals.items():
        v = vs[0]
        if r["offhand_joins_armor"]:
            armor_vals[key] = armor_vals.get(key, 0) + v
        else:  # same enchant on armor and offhand: offhand only counts if the armor total is lower
            armor_vals[key] = max(armor_vals.get(key, 0), v, key=abs)
    for src in (armor_vals, acc_best):
        for key, v in src.items():
            st = E[key]["stat"]
            flat[st] = flat.get(st, 0) + v
    for st, (lo, hi) in r["caps"].items():
        if st in flat:
            if lo is not None:
                flat[st] = max(lo, flat[st])
            if hi is not None:
                flat[st] = min(hi, flat[st])

    # Relative: one entry per write condition, permille summed (held weapon) / Max (accessories)
    acc_rel = {}
    for slot, ens in build.items():
        for key, lvl in ens:
            e = E[key]
            if e["mode"] != "rel":
                continue
            p = value(key, lvl, r) * 10
            if slot in ACC_SLOTS and r["acc_max"]:
                acc_rel[key] = max(acc_rel.get(key, 0), p)
            else:
                rel.setdefault((e["stat"], e["cond"]), 0)
                rel[(e["stat"], e["cond"])] += p
    for key, p in acc_rel.items():
        k = (E[key]["stat"], E[key]["cond"])
        rel[k] = rel.get(k, 0) + p
    return flat, rel


def solve_bucket(base, perms):
    """Model B fixed point: raw_i = p_i x (1000 + base + sum_{j != i} raw_j). Returns the bucket total."""
    raws = [0.0] * len(perms)
    for _ in range(60):
        tot = sum(raws)
        raws = [p / 1000 * (1000 + max(0, base + tot - raws[i])) for i, p in enumerate(perms)]
    return base + sum(raws)


def stats(stage, build, r, weapon="melee", vs_boss=False):
    s = STAGES[stage]
    flat, rel = fold(build, r, s)
    g = lambda k: flat.get(k, 0)
    out = {}
    # ---- offence
    cc = min(100, s["cc"] + g("crit"))
    cd = s["cd"] + g("critdmg")
    crit = 1 + cc / 100 * (0.5 + cd / 100)
    # Spellsurge (magic only): +surge x 10 raw magic damage while a crit happened in the last 6 s
    D0 = s["D"]
    if weapon == "magic" and g("surge"):
        hits6 = 6 * s["hits"] * (1 + s["AS"] / 1000)
        uptime = 1 - (1 - cc / 100) ** hits6
        D0 += g("surge") * 10 * uptime
    D = solve_bucket(D0, [p for (st, _), p in rel.items() if st == "dmg"])
    AS = solve_bucket(s["AS"], [p for (st, _), p in rel.items() if st == "as"])
    C = (s["C"] + g("bossC")) if vs_boss else 0
    base = (10 + 10 * s["L"]) * 1.7
    hit = base * (1 + D / 1000) * crit * (1 + C / 100)
    rate = s["hits"] * (1 + AS / 1000)
    out.update(D=D, AS=AS, cc=cc, cd=cd, crit=crit, hit=hit, rate=rate, dps=hit * rate)
    # ---- defence
    hp = s["flatHP"] * max(0, 1 + (s["hpPct"] + g("hp")) / 100)
    dt = min(900, s["DT"] + g("dt"))
    taken = (1000 + dt) / 1000
    dodge = min(90, s["dodge"] + g("dodge"))
    bw = min(90, s["BW"] + g("bw")) if vs_boss else 0
    out.update(hp=hp, dt=dt, dodge=dodge, bw=bw,
               ehp=hp / taken / (1 - dodge / 100) / ((100 - bw) / 100))
    out["move"] = 1 + (s["move"] + g("move")) / 1000
    # ---- sustain (hp/s)
    regen_eff = 1 + g("regen") / 100
    out["regen"] = (s["hot"] + hp * s["hotPct"] / 1000) * regen_eff  # hot in hp/s, hotPct in tenths of %max/s
    out["leech"] = (s["loh"] + g("loh")) * rate
    out["sustain"] = out["regen"] + out["leech"]
    out["sustain_pct"] = out["sustain"] / hp * 100
    return out


# --------------------------------------------------------------------------------------------------
# Builds
# --------------------------------------------------------------------------------------------------
VANILLA = {}

TYPICAL = {  # 1-2 enchants per item, all at II
    "weapon": [("sharpness", 2), ("swiftblade", 2)],
    "helm": [("vitality", 2), ("evasion", 2)],
    "chest": [("resilience", 2), ("regeneration", 2)],
    "pants": [("swiftness", 2), ("resilience", 2)],
    "ring1": [("might", 2), ("gourmet", 2)],
    "ring2": [("precision", 2), ("angler", 2)],
    "necklace": [("hardiness", 2)],
    "offhand": [("resilience", 2)],
}

ACC_BEST = {  # every Max-stacked combat stat once at III; offhand carries the armor-shared three
    "ring1": [("might", 3), ("haste", 3), ("precision", 3)],
    "ring2": [("ferocity", 3), ("hardiness", 3), ("fleetfoot", 3)],
    "necklace": [("greenthumb", 3), ("gourmet", 3), ("angler", 3)],
    "offhand": [("resilience", 3), ("vitality", 3), ("swiftness", 3)],
}

ARMOR_POOL = ["resilience", "vitality", "evasion", "regeneration", "bosswarden", "thorns"]
PANTS_POOL = ARMOR_POOL + ["swiftness"]
WEAPON_POOLS = {
    "melee": ["sharpness", "swiftblade", "keen", "bossbane", "lifesteal", "fireaspect", "knockback", "venom", "stunning"],
    "magic": ["arcane", "quickdraw", "keen", "bossbane", "spellsurge"],
}


def best_weapon(stage, r, weapon, vs_boss, base):
    best = None
    for combo in itertools.combinations(WEAPON_POOLS[weapon], 3):
        b = dict(base, weapon=[(k, 3) for k in combo])
        v = stats(stage, b, r, weapon, vs_boss)["dps"]
        if best is None or v > best[0]:
            best = (v, combo)
    return best[1]


def best_armor(stage, r, vs_boss, base):
    best = None
    a3 = list(itertools.combinations(ARMOR_POOL, 3))
    p3 = list(itertools.combinations(PANTS_POOL, 3))
    for h, c in itertools.combinations_with_replacement(a3, 2):  # helm/chest pools are identical
        if True:
            for p in p3:
                b = dict(base, helm=[(k, 3) for k in h], chest=[(k, 3) for k in c], pants=[(k, 3) for k in p])
                v = stats(stage, b, r, "melee", vs_boss)["ehp"]
                if best is None or v > best[0] + 1e-9:
                    best = (v, (h, c, p))
    return best[1]


def best_build(stage, r, weapon="melee", vs_boss=False):
    b = dict(ACC_BEST)
    b["weapon"] = [(k, 3) for k in best_weapon(stage, r, weapon, vs_boss, b)]
    h, c, p = best_armor(stage, r, vs_boss, b)
    b.update(helm=[(k, 3) for k in h], chest=[(k, 3) for k in c], pants=[(k, 3) for k in p])
    return b


def degenerate():
    """Named stacks (armor filled with the stack, rest vanilla unless stated)."""
    three = lambda k: {s: [(k, 3)] for s in ARMOR_SLOTS}
    return {
        "Evasion III x3 armor": (three("evasion"), "melee", False, {}),
        "Evasion III x3, dodge-built vanilla (40%)": (three("evasion"), "melee", False, {"dodge": 40}),
        "Resilience III x3 + offhand": (dict(three("resilience"), offhand=[("resilience", 3)]), "melee", False, {}),
        "Vitality III x3 + offhand + Hardiness": (dict(three("vitality"), offhand=[("vitality", 3)],
                                                     ring1=[("hardiness", 3)]), "melee", False, {}),
        "Tank: Res+Vit+Eva III on all 3 + offhand Res/Vit + Hardiness":
            ({**{s: [("resilience", 3), ("vitality", 3), ("evasion", 3)] for s in ARMOR_SLOTS},
              "offhand": [("resilience", 3), ("vitality", 3)], "ring1": [("hardiness", 3)]}, "melee", False, {}),
        "Boss tank: Res+Vit+Bosswarden x3 + offhand (vs boss)":
            ({**{s: [("resilience", 3), ("vitality", 3), ("bosswarden", 3)] for s in ARMOR_SLOTS},
              "offhand": [("resilience", 3), ("vitality", 3)], "ring1": [("hardiness", 3)]}, "melee", True, {}),
        "Speed: Swiftness pants + offhand + Fleetfoot": ({"pants": [("swiftness", 3)], "offhand": [("swiftness", 3)],
                                                          "ring1": [("fleetfoot", 3)]}, "melee", False, {}),
        "Crit: Keen + Precision + Ferocity": ({"weapon": [("keen", 3)], "ring1": [("precision", 3), ("ferocity", 3)]},
                                              "melee", False, {}),
        "Crit cap probe: same, vanilla crit 85%": ({"weapon": [("keen", 3)], "ring1": [("precision", 3), ("ferocity", 3)]},
                                                   "melee", False, {"cc": 85}),
        "Melee DPS: Sharp+Swift+Keen + Might/Haste/Prec/Fero": (
            {"weapon": [("sharpness", 3), ("swiftblade", 3), ("keen", 3)],
             "ring1": [("might", 3), ("haste", 3), ("precision", 3)], "ring2": [("ferocity", 3)]}, "melee", False, {}),
        "Boss DPS: Sharp+Swift+Bossbane + accessories (vs boss)": (
            {"weapon": [("sharpness", 3), ("swiftblade", 3), ("bossbane", 3)],
             "ring1": [("might", 3), ("haste", 3), ("precision", 3)], "ring2": [("ferocity", 3)]}, "melee", True, {}),
        "Magic: Arcane+QuickDraw+Spellsurge + accessories": (
            {"weapon": [("arcane", 3), ("quickdraw", 3), ("spellsurge", 3)],
             "ring1": [("might", 3), ("haste", 3), ("precision", 3)], "ring2": [("ferocity", 3)]}, "magic", False, {}),
        "Sustain: Lifesteal III + Regeneration III x3": (
            {"weapon": [("lifesteal", 3), ("swiftblade", 3)], **{s: [("regeneration", 3)] for s in ARMOR_SLOTS},
             "ring1": [("haste", 3)]}, "melee", False, {}),
    }


# --------------------------------------------------------------------------------------------------
# Reporting
# --------------------------------------------------------------------------------------------------


def pct(a, b):
    return (a / b - 1) * 100


def flags(o, v):
    f = []
    if pct(o["dps"], v["dps"]) > THRESH["dps"] * 100:
        f.append("DPS")
    if pct(o["ehp"], v["ehp"]) > THRESH["ehp"] * 100:
        f.append("EHP")
    if pct(o["move"], v["move"]) > THRESH["move"] * 100:
        f.append("MOVE")
    if o["dodge"] >= 60:
        f.append("DODGE")
    if o["cc"] >= 100:
        f.append("CRIT CAP")
    if o["sustain_pct"] > 5 and o["sustain"] > 2 * v["sustain"]:
        f.append("SUSTAIN")
    return ", ".join(f) or "-"


def row(label, o, v):
    return (f"| {label} | {pct(o['dps'], v['dps']):+.0f}% | {pct(o['ehp'], v['ehp']):+.0f}% | "
            f"{pct(o['move'], v['move']):+.0f}% | {o['cc']:.0f} | {o['dodge']:.0f} | {o['dt'] / 10:+.0f}% | "
            f"{o['hp']:.0f} | {o['sustain']:.0f} ({o['sustain_pct']:.1f}%) | {flags(o, v)} |")


HEAD = ("| Build | DPS | EHP | Move | Crit % | Dodge % | Dmg taken | Max HP | Sustain hp/s (%max/s) | Flags |\n"
        "|---|---|---|---|---|---|---|---|---|---|")


def fmt_build(b):
    order = ["weapon"] + ARMOR_SLOTS + ACC_SLOTS
    return "; ".join(f"{s}: " + "/".join(f"{k} {'I' * l}" for k, l in b[s]) for s in order if s in b)


def report(r, out):
    out(f"\n## Rules: {r['name']}\n")
    for stage in STAGES:
        s0 = dict(STAGES[stage])
        out(f"\n### {stage}\n")
        out(HEAD)
        res = {}
        for label, build, weapon, boss in [("vanilla", VANILLA, "melee", False),
                                           ("typical (II, 1-2 per item)", TYPICAL, "melee", False),
                                           ("best all-round (III x3, optimized)", None, "melee", False),
                                           ("best vs boss (III x3, optimized)", None, "melee", True),
                                           ("best magic (III x3, optimized)", None, "magic", False)]:
            if build is None:
                build = best_build(stage, r, weapon, boss)
                res[label] = build
            v = stats(stage, VANILLA, r, weapon, boss)
            out(row(label, stats(stage, build, r, weapon, boss), v))
        for label, (build, weapon, boss, over) in degenerate().items():
            STAGES[stage].update(over)
            v = stats(stage, VANILLA, r, weapon, boss)
            out(row(label, stats(stage, build, r, weapon, boss), v))
            STAGES[stage].clear()
            STAGES[stage].update(s0)
        out("")
        for label, b in res.items():
            out(f"- {label}: {fmt_build(b)}")


def baseline(out):
    out("## Vanilla baselines (model inputs)\n")
    out("| Stage | Item L | Dmg bucket | Atk-speed bucket | Crit % | Crit dmg | Boss C | Flat HP | HP% | Dmg taken | Dodge | Move | Bosswarden | Regen hp/s | Lifesteal |")
    out("|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|")
    for k, s in STAGES.items():
        out(f"| {k} | {s['L']} | +{s['D'] / 10:.0f}% | +{s['AS'] / 10:.0f}% | {s['cc']} | +{s['cd']}% | +{s['C']}% | "
            f"{s['flatHP']} | +{s['hpPct']}% | {s['DT'] / 10:+.0f}% | {s['dodge']}% | +{s['move'] / 10:.0f}% | {s['BW']}% | "
            f"{s['hot']} + {s['hotPct'] / 10}%max | {s['loh']}/hit |")


def summary(out):
    out("\n## Summary: best builds per rule set (DPS / EHP vs vanilla)\n")
    out("| Rules | Stage | best all-round | best vs boss | best magic | Lifesteal+Regen sustain (%max/s, vanilla) |")
    out("|---|---|---|---|---|---|")
    sus = {"weapon": [("lifesteal", 3), ("swiftblade", 3)], **{s: [("regeneration", 3)] for s in ARMOR_SLOTS},
           "ring1": [("haste", 3)]}
    for r in (CURRENT, PROPOSED, ALT_MAX):
        for st in STAGES:
            cells = []
            for w, boss in (("melee", False), ("melee", True), ("magic", False)):
                o = stats(st, best_build(st, r, w, boss), r, w, boss)
                v = stats(st, VANILLA, r, w, boss)
                cells.append(f"{pct(o['dps'], v['dps']):+.0f}% / {pct(o['ehp'], v['ehp']):+.0f}%")
            o, v = stats(st, sus, r), stats(st, VANILLA, r)
            out(f"| {r['name']} | {st} | " + " | ".join(cells) + f" | {o['sustain_pct']:.1f} ({v['sustain_pct']:.1f}) |")


def main():
    lines = []
    out = lines.append
    baseline(out)
    report(CURRENT, out)
    report(PROPOSED, out)
    summary(out)
    text = "\n".join(lines)
    if "--brief" in sys.argv:
        text = "\n".join(l for l in lines if l.startswith("|") and ("best" in l or "Tank" in l or "Magic" in l))
    print(text)


if __name__ == "__main__":
    main()
