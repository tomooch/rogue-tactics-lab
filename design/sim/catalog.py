# Prints the room catalogue used in specs/03_DUNGEON_AND_ROOMS.md from picked.json.
import json
import core_sim as c, screen, rooms
P = json.load(open('picked.json'))
JP = {'moss': '苔', 'bloom': '花'}
ROLE = {'P': '慎重', 'A': '攻める', 'B': 'バランス', 'S': 'なし（修正が要る）'}
for n, (k, r) in P.items():
    al = {a: tuple(v) for a, v in r[0].items()}
    en = {a: (v[0], v[1], tuple(v[2]), v[3], v[4]) for a, v in r[1].items()}
    t = screen.table(rooms.mk((al, en, r[2])))
    res = ' / '.join('%s %d' % (x, t[(p, None)][1]) for p, x in ((0, '慎重'), (1, '攻める'), (2, 'バランス')))
    sv = '、'.join('%s→%s' % ('慎攻バ'[p], '慎攻バ'[q]) for p in (0, 1, 2) for q in (0, 1, 2)
                  if p != q and t[(p, q)][0] == 'V' and t[(p, q)][1] == 4)
    print('### %s — 修正なしで全員生存できる方針: %s\n' % (n, ROLE[k]))
    print('```')
    for y in range(4, -1, -1):
        row = ''
        for x in range(5):
            ch = '・'
            for a, p in list(al.items()) + [(a, v[2]) for a, v in en.items()]:
                if p == (x, y): ch = a + ' '
            row += ch
        print('y=%d  %s' % (y, row))
    print('     x=0 1 2 3 4')
    print('```\n')
    print('- 味方: ' + '、'.join('%s(%d,%d)' % (a, p[0], p[1]) for a, p in al.items()))
    print('- 敵: ' + '、'.join('%s=%sの精 HP %d 攻 %d (%d,%d)' % (a, JP[v[4]], v[0], v[1], v[2][0], v[2][1]) for a, v in en.items()))
    print('- 行動順: ' + ' → '.join(r[2]))
    print('- 修正なしの生存者数: ' + res)
    print('- 危機での修正1回で全員生存になる組: ' + (sv or 'なし'))
    print('- 手数（慎重・攻める・バランス）: %s\n' % '・'.join(str(t[(p, None)][3]) for p in (0, 1, 2)))
