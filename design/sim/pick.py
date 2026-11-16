import json, itertools
import core_sim as c, screen, rooms
from core_sim import *
screen.BAL = 'half'
B = json.load(open('rooms.json'))
def norm(r):
    room, saved, worst, steps = r
    allies = {k: tuple(v) for k, v in room[0].items()}
    en = {k: (v[0], v[1], tuple(v[2]), v[3], v[4]) for k, v in room[1].items()}
    return (allies, en, room[2]), saved, worst, steps
def pick(key, n, want3):
    out = []
    cand = [norm(r) for r in B[key]]
    # prefer rooms where the wrong policy costs exactly one ally (a lesson, not a wipe) and fights are short
    cand.sort(key=lambda r: (abs(r[2]-3) if key != 'none+save' else 0, max(r[3])))
    seen = set()
    for r in cand:
        kinds = tuple(sorted(v[4] for v in r[0][1].values()))
        if (len(kinds) == 3) != want3[len(out)]: continue
        sig = (kinds, r[0][2][0])
        if sig in seen: continue
        seen.add(sig); out.append(r)
        if len(out) == n: break
    return out
sel = {'P': pick('P', 3, [False, False, True]), 'A': pick('A', 3, [False, True, False]),
       'B': pick('B', 2, [False, False]), 'S': pick('none+save', 3, [False, True, False])}
names = {}
i = 1
for k in 'PABS':
    for r in sel[k]:
        names['R%02d' % i] = (k, r); i += 1
for n, (k, (room, saved, worst, steps)) in names.items():
    t = screen.table(rooms.mk(room))
    res = ' '.join('%s:%d' % ('PAB'[p], t[(p, None)][1] if t[(p, None)][0] == 'V' else 0) for p in (0, 1, 2))
    sv = ' '.join('%s>%s' % ('PAB'[p], 'PAB'[q]) for p in (0,1,2) for q in (0,1,2) if p != q and t[(p, q)][0] == 'V' and t[(p, q)][1] == 4)
    print(n, k, 'allies', room[0], 'enemies', {a: (v[4], v[0], v[1], v[2]) for a, v in room[1].items()}, 'order', room[2], '|', res, '| steps', steps, '| saves', sv)
json.dump({n: [k, r[0]] for n, (k, r) in names.items()}, open('picked.json', 'w'))
