class_name GameCore
extends RefCounted

const WIDTH := 7
const HEIGHT := 11
const START := Vector2i(3, 9)
const CHEST := Vector2i(1, 5)
const STAIRS := Vector2i(5, 1)

const MAP_ROWS := PackedStringArray([
    "#######",
    "#.....#",
    "#.#.#.#",
    "#.....#",
    "#.....#",
    "#.....#",
    "##...##",
    "#.....#",
    "#.....#",
    "#.....#",
    "###.###",
])

var turn := 1
var policy := "balance"
var waypoint := START
var relic := ""
var chest_opened := false
var finished := false
var won := false
var counter_hits := 0
var event_log: Array[String] = []
var stats := {}
var party := {}
var enemies: Array = []
var revealed := {}

func _init() -> void:
    reset()

func reset() -> void:
    turn = 1
    policy = "balance"
    waypoint = START
    relic = ""
    chest_opened = false
    finished = false
    won = false
    counter_hits = 0
    event_log = []
    stats = {
        "kills": 0,
        "damage_taken": 0,
        "healing": 0,
        "counters": 0,
        "flanks": 0,
    }
    party = {
        "A": {"id": "A", "pos": START, "hp": 44, "max_hp": 44, "atk": 9, "role": "前衛"},
        "B": {"id": "B", "pos": Vector2i(2, 9), "hp": 36, "max_hp": 36, "atk": 5, "role": "守護"},
        "C": {"id": "C", "pos": Vector2i(4, 9), "hp": 30, "max_hp": 30, "atk": 8, "role": "遊撃"},
        "D": {"id": "D", "pos": Vector2i(3, 10), "hp": 28, "max_hp": 28, "atk": 3, "role": "支援"},
    }
    enemies = [
        {"id": "G1", "name": "ゴブリン", "pos": Vector2i(2, 7), "hp": 15, "max_hp": 15, "atk": 5},
        {"id": "G2", "name": "ゴブリン", "pos": Vector2i(4, 7), "hp": 15, "max_hp": 15, "atk": 5},
        {"id": "G3", "name": "ゴブリン", "pos": Vector2i(2, 3), "hp": 17, "max_hp": 17, "atk": 6},
        {"id": "G4", "name": "ゴブリン", "pos": Vector2i(4, 3), "hp": 17, "max_hp": 17, "atk": 6},
        {"id": "O1", "name": "大鬼", "pos": Vector2i(3, 2), "hp": 30, "max_hp": 30, "atk": 9},
    ]
    revealed = {}
    _reveal_from_party()
    _log("遠征開始")

func set_policy(value: String) -> void:
    if value in ["balance", "safe", "attack"]:
        policy = value
        _log("方針を%sへ変更" % policy_label())

func policy_label() -> String:
    match policy:
        "safe":
            return "慎重"
        "attack":
            return "攻める"
        _:
            return "バランス"

func set_waypoint(cell: Vector2i) -> bool:
    if not is_floor(cell) or not is_revealed(cell):
        return false
    waypoint = cell
    return true

func choose_relic(value: String) -> bool:
    if value not in ["thorn", "bell"] or relic != "":
        return false
    relic = value
    _log("%sを装備" % relic_label())
    return true

func relic_label() -> String:
    match relic:
        "thorn":
            return "荊棘の護符"
        "bell":
            return "巡礼の鈴"
        _:
            return "なし"

func is_floor(cell: Vector2i) -> bool:
    if cell.x < 0 or cell.x >= WIDTH or cell.y < 0 or cell.y >= HEIGHT:
        return false
    return MAP_ROWS[cell.y][cell.x] != "#"

func is_revealed(cell: Vector2i) -> bool:
    return revealed.has(cell)

func alive_party_ids() -> Array[String]:
    var result: Array[String] = []
    for id in ["A", "B", "C", "D"]:
        if party[id]["hp"] > 0:
            result.append(id)
    return result

func alive_enemy_ids() -> Array[String]:
    var result: Array[String] = []
    for enemy in enemies:
        if enemy["hp"] > 0:
            result.append(enemy["id"])
    return result

func known_enemy_ids() -> Array[String]:
    var result: Array[String] = []
    for enemy in enemies:
        if enemy["hp"] > 0 and is_revealed(enemy["pos"]):
            result.append(enemy["id"])
    return result

func get_enemy(enemy_id: String):
    for enemy in enemies:
        if enemy["id"] == enemy_id:
            return enemy
    return null

func party_at(cell: Vector2i, ignore_id := "") -> String:
    for id in alive_party_ids():
        if id != ignore_id and party[id]["pos"] == cell:
            return id
    return ""

func enemy_at(cell: Vector2i) -> String:
    for enemy_id in alive_enemy_ids():
        var enemy = get_enemy(enemy_id)
        if enemy["pos"] == cell:
            return enemy_id
    return ""

func is_blocked(cell: Vector2i, ignore_party_id := "") -> bool:
    if not is_floor(cell):
        return true
    if party_at(cell, ignore_party_id) != "":
        return true
    return enemy_at(cell) != ""

func relic_choice_pending() -> bool:
    return chest_opened and relic == ""

func can_finish() -> bool:
    return alive_enemy_ids().is_empty() and party["A"]["pos"] == STAIRS

func leader_action() -> Array:
    if finished:
        return []
    var events: Array = []
    var a = party["A"]
    if a["hp"] <= 0:
        return events

    var adjacent := _adjacent_known_enemy(a["pos"])
    if adjacent != "":
        events.append_array(_attack_enemy("A", adjacent, 0, "Aの攻撃"))
        return events

    if a["pos"] == waypoint:
        events.append(_event("wait", "A", "Aは周囲を警戒"))
        return events

    var step = _bfs_step(a["pos"], waypoint, "A")
    if step != Vector2i(-1, -1) and enemy_at(step) == "" and party_at(step, "A") == "":
        a["pos"] = step
        events.append(_event("move", "A", "Aが探索先へ進む", {"to": step}))
        _reveal_from_party()
        if step == CHEST and not chest_opened:
            chest_opened = true
            events.append(_event("chest", "A", "宝箱を発見", {"pos": CHEST}))
    else:
        events.append(_event("wait", "A", "Aは進路を確保できず待機"))
    return events

func ally_action(id: String) -> Array:
    if finished or id not in ["B", "C", "D"] or party[id]["hp"] <= 0:
        return []
    match id:
        "B":
            return _b_action()
        "C":
            return _c_action()
        "D":
            return _d_action()
    return []

func enemy_action(enemy_id: String) -> Array:
    if finished:
        return []
    var enemy = get_enemy(enemy_id)
    if enemy == null or enemy["hp"] <= 0 or not is_revealed(enemy["pos"]):
        return []
    var target_id := _nearest_party_id(enemy["pos"])
    if target_id == "":
        return []
    var target = party[target_id]
    if _distance(enemy["pos"], target["pos"]) <= 1:
        var damage: int = enemy["atk"]
        var guarded := false
        if target_id == "A" and _b_is_guarding():
            damage = maxi(1, damage - 2)
            guarded = true
        target["hp"] = maxi(0, target["hp"] - damage)
        if target_id == "A":
            stats["damage_taken"] += damage
            counter_hits += 1
        var events: Array = [_event("damage", enemy_id, "%s → %s  %dダメージ" % [enemy["name"], target_id, damage], {
            "target": target_id,
            "amount": damage,
            "guarded": guarded,
        })]
        if target_id == "A" and target["hp"] > 0:
            events.append_array(_counter_if_ready())
        _check_failure()
        return events

    var step = _bfs_step(enemy["pos"], target["pos"], "")
    if step != Vector2i(-1, -1) and party_at(step) == "" and enemy_at(step) == "":
        enemy["pos"] = step
        return [_event("move", enemy_id, "%sが接近" % enemy["name"], {"to": step})]
    return [_event("wait", enemy_id, "%sは進路を探している" % enemy["name"])]

func end_round() -> void:
    if finished:
        return
    turn += 1
    _reveal_from_party()
    if can_finish():
        finished = true
        won = true
        _log("階段へ到達")

func intent_plan() -> Array:
    var result: Array = []
    if finished:
        return result

    var a = party["A"]
    if a["hp"] > 0:
        var adjacent := _adjacent_known_enemy(a["pos"])
        if adjacent != "":
            result.append({"actor": "A", "kind": "attack", "target": get_enemy(adjacent)["pos"], "icon": "⚔"})
        elif a["pos"] != waypoint:
            var step = _bfs_step(a["pos"], waypoint, "A")
            if step != Vector2i(-1, -1):
                result.append({"actor": "A", "kind": "move", "target": step, "icon": "↗", "ghost": true})

    if party["B"]["hp"] > 0 and party["A"]["hp"] > 0:
        result.append({"actor": "B", "kind": "guard", "target": party["A"]["pos"], "icon": "◆"})

    var c = party["C"]
    if c["hp"] > 0:
        var c_enemy_id := _nearest_known_enemy_id(c["pos"])
        if c_enemy_id != "":
            var c_enemy = get_enemy(c_enemy_id)
            if _distance(c["pos"], c_enemy["pos"]) <= 1:
                result.append({"actor": "C", "kind": "attack", "target": c_enemy["pos"], "icon": "⚔"})
            else:
                var flank := _flank_tile(c["pos"], c_enemy["pos"])
                if flank != Vector2i(-1, -1):
                    result.append({"actor": "C", "kind": "move", "target": flank, "icon": "↗", "ghost": true})

    var d = party["D"]
    if d["hp"] > 0:
        var low_id := _lowest_hp_party_id()
        if low_id != "" and _should_heal(low_id) and _distance(d["pos"], party[low_id]["pos"]) <= _d_heal_range():
            result.append({"actor": "D", "kind": "heal", "target": party[low_id]["pos"], "icon": "+"})
        elif party["A"]["hp"] > 0:
            var back := _d_backline_target()
            result.append({"actor": "D", "kind": "move", "target": back, "icon": "↘", "ghost": true})
    return result

func strategist_note() -> Dictionary:
    var a = party["A"]
    if a["hp"] <= 0:
        return {"level": "危険", "text": "Aが倒れた。今回はここまでだ。"}
    if float(a["hp"]) / float(a["max_hp"]) < 0.35:
        return {"level": "危険", "text": "Aが危険域。Dの支援が届く位置を保ちたい。"}
    var known := known_enemy_ids()
    if known.is_empty():
        if chest_opened and relic != "":
            return {"level": "平常", "text": "先へ進もう。次の接敵で遺物の違いが見える。"}
        return {"level": "平常", "text": "まだ静かだ。隊列を崩さず進もう。"}
    var nearest_id := _nearest_known_enemy_id(a["pos"])
    if nearest_id != "" and _distance(a["pos"], get_enemy(nearest_id)["pos"]) <= 2:
        return {"level": "警戒", "text": "接敵間近。BはAを守り、Cは側面へ回るつもりだ。"}
    return {"level": "警戒", "text": "敵影を確認。仲間の意図を見て次の一手を決めよう。"}

func _b_action() -> Array:
    var b = party["B"]
    var enemy_id := _adjacent_known_enemy(b["pos"])
    if enemy_id != "":
        return _attack_enemy("B", enemy_id, 0, "Bの攻撃")
    var a = party["A"]
    if a["hp"] <= 0 or _distance(b["pos"], a["pos"]) <= 1:
        return [_event("guard", "B", "BはAを守る")]
    var target := _best_adjacent_to(a["pos"], b["pos"], "B")
    var step = _bfs_step(b["pos"], target, "B")
    if step != Vector2i(-1, -1):
        b["pos"] = step
        return [_event("move", "B", "BはAを守る位置へ", {"to": step})]
    return [_event("wait", "B", "Bは守備位置を維持")]

func _c_action() -> Array:
    var c = party["C"]
    var enemy_id := _nearest_known_enemy_id(c["pos"])
    if enemy_id == "":
        return [_event("wait", "C", "Cは周囲を警戒")]
    var enemy = get_enemy(enemy_id)
    if _distance(c["pos"], enemy["pos"]) <= 1:
        var flanking := _other_ally_adjacent_to_enemy("C", enemy["pos"])
        var bonus := 4 if flanking else (2 if policy == "attack" else 0)
        if flanking:
            stats["flanks"] += 1
        return _attack_enemy("C", enemy_id, bonus, "Cの挟撃" if flanking else "Cの攻撃")
    var flank := _flank_tile(c["pos"], enemy["pos"])
    if flank != Vector2i(-1, -1):
        var step = _bfs_step(c["pos"], flank, "C")
        if step != Vector2i(-1, -1):
            c["pos"] = step
            return [_event("move", "C", "Cは側面へ回る", {"to": step})]
    return [_event("wait", "C", "Cは攻め筋を探している")]

func _d_action() -> Array:
    var d = party["D"]
    var low_id := _lowest_hp_party_id()
    if low_id != "" and _should_heal(low_id) and _distance(d["pos"], party[low_id]["pos"]) <= _d_heal_range():
        var amount := 10 if relic == "bell" else 7
        var target = party[low_id]
        var before: int = target["hp"]
        target["hp"] = mini(target["max_hp"], target["hp"] + amount)
        var actual: int = target["hp"] - before
        stats["healing"] += actual
        return [_event("heal", "D", "D → %s  +%d" % [low_id, actual], {"target": low_id, "amount": actual})]
    var enemy_id := _adjacent_known_enemy(d["pos"])
    if policy == "attack" and enemy_id != "":
        return _attack_enemy("D", enemy_id, 0, "Dの攻撃")
    var back := _d_backline_target()
    if back != d["pos"]:
        var step = _bfs_step(d["pos"], back, "D")
        if step != Vector2i(-1, -1):
            d["pos"] = step
            return [_event("move", "D", "Dは後方を維持", {"to": step})]
    return [_event("wait", "D", "Dは支援位置を維持")]

func _attack_enemy(actor_id: String, enemy_id: String, bonus: int, label: String) -> Array:
    var actor = party[actor_id]
    var enemy = get_enemy(enemy_id)
    if enemy == null or enemy["hp"] <= 0:
        return []
    var damage: int = actor["atk"] + bonus
    enemy["hp"] = maxi(0, enemy["hp"] - damage)
    var events: Array = [_event("attack", actor_id, "%s → %s  %dダメージ" % [label, enemy["name"], damage], {
        "target": enemy_id,
        "amount": damage,
        "bonus": bonus,
    })]
    if enemy["hp"] <= 0:
        stats["kills"] += 1
        events.append(_event("defeat", actor_id, "%sを撃破" % enemy["name"], {"target": enemy_id}))
    return events

func _counter_if_ready() -> Array:
    var threshold := 2 if relic == "thorn" else 3
    if counter_hits < threshold:
        return []
    var a = party["A"]
    var targets: Array[String] = []
    for enemy_id in alive_enemy_ids():
        var enemy = get_enemy(enemy_id)
        if _distance(a["pos"], enemy["pos"]) <= 1:
            targets.append(enemy_id)
    if targets.is_empty():
        return []
    counter_hits -= threshold
    stats["counters"] += 1
    var damage := 7 if relic == "thorn" else 5
    var dealt := 0
    var events: Array = [_event("counter", "A", "Aの反撃！", {"targets": targets.duplicate(), "amount": damage})]
    for enemy_id in targets:
        var enemy = get_enemy(enemy_id)
        var actual: int = mini(damage, enemy["hp"])
        enemy["hp"] = maxi(0, enemy["hp"] - damage)
        dealt += actual
        if enemy["hp"] <= 0:
            stats["kills"] += 1
            events.append(_event("defeat", "A", "%sを反撃で撃破" % enemy["name"], {"target": enemy_id}))
    var recover := mini(a["max_hp"] - a["hp"], maxi(1, int(floor(float(dealt) / 4.0))))
    if recover > 0:
        a["hp"] += recover
        stats["healing"] += recover
        events.append(_event("heal", "A", "A 自己回復 +%d" % recover, {"target": "A", "amount": recover}))
    return events

func _check_failure() -> void:
    if party["A"]["hp"] <= 0:
        finished = true
        won = false
        _log("Aが倒れた")

func _should_heal(id: String) -> bool:
    var p = party[id]
    var ratio := float(p["hp"]) / float(p["max_hp"])
    var threshold := 0.80 if policy == "safe" else (0.50 if policy == "attack" else 0.65)
    return ratio < threshold

func _d_heal_range() -> int:
    return 4 if relic == "bell" else 3

func _d_backline_target() -> Vector2i:
    var a = party["A"]["pos"]
    var candidates := [
        a + Vector2i(0, 2),
        a + Vector2i(-1, 2),
        a + Vector2i(1, 2),
        a + Vector2i(0, 1),
    ]
    for cell in candidates:
        if is_floor(cell) and party_at(cell, "D") == "" and enemy_at(cell) == "":
            return cell
    return party["D"]["pos"]

func _b_is_guarding() -> bool:
    return party["B"]["hp"] > 0 and _distance(party["A"]["pos"], party["B"]["pos"]) <= 1

func _other_ally_adjacent_to_enemy(exclude_id: String, enemy_pos: Vector2i) -> bool:
    for id in alive_party_ids():
        if id != exclude_id and _distance(party[id]["pos"], enemy_pos) <= 1:
            return true
    return false

func _lowest_hp_party_id() -> String:
    var ids := alive_party_ids()
    if ids.is_empty():
        return ""
    ids.sort_custom(func(a, b):
        return float(party[a]["hp"]) / float(party[a]["max_hp"]) < float(party[b]["hp"]) / float(party[b]["max_hp"])
    )
    return ids[0]

func _nearest_party_id(from: Vector2i) -> String:
    var ids := alive_party_ids()
    if ids.is_empty():
        return ""
    ids.sort_custom(func(a, b): return _distance(from, party[a]["pos"]) < _distance(from, party[b]["pos"]))
    return ids[0]

func _nearest_known_enemy_id(from: Vector2i) -> String:
    var ids := known_enemy_ids()
    if ids.is_empty():
        return ""
    ids.sort_custom(func(a, b): return _distance(from, get_enemy(a)["pos"]) < _distance(from, get_enemy(b)["pos"]))
    return ids[0]

func _adjacent_known_enemy(from: Vector2i) -> String:
    var ids := known_enemy_ids()
    ids.sort()
    for enemy_id in ids:
        if _distance(from, get_enemy(enemy_id)["pos"]) <= 1:
            return enemy_id
    return ""

func _flank_tile(from: Vector2i, enemy_pos: Vector2i) -> Vector2i:
    var candidates: Array[Vector2i] = [
        enemy_pos + Vector2i(1, 0),
        enemy_pos + Vector2i(-1, 0),
        enemy_pos + Vector2i(0, 1),
        enemy_pos + Vector2i(0, -1),
    ]
    var valid: Array[Vector2i] = []
    for cell in candidates:
        if not is_floor(cell) or party_at(cell, "C") != "" or enemy_at(cell) != "":
            continue
        if policy == "safe" and _distance(cell, party["A"]["pos"]) > 3:
            continue
        valid.append(cell)
    if valid.is_empty():
        return Vector2i(-1, -1)
    valid.sort_custom(func(a, b): return _distance(from, a) < _distance(from, b))
    return valid[0]

func _best_adjacent_to(center: Vector2i, from: Vector2i, ignore_id: String) -> Vector2i:
    var candidates: Array[Vector2i] = [
        center + Vector2i(-1, 0),
        center + Vector2i(1, 0),
        center + Vector2i(0, -1),
        center + Vector2i(0, 1),
    ]
    var valid: Array[Vector2i] = []
    for cell in candidates:
        if is_floor(cell) and party_at(cell, ignore_id) == "" and enemy_at(cell) == "":
            valid.append(cell)
    if valid.is_empty():
        return party[ignore_id]["pos"]
    valid.sort_custom(func(a, b): return _distance(from, a) < _distance(from, b))
    return valid[0]

func _bfs_step(from: Vector2i, target: Vector2i, ignore_party_id: String) -> Vector2i:
    if from == target:
        return from
    if not is_floor(target):
        return Vector2i(-1, -1)
    var queue: Array[Vector2i] = [from]
    var seen := {from: true}
    var previous := {}
    var dirs: Array[Vector2i] = [Vector2i(1, 0), Vector2i(-1, 0), Vector2i(0, 1), Vector2i(0, -1)]
    var index := 0
    while index < queue.size():
        var current := queue[index]
        index += 1
        if current == target:
            break
        for delta in dirs:
            var next := current + delta
            if seen.has(next) or not is_floor(next):
                continue
            var member := party_at(next, ignore_party_id)
            var enemy := enemy_at(next)
            if next != target and (member != "" or enemy != ""):
                continue
            seen[next] = true
            previous[next] = current
            queue.append(next)
    if not seen.has(target):
        return Vector2i(-1, -1)
    var cursor := target
    while previous.has(cursor) and previous[cursor] != from:
        cursor = previous[cursor]
    if previous.has(cursor) and previous[cursor] == from:
        return cursor
    return Vector2i(-1, -1)

func _reveal_from_party() -> void:
    for id in alive_party_ids():
        var center: Vector2i = party[id]["pos"]
        for y in range(HEIGHT):
            for x in range(WIDTH):
                var cell := Vector2i(x, y)
                if _distance(center, cell) <= 3:
                    revealed[cell] = true

func _distance(a: Vector2i, b: Vector2i) -> int:
    return absi(a.x - b.x) + absi(a.y - b.y)

func _event(kind: String, actor: String, text: String, data := {}) -> Dictionary:
    _log(text)
    return {"type": kind, "actor": actor, "text": text, "data": data}

func _log(text: String) -> void:
    event_log.push_front("T%d  %s" % [turn, text])
    if event_log.size() > 20:
        event_log.resize(20)
