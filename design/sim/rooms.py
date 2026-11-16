import random, json
import core_sim as c, screen
from core_sim import *
screen.BAL = 'half'
KINDS = {'moss': ('nearest', (14, 18), (5, 6)), 'bloom': ('lowest', (10, 14), (4, 5))}
def gen(rng, n_enemy):
    cells = [(x, y) for x in range(5) for y in range(5)]; rng.shuffle(cells)
    allies = dict(zip('ABCD', cells[:4]))
    en = {}
    for i in range(n_enemy):
        k = rng.choice(['moss', 'bloom']); b, hp, atk = KINDS[k]
        en['XYZ'[i]] = (rng.randint(*hp), rng.randint(*atk), cells[4+i], b, k)
    order = list('ABCD') + list(en); rng.shuffle(order)
    return allies, en, ''.join(order)
def mk(room):
    allies, en, order = room
    return lambda hp=None: c.build(allies, {k: v[:4] for k, v in en.items()}, order, hp)
def classify(room):
    t = screen.table(mk(room))
    clean = ''.join('PAB'[p] for p in (0, 1, 2) if t[(p, None)][0] == 'V' and t[(p, None)][1] == 4)
    saved = ''.join('PAB'[p] for p in (0, 1, 2) if any(t[(p, q)][0] == 'V' and t[(p, q)][1] == 4 for q in (0, 1, 2) if q != p))
    worst = min(t[(p, None)][1] if t[(p, None)][0] == 'V' else 0 for p in (0, 1, 2))
    steps = [t[(p, None)][3] for p in (0, 1, 2)]
    return clean, saved, worst, steps, t
if __name__ == '__main__':
    rng = random.Random(20261117); buckets = {}
    for i in range(60000):
        room = gen(rng, 2 if i % 3 else 3)
        # no ally starts adjacent to an enemy: the player gets a beat to read the board
        if any(dist(a, e[2]) == 1 for a in room[0].values() for e in room[1].values()): continue
        clean, saved, worst, steps, t = classify(room)
        if max(steps) > 44 or min(steps) < 12: continue
        key = clean if clean else ('none+' + ('save' if saved else 'lost'))
        buckets.setdefault(key, []).append((room, saved, worst, steps))
    for k, v in sorted(buckets.items()): print(k, len(v))
    json.dump({k: v[:400] for k, v in buckets.items()}, open('rooms.json', 'w'))
