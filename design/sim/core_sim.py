# Design-time port of RogueTactics.Core.G1 (G1Battle.cs) used only to screen candidate numbers.
# Not shipped. Extensions (Balanced policy, enemy brains, carried HP) are proposals under test.
import itertools, sys

PROTECT, ATTACK, BALANCED = 0, 1, 2
# Relic switches (specs/04). Defaults are the shipped rules.
R = dict(counter=2, heals=2, heal_range=2, protect_range=1, flank_any=False, flank_bonus=2, guard=3, heal_amount=4)
def dist(a, b): return abs(a[0]-b[0]) + abs(a[1]-b[1])

class U:
    def __init__(s, id, role, hp, atk, pos, brain='anchor', maxhp=None):
        s.id, s.role, s.hp, s.atk, s.pos, s.brain = id, role, hp, atk, pos, brain
        s.maxhp = maxhp or hp
    @property
    def enemy(s): return s.role == 'E'
    @property
    def alive(s): return s.hp > 0

class S:
    def __init__(s, units, order, w=5, h=5):
        s.units, s.order, s.w, s.h = units, order, w, h
        s.step = 0; s.cursor = 0; s.heals = R['heals']; s.guard = False; s.out = None
        s.log = []
    def find(s, id): return next((u for u in s.units if u.id == id), None)
    def empty(s, p): return 0 <= p[0] < s.w and 0 <= p[1] < s.h and not any(u.alive and u.pos == p for u in s.units)
    def nxt(s):
        for i in range(len(s.order)):
            u = s.find(s.order[(s.cursor+i) % len(s.order)])
            if u and u.alive: return u

def flank(s, a, t):
    A = s.find('A')
    if a.role != 'F' or dist(a.pos, t.pos) != 1: return False
    if R['flank_any']: return any(u is not a and not u.enemy and u.alive and dist(u.pos, t.pos) == 1 for u in s.units)
    return bool(A and A.alive and dist(A.pos, t.pos) == 1)

def move_or_hold(s, a, t):
    x, y = a.pos
    for p in ((x, y+1), (x+1, y), (x, y-1), (x-1, y)):
        if s.empty(p) and dist(p, t.pos) < dist(a.pos, t.pos): return ('move', t, p)
    return ('hold', t, a.pos)

def anchor_pressed(s):
    # Balanced policy: fight until the anchor is at half HP or below, then protect.
    A = s.find('A')
    return A.hp * 2 <= A.maxhp

def plan(s, a, pref):
    A = s.find('A')
    protect = pref == PROTECT or (pref == BALANCED and A and A.alive and anchor_pressed(s))
    if a.role == 'P' and protect and A and A.alive:
        if dist(a.pos, A.pos) <= R['protect_range']: return ('protect', A, a.pos)
        return move_or_hold(s, a, A)
    if a.role == 'S' and s.heals > 0:
        w = sorted([u for u in s.units if not u.enemy and u.alive and u.hp <= u.maxhp-4], key=lambda u: (u.hp, u.id))
        if w:
            if dist(a.pos, w[0].pos) <= R['heal_range']: return ('heal', w[0], a.pos)
            return move_or_hold(s, a, w[0])
    foes = [u for u in s.units if u.alive and u.enemy != a.enemy]
    if not foes: return ('hold', None, a.pos)
    if a.enemy and a.brain == 'anchor' and A and A.alive: t = A
    elif a.enemy and a.brain == 'lowest': t = sorted(foes, key=lambda u: (u.hp, dist(a.pos, u.pos), u.id))[0]
    else: t = sorted(foes, key=lambda u: (dist(a.pos, u.pos), u.id))[0]
    if dist(a.pos, t.pos) == 1: return ('attack', t, a.pos)
    return move_or_hold(s, a, t)

def hit(s, a, t, n, kind):
    n = min(t.hp, n); t.hp -= n; s.log.append((s.step, kind, a.id, t.id, n, t.hp))

def resolve(s, pref):
    a = s.nxt(); kind, t, p = plan(s, a, pref); s.step += 1
    if a.role == 'P': s.guard = False
    if kind == 'move': a.pos = p
    elif kind == 'protect': s.guard = True
    elif kind == 'heal': n = min(R['heal_amount'], t.maxhp-t.hp); t.hp += n; s.heals -= 1; s.log.append((s.step, 'heal', a.id, t.id, n, t.hp))
    elif kind == 'attack':
        d = a.atk + (R['flank_bonus'] if flank(s, a, t) else 0)
        B = s.find('B')
        g = t.id == 'A' and s.guard and B and B.alive and dist(B.pos, t.pos) <= R['protect_range']
        if g: d = max(0, d-R['guard'])
        hit(s, a, t, d, 'dmg')
        if t.alive and t.role == 'A' and a.alive and d > 0: hit(s, t, a, R['counter'], 'counter')
    s.cursor = (s.order.index(a.id)+1) % len(s.order)
    if not any(u.enemy and u.alive for u in s.units): s.out = 'V'
    elif not any((not u.enemy) and u.alive for u in s.units): s.out = 'D'
    elif s.step >= 120: s.out = 'L'

ALLY = {'A': ('A', 18, 5), 'B': ('P', 16, 4), 'C': ('F', 12, 4), 'D': ('S', 10, 2)}

def build(allies, enemies, order, hp=None):
    us = []
    for id, pos in allies.items():
        role, mh, atk = ALLY[id]
        us.append(U(id, role, (hp or {}).get(id, mh), atk, pos, maxhp=mh))
    for id, (ehp, atk, pos, brain) in enemies.items():
        us.append(U(id, 'E', ehp, atk, pos, brain))
    return S(us, list(order))

def run(mk, pref, correct_at=None, correct_to=None):
    s = mk(); p = pref
    while s.out is None:
        if correct_at is not None and s.step == correct_at: p = correct_to
        resolve(s, p)
    alive = sum(1 for u in s.units if not u.enemy and u.alive)
    hp = {u.id: u.hp for u in s.units if not u.enemy}
    return s.out, s.step, alive, hp, s

def summary(mk, prefs=(PROTECT, ATTACK, BALANCED)):
    out = []
    for p in prefs:
        o, st, al, hp, _ = run(mk, p)
        out.append((o, st, al, sum(hp.values())))
    return out

G1 = [
 lambda hp=None: build({'A': (2,2), 'B': (1,2), 'C': (3,3), 'D': (1,1)}, {'X': (18,6,(2,3),'anchor'), 'Y': (14,5,(3,2),'anchor')}, 'BXACYD', hp),
 lambda hp=None: build({'A': (4,4), 'B': (2,1), 'C': (1,0), 'D': (0,4)}, {'X': (15,5,(1,3),'anchor'), 'Y': (11,4,(2,3),'anchor')}, 'BDYXAC', hp),
 lambda hp=None: build({'A': (2,3), 'B': (3,0), 'C': (4,3), 'D': (1,4)}, {'X': (18,5,(2,1),'anchor'), 'Y': (22,7,(0,2),'anchor')}, 'YDAXCB', hp),
]

if __name__ == '__main__':
    for i, mk in enumerate(G1):
        print('G1 encounter', i, summary(mk))
        for p in (PROTECT, ATTACK):
            print('   pref', p, run(mk, p)[:4])
