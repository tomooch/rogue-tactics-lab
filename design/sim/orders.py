# Does the expedition still hold when the rooms of each floor are met in a different order?
import itertools
import floors as F
base = [list(f) for f in F.FLOORS]
tally = {n: [0, 0, 99, 0] for n, _ in F.PLAYERS}; total = 0
for perm in itertools.product(*[list(itertools.permutations(f)) for f in base[1:]]):
    F.FLOORS = [base[0]] + [list(p) for p in perm]; total += 1
    for n, ch in F.PLAYERS:
        cl, d, ok, _ = F.expedition(ch)
        t = tally[n]; t[0] += ok; t[1] += d if ok else 0; t[2] = min(t[2], cl); t[3] = max(t[3], d)
print('並び順の総数', total)
for n, t in tally.items(): print('%s: 踏破できる並び %d/%d  最も早い全滅は %d戦を越えた後  踏破時に倒れた延べの最大 %d' % (n, t[0], total, t[2], t[3]))

# Orderings a seed may pick from: a thoughtful player clears, a single fixed policy does not.
good = []
for perm in itertools.product(*[list(itertools.permutations(f)) for f in base[1:]]):
    F.FLOORS = [base[0]] + [list(p) for p in perm]
    r = {n: F.expedition(ch) for n, ch in F.PLAYERS}
    if r['よく考える人'][2] and r['よく考える人'][1] <= 5 and not r['いつも慎重'][2] and not r['いつも攻める'][2]:
        good.append([[n for n, _ in fl] for fl in perm])
import json
json.dump(good, open('good_orders.json', 'w'))
print('seed が選んでよい並び', len(good), '/', total)
