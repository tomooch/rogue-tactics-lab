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
            }
        }
    }
}
