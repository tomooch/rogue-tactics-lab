import sys,unittest
from pathlib import Path
import numpy as np
from PIL import Image,ImageDraw
sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'scripts'))
from clean_contours import clean

class ContourTest(unittest.TestCase):
    def test_removes_contamination_preserves_face_and_adjacent_antialiasing(self):
        image=Image.new('RGBA',(100,100));draw=ImageDraw.Draw(image)
        draw.rectangle((25,15,74,84),fill=(70,80,180,255))
        image.putpixel((50,40),(235,180,150,255)) # design interior
        image.putpixel((24,60),(255,0,0,250)) # mask contamination
        image.putpixel((24,59),(70,80,180,20)) # valid antialiasing
        image.putpixel((3,3),(50,60,100,1)) # isolated haze
        result,_,report=clean(image,(50,50),(40,30,60,50))
        self.assertEqual(result.getpixel((24,60)),(0,0,0,0))
        self.assertEqual(result.getpixel((24,59)),image.getpixel((24,59)))
        self.assertEqual(result.getpixel((50,40)),image.getpixel((50,40)))
        self.assertEqual(result.getpixel((3,3)),(0,0,0,0))
        self.assertEqual(report['protected_design_changes'],0)
        original=np.array(image);cleaned=np.array(result)
        np.testing.assert_array_equal(original[30:50,40:60],cleaned[30:50,40:60])

if __name__=='__main__':unittest.main()
