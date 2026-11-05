# 4人のカードを人物主体へ — 2026-10-26

Humanの最新指示「フィールド的なところは力入れなくて良い。まだ。」に従い、追加field造形を止め、同Draft PR21 baseline befbd8623d127378ebd8ef367cd6f476579a794eから4人のカードUIだけを改善する。144pxカード/48px主操作面/既存hit領域と操作flowを維持。承認4RGBA/顔/目/衣装は直サンプルし、描き直さない。Core/field/camera/scene/能力/eventを変更しない。Codexが制作/実行/採否、Astraは実画像による助言。

## 実画像の問題と反復

Astraは原画とbefore390×844を実閲覧し、顔領域が小さく、下半分の黒い情報欄と分断されていることを最大の課題とした。rootも承認4枚を実見し、表示矩形とUVの縦横比が一致していなかった点を確認した。

**A:** 同じカード内で人物60→88px、表示比率に一致するUV crop、名前fade、6px HPbar、モモだけdisclosureを制作。filtered Edit0/Play1/1、9fresh390×844、232file source guard一致。rootがoverview/policy、Astraがbefore/Aoverview/Apolicyの3実画像を比較。目/口と4人の識別は明瞭になり、明らかな歪みなし。一方、名前が衣装の明暗に埋もれ、全員の額縁アイコンがボタンに見えるため完成として保留。

**B:** 名前の背景を短いfadeから濃紺の不透明面へつなぎ、役割文字の明度を上げる。非操作の役割記号は枠を取り除き文字行へ移動。モモだけ方針行の局所背景とdisclosureを持ち、既存Buttonのfeedbackをこの面へ向ける。開始後は操作面/記号を隠し読み取り専用を表現する。既存実UI検査へportrait比率と名前/役割/HPの横方向fitを追加。

Bのfiltered Edit0/Play1/1・9fresh390×844/source232file不変を確認。rootはB初期/方針/勝利、AstraはA初期/B初期/B勝利の3実画像を閲覧。名前の背景依存を解消し、顔/目/口を保持、役割とHPが独立して読める。モモだけ準備中に方針枠/開示記号があり、勝利時は消える。衣装の見える範囲が減るtradeoffは、顔/名前/状態の優先順位として局所許容。root/AstraともBの局所保持を支持する。原画水準/全体R2完成/Human判断へ読み替えない。

## 全検査と残る失敗

全回帰job `2ce12b63-6782-4312-a54d-4ad4476900e7` はUnity6000.3.25f1 Edit21/21・Play12/13・探索4/4。元の `JapaneseSafeAreaBattlePreservesFrozenEventsAndBothResults` がstep0 X/Cの全sprite矩形重複でFAIL。baselineの同じ矩形/失敗を維持し、assertion/検査を緩めない。両full固定hash・表示済event/HP/死亡・既存方針/任意メモ/停止再開/記録page/比較/retry/探索を対象試験で確認。63fresh390×844、rootは最終初期/勝利も実閲覧。新しいportrait比率/横fitの検査を含めUI検査は通過。

検査中232files不変。前native凍結232filesとの変更はUI2本と検査1本のみで、Art/Core/scenesを維持。

## nativeで見つけた戦闘不能の切れ → 修復中

fresh Simulator job `e0dee8b8-67d7-4e1b-89eb-ad36130066db` はexport/Xcodebuild各0、生成4設定復元/unexpected0。新binary `292fc822a3fd03a79eda163c991fcfba64788c170219042a1c2bb5ff59ac4fd2` を専用QA iOS27.0へinstall/launch。実CUAでシロの顔は方針を開かず、モモの顔（小さな方針行の外側）から方針を開けること、Attack選択でカード更新、一回Startの自動戦闘、開始後の方針枠/開示記号消失を確認した。押下feedbackの色を別stillで測定したとはしない。

ただしシロ0HPの実停止画面では、役割2文字用の32px欄が「戦闘不能」を切り、反撃記号も残った。既存UI検査の通過を完成にせず、このnativeのUI採用は保留。画像3枚/logをprivate保存し、戦闘は途中停止のままで最終hashの観測へ転用しない。

修復Cは戦闘不能時だけ生存時の役割記号を隠し、status行をカード幅へ広げる。既存Attack終端検査に完全なstatus文字/実preferredWidth/役割記号非表示と実描画を追加。full job `36450800-1ab4-4463-b97e-d5e3dd0a8e22` はEdit21/21・Play12/13・探索4/4。失敗はbaselineと同じ全sprite矩形X/C重複のみ、元検査緩和なし。新しい死亡表示の回帰と両fullhash/source232file保護を維持し64fresh390×844を保存。rootは初期/Attack勝利画像を実閲覧し、シロの「戦闘不能」全文/0HP/役割記号非表示と、3人生還を確認した。AstraもC初期/Attack勝利の2実画像を閲覧し、全文/0HP/記号消失・人物識別と生存3人の名前/役割/HP非重複を確認、観測2状態の表示へ追加修正不要と助言。実tapやHuman品質の代理判断ではない。修復版fresh native job `c6fb1ed2-e995-4a77-9147-acfc26f603a4` とpushは未完。

## 修復版の実Simulator再確認

job `c6fb1ed2-e995-4a77-9147-acfc26f603a4` はexport/Xcodebuild各0、生成4設定復元/unexpected0。新binary SHA256 `d3f984988f2439388d6489092fa2f1f8d430cb78fed15c2719e1241dd944780b` をQA iOS27.0へinstall/launch。実CUAでモモの顔から方針を開く→Attack選択/カード更新→一回Startの自動戦闘→シロの「戦闘不能」全文/0HP/反撃記号消失と3人生存→記録→再試行で6体初期HP/顔の明るさ/役割とモモ操作枠の復帰→再び顔から方針を開けることを確認。新native Attack fullhash一致、runtime logにException行なし。初期/死亡表示/再試行/方針の4stillとlogをprivate保存した。

pauseを押そうとした時点では戦闘が終了しボタンがExploreへ変わっており、探索への復帰を観測した。これを最終nativeのpause成功とはしない。最終nativeのProtect/IME/メモ/page/全8室/pause再検証は未実施で、Editorの検査範囲と分ける。MCP stillは368×800最適化JPEG、Editorの390×844PNGと同寸法とはしない。全検査232files/native232filesと現在sourceをSHA照合し不一致0。押下色変化を独立に測定したとはしない。

最終採用はカードUIと死亡表示の局所修復。衣装の露出減少、端で切れる帽子/耳の一部、原画水準の全画面統合品質と既存人物矩形FAILは残る。field追加制作は実施せず、Art/Core/scenes/privatePNG不変。新TestFlight archive/upload/ASC配布確認/物理iPhone/Human試遊なし。local0.1.0(5)は旧Apple build5更新ではない。

private媒体はowner-local `_userwork/hq-evidence/2026-10-26-companion-cards/` に保持。A/B/最終64PNG、最初nativeの欠陥3still/log、修復native4still/logを保存。公開Gitへ原画/skin/画像/動画を入れず、WebGPT別sessionへの画素共有は未成立。公開text/hash/XMLで観測結果をレビューできる。R2全体V0 FAIL、Human品質と実iPhoneの合格を先取りしない。無料/外注なし/private新外部送信なし/mergeなし/Readyなし。

実行source commit: `b53d9d2232dce2d3ac36dd3f4f90db1b85287eda`。凍結sourceのtracked内容もcommitと不一致0。後続commitは検証記録のみ。
