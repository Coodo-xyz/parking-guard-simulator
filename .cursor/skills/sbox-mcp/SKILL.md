---
name: sbox-mcp
description: Drive the s&box editor via the jtc MCP server (48 tools): scene graph, components, assets, files, editor control, C# execution, docs/API search. Use when the user asks to modify scenes, inspect GameObjects, run play mode, read/write project files through s&box, or when s&box MCP is connected.
---

# s&box MCP (jtc.mcp-server)

In-editor MCP server for Parking Guard Simulator. HTTP on `http://localhost:29015/mcp`. Requires s&box editor open with the MCP Server dock active.

## Prerequisites

Before calling any tool:

1. s&box editor is open on `parking_guard_simulator.sbproj`
2. Library `jtc.mcp-server` is installed in `Libraries/jtc.mcp-server/`
3. Dock **Editor → MCP Server** is open and shows **Listening on http://localhost:29015/mcp**
4. Cursor MCP config exists at `.cursor/mcp.json` with server `sbox`

Verify connectivity with `get_server_status` or HTTP health check on `http://localhost:29015/`.

## How to invoke tools

### Preferred: CallMcpTool

```
server: sbox
toolName: <tool_name>
arguments: { ... }
```

### Fallback: HTTP (when CallMcpTool reports server unavailable)

```powershell
$body = '{"jsonrpc":"2.0","id":1,"method":"tools/call","params":{"name":"<tool_name>","arguments":{}}}'
Invoke-WebRequest -Uri "http://localhost:29015/mcp" -Method POST -ContentType "application/json" -Body $body -UseBasicParsing
```

Use `curl.exe` on Windows, not PowerShell `curl` alias.

## Project context

| Item | Value |
|------|-------|
| Startup scene | `scenes/parking_lot.scene` |
| Code root | `Code/` (MCP `file_write` routes `.cs` here automatically) |
| Assets root | `Assets/` |
| Key scene objects | `GameManager`, `Systems`, `Parking Spots`, `Barriers`, `HUD`, `Player Controller` |
| Game components | `Barrier`, `ParkingSpot`, `Vehicle`, `GameManager`, `CashSystem`, `PopularitySystem`, `EventSystem` |

## Workflow patterns

### Read scene state

1. `editor_scene_info` — confirm active scene
2. `scene_get_hierarchy` — readable tree (preferred for overview)
3. `scene_list_objects` — flat list with IDs and positions
4. `scene_find_objects` with pattern `*Barrier*` or `scene_find_by_component` with `Barrier`

### Modify a GameObject

1. Find object ID via `scene_find_objects` or `scene_get_hierarchy`
2. `scene_get_object` or `component_list` to inspect
3. `component_set` / `scene_set_transform` / `component_add` to change
4. `editor_save_scene` to persist

### Attach game logic to scene objects

1. `scene_find_objects` → get `objectId`
2. `component_add` with type `Barrier`, `ParkingSpot`, etc.
3. `component_set` for `[Property]` fields
4. `editor_save_scene`

### Code + scene together

- Edit C# via workspace files OR `file_read` / `file_write` (paths relative to project root, e.g. `Code/Entities/Barrier.cs`)
- After code changes, wait for s&box hot-reload; check `editor_console_output` for compile errors
- Use `editor_play` / `editor_stop` to test

### Cloud assets

1. `asset_search` with query
2. `asset_fetch` for metadata
3. `asset_mount` — auto-adds to `parking_guard_simulator.sbproj` PackageReferences
4. `component_set` with cloud ident on Model/Material fields

### Docs / API lookup

Works even without a scene loaded:

1. `sbox_search_docs` or `sbox_search_api`
2. `sbox_get_doc_page` / `sbox_get_api_type` (use `start_index` for pagination)

## Safety rules

- Call `editor_save_scene` after scene modifications
- Prefer `editor_undo` over manual revert when experimenting
- Use `scene_get_object` before `scene_delete_object`
- Check `editor_is_playing` before scene writes (stop play mode if needed)
- `[Sync]` properties are host-only in multiplayer — do not set via MCP at runtime without understanding network ownership
- `file_write` changes are immediate; prefer reading first with `file_read`

## Tool catalog

Full parameter reference: [tools-reference.md](tools-reference.md)

| Category | Tools |
|----------|-------|
| Scene (12) | `scene_list_objects`, `scene_get_object`, `scene_create_object`, `scene_delete_object`, `scene_clone_object`, `scene_reparent_object`, `scene_set_transform`, `scene_get_hierarchy`, `scene_load`, `scene_find_objects`, `scene_find_by_component`, `scene_find_by_tag` |
| Tags (3) | `tag_add`, `tag_remove`, `tag_list` |
| Components (5) | `component_list`, `component_get`, `component_set`, `component_add`, `component_remove` |
| Assets (4) | `asset_search`, `asset_fetch`, `asset_mount`, `asset_browse_local` |
| Editor (11) | `editor_get_selection`, `editor_select_object`, `editor_undo`, `editor_redo`, `editor_save_scene`, `editor_take_screenshot`, `editor_play`, `editor_stop`, `editor_is_playing`, `editor_scene_info`, `editor_console_output` |
| Files (4) | `file_read`, `file_write`, `file_list`, `project_info` |
| Execution (3) | `execute_csharp`, `console_run`, `get_server_status` |
| Docs (6) | `sbox_search_docs`, `sbox_get_doc_page`, `sbox_list_doc_categories`, `sbox_search_api`, `sbox_get_api_type`, `sbox_cache_status` |

## Common argument formats

- **objectId**: UUID from `scene_list_objects` or `scene_find_objects`
- **position / rotation / scale**: `"x,y,z"` string (e.g. `"0,-380,64"`)
- **componentType**: short name (`Barrier`) or full name (`ParkingGuardSimulator.Barrier`)
- **file path**: project-relative (`Code/GameManager.cs`, `Assets/scenes/parking_lot.scene`)

## Troubleshooting

| Symptom | Fix |
|---------|-----|
| MCP server not found in Cursor | Reload MCP; confirm `.cursor/mcp.json` and dock is listening |
| Tool timeout | s&box editor closed or dock closed — reopen dock |
| Empty scene | Wrong scene loaded — `scene_load` with `scenes/parking_lot.scene` |
| Component not found | Check exact type name via `component_list` |
| Compile errors after `file_write` | Read `editor_console_output`; fix in `Code/` |
