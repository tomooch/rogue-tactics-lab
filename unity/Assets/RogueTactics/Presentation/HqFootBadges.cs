using UnityEngine;
namespace RogueTactics.Presentation
{
    public sealed partial class MobileG1View
    {
        private void LayoutFootBadges()
        {
            // Follow the visible owner, including movement and paused tween poses.
            // Never move another actor's HP across the board to solve a rectangle collision.
            foreach(var entry in bodyRects)
            {
                var label=badges[entry.Key].GetComponent<RectTransform>();var size=label.rect.size;var body=entry.Value;
                label.anchoredPosition=new Vector2(body.center.x-size.x/2,body.yMin-5);
                // Protect another actor's head/upper silhouette, while retaining leg-level HP.
                // Keep its owner anchor; hide while occluded instead of relocating it.
                var strip=new Rect(label.anchoredPosition.x,label.anchoredPosition.y-size.y,size.x,size.y);
                var occluded=false;
                foreach(var other in bodyRects)
                    if(other.Key!=entry.Key)
                    {
                        var protectedBody=other.Value;
                        protectedBody.yMin+=protectedBody.height*.35f;
                        if(strip.Overlaps(protectedBody)){occluded=true;break;}
                    }
                badges[entry.Key].alpha=occluded?0:1;
            }
        }
    }
}
