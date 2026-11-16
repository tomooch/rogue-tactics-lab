# How much does each relic change the fourteen fights? Full HP, no correction, three policies.
import core_sim as c, screen, floors as F
RELICS = [('荊棘の護符 反撃 2→3', dict(counter=3)), ('朝露の雫 回復 2回×4 → 1回×6', dict(heals=1, heal_amount=6)),
          ('見張りの灯 2マス先から守れる', dict(protect_range=2)), ('連携の輪 誰の隣でも挟撃', dict(flank_any=True)),
          ('巡礼の鈴 回復 2回×4 → 3回×3', dict(heals=3, heal_amount=3)), ('花盾の紋 守り 3→4', dict(guard=4)),
          # rejected
          ('（不採用）荊棘 反撃 2→4', dict(counter=4)), ('（不採用）遠見の葉 回復が3マス届く', dict(heal_range=3)),
          ('（不採用）花盾 守り5・反撃0', dict(guard=5, counter=0))]
fights = [(n, mk) for fl in F.FLOORS for n, mk in fl]
def all_results():
    out = {}
    for n, mk in fights:
        for p in (0, 1, 2):
            r = screen.play(mk, p); out[(n, p)] = (r[0], r[1], sum(r[2].values()), r[3])
    return out
base = all_results(); default = dict(c.R)
print('遺物 | 生存者数が変わる | 増える/減る | 方針別 | HP合計が変わる | 手数が変わる  （14戦×3方針=42通り中）')
for name, mod in RELICS:
    c.R.update(default); c.R.update(mod); r = all_results()
    surv = [(k, base[k][1], r[k][1]) for k in base if base[k][1] != r[k][1]]
    up = sum(1 for _, a, b in surv if b > a); dn = len(surv) - up
    hp = sum(1 for k in base if base[k][2] != r[k][2]); st = sum(1 for k in base if base[k][3] != r[k][3])
    by = ' '.join('%s+%d-%d' % ('慎攻バ'[p], sum(1 for k, a, b in surv if k[1] == p and b > a), sum(1 for k, a, b in surv if k[1] == p and b < a)) for p in (0, 1, 2))
    print('%s | %d | +%d/-%d | %s | %d | %d' % (name, len(surv), up, dn, by, hp, st))
c.R.update(default)
assert all_results() == base
