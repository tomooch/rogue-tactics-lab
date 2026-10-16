#!/usr/bin/env python3
"""Offline, deterministic authoring of the HQ sky-ruins OBJ set (stdlib only)."""
from __future__ import annotations

import json
import math
import random
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "unity/Assets/RogueTactics/Art/HqRuins"

PALETTE = {
    "Limestone": (.91,.83,.67), "Cliff": (.57,.50,.40), "Detail": (.73,.65,.50),
    "Foliage": (.20,.43,.27), "Flowers": (.98,.88,.82), "Blue": (.12,.42,.69),
    "Gold": (.91,.64,.23), "Water": (.32,.76,.88), "Distant": (.68,.76,.76),
    "Warm": (1.0,.57,.19),
}

class Obj:
    def __init__(self, name):
        self.name, self.tris = name, []
    def tri(self, a, b, c, color=None):
        color = color or PALETTE[self.name]
        ax,ay,az=a; bx,by,bz=b; cx,cy,cz=c
        ux,uy,uz=bx-ax,by-ay,bz-az; vx,vy,vz=cx-ax,cy-ay,cz-az
        n=(uy*vz-uz*vy, uz*vx-ux*vz, ux*vy-uy*vx)
        length=math.sqrt(sum(q*q for q in n))
        if length < 1e-8: return
        n=tuple(q/length for q in n)
        self.tris.append(((a,b,c), n, color))
    def quad(self,a,b,c,d,color=None):
        self.tri(a,b,c,color); self.tri(a,c,d,color)
    def export(self,path):
        lines=["# Offline authored HQ sky ruins; local coordinates, Unity Y-up.",
               "# v includes RGB vertex-color extension; vt/vn/f are standard OBJ records.",
               f"o {self.name}",f"g {self.name}",f"usemtl {self.name}"]
        vi=1; ti=1; ni=1
        for verts,n,col in self.tris:
            for x,y,z in verts:
                lines.append(f"v {x:.5f} {y:.5f} {z:.5f} {col[0]:.4f} {col[1]:.4f} {col[2]:.4f}")
            # World-planar UVs retain detail scale across differently sized islands.
            for x,y,z in verts: lines.append(f"vt {x*.22:.5f} {z*.22:.5f}")
            lines.append(f"vn {n[0]:.6f} {n[1]:.6f} {n[2]:.6f}")
            lines.append(f"f {vi}/{ti}/{ni} {vi+1}/{ti+1}/{ni} {vi+2}/{ti+2}/{ni}")
            vi+=3; ti+=3; ni+=1
        path.write_text("\n".join(lines)+"\n",encoding="utf-8")
    def bounds(self):
        pts=[p for tri,_,_ in self.tris for p in tri]
        return [[round(min(p[i] for p in pts),3),round(max(p[i] for p in pts),3)] for i in range(3)] if pts else []

def rounded(w,d,cut=.14,seed=0):
    x,z=w/2,d/2; c=min(w,d)*cut
    pts=[(-x,-z+c),(-x+c,-z),(x-c,-z),(x,-z+c),(x,z-c),(x-c,z),(-x+c,z),(-x,z-c)]
    return [(px*(1+.018*math.sin(seed*1.7+i*2.1)),pz*(1+.018*math.sin(seed*1.7+i*2.1))) for i,(px,pz) in enumerate(pts)]

def slab(obj,c,w,d,h=.25,bevel=.10,seed=0,color=None):
    pts=rounded(w,d,.15,seed); inside=[(x*.91,z*.91) for x,z in pts]
    bottom=[]; shoulder=[]; top=[]
    for (x,z),(ix,iz) in zip(pts,inside):
        bottom.append((c[0]+x,c[1]-h/2,c[2]+z))
        shoulder.append((c[0]+x,c[1]+h/2-bevel,c[2]+z))
        top.append((c[0]+ix,c[1]+h/2,c[2]+iz))
    for i in range(len(pts)):
        j=(i+1)%len(pts)
        # footprint winding is clockwise viewed from above in Unity X/Z; reverse side for outward normals.
        obj.quad(bottom[j],bottom[i],shoulder[i],shoulder[j],color)
        obj.quad(shoulder[j],shoulder[i],top[i],top[j],color)
    for i in range(1,len(top)-1): obj.tri(top[0],top[i+1],top[i],color)

def prism_ring(obj,outer,inner,y,color):
    for i in range(len(outer)):
        j=(i+1)%len(outer)
        obj.quad((outer[i][0],y,outer[i][1]),(outer[j][0],y,outer[j][1]),
                 (inner[j][0],y,inner[j][1]),(inner[i][0],y,inner[i][1]),color)

def island_layers(cliff,limestone,detail):
    # Open center is deliberately left free of authored faces; Core's 5x5 floor owns it.
    outer=[(-7,-5),(-6.2,-9.4),(-4,-10.8),(0,-11),(4.4,-10.4),(6.6,-8.7),(7,-4.1),(6.8,5.8),(5.4,9.7),(2,10.8),(-2.8,10.9),(-5.8,9.5),(-7,6)]
    lower=[(x*.91,z*.91) for x,z in outer]
    # Cliff wall is a tapered, broken extruded ring around the field, top under/behind floor edge.
    for i in range(len(outer)):
        j=(i+1)%len(outer)
        a,b=outer[i],outer[j]; la,lb=lower[i],lower[j]
        cliff.quad((a[0],-.25,a[1]),(b[0],-.25,b[1]),(lb[0],-3.8,lb[1]),(la[0],-3.8,la[1]),
                   (.58+.06*math.sin(i*2),.51+.04*math.cos(i),.41))
        # Thin upper bevel lip, with varied warm face colors.
        inner=(a[0]*.94,a[1]*.94); innerb=(b[0]*.94,b[1]*.94)
        limestone.quad((a[0],-.18,a[1]),(inner[0],-.06,inner[1]),(innerb[0],-.06,innerb[1]),(b[0],-.18,b[1]),
                       (.91,.84,.69) if i%3 else (.86,.77,.59))
    # Side courses and narrow end stones; every footprint clears the Core floor bounds.
    seed=0
    for side in (-1,1):
        for i in range(8):
            z=-8.2+i*2.33; x=side*(6.2+.12*(i%2))
            slab(limestone,(x,-.055,z),1.55,2.05,.22,.09,seed,
                 (.90,.82,.66) if i%4 else (.83,.76,.62)); seed+=1
    for side in (-1,1):
        z=side*10.62
        for i in range(8):
            x=-4.45+i*1.27
            slab(limestone,(x,-.055,z),1.05,.62,.20,.075,seed,
                 (.89,.81,.65) if i%3 else (.82,.74,.60)); seed+=1
    # Sparse unique chisel joints and crack inlays; never a repeated tile grid.
    for i in range(20):
        a=i*2.399; x=math.cos(a)*(5.85+(i%4)*.24); z=math.sin(a)*(8.9+(i%3)*.35)
        p=(x,-.035,z); q=(x+.18*math.cos(a+1.2),-.034,z+.23*math.sin(a+1.2))
        detail.quad(p,(p[0]+.025,p[1],p[2]),(q[0]+.018,q[1],q[2]),q,(.43,.38,.30))
    return outer

def clip_halfplane(poly,nx,nz,c):
    """Clip X/Z polygon to nx*x+nz*z <= c."""
    out=[]
    if not poly:return out
    for i,a in enumerate(poly):
        b=poly[(i+1)%len(poly)]; fa=nx*a[0]+nz*a[1]-c; fb=nx*b[0]+nz*b[1]-c
        ina=fa<=1e-8; inb=fb<=1e-8
        if ina:out.append(a)
        if ina != inb:
            t=fa/(fa-fb);out.append((a[0]+(b[0]-a[0])*t,a[1]+(b[1]-a[1])*t))
    return out

def courtyard(limestone,detail):
    """Non-repeating broken-stone floor beneath actors; Core itself is pure data."""
    rng=random.Random(2187); sites=[]
    for row in range(9):
        for col in range(7):
            sites.append((-5.55+col*1.85+rng.uniform(-.48,.48),
                          -9.35+row*2.34+rng.uniform(-.65,.65)))
    bounds=[(-6.35,-10.8),(6.35,-10.8),(6.35,10.8),(-6.35,10.8)]
    for idx,(sx,sz) in enumerate(sites):
        poly=bounds[:]
        for j,(tx,tz) in enumerate(sites):
            if j==idx:continue
            poly=clip_halfplane(poly,tx-sx,tz-sz,(tx*tx+tz*tz-sx*sx-sz*sz)*.5)
            if not poly:break
        if len(poly)<3:continue
        cx=sum(p[0] for p in poly)/len(poly); cz=sum(p[1] for p in poly)/len(poly)
        top=[(cx+(x-cx)*.986,cz+(z-cz)*.986) for x,z in poly]
        tops=[(x,-.02,z) for x,z in top]
        shoulders=[(x,-.075,z) for x,z in poly]
        bottoms=[(cx+(x-cx)*1.006,-.17,cz+(z-cz)*1.006) for x,z in poly]
        shade=(idx*17)%9; color=(.88+.012*shade,.80+.012*shade,.64+.011*shade)
        for k in range(len(poly)):
            q=(k+1)%len(poly)
            limestone.quad(bottoms[q],bottoms[k],shoulders[k],shoulders[q],(.61,.53,.42))
            limestone.quad(shoulders[q],shoulders[k],tops[k],tops[q],color)
        for k in range(1,len(poly)-1):limestone.tri(tops[0],tops[k+1],tops[k],color)
        if idx%7==2:
            a=(cx-.17,-.014,cz-.09); b=(cx+.13,-.014,cz+.12)
            dx,dz=b[0]-a[0],b[2]-a[2]; length=math.hypot(dx,dz); nx,nz=-dz/length*.012,dx/length*.012
            detail.quad((a[0]+nx,a[1],a[2]+nz),(b[0]+nx,b[1],b[2]+nz),
                        (b[0]-nx,b[1],b[2]-nz),(a[0]-nx,a[1],a[2]-nz),(.46,.40,.31))

def column(obj,c,r,h,seed):
    # A faceted, tapering column with a broad crown; no primitive cubes.
    n=9; rings=[]
    for y,rr in ((0,r),(h*.55,r*.82),(h*.83,r*.88),(h*.88,r*1.17)):
        rings.append([(c[0]+math.cos(2*math.pi*i/n+seed*.1)*rr,y+c[1],c[2]+math.sin(2*math.pi*i/n+seed*.1)*rr) for i in range(n)])
    for k in range(len(rings)-1):
        for i in range(n):
            j=(i+1)%n; obj.quad(rings[k][j],rings[k][i],rings[k+1][i],rings[k+1][j],(.71,.63,.49) if (i+k+seed)%4 else (.91,.83,.68))
    for i in range(1,n-1): obj.tri(rings[-1][0],rings[-1][i+1],rings[-1][i])

def arch(limestone,cliff,detail):
    # Broken, open-backed arch on the far rim, z=+7 to +9.5; ring blocks follow a real curve.
    z=8.0; cx=0; radius=2.65; centerY=.95
    for i in range(15):
        a=math.pi*i/14
        x=cx+math.cos(a)*radius; y=centerY+math.sin(a)*2.15
        tang=(-math.sin(a),math.cos(a))
        block=rounded(.62,.78,.17,i)
        # horizontal tangent and vertical normal map a local beveled stone footprint into arch plane.
        points=[]
        for px,pz in block:
            points.append((x+tang[0]*px,y+pz,z))
        lo=[(px,y-.22,pz) for px,_,pz in points]; hi=[(px,y+.22,pz) for px,_,pz in points]
        for j in range(len(lo)):
            k=(j+1)%len(lo); limestone.quad(lo[k],lo[j],hi[j],hi[k],(.90,.82,.67) if i%3 else (.81,.74,.61))
        for j in range(1,len(hi)-1):limestone.tri(hi[0],hi[j+1],hi[j])
        # Fine gold leaf in selected joints, not a continuous outline.
        if i in (2,5,8,11):
            detail.quad((x-.22,y+.21,z-.41),(x+.22,y+.21,z-.41),(x+.22,y+.23,z-.41),(x-.22,y+.23,z-.41),(.73,.53,.25))
    column(cliff,(-radius,0,z),.66,2.25,1); column(limestone,(radius,0,z),.66,2.25,2)
    slab(limestone,(0,3.25,z),1.15,.92,.35,.12,5)

def terrace(limestone,cliff,detail):
    # Narrow side shelves step down from the island without entering the 5x5 walk zone.
    for side in (-1,1):
        for i in range(5):
            z=-7.0+i*3.45; x=side*(6.2+.12*(i%2)); y=-.24-i*.23
            slab(limestone,(x,y,z),1.55,2.05,.3,.12,40+i+side)
            # chipped shadow edge below each shelf
            slab(cliff,(x,y-.30,z),1.7,2.18,.34,.08,55+i+side,(.56,.49,.39))
            if i%2==0:
                detail.quad((x-.52,y+.17,z-.62),(x+.32,y+.17,z-.57),(x+.27,y+.18,z-.54),(x-.48,y+.18,z-.60),(.71,.59,.39))

def waterfall(obj,x,z,width,height,seed):
    # Layered broad ribbons face camera; exposed at front edge and outer side edges.
    y=-.015; wobble=.16*math.sin(seed*1.7)
    topL=(x-width/2,y,z); topR=(x+width/2,y,z+.06)
    botR=(x+width*.34+wobble,-height,z+.2); botL=(x-width*.43,-height,z-.1)
    obj.quad(topL,topR,botR,botL,(.30,.72,.84))
    obj.quad((topL[0]+.14,topL[1],z-.025),(topL[0]+.23,topL[1],z-.03),(botL[0]+.09,-height*.81,z-.055),(botL[0]+.02,-height*.88,z-.06),(.55,.89,.94))
    # fractured spray at the lower edge
    obj.tri(botL,botR,(x+wobble,-height-.28,z+.12),(.56,.88,.93))

def flora(foliage,flowers,center,count,scale,seed):
    # Deterministic clustered leaves form deliberate clumps rather than an even carpet.
    for i in range(count):
        a=i*2.399+seed; rr=scale*(.12+.82*((i*7%13)/13))
        x=center[0]+math.cos(a)*rr; z=center[2]+math.sin(a)*rr
        y=center[1]+.12*math.sin(i*1.8)
        length=.22+.11*(i%4); width=.095+.025*(i%3)
        d=(math.cos(a+.7),0,math.sin(a+.7)); p=(x,y,z)
        tip=(x+math.cos(a)*length,y+.22,z+math.sin(a)*length)
        left=(x-d[0]*width,y+.04,z-d[2]*width); right=(x+d[0]*width,y+.04,z+d[2]*width)
        foliage.tri(p,left,tip,(.19,.42+.025*(i%3),.25)); foliage.tri(p,tip,right,(.24,.50,.30))
        if i%3==0:
            flower=(x+math.sin(i)*.13,y+.24,z+math.cos(i)*.13); flower_mesh(flowers,flower,.14 if i%2 else .17)

def flower_mesh(obj,c,r):
    for i in range(5):
        a=i*2*math.pi/5; b=(i+1)*2*math.pi/5
        p=(c[0]+math.cos(a)*r,c[1]+.015,c[2]+math.sin(a)*r)
        q=(c[0]+math.cos(b)*r,c[1]+.015,c[2]+math.sin(b)*r)
        obj.tri(c,p,q,(.99,.89,.84) if i%3 else (.97,.75,.70))
    obj.tri((c[0]-.025,c[1]+.025,c[2]),(c[0]+.025,c[1]+.025,c[2]),(c[0],c[1]+.032,c[2]+.035),(.91,.66,.31))

def banner(blue,gold,pole):
    x,y,z=pole; top=y+2.65
    # pole facets
    column(gold,(x,y,z),.055,2.7,9)
    a=(x,top,z); b=(x+1.0,top-.10,z+.06); c=(x+.95,y+.48,z+.15); d=(x,y+.48,z+.02)
    mid=((a[0]+c[0])*.5,(a[1]+c[1])*.5-.06,(a[2]+c[2])*.5+.16)
    tip=(x+.55,y+.05,z+.20)
    blue.quad(a,b,mid,d,(.10,.36,.62)); blue.quad(mid,b,c,tip,(.13,.45,.72))
    # gold hem + a tiny gold star motif
    gold.quad(d,(d[0],d[1]+.07,d[2]),(c[0],c[1]+.07,c[2]),c,(.87,.62,.22))
    star=(x+.48,y+1.65,z+.19); gold.tri(star,(star[0]-.19,star[1],star[2]),(star[0],star[1]+.25,star[2]),(.97,.76,.35))
    gold.tri(star,(star[0],star[1]+.25,star[2]),(star[0]+.19,star[1],star[2]),(.97,.76,.35))

def beam(obj,a,b,width,depth,color):
    # Locally authored rectangular beam, used only for small lantern frames.
    x0,y0,z0=a; x1,y1,z1=b
    v=[(x0-width/2,y0-depth/2,z0-depth/2),(x0+width/2,y0-depth/2,z0-depth/2),
       (x0+width/2,y0-depth/2,z0+depth/2),(x0-width/2,y0-depth/2,z0+depth/2),
       (x1-width/2,y1+depth/2,z1-depth/2),(x1+width/2,y1+depth/2,z1-depth/2),
       (x1+width/2,y1+depth/2,z1+depth/2),(x1-width/2,y1+depth/2,z1+depth/2)]
    for i,j,k,l in ((0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)):
        obj.quad(v[i],v[j],v[k],v[l],color)

def lantern(detail,warm,base):
    x,y,z=base
    # small open-frame lantern casing and flame, all clear of combat cells
    for dx,dz in ((-.13,-.13),(.13,-.13),(.13,.13),(-.13,.13)):
        beam(detail,(x+dx,y,z+dz),(x+dx,y+.43,z+dz),.035,.035,(.58,.38,.17))
    for yy in (y+.04,y+.43):
        beam(detail,(x-.15,yy,z),(x+.15,yy,z),.035,.035,(.78,.56,.26))
        beam(detail,(x,yy,z-.15),(x,yy,z+.15),.035,.035,(.78,.56,.26))
    warm.tri((x,y+.12,z),(x-.075,y+.25,z),(x,y+.4,z),(.98,.65,.20))
    warm.tri((x,y+.12,z),(x,y+.4,z),(x+.075,y+.24,z),(.99,.85,.43))

def make_assets():
    objs={name:Obj(name) for name in PALETTE}
    outer=island_layers(objs["Cliff"],objs["Limestone"],objs["Detail"])
    courtyard(objs["Limestone"],objs["Detail"])
    terrace(objs["Limestone"],objs["Cliff"],objs["Detail"])
    arch(objs["Limestone"],objs["Cliff"],objs["Detail"])
    # A handful of unequal floating islets in the near background create readable depth.
    for idx,(x,y,z,s) in enumerate(((-7.2,2.5,3.6,1.0),(7.15,3.3,4.8,.78),(-4.9,3.7,6.6,.62),(4.8,3.9,7.8,.48))):
        slab(objs["Distant"],(x,y,z),3.0*s,2.3*s,.65*s,.2*s,idx,(.68,.76,.77))
        column(objs["Distant"],(x-.45*s,y+.25*s,z),.35*s,1.1*s,idx+3)
        # pointed hanging underside gives each island a distinct floating silhouette
        objs["Distant"].tri((x-1.1*s,y-.3*s,z),(x+1.1*s,y-.3*s,z),(x,y-1.8*s,z),(.54,.65,.66))
    # Cascades from both sides and clearly visible across the foreground edge.
    waterfall(objs["Water"],-6.75,-1.2,1.55,3.9,1)
    waterfall(objs["Water"],6.72,2.4,1.4,3.7,2)
    # Front-left cascade is replaced by the bounded HeroLedge authored sheet.
    waterfall(objs["Water"],2.9,-11.16,1.35,3.5,4)
    # Floral clusters mark four ruin bases and the far arch rather than every tile.
    for c,n,r,s in (((-6.25,0,3.8),65,.78,1),((6.2,0,4.5),58,.75,2),((-6.2,.05,8.0),50,.72,3),((6.15,.05,8.5),48,.7,4),((-6.15,2.8,7.3),42,.68,5)):
        flora(objs["Foliage"],objs["Flowers"],c,n,r,s)
    # Foreground ledge gardens and hanging ivy chains are authored to read at phone size.
    for side in (-1,1):
        for z in (-8.2,-4.0):
            flora(objs["Foliage"],objs["Flowers"],(side*6.0,.05,z),45,.62,int(z*7+side))
            for drop in range(4):
                flora(objs["Foliage"],objs["Flowers"],(side*6.5,-.4-drop*.55,z+.16*drop),12,.30,drop+18)
    # Blue/gold banners at the side terraces, held away from the core floor.
    banner(objs["Blue"],objs["Gold"],(-6.25,.0,5.2)); banner(objs["Blue"],objs["Gold"],(6.2,.0,5.7))
    lantern(objs["Detail"],objs["Warm"],(-5.6,1.1,6.1)); lantern(objs["Detail"],objs["Warm"],(5.65,1.3,6.6))
    # Disconnected cliff plates hang below the rim; intentional gaps expose open sky.
    for i in range(18):
        a=2*math.pi*i/18; x=math.cos(a)*6.1; z=math.sin(a)*9.5
        w=.75+(i%3)*.28; h=.5+(i%4)*.22
        slab(objs["Cliff"],(x,-2.6-(i%3)*.18,z),w,1.15,h,.08,i+90,(.54,.47,.38) if i%2 else (.64,.55,.43))
    return objs

def validate(objs):
    expected=set(PALETTE); assert set(objs)==expected
    for name,obj in objs.items():
        assert obj.tris, f"empty authored layer {name}"
        # Every triangle has a finite unit normal and nonzero area from Obj.tri.
        for _,n,_ in obj.tris:
            assert abs(sum(v*v for v in n)-1)<1e-5
    # Correct axis/range sanity for origin-centered board and prescribed visible front/back edges.
    bb=objs["Cliff"].bounds(); assert bb[0][0] <= -6.9 and bb[0][1] >= 6.9
    assert bb[2][0] <= -10.8 and bb[2][1] >= 10.8
    assert min(v[1] for obj in objs.values() for tri,_,_ in obj.tris for v in tri) <= -3.8
    # Distant silhouettes fit the 55-degree yaw-0, orthographic-size 11.5 candidate.
    # View-up projection for that camera is y*cos(55°) + z*sin(55°).
    distant=[v for tri,_,_ in objs["Distant"].tris for v in tri]
    assert max(abs(v[0]) for v in distant) < 11.5
    assert max(abs(v[1]*math.cos(math.radians(55))+v[2]*math.sin(math.radians(55))) for v in distant) < 11.5
    wb=objs["Water"].bounds(); assert wb[2][0] < -11.1 and wb[1][0] <= -3.8 and wb[1][1] >= -.02

def main():
    OUT.mkdir(parents=True,exist_ok=True)
    objs=make_assets(); validate(objs)
    manifest={"authoring":"tools/author_sky_ruins.py (deterministic local Python standard library; no external assets)",
              "coordinate_space":"Unity local X/Y/Z, Y-up; origin is board center; Z already uses DisplayDepth=2 spacing; do not stretch Z",
              "source_art":"User-provided local concept image was viewed read-only for composition; not copied, embedded, uploaded, or used as a texture.",
              "license":"Original procedural geometry authored for this repository; no third-party mesh or texture source.",
              "core_floor":"Core is pure data and has no mesh. Limestone contains a non-repeating broken-stone courtyard at y=-0.02..-0.17 beneath actors; it has no collider or gameplay semantics.",
              "layers":{}}
    for name,obj in objs.items():
        path=OUT/(name+".obj"); obj.export(path)
        manifest["layers"][name]={"file":path.name,"triangles":len(obj.tris),"bounds":obj.bounds(),"material_key":name}
    (OUT/"provenance.json").write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
    print(json.dumps({k:{"triangles":v["triangles"],"bounds":v["bounds"]} for k,v in manifest["layers"].items()},indent=2))

if __name__=="__main__": main()
