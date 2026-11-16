# Sweeps the recovery rule: after each fight / on descending / revival / fountain.
import itertools
from floors import expedition, PLAYERS
print('戦闘後 階移動 復帰 泉 | ' + ' | '.join(n for n, _ in PLAYERS))
for after, descend, revive, fount in itertools.product((0, .25, .34, .5), (.5, 1.0), (.25, .5), (0, .5)):
    row = []
    for _, ch in PLAYERS:
        cl, d, ok, _t = expedition(ch, after, descend, revive, fount)
        row.append('%s%2d/14 延べ%d' % ('踏破 ' if ok else '全滅 ', cl, d))
    print('%.2f %.1f %.2f %.1f | %s' % (after, descend, revive, fount, ' | '.join(row)))
