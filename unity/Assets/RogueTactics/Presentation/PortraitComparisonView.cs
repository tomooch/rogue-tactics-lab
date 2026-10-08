using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace RogueTactics.Presentation
{
    // Author-written pre-execution UX fixture. No Core state, planning, or execution.
    public sealed class PortraitComparisonView : MonoBehaviour
    {
        public static IReadOnlyList<string> Actions { get; } = System.Array.AsReadOnly(new[] { "Cのそばへ移動して守る", "左の敵に向かう", "後ろへ退く", "Cを回復する" });
        public static IReadOnlyList<string> Reasons { get; } = System.Array.AsReadOnly(new[] { "CのHPが少ない。そばで守りたい。", "左の敵を引き受け、進む道をつくりたい。", "傷が深い。後方で立て直したい。", "傷ついたCを支えたい。" });
        static readonly string[] Roles = { "前衛", "守護", "遊撃", "支援" };
        static readonly Color Ink = new Color32(226, 235, 226, 255);
        static readonly Color Muted = new Color32(168, 187, 178, 255);
        static readonly Color Accent = new Color32(172, 216, 177, 255);
        readonly Text[] reasons = new Text[4];
        readonly Image[] cards = new Image[4];
        readonly Button[] selectors = new Button[4];
        readonly List<Material> materials = new List<Material>();
        RenderTexture fieldTarget;
        Font font;
        Canvas canvas;
        GameObject dialog;
        Button variantA, variantB;
        public bool ShowAllReasons { get; private set; } = true;
        public int SelectedCompanion { get; private set; }
        public bool ExplanationOpen => dialog != null && dialog.activeSelf;
        public Button VariantAButton => variantA;
        public Button VariantBButton => variantB;
        public Button CompanionButton(int index) => selectors[index];
        public Button CorrectionButton { get; private set; }
        public Button EntrustButton { get; private set; }
        public Button CloseButton { get; private set; }
        public bool ReasonVisible(int index) => reasons[index].gameObject.activeSelf;

        void Awake()
        {
            font = Font.CreateDynamicFontFromOSFont(new[] { "Hiragino Sans", "Yu Gothic", "Noto Sans CJK JP", "Arial" }, 16);
            var camera = new GameObject("UX Camera").AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.transform.position = new Vector3(0, 0, -10);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(20, 33, 31, 255);
            camera.orthographic = true;
            camera.orthographicSize = 5;
            camera.cullingMask = 1 << 5; // UI only; fictional geometry uses Default.
            canvas = new GameObject("Portrait UX", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>();
            canvas.gameObject.layer = 5;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1;
            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(390, 844);
            scaler.matchWidthOrHeight = 0;
            new GameObject("UX Event System", typeof(EventSystem), typeof(InputSystemUIInputModule));
            Label(canvas.transform, "MockLabel", "UX比較モック  /  行動前の架空の戦況", 16, 13, 358, 28, 14, Muted);
            variantA = Button(canvas.transform, "VariantA", "A  全員の理由", 16, 50, 175, 44, () => SetVariant(true));
            variantB = Button(canvas.transform, "VariantB", "B  選んだ仲間の理由", 199, 50, 175, 44, () => SetVariant(false));
            Label(canvas.transform, "PlayerGoal", "あなたが掲げた方針：仲間を守って進む", 16, 103, 358, 27, 16, Ink);
            var strategist = Panel(canvas.transform, "StrategistE_OffBoard", 16, 137, 358, 125, new Color32(42, 59, 54, 255));
            Label(strategist.transform, "ProposalHeading", "E  軍師の作戦提案", 12, 7, 334, 23, 17, Accent);
            Label(strategist.transform, "Proposal", "A 前方を守る    B 左の敵に向かう\nC 後ろへ退く    D Cを回復する", 12, 33, 334, 40, 15, Ink);
            Label(strategist.transform, "StrategistReason", "理由：左右の敵に備え、Cを後方で支える。", 12, 77, 334, 20, 13, Muted);
            Label(strategist.transform, "NeutralDifference", "Aの意図は、前方よりCのそばを守ること。", 12, 100, 334, 20, 13, Ink);
            Label(canvas.transform, "FieldLabel", "同じ戦場  ·  Cは負傷中", 16, 267, 358, 22, 13, Muted);
            BuildDiorama();
            Label(canvas.transform, "IntentHeading", "仲間の意図  /  タップで選択", 16, 483, 358, 25, 15, Muted);
            for (var i = 0; i < 4; i++)
            {
                var index = i;
                var button = Button(canvas.transform, "Companion" + "ABCD"[i], "", 16, 513 + i * 57, 358, 53, () => SelectCompanion(index));
                selectors[i] = button;
                cards[i] = button.GetComponent<Image>();
                Label(button.transform, "Action" + i, "ABCD"[i] + "  " + Roles[i] + "  /  " + Actions[i], 10, 5, 338, 23, 16, Ink);
                reasons[i] = Label(button.transform, "Reason" + i, "理由：" + Reasons[i], 10, 29, 338, 20, 13, Muted);
            }
            Label(canvas.transform, "CorrectionBudget", "意図の修正  残り1回 / この戦況（表示例）", 16, 746, 358, 23, 14, Muted);
            EntrustButton = Button(canvas.transform, "Entrust", "仲間に託す", 16, 778, 175, 48, () => OpenExplanation(false));
            CorrectionButton = Button(canvas.transform, "Correction", "選んだ仲間を修正", 199, 778, 175, 48, () => OpenExplanation(true));
            BuildDialog();
            Refresh();
        }

        void OnDestroy()
        {
            foreach (var material in materials) Destroy(material);
            if (fieldTarget != null) { fieldTarget.Release(); Destroy(fieldTarget); }
            if (font != null) Destroy(font);
        }
        public void SetVariant(bool all) { ShowAllReasons = all; Refresh(); }
        public void SelectCompanion(int index)
        {
            if (index < 0 || index >= 4) throw new System.ArgumentOutOfRangeException(nameof(index));
            SelectedCompanion = index; Refresh();
        }
        void Refresh()
        {
            for (var i = 0; i < 4; i++)
            {
                reasons[i].gameObject.SetActive(ShowAllReasons || SelectedCompanion == i);
                cards[i].color = i == SelectedCompanion ? new Color32(54, 79, 65, 255) : new Color32(33, 49, 44, 255);
            }
            variantA.GetComponent<Image>().color = ShowAllReasons ? new Color32(54, 79, 65, 255) : new Color32(33, 49, 44, 255);
            variantB.GetComponent<Image>().color = !ShowAllReasons ? new Color32(54, 79, 65, 255) : new Color32(33, 49, 44, 255);
        }
        void BuildDialog()
        {
            dialog = Panel(canvas.transform, "Explanation", 0, 0, 390, 844, new Color(0.04f, 0.09f, 0.08f, 0.96f)).gameObject;
            var card = Panel(dialog.transform, "ExplanationCard", 24, 280, 342, 244, new Color32(42, 59, 54, 255));
            Label(card.transform, "ExplanationTitle", "", 18, 20, 306, 32, 20, Ink);
            Label(card.transform, "ExplanationBody", "", 18, 67, 306, 100, 16, Muted);
            CloseButton = Button(card.transform, "Close", "比較画面に戻る", 18, 183, 306, 44, () => dialog.SetActive(false));
            dialog.SetActive(false);
        }
        void OpenExplanation(bool correction)
        {
            dialog.SetActive(true);
            dialog.transform.Find("ExplanationCard/ExplanationTitle").GetComponent<Text>().text = correction ? "仲間" + "ABCD"[SelectedCompanion] + "の意図を修正" : "仲間に託す";
            dialog.transform.Find("ExplanationCard/ExplanationBody").GetComponent<Text>().text = correction
                ? "ここから意図の修正に進む導線です。\nこのモックでは修正は実行しません。\n残り1回の表示も変わりません。"
                : "ここから仲間に託す導線です。\nこのモックでは戦闘は進みません。\n行動前の表示だけを比較できます。";
        }

        void BuildDiorama()
        {
            // Fixed illustrative geometry, unrelated to GridPosition or battle rules.
            var field = new GameObject("Fictional Diorama");
            var fieldCamera = new GameObject("Field Camera").AddComponent<Camera>();
            fieldCamera.depth = 1;
            fieldCamera.cullingMask = 1;
            var target = new RenderTexture(716, 366, 24);
            fieldTarget = target;
            fieldCamera.targetTexture = target;
            var fieldImage = Rect(canvas.transform, "DioramaRender", 16, 292, 358, 183).gameObject.AddComponent<RawImage>();
            fieldImage.texture = target; fieldImage.raycastTarget = false;
            fieldCamera.orthographic = true; fieldCamera.orthographicSize = 3.3f;
            fieldCamera.transform.position = new Vector3(0, 8, -10);
            fieldCamera.transform.LookAt(Vector3.zero);
            fieldCamera.clearFlags = CameraClearFlags.SolidColor;
            fieldCamera.backgroundColor = new Color32(24, 39, 34, 255);
            Shape(field.transform, "Ground", PrimitiveType.Cube, new Vector3(0, -0.35f, 0), new Vector3(10, 0.6f, 5.7f), new Color32(68, 81, 65, 255));
            Shape(field.transform, "RockLeft", PrimitiveType.Cube, new Vector3(-4, 0, 1.2f), new Vector3(1.2f, 1.1f, 1), new Color32(91, 99, 86, 255));
            Shape(field.transform, "RockRight", PrimitiveType.Cube, new Vector3(3.9f, 0, 2.1f), new Vector3(1.3f, 1.6f, 1), new Color32(91, 99, 86, 255));
            var positions = new[] { new Vector3(0, 0.6f, 0.2f), new Vector3(-2.3f, 0.6f, 0), new Vector3(1.9f, 0.6f, -1.1f), new Vector3(0.1f, 0.6f, -1.8f) };
            for (var i = 0; i < 4; i++)
            {
                Shape(field.transform, "Pawn" + "ABCD"[i], PrimitiveType.Capsule, positions[i], new Vector3(0.55f, 0.55f, 0.55f), new Color32(159, 196, 163, 255));
                var screen = fieldCamera.WorldToViewportPoint(positions[i] + Vector3.up * 0.85f);
                var x = 16 + screen.x * 358 - 20;
                var y = 292 + (1 - screen.y) * 183 - 10;
                Button(canvas.transform, "PawnInspect" + i, "ABCD"[i].ToString(), x, y, 40, 32, CaptureSelect(i));
            }
            Shape(field.transform, "EnemyLeft", PrimitiveType.Capsule, new Vector3(-2.5f, 0.6f, 2), new Vector3(0.65f, 0.55f, 0.65f), new Color32(183, 151, 122, 255));
            Shape(field.transform, "EnemyRight", PrimitiveType.Capsule, new Vector3(2.5f, 0.6f, 2.2f), new Vector3(0.65f, 0.55f, 0.65f), new Color32(183, 151, 122, 255));
            Label(canvas.transform, "EnemyLabel", "敵", 170, 299, 50, 22, 13, Ink);
            Label(canvas.transform, "LowHp", "C  HP少", 287, 445, 76, 23, 12, Ink);
        }
        UnityEngine.Events.UnityAction CaptureSelect(int index) => () => SelectCompanion(index);
        void Shape(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 scale, Color color)
        {
            var shape = GameObject.CreatePrimitive(type); shape.name = name; shape.transform.SetParent(parent);
            shape.transform.position = position; shape.transform.localScale = scale;
            Destroy(shape.GetComponent<Collider>());
            var material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            materials.Add(material);
            material.SetColor("_BaseColor", color); shape.GetComponent<Renderer>().sharedMaterial = material;
        }
        static RectTransform Rect(Transform parent, string name, float x, float y, float width, float height)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.gameObject.layer = 5;
            rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(width, height); return rect;
        }
        Image Panel(Transform parent, string name, float x, float y, float width, float height, Color color)
        { var image = Rect(parent, name, x, y, width, height).gameObject.AddComponent<Image>(); image.color = color; return image; }
        Text Label(Transform parent, string name, string value, float x, float y, float width, float height, int size, Color color)
        {
            var text = Rect(parent, name, x, y, width, height).gameObject.AddComponent<Text>();
            text.text = value; text.font = font; text.fontSize = size; text.color = color; text.raycastTarget = false;
            text.alignment = TextAnchor.MiddleLeft; text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }
        Button Button(Transform parent, string name, string value, float x, float y, float width, float height, UnityEngine.Events.UnityAction action)
        {
            var image = Panel(parent, name, x, y, width, height, new Color32(33, 49, 44, 255));
            var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image; button.onClick.AddListener(action);
            var label = Label(image.transform, "Label", value, 0, 0, width, height, 15, Ink); label.alignment = TextAnchor.MiddleCenter;
            return button;
        }
    }
}
