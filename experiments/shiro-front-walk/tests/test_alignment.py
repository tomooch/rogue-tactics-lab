import sys,unittest
from pathlib import Path
from PIL import Image,ImageDraw
sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'scripts'))
from build_alignment_ab import translate,validate_frame
class AlignmentRegression(unittest.TestCase):
 def frame(self):
  im=Image.new('RGBA',(512,512));ImageDraw.Draw(im).rectangle((80,80,350,470),fill=(120,130,170,255));im.putpixel((80,100),(120,130,170,33));return im
 def test_translation_keeps_antialias_pixels_and_y(self):
  a=self.frame();b=translate(a,25);self.assertEqual(b.getpixel((105,100)),(120,130,170,33));validate_frame(a,b,25,[252,160],[260,250])
 def test_old_sole_anchor_rejected_for_B(self):
  a=self.frame()
  with self.assertRaises(AssertionError):validate_frame(a,a,0,[220,160],[230,250])
 def test_gutter_failure_cannot_be_hidden_by_shrinking(self):
  with self.assertRaises(ValueError):translate(self.frame(),-41)
if __name__=='__main__':unittest.main()
