"""Geometric outcome regressions for the formerly zero-depth arch ring."""
import collections
import unittest
import author_sky_ruins as author


class ArchGeometryTests(unittest.TestCase):
    def test_each_ring_stone_is_closed_and_has_positive_solid_volume(self):
        for stone in author.arch_ring_stones():
            points=[p for tri,_,_ in stone.tris for p in tri]
            for axis in range(3):
                self.assertGreater(max(p[axis] for p in points)-min(p[axis] for p in points),.1)
            self.assertGreater(author.signed_volume(stone),.02)
            edges=collections.Counter()
            for tri,_,_ in stone.tris:
                keys=[tuple(round(v,6) for v in p) for p in tri]
                for a,b in zip(keys,keys[1:]+keys[:1]):edges[tuple(sorted((a,b)))]+=1
            self.assertTrue(all(count==2 for count in edges.values()),"Every welded edge must have two incident faces")

    def test_adjacent_voussoirs_share_a_bearing_face(self):
        stones=author.arch_ring_stones()
        vertices=[{tuple(round(v,6) for v in p) for tri,_,_ in stone.tris for p in tri} for stone in stones]
        for left,right in zip(vertices,vertices[1:]):
            self.assertGreaterEqual(len(left & right),4,"Neighbours need a shared bearing face, not a visible air gap")

    def test_ring_retains_an_open_passage_instead_of_a_filled_wall(self):
        # A solid slab could pass the volume test; the opening must remain empty.
        for tri,_,_ in [t for stone in author.arch_ring_stones() for t in stone.tris]:
            a,b,c=tri;px,py=0.,3.
            det=(b[1]-c[1])*(a[0]-c[0])+(c[0]-b[0])*(a[1]-c[1])
            if abs(det)<1e-10:continue
            u=((b[1]-c[1])*(px-c[0])+(c[0]-b[0])*(py-c[1]))/det
            v=((c[1]-a[1])*(px-c[0])+(a[0]-c[0])*(py-c[1]))/det
            self.assertFalse(u>=0 and v>=0 and u+v<=1,"Central sightline must pass through the arch")


if __name__=="__main__":unittest.main()
