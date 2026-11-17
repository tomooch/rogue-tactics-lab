# Is each room a fixed answer to memorise, or does the best policy depend on the party's condition?
# For every fight, the best policy (no correction) under different incoming HP.
import math
import core_sim as c, screen, floors as F
MAX = F.MAX
PROFILES = [('満タン', {}), ('シロが半分', {'A': .5}), ('モモが半分', {'B': .5}), ('ガルが半分', {'C': .5}), ('ミャルが半分', {'D': .5}),
            ('全員7割', {k: .7 for k in MAX}), ('シロ3割', {'A': .3}), ('前衛が半分', {'A': .5, 'B': .5})]
def hp(profile): return {k: max(1, math.ceil(MAX[k]*profile.get(k, 1))) for k in MAX}
def score(r): return (r[0] == 'V', r[1], sum(r[2].values()))
def best_set(mk, h, pols=(0, 1, 2)):
    res = {p: score(screen.play(lambda: mk(dict(h)), p)) for p in pols}
    top = max(res.values()); return ''.join('慎攻バ'[p] for p in pols if res[p] == top), res
if __name__ == '__main__':
    fixed = 0; rows = []
    for fl in F.FLOORS:
        for n, mk in fl:
            outs = [best_set(mk, hp(pr))[0] for _, pr in PROFILES]
            distinct = len(set(outs)); rows.append((n, outs, distinct)); fixed += distinct == 1
    print('部屋 | ' + ' | '.join(n for n, _ in PROFILES) + ' | 答えの種類')
    for n, outs, d in rows: print('%s | %s | %d' % (n, ' | '.join(outs), d))
    print('一行の状態に関係なく答えが同じ部屋: %d / %d' % (fixed, len(rows)))
