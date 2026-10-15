using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace RogueTactics.Presentation
{
    public sealed partial class MobileG1View
    {
        private readonly List<Rect> placedFootLabels=new List<Rect>(6);
        private void LayoutFootBadges()
        {
            placedFootLabels.Clear();
            // The label stays below its own silhouette. A short lateral displacement keeps front/rear neighbours clear.
            foreach(var entry in bodyRects.OrderBy(item=>item.Value.yMin))
            {
                var label=badges[entry.Key].GetComponent<RectTransform>();var size=label.rect.size;var body=entry.Value;
                var preferred=Mathf.Clamp(body.center.x-size.x/2,4,Field.rect.width-size.x-4);var top=body.yMin-5;
                var chosen=new Rect(preferred,top-size.y,size.x,size.y);var best=float.MaxValue;
                for(var distance=0;distance<Field.rect.width;distance+=2)
                {
                    var found=false;
                    for(var side=0;side<2;side++)
                    {
                        var x=preferred+(side==0?-distance:distance);if(x<4 || x+size.x>Field.rect.width-4)continue;
                        var candidate=new Rect(x,top-size.y,size.x,size.y);var cost=0f;
                        foreach(var r in bodyRects.Values)cost+=IntersectionArea(candidate,r);
                        foreach(var r in placedFootLabels)cost+=IntersectionArea(candidate,r);
                        if(cost<best){best=cost;chosen=candidate;}
                        if(cost<=0){found=true;break;}
                    }
                    if(found)break;
                }
                label.anchoredPosition=new Vector2(chosen.x,chosen.yMax);placedFootLabels.Add(chosen);
            }
        }
        private static float IntersectionArea(Rect a,Rect b)=>Mathf.Max(0,Mathf.Min(a.xMax,b.xMax)-Mathf.Max(a.xMin,b.xMin))*Mathf.Max(0,Mathf.Min(a.yMax,b.yMax)-Mathf.Max(a.yMin,b.yMin));
    }
}
