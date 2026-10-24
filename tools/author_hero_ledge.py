#!/usr/bin/env python3
"""Author one camera-readable front-left ruin ledge; no assets or packages from outside."""
from __future__ import annotations

import importlib.util
import json
import math
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/"unity/Assets/RogueTactics/Art/HqRuins"
HELPER=ROOT/"tools/author_sky_ruins.py"
spec=importlib.util.spec_from_file_location("sky_ruins_authoring",HELPER)
sky=importlib.util.module_from_spec(spec); spec.loader.exec_module(sky)

COLORS={
 "HeroLedgeLimestone":(.90,.82,.66), "HeroLedgeCliff":(.59,.51,.41),
 "HeroLedgeFoliage":(.20,.45,.27), "HeroLedgeFlowers":(.98,.88,.80),
 "HeroLedgeWater":(.27,.72,.83), "HeroLedgeFoam":(.76,.94,.92),
 "HeroLedgeDetail":(.70,.60,.43),
}
sky.PALETTE.update(COLORS)

def vadd(a,b):return tuple(x+y for x,y in zip(a,b))
def mix(a,b,t):return tuple(x+(y-x)*t for x,y in zip(a,b))

def beveled_brick(obj,c,w,d,h,bevel,seed,rotation=0,color=None):
    """Irregular rounded/chipped masonry block with separate bevel and side courses."""
    pts=sky.rounded(w,d,.18,seed)
    # Cut one or two different corners more deeply so no two stones share a silhouette.
    for i in (seed%8,(seed*3+1)%8):
        x,z=pts[i]; pts[i]=(x*.91,z*.91)
    ca,sa=math.cos(rotation),math.sin(rotation)
    pts=[(x*ca-z*sa,x*sa+z*ca) for x,z in pts]
    inner=[(x*.91,z*.91) for x,z in pts]
    bottom=[]; shoulder=[]; top=[]
    for (x,z),(ix,iz) in zip(pts,inner):
        bottom.append((c[0]+x,c[1]-h*.5,c[2]+z))
        shoulder.append((c[0]+x,c[1]+h*.5-bevel,c[2]+z))
        top.append((c[0]+ix,c[1]+h*.5,c[2]+iz))
    tint=color or COLORS[obj.name]
    for i in range(8):
        j=(i+1)%8
        obj.quad(bottom[j],bottom[i],shoulder[i],shoulder[j],tuple(q*.82 for q in tint))
        # Slight face-to-face variation suggests pale worn courses under directional light.
        shade=.96+.035*math.sin(seed*1.71+i*2.6)
        obj.quad(shoulder[j],shoulder[i],top[i],top[j],tuple(q*shade for q in tint))
    for i in range(1,7):obj.tri(top[0],top[i+1],top[i],tuple(q*1.025 for q in tint))

def rock_mass(obj,c,rx,rz,height,seed):
    """Fractured tapered cliff volume built from offset polygon rings and broken facets."""
    n=7+(seed%3); rings=[]
    profiles=[(-height,.34,.32),(-height*.72,.82,.74),(-height*.34,1.08,.91),(-height*.06,.76,.84)]
    for ri,(dy,xf,zf) in enumerate(profiles):
        ring=[]
        for i in range(n):
            a=2*math.pi*i/n+seed*.37+ri*.12
            jitter=1+.13*math.sin(seed*2+i*3.1+ri)
            ring.append((c[0]+math.cos(a)*rx*xf*jitter,c[1]+dy,c[2]+math.sin(a)*rz*zf*jitter))
        rings.append(ring)
    for r in range(len(rings)-1):
        for i in range(n):
            j=(i+1)%n
            color=(.55,.48,.38) if (i+r+seed)%3 else (.69,.59,.46)
            # Triangulated, offset faces read as broken strata rather than one extruded wall.
            if (i+r)%2:obj.tri(rings[r][i],rings[r][j],rings[r+1][j],color);obj.tri(rings[r][i],rings[r+1][j],rings[r+1][i],color)
            else:obj.quad(rings[r][j],rings[r][i],rings[r+1][i],rings[r+1][j],color)
    bottom=(c[0]+.04,c[1]-height,c[2]-.03)
    for i in range(n):obj.tri(bottom,rings[0][(i+1)%n],rings[0][i],(.48,.42,.34))

def vine_leaf(obj,base,angle,length,width,seed):
    """Convex rounded leaf: scalloped outline plus raised center ridge."""
    dx,dz=math.cos(angle),math.sin(angle); sx,sz=-dz,dx
    # Five stations form a plump leaf with a softly pointed tip and thick central fold.
    stations=[(0,0),(.28,.74),(.62,1.0),(1,0),(.62,-1.0),(.28,-.74)]
    pts=[]
    for along,across in stations:
        pts.append((base[0]+dx*length*along+sx*width*across,
                    base[1]+.055*math.sin(along*math.pi)+.015*(seed%3),
                    base[2]+dz*length*along+sz*width*across))
    ridge=(base[0]+dx*length*.47,base[1]+.025,base[2]+dz*length*.47)
    for i in range(len(pts)):
        j=(i+1)%len(pts)
        obj.tri(ridge,pts[i],pts[j],(.18+.02*(seed%3),.39+.025*(seed%4),.23))

def blossom(obj,c,r,seed):
    # Five plump pale petals, lifted above the leaves, with a warm center.
    center=(c[0],c[1]+.015,c[2])
    for i in range(5):
        a=2*math.pi*i/5+seed*.31
        tip=(c[0]+math.cos(a)*r,c[1]+.04,c[2]+math.sin(a)*r)
        side=(-math.sin(a)*r*.48,.02,math.cos(a)*r*.48)
        left=(c[0]+side[0],c[1]+side[1],c[2]+side[2])
        right=(c[0]-side[0],c[1]+side[1],c[2]-side[2])
        obj.tri(center,left,tip,(.98,.89,.82));obj.tri(center,tip,right,(1,.93,.87))
    obj.tri((c[0]-.035,c[1]+.055,c[2]),(c[0]+.035,c[1]+.055,c[2]),(c[0],c[1]+.06,c[2]+.04),(.93,.67,.31))

def waterfall(water,foam):
    """Curved vertical sheet in front of the ledge, with visible broken foam lips."""
    rows=9; cols=5; grid=[]
    for r in range(rows):
        t=r/(rows-1); y=.08-4.16*t
        z=-11.29-.04*math.sin(t*math.pi)+.03*t
        row=[]
        for col in range(cols):
            u=col/(cols-1)-.5
            x=-2.27+u*(1.08+.22*t)+.08*math.sin(t*4+col*.6)
            row.append((x,y,z+.025*math.sin(u*math.pi*2+t*4)))
        grid.append(row)
    for r in range(rows-1):
        for col in range(cols-1):
            a,b,c,d=grid[r][col],grid[r][col+1],grid[r+1][col+1],grid[r+1][col]
            # A continuous body: flow variation belongs to the UV shader, not tile colours.
            water.quad(a,b,c,d,(.25,.70,.83))
    # Pale, curved ledges and spray breaks at uneven heights.
    for idx,t in enumerate((.02,.94)):
        y=.08-4.16*t; z=-11.34+.025*t; half=.47+.06*math.sin(idx)
        x=-2.27+.08*math.sin(t*4)
        foam.quad((x-half,y,z),(x-half*.62,y+.06,z-.025),(x+half*.68,y+.05,z-.035),(x+half,y-.015,z),(.78,.95,.94))
        if idx in (1,3):
            foam.tri((x+half*.45,y,z-.04),(x+half*.82,y-.13,z-.045),(x+half*.75,y+.02,z-.03),(.68,.90,.92))

def author():
    names=list(COLORS)
    meshes={name:sky.Obj(name) for name in names}
    stone=meshes["HeroLedgeLimestone"]; cliff=meshes["HeroLedgeCliff"]
    foliage=meshes["HeroLedgeFoliage"]; flowers=meshes["HeroLedgeFlowers"]
    detail=meshes["HeroLedgeDetail"]

    # Four capstones make a broad broken ledge; irregular overlaps and seams catch light.
    caps=[(-5.80,-10.58,1.65,1.20,.25,.04),(-4.25,-10.43,1.72,1.38,.28,-.04),
          (-2.52,-10.49,1.60,1.22,.24,.035),(-1.19,-10.54,1.30,1.30,.26,-.06)]
    for i,(x,z,w,d,h,rot) in enumerate(caps):
        beveled_brick(stone,(x,-.015,z),w,d,h,.12,i,rot,(.92,.84,.69) if i%2==0 else (.86,.78,.64))
    # Staggered courses, varied joints, broken corners, and proud lintel stones.
    courses=[
      [(-6.08,1.15,-10.91,.58),(-4.82,1.22,-10.99,.67),(-3.43,1.48,-10.88,.60),(-1.83,1.43,-11.02,.69),(-1.0,.92,-10.93,.55)],
      [(-5.52,1.58,-10.67,.60),(-3.82,1.60,-10.72,.71),(-2.04,1.65,-10.65,.59),(-.95,.78,-10.71,.63)],
      [(-6.08,1.03,-10.37,.72),(-4.63,1.78,-10.40,.79),(-2.66,1.75,-10.32,.66),(-.99,.92,-10.39,.72)]
    ]
    for row,blocks in enumerate(courses):
        y=-.36-row*.61
        for col,(x,w,z,d) in enumerate(blocks):
            h=.50+.10*((row+col)%3); rotation=((col%3)-1)*.035
            beveled_brick(stone,(x,y,z),w,d,h,.12+(col%2)*.035,row*11+col,rotation,
                          (.91,.83,.68) if (row+col)%3 else (.82,.74,.59))
            # A few inset joints and chipped-face grooves, restrained at phone scale.
            if (row+col)%3==0:
                x0=x-.20; y0=y+.05
                detail.quad((x0,y0,z-d*.51),(x0+.025,y0,z-d*.51),(x0+.15,y0+.008,z-d*.49),(x0+.13,y0+.008,z-d*.49),(.48,.41,.31))

    # Several independent fractured volumes sit under the masonry; broken gaps reveal depth.
    rocks=[(-6.0,-10.15,.55,.82,3.7,1),(-5.15,-10.85,.72,.58,2.7,3),
           (-3.86,-10.08,.82,.78,3.35,5),(-2.85,-10.88,.65,.55,4.05,7),
           (-1.62,-10.17,.74,.75,3.1,9),(-1.15,-10.82,.50,.48,2.6,11)]
    for x,z,rx,rz,h,seed in rocks:
        rock_mass(cliff,(x,-.15,z),rx,rz,h,seed)
    # Uneven dropped strata overlap only at their edges, leaving shadow clefts between volumes.
    for i,(x,z,w,h) in enumerate(((-5.95,-9.9,1.38,.52),(-4.93,-10.0,1.42,.43),(-3.5,-9.95,1.62,.57),(-1.85,-9.94,1.38,.46),(-.94,-9.92,.75,.48))):
        beveled_brick(cliff,(x,-2.15-i%2*.25,z),w,.62,h,.08,30+i,(i%3-1)*.06,(.56,.49,.39))
    # Keep this patch inside the agreed corner box with a visible seam to neighboring ledge art.
    cliff.tris=[(tuple((x,y,z+.04) for x,y,z in verts),normal,color) for verts,normal,color in cliff.tris]

    # A clustered ivy crown grows over the ledge and drapes toward the cliff face.
    clusters=[(-5.78,-10.22,-.02,31,.58),(-4.43,-10.20,.0,27,.70),
              (-2.96,-10.18,.0,34,.66),(-1.55,-10.16,-.03,29,.62)]
    leaf_id=0
    for cx,cz,cy,count,spread in clusters:
        for i in range(count):
            a=i*2.399+leaf_id*.17; r=spread*(.12+.80*((i*7%17)/17))
            x=cx+math.cos(a)*r; z=cz+math.sin(a)*r*.45
            y=cy+.05*math.sin(i*2.2)
            vine_leaf(foliage,(x,y,z),a+.35,.30+.12*(i%3),.095+.025*(i%2),leaf_id)
            if i%5==0:blossom(flowers,(x,y+.01,z-.03),.105+.02*(i%2),leaf_id)
            leaf_id+=1
    # Five visible dangling vines, tapering and curving between separate stone courses.
    for strand,(x,z0,length) in enumerate(((-6.22,-11.06,2.6),(-5.02,-11.06,3.35),(-3.44,-11.06,2.9),(-1.93,-11.05,3.65),(-1.18,-11.04,2.4))):
        for i in range(14):
            t=i/13; y=-.12-length*t
            xleaf=x+.10*math.sin(t*4+strand); z=z0-.07*math.sin(t*math.pi)
            ang=(.2 if i%2 else 2.4)+.12*math.sin(i+strand)
            vine_leaf(foliage,(xleaf,y,z),ang,.31,.105,100+strand*20+i)
            if i in (4,10):blossom(flowers,(xleaf,y+.04,z-.015),.09,200+strand*2+i)

    # Small cascaded sheet and broken foam shelves at the ledge's front face.
    waterfall(meshes["HeroLedgeWater"],meshes["HeroLedgeFoam"])
    # Gold mineral seams are short and discontinuous, like exposed veins in the limestone.
    for i in range(11):
        x=-6.25+i*.52; z=-10.12+.12*math.sin(i*1.9); y=-.005
        detail.quad((x,y,z),(x+.20,y,z+.018),(x+.27,y+.006,z+.035),(x+.06,y+.006,z+.018),(.78,.61,.31))
    return meshes

def export_custom(obj,path,vertical_uv=False):
    lines=["# Authored Hero Ledge prototype; no source image or external mesh data.",
           "# Unity local axes (Y up), origin board center; DisplayDepth=2 already authored in Z.",
           "# Vertex RGB extension; standard vt/vn and triangulated f records.",
           f"o {obj.name}",f"g {obj.name}",f"usemtl {obj.name}"]
    vi=ti=ni=1
    for verts,n,color in obj.tris:
        for x,y,z in verts:lines.append(f"v {x:.5f} {y:.5f} {z:.5f} {color[0]:.4f} {color[1]:.4f} {color[2]:.4f}")
        for x,y,z in verts:
            uv=(x*.42,(.15-y)*.38) if vertical_uv else (x*.34,z*.34)
            lines.append(f"vt {uv[0]:.5f} {uv[1]:.5f}")
        lines.append(f"vn {n[0]:.6f} {n[1]:.6f} {n[2]:.6f}")
        lines.append(f"f {vi}/{ti}/{ni} {vi+1}/{ti+1}/{ni} {vi+2}/{ti+2}/{ni}")
        vi+=3;ti+=3;ni+=1
    path.write_text("\n".join(lines)+"\n",encoding="utf-8")

def main():
    OUT.mkdir(parents=True,exist_ok=True); meshes=author()
    total=sum(len(m.tris) for m in meshes.values())
    assert total<6000,total
    for name,obj in meshes.items():
        b=obj.bounds()
        assert -6.7<=b[0][0] and b[0][1]<=-.5,(name,b[0])
        assert -4.2<=b[1][0] and b[1][1]<=.15,(name,b[1])
        assert -11.4<=b[2][0] and b[2][1]<=-9.3,(name,b[2])
    manifest={"scope":"one authored front-left hero ledge corner only; no gameplay/collider data",
      "coordinates":"origin board center; Unity local X/Y/Z, Y-up; DisplayDepth=2 already authored; no Z stretch",
      "reference":"baseline-contact.png inspected for composition. Original private concept was not copied or uploaded.",
      "provenance":"Original deterministic geometry authored in tools/author_hero_ledge.py using the repository's local OBJ helper; no external meshes, textures, packages, or services.",
      "prototype_limit":"This is a bounded representative corner for camera/import review, not a complete environment or visual acceptance claim.","layers":{}}
    for name,obj in meshes.items():
        file=OUT/(name+".obj"); export_custom(obj,file,vertical_uv=name=="HeroLedgeWater")
        manifest["layers"][name]={"file":file.name,"triangles":len(obj.tris),"bounds":obj.bounds(),"uv":"world X/Y sheet" if name=="HeroLedgeWater" else "world planar X/Z"}
    (OUT/"HeroLedge_provenance.json").write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
    print(json.dumps({"total_triangles":total,"layers":manifest["layers"]},indent=2))

if __name__=="__main__":main()
