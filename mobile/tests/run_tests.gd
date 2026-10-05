extends SceneTree

const GameCore = preload("res://scripts/game_core.gd")
var failures := 0

func _initialize() -> void:
    _test_initial_state()
    _test_waypoint_and_determinism()
    _test_b_guard()
    _test_c_flank()
    _test_d_heal()
    _test_counter_relic()
    _test_no_post_death_counter()
    _test_intent_matches_next_actor_plan()
    _test_canonical_floor_can_complete()
    if failures == 0:
        print("Godot mobile core: all tests passed")
        quit(0)
    else:
        push_error("Godot mobile core: %d test(s) failed" % failures)
        quit(1)

func _check(condition: bool, message: String) -> void:
    if not condition:
        failures += 1
        push_error(message)

func _test_initial_state() -> void:
    var c = GameCore.new()
    _check(c.party.size() == 4, "party must have four active members")
    _check(c.party["A"]["pos"] == GameCore.START, "A starts at documented position")
    _check(c.relic == "", "run starts without relic")
    _check(c.policy == "balance", "default policy is balance")

func _test_waypoint_and_determinism() -> void:
    var a = GameCore.new()
    var b = GameCore.new()
    var target := Vector2i(3, 8)
    _check(a.set_waypoint(target), "revealed floor should accept waypoint")
    _check(b.set_waypoint(target), "same waypoint should be valid in clone")
    var ea = a.leader_action()
    var eb = b.leader_action()
    _check(a.party["A"]["pos"] == b.party["A"]["pos"], "same inputs produce same A position")
    _check(str(ea) == str(eb), "same inputs produce same leader events")

func _test_b_guard() -> void:
    var c = GameCore.new()
    c.party["A"]["pos"] = Vector2i(3, 5)
    c.party["B"]["pos"] = Vector2i(2, 5)
    c.revealed[Vector2i(3, 4)] = true
    var e = c.get_enemy("G1")
    e["pos"] = Vector2i(3, 4)
    e["atk"] = 7
    var before: int = c.party["A"]["hp"]
    c.enemy_action("G1")
    _check(c.party["A"]["hp"] == before - 5, "B guard reduces adjacent attack by 2")

func _test_c_flank() -> void:
    var c = GameCore.new()
    c.party["A"]["pos"] = Vector2i(3, 5)
    c.party["C"]["pos"] = Vector2i(4, 4)
    var e = c.get_enemy("G1")
    e["pos"] = Vector2i(3, 4)
    e["hp"] = 20
    c.revealed[e["pos"]] = true
    c.ally_action("C")
    _check(e["hp"] == 8, "C flank should deal base 8 + 4 when another ally is adjacent")
    _check(c.stats["flanks"] == 1, "flank stat increments")

func _test_d_heal() -> void:
    var c = GameCore.new()
    c.party["A"]["pos"] = Vector2i(3, 7)
    c.party["D"]["pos"] = Vector2i(3, 9)
    c.party["A"]["hp"] = 20
    c.ally_action("D")
    _check(c.party["A"]["hp"] == 27, "D heals 7 without bell")
    var c2 = GameCore.new()
    c2.choose_relic("bell")
    c2.party["A"]["pos"] = Vector2i(3, 6)
    c2.party["D"]["pos"] = Vector2i(3, 9)
    c2.party["A"]["hp"] = 20
    c2.ally_action("D")
    _check(c2.party["A"]["hp"] == 30, "bell raises D heal to 10")

func _test_counter_relic() -> void:
    var c = GameCore.new()
    c.choose_relic("thorn")
    c.party["A"]["pos"] = Vector2i(3, 5)
    c.party["B"]["pos"] = Vector2i(2, 5)
    var e = c.get_enemy("G1")
    e["pos"] = Vector2i(3, 4)
    c.revealed[e["pos"]] = true
    c.counter_hits = 1
    var hp_before: int = e["hp"]
    c.enemy_action("G1")
    _check(e["hp"] < hp_before, "thorn triggers counter on second hit")
    _check(c.stats["counters"] == 1, "counter stat increments")

func _test_no_post_death_counter() -> void:
    var c = GameCore.new()
    c.party["A"]["pos"] = Vector2i(3, 5)
    c.party["B"]["pos"] = Vector2i(1, 5)
    c.party["A"]["hp"] = 3
    c.counter_hits = 2
    var e = c.get_enemy("O1")
    e["pos"] = Vector2i(3, 4)
    e["atk"] = 20
    c.revealed[e["pos"]] = true
    var enemy_hp: int = e["hp"]
    c.enemy_action("O1")
    _check(c.party["A"]["hp"] == 0, "lethal hit kills A")
    _check(e["hp"] == enemy_hp, "lethal hit emits no counter damage")
    _check(c.stats["counters"] == 0, "lethal hit does not increment counter")

func _test_intent_matches_next_actor_plan() -> void:
    var c = GameCore.new()
    c.set_waypoint(Vector2i(3, 8))
    var plan = c.intent_plan()
    var a_intent = plan.filter(func(v): return v["actor"] == "A")
    _check(a_intent.size() == 1, "A should expose one next intent")
    var target: Vector2i = a_intent[0]["target"]
    c.leader_action()
    _check(c.party["A"]["pos"] == target, "A intent destination matches actual leader move")

func _resolve_round(c) -> void:
    c.leader_action()
    for id in ["B", "C", "D"]:
        if c.finished:
            return
        c.ally_action(id)
    var enemy_ids = c.alive_enemy_ids().duplicate()
    for enemy_id in enemy_ids:
        if c.finished:
            return
        c.enemy_action(enemy_id)
    if not c.finished:
        c.end_round()

func _test_canonical_floor_can_complete() -> void:
    var c = GameCore.new()
    var path := [
        Vector2i(3, 8), Vector2i(3, 7), Vector2i(3, 6),
        Vector2i(2, 6), Vector2i(2, 5), Vector2i(1, 5),
        Vector2i(2, 5), Vector2i(3, 5), Vector2i(3, 4),
        Vector2i(3, 3), Vector2i(3, 2), Vector2i(3, 1),
        Vector2i(4, 1), Vector2i(5, 1),
    ]
    var index := 0
    var rounds := 0
    while not c.finished and rounds < 100:
        var target: Vector2i = path[index]
        if c.is_revealed(target):
            c.set_waypoint(target)
        _resolve_round(c)
        if c.relic_choice_pending():
            c.choose_relic("thorn")
        if c.party["A"]["pos"] == target and index < path.size() - 1:
            index += 1
        rounds += 1
    _check(c.finished and c.won, "canonical real-combat run reaches stairs and wins")
    _check(c.stats["kills"] == 5, "canonical run defeats all five enemies")
    _check(c.relic == "thorn", "canonical run includes the relic choice")
