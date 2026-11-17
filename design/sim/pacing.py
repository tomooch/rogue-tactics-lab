# Pacing of each fight: when the first blow lands, when the first crisis comes, how many beats are "quiet".
# Full HP, no correction. Feeds overall/01_EXPERIENCE.md.
import core_sim as c, screen, floors as F
def trace(mk, pref):
    s = mk(); rows = []; first_hit = first_crisis = None; crises = 0; was = False; quiet = 0; low = 1.0; downs = []
    while s.out is None:
        th = screen.threatened(s, pref)
        if th and not was: crises += 1; first_crisis = first_crisis or s.step + 1
        was = bool(th)
        a = s.nxt(); kind = c.plan(s, a, pref)[0]; n = len(s.log)
        c.resolve(s, pref)
        if kind in ('move', 'hold'): quiet += 1
        if len(s.log) > n and first_hit is None and any(e[1] == 'dmg' for e in s.log[n:]): first_hit = s.step
        for u in s.units:
            if not u.enemy:
                low = min(low, u.hp / u.maxhp)
                if u.hp == 0 and u.id not in downs: downs.append(u.id)
    return dict(steps=s.step, first_hit=first_hit, first_crisis=first_crisis, crises=crises, quiet=quiet, low=low, downs=''.join(downs))
if __name__ == '__main__':
    print('部屋 方針 | 手数 | 最初の一撃 | 最初の危機 | 危機の回数 | 静かな手 | 倒れた仲間')
    tot = dict(steps=0, quiet=0, n=0, nocrisis=0, fh=0)
    for fl in F.FLOORS:
        for n, mk in fl:
            for p in (0, 1, 2):
                t = trace(mk, p)
                print('%s %s | %2d | %2d | %s | %d | %2d (%d%%) | %s' % (n, '慎攻バ'[p], t['steps'], t['first_hit'], t['first_crisis'] or '-', t['crises'], t['quiet'], 100*t['quiet']//t['steps'], t['downs'] or '-'))
                tot['steps'] += t['steps']; tot['quiet'] += t['quiet']; tot['n'] += 1; tot['fh'] += t['first_hit']; tot['nocrisis'] += t['first_crisis'] is None
    print('平均手数 %.1f  静かな手の割合 %d%%  最初の一撃までの平均 %.1f手  危機が一度も来ない組 %d/%d' % (tot['steps']/tot['n'], 100*tot['quiet']//tot['steps'], tot['fh']/tot['n'], tot['nocrisis'], tot['n']))

# Seconds of presentation per fight, from the durations in Presentation/HqMotion.cs (PresentEvent).
# This is the floor: any wait between steps outside PresentEvent is not included.
SEC = dict(move=.38+.52+.14, hold=.38, attack=.38+.62+.40, heal=.38+.60+.40, protect=.38+.50+.40, counter=.50+.40, death=.72+.14)
def seconds(mk, pref):
    s = mk(); total = quiet = 0.0
    while s.out is None:
        a = s.nxt(); kind = c.plan(s, a, pref)[0]; n = len(s.log); before = {u.id: u.alive for u in s.units}
        c.resolve(s, pref)
        t = SEC[kind] + sum(SEC['counter'] for e in s.log[n:] if e[1] == 'counter') + sum(SEC['death'] for u in s.units if before[u.id] and not u.alive)
        total += t
        if kind in ('move', 'hold'): quiet += t
    return total, quiet
if __name__ == '__main__':
    rows = [(n, p) + seconds(mk, p) for fl in F.FLOORS for n, mk in fl for p in (0, 1, 2)]
    tt = sum(r[2] for r in rows); qq = sum(r[3] for r in rows)
    print('演出の秒数（下限）: 平均 %.0f秒  最短 %.0f秒  最長 %.0f秒  うち歩きと待ちが占める時間 %d%%' % (tt/len(rows), min(r[2] for r in rows), max(r[2] for r in rows), 100*qq/tt))
    for name in ('G0', 'R05', 'R06'):
        print('  ', name, ' '.join('%s %.0f秒' % ('慎攻バ'[r[1]], r[2]) for r in rows if r[0] == name))

# Proposal (overall/01 section 4): consecutive quiet beats (walks and waits) are shown overlapping,
# each starting .18s after the previous one, instead of one after another. Core order is unchanged.
def seconds_overlapped(mk, pref, stagger=.18):
    s = mk(); total = 0.0; run = []
    def flush():
        nonlocal total, run
        if run: total += max(d + i*stagger for i, d in enumerate(run)); run = []
    while s.out is None:
        a = s.nxt(); kind = c.plan(s, a, pref)[0]; n = len(s.log); before = {u.id: u.alive for u in s.units}
        c.resolve(s, pref)
        if kind in ('move', 'hold'): run.append(SEC[kind]); continue
        flush()
        total += SEC[kind] + sum(SEC['counter'] for e in s.log[n:] if e[1] == 'counter') + sum(SEC['death'] for u in s.units if before[u.id] and not u.alive)
    flush(); return total
if __name__ == '__main__':
    a = [seconds(mk, p)[0] for fl in F.FLOORS for n, mk in fl for p in (0, 1, 2)]
    b = [seconds_overlapped(mk, p) for fl in F.FLOORS for n, mk in fl for p in (0, 1, 2)]
    print('歩きを重ねて見せた場合: 平均 %.0f秒 → %.0f秒  最長 %.0f秒 → %.0f秒' % (sum(a)/len(a), sum(b)/len(b), max(a), max(b)))
    import collections
    runs = collections.Counter()
    for fl in F.FLOORS:
        for n, mk in fl:
            for p in (0, 1, 2):
                s = mk(); k = 0
                while s.out is None:
                    kind = c.plan(s, s.nxt(), p)[0]; c.resolve(s, p)
                    if kind in ('move', 'hold'): k += 1
                    else:
                        if k: runs[min(k, 6)] += 1
                        k = 0
                if k: runs[min(k, 6)] += 1
    print('静かな手が続く長さの分布（6は6以上）:', dict(sorted(runs.items())))
