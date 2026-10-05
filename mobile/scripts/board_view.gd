class_name BoardView
extends Control

signal cell_selected(cell: Vector2i)

const PARTY_COLORS := {
    "A": Color("#71a5cf"),
    "B": Color("#68e9b1"),
    "C": Color("#8c9ff2"),
    "D": Color("#e9bf64"),
}
const ENEMY_COLOR := Color("#f88d90")
const VOID_COLOR := Color("#0b1118")
const WALL_COLOR := Color("#17232d")
const FLOOR_A := Color("#273642")
const FLOOR_B := Color("#2d3b47")
const GRID_COLOR := Color("#405566")

var core: GameCore
var selected := Vector2i(-1, -1)
var _polygons := {}
var _font: Font

func _ready() -> void:
    mouse_filter = Control.MOUSE_FILTER_STOP
    _font = get_theme_default_font()
    queue_redraw()

func set_core(value: GameCore) -> void:
    core = value
    queue_redraw()

func set_selected(cell: Vector2i) -> void:
    selected = cell
    queue_redraw()

func _notification(what: int) -> void:
    if what == NOTIFICATION_RESIZED:
        queue_redraw()

func _gui_input(event: InputEvent) -> void:
    var point := Vector2.ZERO
    var pressed := false
    if event is InputEventScreenTouch and event.pressed:
        point = event.position
        pressed = true
    elif event is InputEventMouseButton and event.button_index == MOUSE_BUTTON_LEFT and event.pressed:
        point = event.position
        pressed = true
    if not pressed or core == null:
        return
    var cell := _pick_cell(point)
    if cell != Vector2i(-1, -1) and core.is_floor(cell) and core.is_revealed(cell):
        cell_selected.emit(cell)
        accept_event()

func _draw() -> void:
    draw_rect(Rect2(Vector2.ZERO, size), Color("#101821"))
    if core == null:
        return
    _polygons.clear()
    for y in range(GameCore.HEIGHT):
        for x in range(GameCore.WIDTH):
            var cell := Vector2i(x, y)
            var poly := _cell_polygon(cell)
            _polygons[cell] = poly
            var revealed := core.is_revealed(cell)
            var fill := VOID_COLOR
            if revealed:
                fill = WALL_COLOR if not core.is_floor(cell) else (FLOOR_A if (x + y) % 2 == 0 else FLOOR_B)
            draw_colored_polygon(poly, fill)
            if revealed:
                draw_polyline(PackedVector2Array([poly[0], poly[1], poly[2], poly[3], poly[0]]), GRID_COLOR, 1.5, true)
            if selected == cell and revealed and core.is_floor(cell):
                draw_polyline(PackedVector2Array([poly[0], poly[1], poly[2], poly[3], poly[0]]), Color("#8fb8ff"), 4.0, true)

    if core.is_revealed(GameCore.CHEST) and not core.chest_opened:
        _draw_object(GameCore.CHEST, "箱", Color("#c79a4a"))
    if core.is_revealed(GameCore.STAIRS):
        _draw_object(GameCore.STAIRS, "⇧", Color("#d7e1ea"))

    _draw_intents()
    _draw_enemies()
    _draw_party()

func _draw_intents() -> void:
    for intent in core.intent_plan():
        var actor: String = intent["actor"]
        if not core.party.has(actor):
            continue
        var from: Vector2 = _cell_center(core.party[actor]["pos"])
        var to: Vector2 = _cell_center(intent["target"])
        var color: Color = PARTY_COLORS.get(actor, Color.WHITE)
        draw_line(from, to, color, 4.0, true)
        var direction := (to - from).normalized()
        var side := Vector2(-direction.y, direction.x)
        var arrow := PackedVector2Array([to, to - direction * 12.0 + side * 7.0, to - direction * 12.0 - side * 7.0])
        draw_colored_polygon(arrow, color)
        if intent.get("ghost", false):
            draw_circle(to, 16.0, Color(color, 0.18))
            draw_arc(to, 17.0, 0.0, TAU, 20, Color(color, 0.8), 2.0, true)
        var midpoint := from.lerp(to, 0.5)
        draw_circle(midpoint, 12.0, Color("#101821"))
        draw_arc(midpoint, 12.0, 0.0, TAU, 18, color, 2.0, true)
        draw_string(_font, midpoint + Vector2(-9, 5), str(intent["icon"]), HORIZONTAL_ALIGNMENT_CENTER, 18, 14, Color.WHITE)

func _draw_enemies() -> void:
    for enemy_id in core.alive_enemy_ids():
        var enemy = core.get_enemy(enemy_id)
        if not core.is_revealed(enemy["pos"]):
            continue
        var center := _cell_center(enemy["pos"])
        draw_circle(center, 18.0, Color("#6a3338"))
        draw_arc(center, 19.0, 0.0, TAU, 24, ENEMY_COLOR, 3.0, true)
        var label := "鬼" if enemy["name"] == "大鬼" else "敵"
        draw_string(_font, center + Vector2(-15, 5), label, HORIZONTAL_ALIGNMENT_CENTER, 30, 13, Color.WHITE)
        var ratio := float(enemy["hp"]) / float(enemy["max_hp"])
        draw_rect(Rect2(center + Vector2(-20, -29), Vector2(40, 4)), Color("#2a1a1c"))
        draw_rect(Rect2(center + Vector2(-20, -29), Vector2(40 * ratio, 4)), ENEMY_COLOR)

func _draw_party() -> void:
    for id in core.alive_party_ids():
        var member = core.party[id]
        var center := _cell_center(member["pos"])
        var color: Color = PARTY_COLORS[id]
        draw_circle(center, 19.0, color)
        draw_arc(center, 20.0, 0.0, TAU, 24, Color.WHITE, 2.0, true)
        draw_string(_font, center + Vector2(-12, 7), id, HORIZONTAL_ALIGNMENT_CENTER, 24, 17, Color.WHITE)

func _draw_object(cell: Vector2i, text: String, color: Color) -> void:
    var center := _cell_center(cell)
    draw_circle(center, 16.0, Color(color, 0.22))
    draw_string(_font, center + Vector2(-14, 6), text, HORIZONTAL_ALIGNMENT_CENTER, 28, 16, color)

func _layout_values() -> Dictionary:
    var tile_w := clampf(size.x / 8.4, 38.0, 52.0)
    var tile_h := tile_w * 0.80
    var skew := tile_w * 0.16
    var board_width := GameCore.WIDTH * tile_w + (GameCore.HEIGHT - 1) * skew
    var origin_x := maxf(8.0, (size.x - board_width) * 0.5)
    var origin_y := 16.0
    return {"tile_w": tile_w, "tile_h": tile_h, "skew": skew, "origin": Vector2(origin_x, origin_y)}

func _cell_polygon(cell: Vector2i) -> PackedVector2Array:
    var l := _layout_values()
    var w: float = l["tile_w"]
    var h: float = l["tile_h"]
    var skew: float = l["skew"]
    var origin: Vector2 = l["origin"]
    var x := origin.x + cell.x * w + (GameCore.HEIGHT - 1 - cell.y) * skew
    var y := origin.y + cell.y * h
    var lean := skew * 0.75
    return PackedVector2Array([
        Vector2(x + lean, y),
        Vector2(x + w, y),
        Vector2(x + w - lean, y + h),
        Vector2(x, y + h),
    ])

func _cell_center(cell: Vector2i) -> Vector2:
    var poly := _cell_polygon(cell)
    return (poly[0] + poly[1] + poly[2] + poly[3]) / 4.0

func _pick_cell(point: Vector2) -> Vector2i:
    for cell in _polygons.keys():
        if Geometry2D.is_point_in_polygon(point, _polygons[cell]):
            return cell
    return Vector2i(-1, -1)
