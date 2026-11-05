using RogueTactics.Core.G1;
using UnityEngine;
using UnityEngine.UI;

namespace RogueTactics.Presentation
{
    public sealed partial class MobileG1View
    {
        private RelationAccent sourceAccent,targetAccent;
        private string relationActor="",relationTarget="";
        private ActionKind relationKind;
        private bool relationPreview;
        private bool guardImpact,counterImpact;
        private void BuildRelationCues()
        {
            RelationAccent Add(string name){var r=Rect(Field,name,0,0,1,1);var a=r.gameObject.AddComponent<RelationAccent>();a.raycastTarget=false;var shadow=r.gameObject.AddComponent<Shadow>();shadow.effectColor=new Color(.025f,.06f,.08f,.9f);shadow.effectDistance=new Vector2(1,-1);return a;}
            sourceAccent=Add("Actual actor accent");targetAccent=Add("Actual target accent");
        }
        private void ShowRelation(string actor,string target,ActionKind kind,bool preview)
        {relationActor=actor;relationTarget=target;relationKind=kind;relationPreview=preview;guardImpact=counterImpact=false;RefreshRelationCues();}
        private void ShowImpactRelation(BattleEvent e)
        {ShowRelation(e.Actor,e.Target,e.Kind==EventKind.Heal?ActionKind.Heal:ActionKind.Attack,false);guardImpact=e.Detail.StartsWith("Guard");counterImpact=e.Kind==EventKind.Counter;RefreshRelationCues();}
        private void ClearRelation(){relationActor=relationTarget="";RefreshRelationCues();}
        private void RefreshRelationCues()
        {
            if(sourceAccent==null)return;
            var active=!terminalSeen && Session.Started && relationKind!=ActionKind.Move;
            Rect source=default,target=default;
            var hasSource=active && bodyRects.TryGetValue(relationActor,out source);
            var hasTarget=active && bodyRects.TryGetValue(relationTarget,out target);
            var color=relationPreview?new Color(.35f,.76f,1):relationKind==ActionKind.Heal?new Color(.40f,.95f,.67f):relationKind==ActionKind.Protect?new Color(.35f,.90f,.95f):new Color(1,.75f,.35f);
            sourceAccent.gameObject.SetActive(hasSource);targetAccent.gameObject.SetActive(hasTarget && relationActor!=relationTarget);
            if(hasSource)sourceAccent.Place(source,color,relationPreview,0);
            if(hasTarget)targetAccent.Place(target,guardImpact?new Color(.35f,.90f,.95f):color,relationPreview,guardImpact?1:counterImpact?4:relationKind==ActionKind.Heal?2:relationKind==ActionKind.Protect?1:3);
        }
    }
    // Two local corner accents identify the actual participants. No graph/path is drawn for a non-move.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RelationAccent:MaskableGraphic
    {
        private bool preview;private int symbol;
        public void Place(Rect body,Color tint,bool isPreview,int glyph)
        {rectTransform.anchoredPosition=new Vector2(body.xMin-4,body.yMax+4);rectTransform.sizeDelta=body.size+Vector2.one*8;color=tint;preview=isPreview;symbol=glyph;SetVerticesDirty();}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var w=rectTransform.rect.width;var h=rectTransform.rect.height;var thickness=preview?1.5f:2.4f;var arm=8f;
            void Quad(float x,float y,float width,float height){var i=vh.currentVertCount;vh.AddVert(new Vector3(x,y,0),color,Vector2.zero);vh.AddVert(new Vector3(x+width,y,0),color,Vector2.zero);vh.AddVert(new Vector3(x+width,y+height,0),color,Vector2.zero);vh.AddVert(new Vector3(x,y+height,0),color,Vector2.zero);vh.AddTriangle(i,i+1,i+2);vh.AddTriangle(i,i+2,i+3);}
            foreach(var x in new[]{0f,w-arm}){Quad(x,0,arm,thickness);Quad(x,-h,arm,thickness);}
            foreach(var x in new[]{0f,w-thickness}){Quad(x,-arm,thickness,arm);Quad(x,-h,thickness,arm);}
            // A small cross/shield/impact sign sits outside the target silhouette, never over its face.
            if(!preview && symbol==2){Quad(w+2,-9,10,2);Quad(w+6,-13,2,10);}
            if(!preview && symbol==1){Quad(w+3,-4,8,2);Quad(w+3,-12,2,8);Quad(w+9,-12,2,8);Quad(w+5,-14,4,2);}
            if(!preview && symbol==3){Quad(w+3,-9,9,2);Quad(w+7,-13,2,10);}
            if(!preview && symbol==4){Quad(w+3,-5,9,2);Quad(w+3,-11,2,6);Quad(w+3,-11,7,2);}
        }
    }
}
