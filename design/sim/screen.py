import random, json, sys
import core_sim as c
from core_sim import *

BAL = 'half'  # the adopted definition; 'adj' and 'threat' are the rejected alternatives
def pressed(s):
    A = s.find('A')
    if BAL == 'adj': return any(u.enemy and u.alive and dist(u.pos, A.pos) == 1 for u in s.units)
    if BAL == 'half': return A.hp * 2 <= A.maxhp
    if BAL == 'threat': return 'A' in threatened(s, PROTECT) or any(u.enemy and u.alive and dist(u.pos, A.pos) == 1 for u in s.units) and A.hp*2 <= A.maxhp
c.anchor_pressed = pressed

def threatened(s, pref):
    inc = {}
    for i in range(len(s.order)):
        u = s.find(s.order[(s.cursor+i) % len(s.order)])
        if not (u and u.alive): continue
        k, t, p = c.plan(s, u, pref)
        if k != 'attack' or not u.enemy or t.enemy: continue
        B = s.find('B')
        g = t.id == 'A' and s.guard and B and B.alive and dist(B.pos, t.pos) <= c.R['protect_range']
        inc[t.id] = inc.get(t.id, 0) + max(0, u.atk - (c.R['guard'] if g else 0))
    return [u.id for u in s.units if not u.enemy and u.alive and inc.get(u.id, 0) >= u.hp]

def play(mk, pref, corr=None):
    s = mk(); p = pref; left = 1
    while s.out is None:
        if corr is not None and left and corr != p and threatened(s, p): p = corr; left = 0
        c.resolve(s, p)
    return s.out, sum(1 for u in s.units if not u.enemy and u.alive), {u.id: u.hp for u in s.units if not u.enemy}, s.step

def table(mk):
    t = {}
    for p in (PROTECT, ATTACK, BALANCED):
        t[(p, None)] = play(mk, p)
        for q in (PROTECT, ATTACK, BALANCED):
            if q != p: t[(p, q)] = play(mk, p, q)
    return t

if __name__ == '__main__':
    for mode in ('adj', 'half', 'threat'):
        BAL = mode
        print('--- balanced =', mode)
        for i, mk in enumerate(c.G1):
            t = table(mk)
            print(i, ' '.join('%s%s:%s%d/%d' % ('PAB'[p], '-' if q is None else '>'+'PAB'[q], o[0], o[1], sum(o[2].values())) for (p, q), o in t.items()))
