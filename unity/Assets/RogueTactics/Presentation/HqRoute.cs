using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace RogueTactics.Presentation
{
    public sealed partial class MobileG1View
    {
        // Presentation-only visibility graph around projected silhouettes and labels.
        // This routes a known Core relationship; it never chooses a gameplay path or target.
        private Vector2[] ClearRoute(Rect from,Rect to,Rect[] obstacles,Vector2[] fallback)
        {
            var blocked=obstacles.Select(r=>new Rect(r.x-5,r.y-5,r.width+10,r.height+10)).ToArray();
            var nodes=new List<Vector2>();
            foreach(var r in new[]{from,to})nodes.AddRange(new[]{new Vector2(r.xMin-8,r.center.y),new Vector2(r.xMax+8,r.center.y),new Vector2(r.center.x,r.yMin-8),new Vector2(r.center.x,r.yMax+8)});
            foreach(var r in blocked)nodes.AddRange(new[]{new Vector2(r.xMin-1,r.yMin-1),new Vector2(r.xMin-1,r.yMax+1),new Vector2(r.xMax+1,r.yMin-1),new Vector2(r.xMax+1,r.yMax+1)});
            var valid=nodes.Select(p=>p.x>=1 && p.x<=Field.rect.width-1 && p.y>=-Field.rect.height+1 && p.y<=-1 && !blocked.Any(r=>r.Contains(p))).ToArray();
            var costs=Enumerable.Repeat(float.PositiveInfinity,nodes.Count).ToArray();var previous=Enumerable.Repeat(-1,nodes.Count).ToArray();var used=new bool[nodes.Count];
            for(var i=0;i<4;i++)if(valid[i])costs[i]=0;
            for(var turn=0;turn<nodes.Count;turn++)
            {
                var current=-1;for(var i=0;i<nodes.Count;i++)if(valid[i] && !used[i] && (current<0 || costs[i]<costs[current]))current=i;
                if(current<0 || float.IsPositiveInfinity(costs[current]))break;
                if(current>=4 && current<8)
                {
                    var path=new List<Vector2>();for(var at=current;at>=0;at=previous[at])path.Add(nodes[at]);path.Reverse();return path.ToArray();
                }
                used[current]=true;
                for(var next=0;next<nodes.Count;next++)
                {
                    if(!valid[next] || used[next] || blocked.Any(r=>SegmentHits(nodes[current],nodes[next],r)))continue;
                    var cost=costs[current]+Vector2.Distance(nodes[current],nodes[next]);
                    if(cost<costs[next]){costs[next]=cost;previous[next]=current;}
                }
            }
            return fallback;
        }
        private static bool SegmentHits(Vector2 a,Vector2 b,Rect r)
        {
            var d=b-a;var low=0f;var high=1f;
            for(var axis=0;axis<2;axis++)
            {
                var start=axis==0?a.x:a.y;var delta=axis==0?d.x:d.y;var min=axis==0?r.xMin:r.yMin;var max=axis==0?r.xMax:r.yMax;
                if(Mathf.Abs(delta)<.0001f){if(start<min || start>max)return false;continue;}
                var enter=(min-start)/delta;var exit=(max-start)/delta;if(enter>exit){var swap=enter;enter=exit;exit=swap;}
                low=Mathf.Max(low,enter);high=Mathf.Min(high,exit);if(low>high)return false;
            }
            return true;
        }
    }
}
