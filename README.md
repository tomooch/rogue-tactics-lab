# Rogue Tactics Lab

最終製品は **iOS / Android 向け native game**。開発方向は **Unity 6.3 LTS (6000.3.x) / C# / URP / portrait mobile**。

Current implementation contract は [GitHub Issue #1](https://github.com/tomooch/rogue-tactics-lab/issues/1)。設計概要は [DESIGN.md](DESIGN.md)、実装・検証の現在地は [VERIFICATION.md](VERIFICATION.md) を参照。

## 現在地

- accepted main の実装は historical browser v0 のみ。ソースと当時の記録は [legacy/web-v0/](legacy/web-v0/README.md) に保存している。
- Unity native implementation はまだ存在しない。将来の配置先は `unity/`。
- [Godot PR #2](https://github.com/tomooch/rogue-tactics-lab/pull/2) は closed / unmerged。流用しない。
- [Unity bootstrap PR #3](https://github.com/tomooch/rogue-tactics-lab/pull/3) は Draft、environment audit only / blocked。Unity Hub / 6000.3.x Editor 未検出のため、プロジェクト生成は未実施。
- gameplay controls は未決。探索操作・プレイヤーの立場、委譲の解決範囲、意図の表示手数を実装で確定しない。

## Historical browser v0

[公開 Web v0](https://tomooch.github.io/rogue-tactics-lab/) は従来の戦闘視認性実験。native 製品や Unity 実装の証拠ではない。

repo root をコマンド入口として維持する。Node.js と Python 3 が必要。追加パッケージ不要。

```sh
npm test
npm run check
npm run build
npm start
```

`npm start` は Web v0 を http://127.0.0.1:4173 で配信する。`npm run build` は `legacy/web-v0/_site/` に runtime 5 files、`revision.json`、`.nojekyll` を生成する。生成物は Git 管理外。

Pages workflow は root でテストと build を実行し、この生成物を公開する。公開内容は Web v0 のまま。`revision.json` は公開 commit SHA と、その SHA の `legacy/web-v0/VERIFICATION.md` を指す。main push は Pages 公開を伴うため、明示的な承認なしに merge / main push しない。
