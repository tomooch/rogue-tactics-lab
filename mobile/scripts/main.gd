extends Control

const GameCoreScript = preload("res://scripts/game_core.gd")
const BoardViewScript = preload("res://scripts/board_view.gd")

var core: GameCore
var board: BoardView
var selected := GameCore.START
var advancing := false

var status_label: Label
var turn_label: Label
var advisor_badge: Label
var advisor_text: Label
var policy_label: Label
var relic_label: Label
var selection_label: Label
var log_label: Label
var advance_button: Button
var reward_layer: Control
var end_layer: Control
var end_title: Label
var end_stats: Label
var member_buttons := {}

func _ready() -> void:
    DisplayServer.screen_set_orientation(DisplayServer.SCREEN_PORTRAIT)
    core = GameCoreScript.new()
    selected = core.waypoint
    _build_ui()
    _refresh_all()

func _build_ui() -> void:
    var background := ColorRect.new()
    background.color = Color("#101821")
    background.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
    add_child(background)

    var margin := MarginContainer.new()
    margin.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
    margin.add_theme_constant_override("margin_left", 12)
    margin.add_theme_constant_override("margin_right", 12)
    margin.add_theme_constant_override("margin_top", 12)
    margin.add_theme_constant_override("margin_bottom", 12)
    add_child(margin)

    var column := VBoxContainer.new()
    column.add_theme_constant_override("separation", 8)
    margin.add_child(column)

    var header := HBoxContainer.new()
    column.add_child(header)
    var title_box := VBoxContainer.new()
    title_box.size_flags_horizontal = Control.SIZE_EXPAND_FILL
    header.add_child(title_box)
    title_box.add_child(_label("Rogue Tactics Lab", 18, true))
    var sub := _label("4人の意図を読んで、探索先と方針だけを決める", 11, false, Color("#a9b8c5"))
    title_box.add_child(sub)
    var reset := Button.new()
    reset.text = "やり直す"
    reset.custom_minimum_size = Vector2(84, 40)
    reset.pressed.connect(_reset)
    header.add_child(reset)

    var board_panel := PanelContainer.new()
    board_panel.size_flags_vertical = Control.SIZE_EXPAND_FILL
    board_panel.add_theme_stylebox_override("panel", _panel_style(Color("#17242e"), Color("#405566"), 14))
    column.add_child(board_panel)
    var board_box := VBoxContainer.new()
    board_box.add_theme_constant_override("separation", 0)
    board_panel.add_child(board_box)

    var board_header := HBoxContainer.new()
    board_box.add_child(board_header)
    status_label = _label("探索中", 12, true)
    status_label.size_flags_horizontal = Control.SIZE_EXPAND_FILL
    board_header.add_child(status_label)
    turn_label = _label("TURN 1", 11, false, Color("#a9b8c5"))
    board_header.add_child(turn_label)

    board = BoardViewScript.new()
    board.custom_minimum_size = Vector2(0, 390)
    board.size_flags_vertical = Control.SIZE_EXPAND_FILL
    board.set_core(core)
    board.set_selected(selected)
    board.cell_selected.connect(_on_cell_selected)
    board_box.add_child(board)

    var advisor := PanelContainer.new()
    advisor.add_theme_stylebox_override("panel", _panel_style(Color("#15212a"), Color("#405566"), 12))
    column.add_child(advisor)
    var advisor_margin := MarginContainer.new()
    advisor_margin.add_theme_constant_override("margin_left", 10)
    advisor_margin.add_theme_constant_override("margin_right", 10)
    advisor_margin.add_theme_constant_override("margin_top", 8)
    advisor_margin.add_theme_constant_override("margin_bottom", 8)
    advisor.add_child(advisor_margin)
    var advisor_row := HBoxContainer.new()
    advisor_margin.add_child(advisor_row)
    var strategist := _label("軍師 E", 12, true)
    strategist.custom_minimum_size = Vector2(54, 0)
    advisor_row.add_child(strategist)
    advisor_text = _label("", 11, true, Color("#edf4fa"))
    advisor_text.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
    advisor_text.size_flags_horizontal = Control.SIZE_EXPAND_FILL
    advisor_row.add_child(advisor_text)
    advisor_badge = _label("平常", 10, true, Color("#a9b8c5"))
    advisor_row.add_child(advisor_badge)

    var party_row := HBoxContainer.new()
    party_row.add_theme_constant_override("separation", 5)
    column.add_child(party_row)
    for id in ["A", "B", "C", "D"]:
        var button := Button.new()
        button.size_flags_horizontal = Control.SIZE_EXPAND_FILL
        button.custom_minimum_size = Vector2(0, 58)
        button.text = id + "\n" + core.party[id]["role"]
        button.pressed.connect(_inspect_member.bind(id))
        party_row.add_child(button)
        member_buttons[id] = button

    var info_panel := PanelContainer.new()
    info_panel.add_theme_stylebox_override("panel", _panel_style(Color("#15212a"), Color("#405566"), 12))
    column.add_child(info_panel)
    var info_margin := MarginContainer.new()
    info_margin.add_theme_constant_override("margin_left", 10)
    info_margin.add_theme_constant_override("margin_right", 10)
    info_margin.add_theme_constant_override("margin_top", 8)
    info_margin.add_theme_constant_override("margin_bottom", 8)
    info_panel.add_child(info_margin)
    var info_row := HBoxContainer.new()
    info_margin.add_child(info_row)
    selection_label = _label("探索先", 11, true)
    selection_label.size_flags_horizontal = Control.SIZE_EXPAND_FILL
    selection_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
    info_row.add_child(selection_label)
    relic_label = _label("遺物：なし", 10, false, Color("#a9b8c5"))
    info_row.add_child(relic_label)

    var policy_row := HBoxContainer.new()
    policy_row.add_theme_constant_override("separation", 5)
    column.add_child(policy_row)
    policy_label = _label("方針", 11, true)
    policy_label.custom_minimum_size = Vector2(42, 44)
    policy_row.add_child(policy_label)
    for value in ["balance", "safe", "attack"]:
        var b := Button.new()
        b.text = {"balance":"バランス", "safe":"慎重", "attack":"攻める"}[value]
        b.size_flags_horizontal = Control.SIZE_EXPAND_FILL
        b.custom_minimum_size = Vector2(0, 44)
        b.pressed.connect(_set_policy.bind(value))
        b.name = "Policy_" + value
        policy_row.add_child(b)

    advance_button = Button.new()
    advance_button.text = "この方針で 1手進める"
    advance_button.custom_minimum_size = Vector2(0, 52)
    advance_button.pressed.connect(_advance_round)
    column.add_child(advance_button)

    log_label = _label("", 10, false, Color("#9fb0bd"))
    log_label.custom_minimum_size = Vector2(0, 34)
    log_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
    column.add_child(log_label)

    reward_layer = _modal_layer()
    add_child(reward_layer)
    var reward_card := _modal_card(reward_layer, "遺物を1つ選ぶ", "1つだけ変えて、次の戦いで違いを見よう。")
    var thorn := Button.new()
    thorn.text = "荊棘の護符\nA：2回被弾で反撃 / 反撃7"
    thorn.custom_minimum_size = Vector2(0, 64)
    thorn.pressed.connect(_choose_relic.bind("thorn"))
    reward_card.add_child(thorn)
    var bell := Button.new()
    bell.text = "巡礼の鈴\nD：回復10 / 射程4"
    bell.custom_minimum_size = Vector2(0, 64)
    bell.pressed.connect(_choose_relic.bind("bell"))
    reward_card.add_child(bell)
    reward_layer.visible = false

    end_layer = _modal_layer()
    add_child(end_layer)
    var end_card := _modal_card(end_layer, "遠征結果", "")
    end_title = _label("", 22, true)
    end_card.add_child(end_title)
    end_stats = _label("", 12, false, Color("#d8e2e9"))
    end_stats.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
    end_card.add_child(end_stats)
    var replay := Button.new()
    replay.text = "もう一度遊ぶ"
    replay.custom_minimum_size = Vector2(0, 48)
    replay.pressed.connect(_reset)
    end_card.add_child(replay)
    end_layer.visible = false

func _label(text: String, size_px: int, bold := false, color := Color("#edf4fa")) -> Label:
    var label := Label.new()
    label.text = text
    label.add_theme_font_size_override("font_size", size_px)
    label.add_theme_color_override("font_color", color)
    if bold:
        label.add_theme_constant_override("outline_size", 1)
    return label

func _panel_style(fill: Color, border: Color, radius: int) -> StyleBoxFlat:
    var style := StyleBoxFlat.new()
    style.bg_color = fill
    style.border_color = border
    style.set_border_width_all(1)
    style.set_corner_radius_all(radius)
    return style

func _modal_layer() -> Control:
    var layer := Control.new()
    layer.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
    var shade := ColorRect.new()
    shade.color = Color(0, 0, 0, 0.72)
    shade.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
    shade.mouse_filter = Control.MOUSE_FILTER_STOP
    layer.add_child(shade)
    return layer

func _modal_card(layer: Control, title_text: String, subtitle_text: String) -> VBoxContainer:
    var center := CenterContainer.new()
    center.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
    layer.add_child(center)
    var panel := PanelContainer.new()
    panel.custom_minimum_size = Vector2(330, 0)
    panel.add_theme_stylebox_override("panel", _panel_style(Color("#17242e"), Color("#5b7282"), 16))
    center.add_child(panel)
    var margin := MarginContainer.new()
    margin.add_theme_constant_override("margin_left", 16)
    margin.add_theme_constant_override("margin_right", 16)
    margin.add_theme_constant_override("margin_top", 16)
    margin.add_theme_constant_override("margin_bottom", 16)
    panel.add_child(margin)
    var box := VBoxContainer.new()
    box.add_theme_constant_override("separation", 10)
    margin.add_child(box)
    box.add_child(_label(title_text, 18, true))
    if subtitle_text != "":
        var subtitle := _label(subtitle_text, 11, false, Color("#a9b8c5"))
        subtitle.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
        box.add_child(subtitle)
    return box

func _on_cell_selected(cell: Vector2i) -> void:
    if advancing or core.finished or core.relic_choice_pending():
        return
    if core.set_waypoint(cell):
        selected = cell
        board.set_selected(cell)
        _refresh_all()

func _set_policy(value: String) -> void:
    if advancing or core.finished:
        return
    core.set_policy(value)
    _refresh_all()

func _advance_round() -> void:
    if advancing or core.finished or core.relic_choice_pending():
        return
    advancing = true
    _set_controls_enabled(false)
    await _play_events(core.leader_action())
    if core.finished:
        _finish_if_needed()
        advancing = false
        return
    for id in ["B", "C", "D"]:
        await _play_events(core.ally_action(id))
        if core.finished:
            break
    if not core.finished:
        var enemy_ids := core.alive_enemy_ids().duplicate()
        for enemy_id in enemy_ids:
            await _play_events(core.enemy_action(enemy_id))
            if core.finished:
                break
    if not core.finished:
        core.end_round()
    advancing = false
    _refresh_all()
    if core.relic_choice_pending():
        reward_layer.visible = true
    _finish_if_needed()
    _set_controls_enabled(not core.finished and not core.relic_choice_pending())

func _play_events(events: Array) -> void:
    for event in events:
        _refresh_all()
        status_label.text = event.get("text", "")
        await get_tree().create_timer(0.22).timeout

func _choose_relic(value: String) -> void:
    if core.choose_relic(value):
        reward_layer.visible = false
        _refresh_all()
        _set_controls_enabled(true)

func _inspect_member(id: String) -> void:
    var member = core.party[id]
    var detail := ""
    match id:
        "A":
            detail = "攻撃%d。%d回被弾で周囲へ反撃。反撃後に少し自己回復。" % [member["atk"], 2 if core.relic == "thorn" else 3]
        "B":
            detail = "攻撃%d。Aの隣を維持し、隣接中はAへの被ダメージを2軽減。" % member["atk"]
        "C":
            detail = "攻撃%d。側面へ回り、仲間と同じ敵を挟むと+4ダメージ。" % member["atk"]
        "D":
            detail = "攻撃%d。HPが減った仲間を優先して回復。現在の回復量%d。" % [member["atk"], 10 if core.relic == "bell" else 7]
    selection_label.text = "%s / %s  HP %d/%d  — %s" % [id, member["role"], member["hp"], member["max_hp"], detail]

func _refresh_all() -> void:
    board.queue_redraw()
    board.set_selected(selected)
    turn_label.text = "TURN %d" % core.turn
    if core.finished:
        status_label.text = "突破" if core.won else "遠征失敗"
    elif not core.known_enemy_ids().is_empty():
        status_label.text = "敵発見"
    else:
        status_label.text = "探索中"
    var note := core.strategist_note()
    advisor_badge.text = note["level"]
    advisor_text.text = note["text"]
    policy_label.text = "方針：%s" % core.policy_label()
    relic_label.text = "遺物：%s" % core.relic_label()
    if selection_label.text.begins_with("探索先") or selected == core.waypoint:
        selection_label.text = "探索先：(%d, %d)" % [core.waypoint.x, core.waypoint.y]
    log_label.text = "  /  ".join(core.event_log.slice(0, 3))
    for id in ["A", "B", "C", "D"]:
        var member = core.party[id]
        member_buttons[id].text = "%s %s\nHP %d/%d" % [id, member["role"], member["hp"], member["max_hp"]]
    for value in ["balance", "safe", "attack"]:
        var policy_button := find_child("Policy_" + value, true, false) as Button
        if policy_button:
            policy_button.modulate = Color.WHITE if core.policy == value else Color(0.78, 0.82, 0.86)
    advance_button.text = "この方針で 1手進める"

func _set_controls_enabled(enabled: bool) -> void:
    advance_button.disabled = not enabled
    for value in ["balance", "safe", "attack"]:
        var policy_button := find_child("Policy_" + value, true, false) as Button
        if policy_button:
            policy_button.disabled = not enabled

func _finish_if_needed() -> void:
    if not core.finished:
        return
    end_layer.visible = true
    end_title.text = "B2 突破！" if core.won else "遠征失敗"
    end_stats.text = "TURN %d\n撃破 %d　被ダメ %d　回復 %d\n反撃 %d　挟撃 %d\n遺物：%s" % [
        core.turn,
        core.stats["kills"],
        core.stats["damage_taken"],
        core.stats["healing"],
        core.stats["counters"],
        core.stats["flanks"],
        core.relic_label(),
    ]

func _reset() -> void:
    core.reset()
    selected = core.waypoint
    reward_layer.visible = false
    end_layer.visible = false
    advancing = false
    board.set_core(core)
    board.set_selected(selected)
    selection_label.text = "探索先：(%d, %d)" % [selected.x, selected.y]
    _refresh_all()
    _set_controls_enabled(true)
