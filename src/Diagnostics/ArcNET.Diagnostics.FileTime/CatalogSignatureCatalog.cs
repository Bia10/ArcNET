namespace ArcNET.Diagnostics;

public static class CatalogSignatureCatalog
{
    public static IReadOnlyList<string> Keys => s_keys;

    public static int Count => s_signaturesByNormalizedKey.Count;

    public static bool HasSignature(string key) =>
        s_signaturesByNormalizedKey.ContainsKey(CatalogAddressResolver.NormalizeKey(key));

    public static bool TryGetPattern(string key, out string pattern) =>
        s_signaturesByNormalizedKey.TryGetValue(CatalogAddressResolver.NormalizeKey(key), out pattern!);

    public static IReadOnlyDictionary<string, string> SignaturesByNormalizedKey => s_signaturesByNormalizedKey;

    private static readonly SignatureEntry[] s_entries =
    [
        new("area_get_last_known_area", "56 8B 74 24 08 57 8B 7C 24 10 8B C6 0B C7 74 4B"),
        new("area_is_known", "56 8B 74 24 08 57 8B 7C 24 10 8B C6 0B C7 74 60"),
        new("area_reset_last_known_area", "83 EC 38 53 8B 5C 24 40 55 8B"),
        new("area_set_known", "83 EC 40 53 8B 5C 24 50 55"),
        new("background_clear", "56 8B 74 24 0C 57 8B 7C 24 0C 6A 00 68"),
        new("background_description_get_body", "83 EC 08 8B 44 24 0C 3D E8 03 00 00 89 44 24 00 7D 0C 85 C0 75 40"),
        new("background_description_get_name", "8B 44 24 04 50 E8 46 FF"),
        new("background_educate_followers", "83 EC 58 56 8B 74 24 64 57 8B 7C 24 64 68 4F 01 00 00 56 57 E8 37"),
        new("background_get", "56 8B 74 24 08 57 8B 7C 24 10 8B C6 0B C7 74 26"),
        new("background_get_description", "83 EC 08 8B 44 24 0C 8B 15 5C D8"),
        new("background_set", "53 8B 5C 24 08 56 8B 74 24 14 57 8B 7C 24 14 56 68"),
        new("background_text_get", "8B 44 24 08 8B 4C 24 04 68 02"),
        new("bless_add", "83 EC 58 53 56 8B 74 24 68 57 8B 7C 24 68 68"),
        new("bless_get_logbook_data", "53 8B 5C 24 0C 55 8B 6C 24 0C 68 4F 01 00 00 53 55 E8 8A"),
        new("bless_remove", "83 EC 4C 53 55 56 8B 74 24 60 57 8B 7C 24 60 68 4F 01 00 00 56 57 E8 35"),
        new("combat_turn_based_whos_turn_set", "A1 2C C2 5F 00 56 85 C0 57 0F"),
        new("critter_give_xp", "83 EC 58 53 55 56 8B 74 24 6C 57"),
        new("critter_kill", "81 EC C0 00 00 00 53 8B"),
        new("curse_add", "83 EC 58 55 56 8B 74 24"),
        new("curse_get_logbook_data", "53 8B 5C 24 0C 55 8B 6C 24 0C 68 4F 01 00 00 53 55 E8 3A"),
        new("curse_remove", "83 EC 4C 53 55 56 8B 74 24 60 57 8B 7C 24 60 68 4F 01 00 00 56 57 E8 E5"),
        new("effect_add", "A0 6C AC 62 00 83 EC 44 A8 01 53 8B"),
        new("effect_count_effects_of_type", "53 8B 5C 24 08 57 8B 7C 24 10 68 4F 01 00 00 57 53 E8 EA"),
        new(
            "effect_remove_all_caused_by",
            "A0 6C AC 62 00 83 EC 40 A8 01 53 8B 5C 24 4C 55 8B 6C 24 4C 56 57 74 4F A8 02 74 42 53 8D 44 24 14 55 50 C7 44 24 34 56 00 00 00 C7 44 24 38 04"
        ),
        new(
            "effect_remove_all_typed",
            "A0 6C AC 62 00 83 EC 40 A8 01 53 8B 5C 24 4C 55 8B 6C 24 4C 56 57 74 4F A8 02 74 42 53 8D 44 24 14 55 50 C7 44 24 34 56 00 00 00 C7 44 24 38 02"
        ),
        new("effect_remove_internal", "83 EC 0C 53 56 8B 74 24 1C 57 8B 7C 24 1C 68"),
        new(
            "effect_remove_one_caused_by",
            "A0 6C AC 62 00 83 EC 40 A8 01 53 8B 5C 24 4C 55 8B 6C 24 4C 56 57 74 4F A8 02 74 42 53 8D 44 24 14 55 50 C7 44 24 34 56 00 00 00 C7 44 24 38 03"
        ),
        new("effect_remove_one_typed", "A0 6C AC 62 00 83 EC 40 A8 01 53 8B 5C 24 4C 55 8B 6C 24 4C 56 57 74 53"),
        new("gamelib_draw", "A1 D8 AD 59 00 81 EC 54"),
        new("gamelib_draw_game", "E8 5B B4 11 00 85 C0 75"),
        new("gamelib_invalidate_rect", "8B 44 24 04 83 EC 10 85 C0 74"),
        new("item_equipped", "8B 44 24 14 53 8B 5C 24 0C"),
        new("item_find_key_ring", "83 EC 0C 8B 44 24 10 53 55 8B"),
        new("item_force_remove", "81 EC 40 01 00 00 33 C0"),
        new("item_get_keys", "53 8B 5C 24 08 55 8B 6C 24 10 68"),
        new("item_insert", "A0 6C AC 62 00 81 EC 3C"),
        new("item_unequipped", "8B 44 24 14 53 55 56 8B 74 24 1C"),
        new("level_recalc", "81 EC 60 08 00 00 53 8B"),
        new("light_draw", "A1 B8 2E 60 00 83 EC 18"),
        new("logbook_add_injury", "53 55 8B 6C 24 18 56 8B 74 24 18 57 8B C6"),
        new("logbook_add_kill", "8B 44 24 0C 53 8B 5C 24 14 55 56"),
        new("logbook_get_kills", "53 56 8B 74 24 10 57 8B 7C 24 10 6A 00 68"),
        new("map_open_in_game", "A1 F0 11 5D 00 81 EC 08"),
        new("obj_array_field_int32_get", "53 8B 5C 24 08 55 56 57 8B 7C 24 18 57 53 E8 3D"),
        new("obj_array_field_int32_set", "53 8B 5C 24 08 55 56 57 8B 7C 24 18 57 53 E8 CD 13"),
        new("obj_array_field_int64_set", "53 8B 5C 24 08 55 56 57 8B 7C 24 18 57 53 E8 5D"),
        new("obj_array_field_length_set", "53 8B 5C 24 08 55 56 57 8B 7C 24 18 57 53 E8 ED"),
        new("obj_array_field_obj_set", "83 EC 18 53 8B 5C 24 20 55 56 57 8B 7C 24 30 57 53 E8 5A"),
        new("obj_array_field_pc_quest_get", "53 8B 5C 24 08 55 56 57 8B 7C 24 18 57 53 E8 3D 0F"),
        new("obj_array_field_pc_quest_set", "53 8B 5C 24 08 55 56 57 8B 7C 24 18 57 53 E8 AD"),
        new("obj_array_field_script_get", "53 8B 5C 24 08 55 56 57 8B 7C 24 18 57 53 E8 CD 0E"),
        new("obj_array_field_script_set", "53 8B 5C 24 08 55 56 57 8B 7C 24 18 57 53 E8 6D 0E"),
        new("obj_array_field_u_int32_set", "53 8B 5C 24 08 55 56 57 8B 7C 24 18 57 53 E8 2D"),
        new("obj_field_handle_set", "83 EC 18 53 8B 5C 24 20 55 56 57 8B 7C 24 30 57 53 E8 EA"),
        new("obj_field_int32_get", "53 8B 5C 24 0C 55 8B 6C 24 0C 56 57 53 55 E8 6D 1A"),
        new("obj_field_int32_set", "53 8B 5C 24 08 55 56 57 8B 7C 24 18 57 53 E8 CD 19"),
        new("obj_field_int64_set", "53 8B 5C 24 0C 55 8B 6C 24 0C 56 57 53 55 E8 FD"),
        new("object_create", "83 EC 18 56 8B 44 24 30"),
        new("object_destroy", "A1 58 2F 5E 00 83 EC 78"),
        new("object_draw", "55 8B EC 81 EC 40 01 00 00 A1"),
        new("object_get_resistance", "83 EC 08 8B 44 24 14 53 55 8B"),
        new("object_hover_draw", "81 EC CC 00 00 00 53 55"),
        new("object_script_execute", "8B 0D 58 2F 5E 00 83 EC"),
        new("prototype_handle_by_object_type", "8B 44 24 04 8B 0D 2C 88"),
        new("prototype_handle_by_proto_number", "8B 44 24 04 83 EC 18 8D"),
        new("quest_get_logbook_data", "B8 00 7D 00 00 E8 96 5A"),
        new("quest_global_state_get", "8B 44 24 04 8B 0D 18 F4 5F 00 8B"),
        new("quest_global_state_set", "A1 18 F4 5F 00 83 EC 0C"),
        new("quest_state_get", "83 EC 10 56 8B 74 24 1C 57 8B 7C 24 1C 68"),
        new("quest_state_set", "53 8B 5C 24 0C 55 8B 6C 24 0C 56 8B 74 24 18 57 56"),
        new("reaction_adj", "83 EC 08 53 55 56 57 E8 B4"),
        new("reputation_add", "83 EC 40 53 8B 5C 24 4C 55 8B 6C 24 4C 56 68"),
        new("reputation_get_logbook_data", "53 8B 5C 24 0C 55 8B 6C 24 0C 68 4F 01 00 00 53 55 E8 5A"),
        new("reputation_remove", "83 EC 48 53 8B 5C 24 54"),
        new("roof_draw", "A1 40 2E 5E 00 81 EC A0"),
        new("rumor_get_logbook_data", "B8 80 3E 00 00 E8 36 53"),
        new("rumor_known_get", "56 8B 74 24 0C 57 8B 7C 24 0C 68 4F 01 00 00 56 57 E8 BA 13"),
        new("rumor_known_set", "83 EC 48 53 55 56 E8 D5"),
        new("rumor_qstate_get", "8B 4C 24 04 56 81 E9 E8"),
        new("rumor_qstate_set", "83 EC 08 56 E8 07 D5 FD"),
        new("script_global_flag_get", "8B 4C 24 04 8B C1 99 83"),
        new("script_global_flag_set", "83 EC 10 56 57 E8 76 DE"),
        new("script_global_var_get", "8B 44 24 04 8B 0D A0 2F"),
        new("script_global_var_set", "83 EC 10 56 57 E8 06 DF"),
        new(
            "script_local_counter_get",
            "8B 4C 24 0C 8B 54 24 08 83 EC 0C 8D 44 24 00 50 8B 44 24 14 51 6A 20 52 50 E8 52"
        ),
        new(
            "script_local_counter_set",
            "83 EC 0C 8D 44 24 00 53 8B 5C 24 14 56 8B 74 24 20 57 8B 7C 24 20 50 56 6A 20 57 53 E8 FF"
        ),
        new(
            "script_local_flag_get",
            "8B 4C 24 0C 8B 54 24 08 83 EC 0C 8D 44 24 00 50 8B 44 24 14 51 6A 20 52 50 E8 12"
        ),
        new(
            "script_local_flag_set",
            "83 EC 0C 8D 44 24 00 53 8B 5C 24 14 56 8B 74 24 20 57 8B 7C 24 20 50 56 6A 20 57 53 E8 BF"
        ),
        new("script_pc_flag_get", "53 8B 5C 24 08 57 8B 7C 24 10 68 4F 01 00 00 57 53 E8 5A"),
        new("script_pc_flag_set", "53 8B 5C 24 0C 55 8B 6C 24 0C 68 4F 01 00 00 53 55 E8 FA"),
        new("script_pc_var_get", "56 8B 74 24 0C 57 8B 7C 24 0C 68 4F 01 00 00 56 57 E8 DA 1E"),
        new("script_pc_var_set", "56 8B 74 24 0C 57 8B 7C 24 0C 68 4F 01 00 00 56 57 E8 9A 1E"),
        new("script_story_state_get", "A1 A4 2F 5E 00 C3 90 90"),
        new("script_story_state_set", "83 EC 10 56 E8 F7 DA 05"),
        new("spell_add", "83 EC 10 53 55 56 57 E8"),
        new("spell_add_end_exclusive", "8B 4C 24 04 56 57 8B 7C"),
        new("spell_college_level_get", "56 8B 74 24 0C 57 8B 7C 24 0C 68 4F 01 00 00 56 57 E8 DA 51"),
        new("spell_college_level_set", "56 8B 74 24 0C 57 8B 7C 24 0C 68 4F 01 00 00 56 57 E8 5A 51"),
        new("spell_remove", "53 55 8B 6C 24 14 B8 67"),
        new("spell_remove_end_exclusive", "8B 44 24 04 8B 04 85 74"),
        new("stat_base_get", "53 8B 5C 24 08 57 8B 7C 24 10 68 4F 01 00 00 57 53 E8 4A"),
        new("stat_base_set", "81 EC A4 00 00 00 53 8B 9C 24 B0 00 00 00 55 8B"),
        new("tech_learn_schematic", "83 EC 20 8B 44 24 30 8B 4C 24 2C"),
        new("teleport_do", "A1 44 18 60 00 85 C0 74 0F"),
        new("text_bubble_draw", "A1 B4 2A 60 00 83 EC 30"),
        new("text_conversation_draw", "A1 48 F5 5F 00 85 C0 75 19"),
        new("text_floater_draw", "A1 EC 28 60 00 83 EC 78"),
        new("tig_video_flip", "A1 14 03 61 00 83 EC 10"),
        new("tig_window_blit_art", "83 EC 10 55 8B 6C 24 18"),
        new("tig_window_compose_dirty_rect", "81 EC 58 02 00 00 53 55"),
        new("tig_window_copy_from_v_buffer", "8B 44 24 04 83 EC 3C 83"),
        new("tig_window_display", "83 EC 2C A1 24 F1 60 00"),
        new("tig_window_invalidate_rect", "A1 24 F1 60 00 83 EC 10"),
        new("tile_draw", "A1 F4 2D 60 00 81 EC 1C"),
        new("time_event_add_delay", "8B 44 24 08 8B 4C 24 04 6A 00 50 51 E8 8F"),
        new("time_event_notify_pc_teleported", "81 EC 08 01 00 00 6A 01"),
        new("ui_show_inven_loot", "A1 78 86 5E 00 85 C0 74"),
        new("ui_spell_add", "A1 A0 86 5E 00 85 C0 74"),
        new("ui_spell_maintain_add", "A1 A4 86 5E 00 85 C0 74"),
        new("ui_spell_maintain_end", "A1 A8 86 5E 00 85 C0 74"),
        new("ui_start_dialog", "A1 40 87 5E 00 85 C0 74"),
        new("update_follower_level", "53 8B 5C 24 08 55 56 57 8B 7C 24 18 6A 11"),
        new("wmap_load_worldmap_info", "81 EC 10 01 00 00 53 55"),
        new("wmap_rnd_encounter_check", "A1 48 C7 64 00 83 EC 08"),
        new("wmap_ui_encounter_start", "68 A0 0F 00 00 6A 00 6A"),
    ];

    private static readonly Dictionary<string, string> s_signaturesByNormalizedKey = s_entries.ToDictionary(
        static entry => CatalogAddressResolver.NormalizeKey(entry.Key),
        static entry => entry.Pattern,
        StringComparer.OrdinalIgnoreCase
    );

    private static readonly string[] s_keys = [.. s_entries.Select(static entry => entry.Key)];

    private readonly record struct SignatureEntry(string Key, string Pattern);
}
