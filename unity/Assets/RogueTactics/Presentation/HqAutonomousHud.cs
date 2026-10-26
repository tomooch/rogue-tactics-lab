using System.Linq;
using RogueTactics.Core.G1;
using UnityEngine;
using UnityEngine.UI;

namespace RogueTactics.Presentation
{
    public sealed partial class MobileG1View
    {
        private Text optionalMemoCaption;
        private RectTransform terminalTransportReserve;
        private Text terminalTransportResult;
        private bool AutonomousProduct=>HqWorld && ExplorationEnabled && !DeveloperMode;
        private bool NormalJournal=>modalObject.activeSelf && !showingResults && !referenceSettingsOpen &&
            (expandedKnownMap==null || !expandedKnownMap.gameObject.activeSelf);
        private bool RecordMemoVisible=>AutonomousProduct && NormalJournal && !Session.Started;

        private void BuildAutonomousHud()
        {
            if(!ExplorationEnabled)return;
            terminalTransportReserve=Rect(SafeRoot,"Terminal transport reserve",0,0,104,48);
            terminalTransportReserve.gameObject.AddComponent<Image>().color=Color.clear;
            terminalTransportResult=Label(terminalTransportReserve,"Terminal result","",0,2,104,44,12,TextAnchor.MiddleCenter);
            var resultOutline=terminalTransportResult.gameObject.AddComponent<Outline>();
            resultOutline.effectColor=new Color(.025f,.045f,.08f,.95f);resultOutline.effectDistance=new Vector2(1,-1);
            terminalTransportReserve.gameObject.SetActive(false);
            MakeButton(modal,"RecordCompare","戦いの記録",218,6,134,48,()=>OpenDetails(true),13);
            MakeButton(modal,"RecordRetry","もう一度試す",190,570,162,48,()=>{CloseReferenceMemo();modalObject.SetActive(false);SyncReferenceModal();ResetBaseline();},13);
            StyleReferenceButton("RecordCompare",HudSymbol.Compare,Gold);
            StyleReferenceButton("RecordRetry",HudSymbol.Reset,new Color(.62f,.74f,.84f));
            optionalMemoCaption=Label(modal,"Optional memo","メモ（任意）",18,438,334,22,12,TextAnchor.MiddleLeft);
        }
        private void CloseReferenceMemo()
        {
            if(!AutonomousProduct)return;
            Prediction.DeactivateInputField();
            if(Prediction.transform.parent!=setup)Prediction.transform.SetParent(setup,false);
            Prediction.gameObject.SetActive(false);
        }
        private bool ApplyAutonomousKeyboard(bool editing)
        {
            if(!RecordMemoVisible)return false;
            var input=Prediction.GetComponent<RectTransform>();
            input.anchoredPosition=new Vector2(18,-(modal.rect.height-166));
            if(editing)input.position=SafeRoot.TransformPoint(new Vector3(18,-100,0));
            return true;
        }
        private void LayoutAutonomousJournal()
        {
            if(!AutonomousProduct || optionalMemoCaption==null)return;
            var record=modalObject.activeSelf && !referenceSettingsOpen &&
                (expandedKnownMap==null || !expandedKnownMap.gameObject.activeSelf);
            var canReview=Session.Trials.Count>0;
            buttons["RecordCompare"].gameObject.SetActive(record && !showingResults && canReview);
            buttons["RecordRetry"].gameObject.SetActive(record && canReview);
            buttons["RecordCompare"].interactable=canReview && !Busy;
            buttons["RecordRetry"].interactable=canReview && !Busy;
            var w=SafeRoot.rect.width;var height=modal.rect.height;
            if(record && !showingResults)
            {
                height=Mathf.Clamp(54+Mathf.Min(280,modalText.preferredHeight)+(RecordMemoVisible?204:126),RecordMemoVisible?320:260,SafeRoot.rect.height-90);
                modal.sizeDelta=new Vector2(w-20,height);modal.anchoredPosition=new Vector2(10,-(SafeRoot.rect.height-height)/2);
                detailsViewport.sizeDelta=new Vector2(w-56,height-(RecordMemoVisible?258:180));
                ResizeButton("Close",18,height-58,w-56);
                ReferenceText(headline,18,8,canReview?w-192:w-56,36,20,TextAnchor.MiddleLeft);
            }
            if(record && showingResults)detailsViewport.sizeDelta=new Vector2(w-56,height-180);
            if(record && !showingResults && canReview)
            {
                ReferenceText(headline,18,8,w-192,36,20,TextAnchor.MiddleLeft);
                PositionReferenceAction("RecordCompare",w-172,6,134,"戦いの記録");
            }
            if(record && canReview)
            {
                var width=(w-64)/2;
                ResizeButton("Close",18,height-58,width);
                PositionReferenceAction("RecordRetry",26+width,height-58,width,"もう一度試す");
            }
            optionalMemoCaption.gameObject.SetActive(RecordMemoVisible);
            if(RecordMemoVisible)
            {
                if(Prediction.transform.parent!=modal)Prediction.transform.SetParent(modal,false);
                Prediction.gameObject.SetActive(true);
                Prediction.placeholder.GetComponent<Text>().text="気になったことをメモ（任意）";
                Prediction.GetComponent<RectTransform>().sizeDelta=new Vector2(w-56,44);
                Prediction.textComponent.rectTransform.sizeDelta=new Vector2(w-76,40);
                Prediction.placeholder.GetComponent<RectTransform>().sizeDelta=new Vector2(w-76,40);
                ReferenceText(optionalMemoCaption,18,height-190,w-56,22,12,TextAnchor.MiddleLeft);
                detailsViewport.sizeDelta=new Vector2(w-56,height-258);
                ApplyAutonomousKeyboard(Prediction.isFocused && TouchScreenKeyboard.isSupported);
            }
            else
            {
                if(Prediction.transform.parent==modal)CloseReferenceMemo();
                Prediction.gameObject.SetActive(false);
            }
            if(record)
            {
                var overflow=modalText.preferredHeight>detailsViewport.rect.height+1;
                buttons["RecordUp"].gameObject.SetActive(overflow);
                buttons["RecordDown"].gameObject.SetActive(overflow);
                ResizeButton("RecordUp",18,height-112,52);ResizeButton("RecordDown",76,height-112,52);
                if(!showingResults)PositionReferenceAction("PresentationSettings",overflow?134:18,height-112,overflow?w-172:w-56,"表示設定");
            }
        }
        private void RefreshAutonomousHud()
        {
            if(terminalTransportReserve!=null)
                terminalTransportReserve.gameObject.SetActive(AutonomousProduct && terminalSeen && !IsExploring && !modalObject.activeSelf && Session.Replay.State.Outcome==Outcome.Victory);
            if(!AutonomousProduct)return;
            buttons["PolicyShortcut"].gameObject.SetActive(false);
            cardValues["B"].transform.parent.Find("Name").GetComponent<Text>().text=Session.Started?"モモ":"モモ・方針";
            buttons["Policy"].interactable=!Session.Started;
            if(!RecordMemoVisible)Prediction.gameObject.SetActive(false);
            if(!Session.Started)
            {
                SafeRoot.Find("Boundary").GetComponent<Text>().text="仲間は自動で戦う・方針はモモから";
                PositionReferenceAction("Start",12,0,SafeRoot.rect.width-24,"仲間に任せて戦う");
                CenterPrimaryAction("Start");
                if(policyEditing){setup.sizeDelta=new Vector2(SafeRoot.rect.width,104);setup.anchoredPosition=new Vector2(0,270);}
            }
            if(terminalSeen)
            {
                buttons["Compare"].gameObject.SetActive(false);buttons["Reset"].gameObject.SetActive(false);
                if(!IsExploring)
                {
                    var alive=CardOrder.Count(id=>!dead.Contains(id));
                    SafeRoot.Find("Boundary").GetComponent<Text>().text=(Session.Replay.State.Outcome==Outcome.Victory?"勝利":"戦闘終了")+"・"+alive+"人が生還";
                    eventCaption.gameObject.SetActive(false);eventBackdrop.gameObject.SetActive(false);
                    if(buttons.ContainsKey("Explore"))
                    {
                        var w=SafeRoot.rect.width;
                        PositionReferenceAction("Explore",12,SafeRoot.rect.height-58,w-136,"遺跡を探索する");
                        CenterPrimaryAction("Explore");
                        terminalTransportReserve.anchoredPosition=new Vector2(w-116,-(SafeRoot.rect.height-58));
                        terminalTransportResult.text=(Session.Replay.State.Outcome==Outcome.Victory?"勝利":"戦闘終了")+"\n"+alive+"人が生還";
                    }
                }
                else
                {
                    var moving=travelRoutine!=null;var w=SafeRoot.rect.width;
                    eventCaption.alignment=TextAnchor.MiddleLeft;
                    eventCaption.rectTransform.anchoredPosition=new Vector2(18,-2);eventCaption.rectTransform.sizeDelta=new Vector2(moving?w-152:w-36,44);
                    eventBackdrop.rectTransform.anchoredPosition=new Vector2(12,0);eventBackdrop.rectTransform.sizeDelta=new Vector2(moving?w-140:w-24,48);
                }
            }
            LayoutAutonomousJournal();
        }
        private void DescribeAutonomousRecord(bool trials)
        {
            if(!AutonomousProduct)return;
            if(trials)
            {
                headline.text="戦いの記録";
                modalText.text=string.Join("\n\n",new[]{Preference.ProtectAnchor,Preference.DirectAttack}.Where(p=>Session.Trials.ContainsKey(p)).Select(p=>{
                    var t=Session.Trials[p];return (p==Preference.ProtectAnchor?"モモがシロを守る":"モモが敵を攻める")+"\n"+
                        (t.Outcome==Outcome.Victory?"勝利":"戦闘終了")+"・"+t.Alive+"人が生還\nHP "+t.Hp.Replace("A:","シロ:").Replace("B:","モモ:").Replace("C:","ガル:").Replace("D:","ミャル:")+
                        (string.IsNullOrWhiteSpace(t.Prediction)?"":"\nメモ："+t.Prediction);
                }));
            }
            else
            {
                headline.text="旅の記録";
                modalText.text="モモの方針："+(Session.Preference==Preference.ProtectAnchor?"シロを守る":"敵を攻める")+
                    (!Session.Started || string.IsNullOrWhiteSpace(Prediction.text)?"":"\nメモ："+Prediction.text)+
                    (captions.Count==0?"\n\n仲間の動きや、気になった出来事をここで見返せます。":"\n\n見た出来事\n"+string.Join("\n",captions.Skip(Mathf.Max(0,captions.Count-6))));
            }
        }
    }
}
