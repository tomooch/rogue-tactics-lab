import tempfile,sys,json,unittest,os
from pathlib import Path
from PIL import Image,ImageDraw
sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'scripts'))
import build_walk

class PipelineTest(unittest.TestCase):
    def make(self, root, opaque=False):
        p=Path(root);p.mkdir(parents=True,exist_ok=True)
        anchors={}
        for i,name in enumerate(build_walk.NAMES):
            im=Image.new('RGBA',(600,620),(255,255,255,255) if opaque else (0,0,0,0))
            d=ImageDraw.Draw(im)
            dx=[-12,-7,0,12,7,0][i]
            d.ellipse((160,20,450,270),fill=(235,235,255,255)) # head
            d.rectangle((240,250,360,490),fill=(78,95,170,255))
            d.rectangle((258+dx,484,290+dx,592),fill=(87,75,166,255))
            d.rectangle((316-dx,484,349-dx,592),fill=(87,75,166,255))
            im.save(p/name)
            anchors[name]={'foot_point':[274+dx,592]}
        (p/'foot_anchors.json').write_text(json.dumps(anchors))
    def test_synthetic_sprite_harness(self):
        with tempfile.TemporaryDirectory() as t:
            inp=Path(t)/'in';out=Path(t)/'out';self.make(inp)
            data=build_walk.assemble(build_walk.load_sources(inp),out,False)
            self.assertTrue(data['pass']);self.assertEqual(len(data['frames']),6)
            with Image.open(out/'shiro_walk_front_atlas.png') as a:
                self.assertEqual(a.size,(1536,1024))
            for name in build_walk.NAMES:
                with Image.open(out/'frames'/name) as a:
                    self.assertEqual(a.getpixel((0,0))[3],0)
            self.assertFalse(data['natural_gait_human_approved'])
    def test_opaque_is_rejected(self):
        with tempfile.TemporaryDirectory() as t:
            self.make(t,True)
            with self.assertRaises(build_walk.QAError):build_walk.load_sources(t)
    def test_missing_anchors_rejected(self):
        with tempfile.TemporaryDirectory() as t:
            self.make(t)
            (Path(t)/'foot_anchors.json').unlink()
            with self.assertRaises(build_walk.QAError):build_walk.load_sources(t)
    def test_anchor_in_empty_gap_is_rejected(self):
        with tempfile.TemporaryDirectory() as t:
            self.make(t)
            path=Path(t)/'foot_anchors.json'
            anchors=json.loads(path.read_text())
            anchors[build_walk.NAMES[0]]['foot_point']=[302,592]
            path.write_text(json.dumps(anchors))
            with self.assertRaises(build_walk.QAError):build_walk.load_sources(t)
    def test_low_alpha_is_not_silently_cropped(self):
        with tempfile.TemporaryDirectory() as t:
            self.make(t)
            path=Path(t)/build_walk.NAMES[0]
            im=Image.open(path);im.putpixel((10,300),(128,0,128,1));im.save(path)
            self.assertEqual(build_walk.load_sources(t)[0][3][0],10)

if __name__=='__main__':unittest.main()
