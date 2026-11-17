# What if the player chooses the order of the rooms on a floor and when to use the fountain?
# Compares a fixed route with a chosen route, for a thoughtful player and for fixed-policy players.
import itertools, math
import core_sim as c, screen, floors as F
MAX = F.MAX
def fight(mk, hp, pols, chooser):
    m = lambda: mk(dict(hp)); opts = []
    for p in pols:
        opts.append(((p, None), screen.play(m, p)))
        for q in pols:
            if q != p: opts.append(((p, q), screen.play(m, p, q)))
    return chooser(opts)
def floor_run(rooms, hp, pols, chooser, fountain_at, after=.25, revive=.25, fount=.5):
    downs = 0
    for i, (n, mk) in enumerate(rooms):
        if i == fountain_at: hp = F.rest(hp, fount)
        ch, res = fight(mk, hp, pols, chooser)
        if res[0] != 'V': return None, downs
        downs += 4 - res[1]
        hp = F.rest({k: (v if v > 0 else math.ceil(MAX[k]*revive)) for k, v in res[2].items()}, after)
    if fountain_at >= len(rooms): hp = F.rest(hp, fount)
    return hp, downs
def expedition(chooser, free):
    hp = dict(MAX); total = 0; picks = []
    for f, fl in enumerate(F.FLOORS):
        pols = (0, 1) if f == 0 else (0, 1, 2)
        if f: hp = F.rest(hp, .5)
        cands = []
        orders = list(itertools.permutations(fl)) if (free and f) else [tuple(fl)]
        fts = range(len(fl)+1) if free else [2]
        for o in orders:
            for ft in fts:
                h, d = floor_run(o, hp, pols, chooser, ft)
                if h is not None: cands.append((d, -sum(h.values()), [n for n, _ in o], ft, h))
        if not cands: return 'ZENMETSU %dF' % (f+1), total, picks
        cands.sort(key=lambda x: x[:2]); d, _, o, ft, hp = cands[0]; total += d; picks.append((o, ft, d, len(cands), len(orders)*len(fts)))
    return 'CLEAR', total, picks
if __name__ == '__main__':
    for name, ch in F.PLAYERS[:4]:
        for free in (False, True):
            r = expedition(ch, free)
            print('%s / %s: %s 倒れた延べ %d' % (name, '道を選ぶ' if free else '決まった道', r[0], r[1]))
            if free and ch is F.best:
                for o, ft, d, ok, tot in r[2]: print('    順 %s 泉は%d戦目の前 倒れた %d  踏破できる道 %d/%d' % (o, ft+1, d, ok, tot))
