using UnityEngine;
using UnityEngine.UI;

namespace RogueTactics.Presentation
{
    public sealed partial class MobileG1View
    {
        // Layout is presentation only: one current-floor marker, no invented
        // floor number, total, locked floor, reward, or traversal state.
        private void BuildReferenceTypography()
        {
            var journey=SafeRoot.Find("Observed journey").GetComponent<RectTransform>();
            journey.Find("Illustrated frame").gameObject.SetActive(false);
            journey.Find("Journey emblem").gameObject.SetActive(false);
            discoveredStatus.gameObject.SetActive(false);
            var state=Rect(journey,"Phase backing",16,-40,80,24);
            Frame(state,new Color(.035f,.075f,.13f,.86f),new Color(.55f,.68f,.80f,.5f),10);
            state.SetAsFirstSibling();
            var heading=Rect(journey,"Floor heading",56,0,40,24);
            Frame(heading,new Color(.035f,.075f,.13f,.86f),new Color(.55f,.68f,.80f,.5f),10);
            Label(heading,"Meaning","階層",0,0,40,24,11,TextAnchor.MiddleCenter);
            var axis=Rect(journey,"Floor axis",75,30,2,92).gameObject.AddComponent<Image>();
            axis.color=new Color(.75f,.79f,.83f,.8f);axis.raycastTarget=false;
            var current=Rect(journey,"Current floor marker",56,40,40,40);
            Frame(current,new Color(.06f,.11f,.17f,.95f),Gold,20);
            Glyph(current,"Current floor emblem",HudSymbol.Compass,7,7,26,Gold);
            var caption=Rect(journey,"Current floor caption",0,48,52,24);
            Frame(caption,new Color(.035f,.075f,.13f,.90f),Gold,8);
            Label(caption,"Meaning","現在階",0,0,52,24,11,TextAnchor.MiddleCenter).color=Gold;
        }
        private static void ReferenceText(Text text,float x,float y,float width,float height,int size,TextAnchor alignment)
        {
            text.rectTransform.anchoredPosition=new Vector2(x,-y);
            text.rectTransform.sizeDelta=new Vector2(width,height);
            text.fontSize=size;text.alignment=alignment;text.lineSpacing=1;
            text.resizeTextForBestFit=false;
        }
        private void LayoutReferenceTypography(float w,float h)
        {
            if(referenceFooter==null || DeveloperMode)return;
            var plate=SafeRoot.Find("Title plate").GetComponent<RectTransform>();
            plate.sizeDelta=new Vector2(Mathf.Min(w-86,226),48);
            ReferenceText(SafeRoot.Find("Title").GetComponent<Text>(),58,14,176,36,20,TextAnchor.MiddleLeft);
            plate.Find("Ruin emblem").GetComponent<RectTransform>().anchoredPosition=new Vector2(8,-8);
            var instruction=SafeRoot.Find("Context instruction").GetComponent<RectTransform>();
            instruction.anchoredPosition=new Vector2(12,-60);instruction.sizeDelta=new Vector2(226,24);
            ReferenceText(SafeRoot.Find("Boundary").GetComponent<Text>(),20,60,210,24,11,TextAnchor.MiddleLeft);
            var journey=SafeRoot.Find("Observed journey").GetComponent<RectTransform>();
            journey.anchoredPosition=new Vector2(w-108,-100);journey.sizeDelta=new Vector2(96,136);
            ReferenceText(journeyStatus,16,-40,80,24,11,TextAnchor.MiddleCenter);
            roster.anchoredPosition=new Vector2(9,-(h-212));roster.sizeDelta=new Vector2(w-18,144);
            var width=(w-36)/4;
            for(var i=0;i<4;i++)
            {
                var id=CardOrder[i];var card=cardValues[id].transform.parent.GetComponent<RectTransform>();
                card.sizeDelta=new Vector2(width,144);
                var art=card.Find("Approved portrait").GetComponent<RectTransform>();
                art.anchoredPosition=new Vector2(5,-6);art.sizeDelta=new Vector2(width-10,60);
                ReferenceText(card.Find("Name").GetComponent<Text>(),4,66,width-8,20,13,TextAnchor.MiddleCenter);
                ReferenceText(card.Find("Role meaning").GetComponent<Text>(),4,86,width-8,20,11,TextAnchor.MiddleCenter);
                var track=card.Find("Health track").GetComponent<RectTransform>();
                track.anchoredPosition=new Vector2(8,-110);track.sizeDelta=new Vector2(width-16,4);
                cardHealth[id].rectTransform.anchoredPosition=new Vector2(8,-110);
                var maxHp=RogueTactics.Core.G1.Fixture.Baseline().Find(id).MaxHp;
                cardHealth[id].rectTransform.sizeDelta=new Vector2((width-16)*(hp.TryGetValue(id,out var displayedHp)?displayedHp:maxHp)/maxHp,4);
                ReferenceText(cardValues[id],4,118,width-8,20,11,TextAnchor.MiddleCenter);
            }
            eventCaption.alignment=TextAnchor.MiddleLeft;
            ReferenceText(headline,18,8,w-56,36,20,TextAnchor.MiddleLeft);
            policyHeading.alignment=TextAnchor.MiddleLeft;
            if(policyEditing)setup.anchoredPosition=new Vector2(0,320);
            foreach(var id in new[]{"Close","ClosePolicy","RecordUp","RecordDown"})
            {
                var button=buttons[id].GetComponent<RectTransform>();
                ReferenceText(button.Find("Label").GetComponent<Text>(),4,2,button.rect.width-8,button.rect.height-4,13,TextAnchor.MiddleCenter);
            }
            if(floorMini!=null)
            {
                var map=floorMini.transform.parent.GetComponent<RectTransform>();
                map.anchoredPosition=new Vector2(12,-96);map.sizeDelta=new Vector2(124,122);
                ReferenceText(map.Find("Known map caption").GetComponent<Text>(),8,2,108,22,11,TextAnchor.MiddleLeft);
                if(discoveredStatus.transform.parent!=map)discoveredStatus.transform.SetParent(map,false);
                discoveredStatus.gameObject.SetActive(true);
                ReferenceText(discoveredStatus,8,24,108,20,11,TextAnchor.MiddleLeft);
                floorMini.rectTransform.anchoredPosition=new Vector2(8,-44);floorMini.rectTransform.sizeDelta=new Vector2(108,70);
                var search=buttons["Map"].GetComponent<RectTransform>();search.anchoredPosition=new Vector2(76,-74);
                orderStrip.anchoredPosition=(Vector2)Field.InverseTransformPoint(SafeRoot.TransformPoint(new Vector3(12,-232,0)));
                PositionReferenceAction("Explore",12,h-268,w-24,"この先の遺跡を探索する");
            }
        }
    }
}
