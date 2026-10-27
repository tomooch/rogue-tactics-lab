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
            var phaseRibbon=state.gameObject.AddComponent<HudRibbonGraphic>();phaseRibbon.Mirrored=true;phaseRibbon.raycastTarget=false;
            state.SetAsFirstSibling();
            // Floor column reads like the reference rail: plain lettering over the sky, a dotted axis and one gold marker.
            var heading=Rect(journey,"Floor heading",56,0,40,24);
            HudOutlined(Label(heading,"Meaning","階層",0,0,40,24,11,TextAnchor.MiddleCenter),new Color(.92f,.95f,1));
            var axis=Rect(journey,"Floor axis",72,24,8,106).gameObject.AddComponent<HudAxisGraphic>();
            axis.color=new Color(.90f,.93f,.97f,.85f);axis.raycastTarget=false;
            var current=Rect(journey,"Current floor marker",56,40,40,40);
            Frame(current,Color.clear,new Color(Gold.r,Gold.g,Gold.b,.55f),20);
            Glyph(current,"Current floor emblem",HudSymbol.Descend,8,8,24,Gold,true,true);
            var caption=Rect(journey,"Current floor caption",0,48,52,24);
            HudOutlined(Label(caption,"Meaning","現在階",0,0,52,24,12,TextAnchor.MiddleRight),Gold);
        }
        private static void HudOutlined(Text label,Color color)
        {
            label.color=color;label.fontStyle=FontStyle.Bold;
            var outline=label.gameObject.AddComponent<Outline>();outline.effectColor=new Color(.02f,.04f,.09f,.92f);outline.effectDistance=new Vector2(1.2f,-1.2f);
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
            plate.Find("Ruin emblem").GetComponent<RectTransform>().anchoredPosition=new Vector2(10,-10);
            var instruction=SafeRoot.Find("Context instruction").GetComponent<RectTransform>();
            instruction.anchoredPosition=new Vector2(12,-60);instruction.sizeDelta=new Vector2(226,24);
            ReferenceText(SafeRoot.Find("Boundary").GetComponent<Text>(),20,60,210,24,11,TextAnchor.MiddleLeft);
            var journey=SafeRoot.Find("Observed journey").GetComponent<RectTransform>();
            journey.anchoredPosition=new Vector2(w-108,-100);journey.sizeDelta=new Vector2(96,136);
            ReferenceText(journeyStatus,16,-40,72,24,11,TextAnchor.MiddleRight);journeyStatus.fontStyle=FontStyle.Bold;
            roster.anchoredPosition=new Vector2(9,-(h-212));roster.sizeDelta=new Vector2(w-18,144);
            var width=(w-36)/4;
            for(var i=0;i<4;i++)
            {
                var id=CardOrder[i];var card=cardValues[id].transform.parent.GetComponent<RectTransform>();
                card.sizeDelta=new Vector2(width,144);
                // Reference card: tall portrait with the role gem in its corner and the name on its lower edge,
                // then actual HP, a thick bar and the role (or Momo's policy) underneath.
                var art=card.Find("Approved portrait").GetComponent<RectTransform>();
                art.anchoredPosition=new Vector2(4,-4);art.sizeDelta=new Vector2(width-8,94);
                // Crop approved pixels to the actual display aspect; never stretch a face.
                var portrait=art.GetComponent<RawImage>();var uvHeight=.43f;
                var uvWidth=art.rect.width/art.rect.height*portrait.texture.height/portrait.texture.width*uvHeight;
                var faceCenter=id=="C"?.55f:.51f;
                portrait.uvRect=new Rect(faceCenter-uvWidth/2,.47f,uvWidth,uvHeight);
                var fade=card.Find("Portrait name fade").GetComponent<RectTransform>();fade.anchoredPosition=new Vector2(4,-68);fade.sizeDelta=new Vector2(width-8,30);
                var name=card.Find("Name").GetComponent<Text>();
                ReferenceText(name,6,76,width-13,20,id=="B"?12:13,TextAnchor.MiddleRight);name.fontStyle=FontStyle.Bold;
                var unavailable=dead.Contains(id);
                var role=card.Find("Core role").GetComponent<RectTransform>();role.gameObject.SetActive(!unavailable);
                role.anchoredPosition=new Vector2(width-27,-9);role.sizeDelta=new Vector2(18,18);
                var roleSymbol=role.Find("Role symbol").GetComponent<RectTransform>();roleSymbol.anchoredPosition=Vector2.zero;roleSymbol.sizeDelta=new Vector2(18,18);
                ReferenceText(cardValues[id],7,99,width-14,15,11,TextAnchor.MiddleLeft);cardValues[id].fontStyle=FontStyle.Bold;
                var track=card.Find("Health track").GetComponent<RectTransform>();
                track.anchoredPosition=new Vector2(7,-115);track.sizeDelta=new Vector2(width-14,7);
                var maxHp=RogueTactics.Core.G1.Fixture.Baseline().Find(id).MaxHp;
                var shownHp=hp.TryGetValue(id,out var displayedHp)?displayedHp:maxHp;
                cardHealth[id].rectTransform.anchoredPosition=new Vector2(7,-115);
                cardHealth[id].rectTransform.sizeDelta=new Vector2((width-14)*shownHp/maxHp,7);
                // Low actual HP reads warm; the bar still only mirrors Core health.
                cardHealth[id].color=shownHp*100<=maxHp*35?new Color(1,.45f,.36f):new Color(.34f,.94f,.49f);
                ReferenceText(card.Find("Role meaning").GetComponent<Text>(),4,123,id=="B" && !unavailable?width-20:width-8,19,11,TextAnchor.MiddleCenter);
                if(id=="B")
                {
                    var policyRow=card.Find("Policy row").GetComponent<RectTransform>();policyRow.anchoredPosition=new Vector2(4,-123);policyRow.sizeDelta=new Vector2(width-8,19);
                    card.Find("Policy disclosure").GetComponent<RectTransform>().anchoredPosition=new Vector2(width-17,-127);
                }
            }
            eventCaption.alignment=TextAnchor.MiddleLeft;
            ReferenceText(headline,46,8,w-84,36,20,TextAnchor.MiddleLeft);
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
                // Initiative sits in the sky gap beside the map, clear of the board and the floor column.
                orderStrip.anchoredPosition=(Vector2)Field.InverseTransformPoint(SafeRoot.TransformPoint(new Vector3(142,-96,0)));
                PositionReferenceAction("Explore",12,h-268,w-24,"この先の遺跡を探索する");
            }
        }
    }
}
