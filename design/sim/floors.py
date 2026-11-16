# Five floors, fourteen fights, HP carried between fights. Prints how four kinds of player fare
# under the adopted recovery rule (specs/01_CORE_RULES.md section 5).
import json, math
import core_sim as c, screen, rooms
from core_sim import *
P = json.load(open('picked.json'))
def room(n):
    k, r = P[n]
    return rooms.mk(({a: tuple(v) for a, v in r[0].items()}, {a: (v[0], v[1], tuple(v[2]), v[3], v[4]) for a, v in r[1].items()}, r[2]))
FLOORS = [[('G0', c.G1[0]), ('G1', c.G1[1]), ('G2', c.G1[2])],
          [(n, room(n)) for n in ('R01', 'R04', 'R09')],
          [(n, room(n)) for n in ('R07', 'R02', 'R05')],
          [(n, room(n)) for n in ('R08', 'R06', 'R10')],
          [(n, room(n)) for n in ('R03', 'R11')]]
MAX = {k: v[1] for k, v in c.ALLY.items()}
def rest(hp, frac): return {k: min(MAX[k], v + math.ceil(MAX[k]*frac)) for k, v in hp.items()}
def expedition(chooser, after=.25, descend=.5, revive=.25, fount=.5):
    hp = dict(MAX); downs = 0; cleared = 0; trace = []
    for f, fl in enumerate(FLOORS):
        pols = (0, 1) if f == 0 else (0, 1, 2)      # the balanced policy appears from the second floor
        if f: hp = rest(hp, descend)
        for i, (n, mk) in enumerate(fl):
            m = lambda mk=mk, hp=hp: mk(dict(hp))
            opts = []
            for p in pols:
                opts.append(((p, None), screen.play(m, p)))
                for q in pols:
                    if q != p: opts.append(((p, q), screen.play(m, p, q)))
            ch, res = chooser(opts)
            start = list(hp.values())
            if res[0] != 'V': return cleared, downs, False, trace + ['%dF %s %s -> 全滅' % (f+1, n, start)]
            cleared += 1; downs += 4 - res[1]
            trace.append('%dF %s %s%s 入 %s 出 %s' % (f+1, n, '慎攻バ'[ch[0]], '' if ch[1] is None else '→'+'慎攻バ'[ch[1]], start, list(res[2].values())))
            hp = {k: (v if v > 0 else math.ceil(MAX[k]*revive)) for k, v in res[2].items()}
            hp = rest(hp, after)
            if i == 1: hp = rest(hp, fount)       # the fountain sits between the second and third fight
    return cleared, downs, True, trace
score = lambda r: (r[0] == 'V', r[1], sum(r[2].values()))
best = lambda opts: max(opts, key=lambda o: score(o[1]))                                   # greedy: best outcome of this fight
nocorr_best = lambda opts: max([o for o in opts if o[0][1] is None], key=lambda o: score(o[1]))
fixed = lambda p: (lambda opts: next(o for o in opts if o[0] == (p, None)))
worst = lambda opts: min(opts, key=lambda o: score(o[1]))
PLAYERS = [('よく考える人', best), ('修正を使わない人', nocorr_best), ('いつも慎重', fixed(0)), ('いつも攻める', fixed(1)), ('最悪を選ぶ人', worst)]
if __name__ == '__main__':
    for name, ch in PLAYERS:
        cl, d, ok, tr = expedition(ch)
        print('%s: %s  勝った戦闘 %d/14  倒れた延べ %d人' % (name, '踏破' if ok else '全滅', cl, d))
        if ch is best:
            for t in tr: print('   ', t)
