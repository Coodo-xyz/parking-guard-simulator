# s&box MCP Tools Reference

Package: `jtc.mcp-server` — 48 tools total.

## Scene graph

### scene_list_objects
List all GameObjects in the current scene (flat array with id, name, enabled, parentId, childCount, position).

### scene_get_object
| Param | Required | Description |
|-------|----------|-------------|
| objectId | yes | GameObject UUID |

Returns components and properties.

### scene_create_object
| Param | Required | Description |
|-------|----------|-------------|
| name | yes | GameObject name |
| parentId | no | Parent UUID |
| position | no | `"x,y,z"` |

### scene_delete_object
| Param | Required |
|-------|----------|
| objectId | yes |

### scene_clone_object
| Param | Required |
|-------|----------|
| objectId | yes |

### scene_reparent_object
| Param | Required |
|-------|----------|
| objectId | yes |
| parentId | yes |

### scene_set_transform
| Param | Required | Description |
|-------|----------|-------------|
| objectId | yes | |
| position | no | `"x,y,z"` |
| rotation | no | `"x,y,z"` |
| scale | no | `"x,y,z"` |

### scene_get_hierarchy
No params. Returns indented text tree.

### scene_load
| Param | Required | Description |
|-------|----------|-------------|
| path | yes | e.g. `scenes/parking_lot.scene` |

### scene_find_objects
| Param | Required | Description |
|-------|----------|-------------|
| query | yes | Name pattern, supports `*` wildcards |

### scene_find_by_component
| Param | Required | Description |
|-------|----------|-------------|
| componentType | yes | e.g. `Barrier`, `ParkingSpot` |

### scene_find_by_tag
| Param | Required |
|-------|----------|
| tag | yes |

## Tags

### tag_add
| Param | Required |
|-------|----------|
| objectId | yes |
| tag | yes |

### tag_remove
| Param | Required |
|-------|----------|
| objectId | yes |
| tag | yes |

### tag_list
| Param | Required |
|-------|----------|
| objectId | yes |

## Components

### component_list
| Param | Required |
|-------|----------|
| objectId | yes |

### component_get
| Param | Required |
|-------|----------|
| objectId | yes |
| componentType | yes |

### component_set
| Param | Required | Description |
|-------|----------|-------------|
| objectId | yes | |
| componentType | yes | |
| property | yes | Property name |
| value | yes | String; supports Model, Material, Color, Vector3, Angles, cloud idents |

### component_add
| Param | Required |
|-------|----------|
| objectId | yes |
| componentType | yes |

### component_remove
| Param | Required |
|-------|----------|
| objectId | yes |
| componentType | yes |

## Assets

### asset_search
| Param | Required | Description |
|-------|----------|-------------|
| query | yes | Search string |
| type | no | Asset type filter |
| amount | no | Max results |

### asset_fetch
| Param | Required | Description |
|-------|----------|-------------|
| ident | yes | Package ident e.g. `facepunch.parking_barrier` |

### asset_mount
| Param | Required | Description |
|-------|----------|-------------|
| ident | yes | Mounts and adds to `.sbproj` PackageReferences |

### asset_browse_local
| Param | Required | Description |
|-------|----------|-------------|
| directory | no | Project-relative directory |
| extension | no | e.g. `vmdl`, `vmat` |

## Editor

### editor_get_selection
No params.

### editor_select_object
| Param | Required |
|-------|----------|
| objectId | yes |

### editor_undo / editor_redo
No params.

### editor_save_scene
No params. Always call after scene edits.

### editor_take_screenshot
| Param | Required | Default |
|-------|----------|---------|
| width | no | 1920 |
| height | no | 1080 |
| path | no | auto |

### editor_play / editor_stop
No params.

### editor_is_playing
No params. Returns boolean.

### editor_scene_info
No params. Name, path, dirty state.

### editor_console_output
No params. Recent log lines.

## Files

### file_read
| Param | Required | Description |
|-------|----------|-------------|
| path | yes | Project-relative path |

### file_write
| Param | Required | Description |
|-------|----------|-------------|
| path | yes | `.cs` files auto-route to `Code/` |
| content | yes | Full file content |

### file_list
| Param | Required | Description |
|-------|----------|-------------|
| directory | no | Default: project root |
| pattern | no | Glob e.g. `*.cs` |

### project_info
No params. Project metadata.

## Execution

### execute_csharp
| Param | Required | Description |
|-------|----------|-------------|
| code | yes | C# expression or block |
| imports | no | Comma-separated namespaces |

### console_run
| Param | Required | Description |
|-------|----------|-------------|
| command | yes | s&box console command |

### get_server_status
No params. Listener URL, request count, clients.

## Docs & API

### sbox_search_docs
| Param | Required | Default |
|-------|----------|---------|
| query | yes | |
| limit | no | 10 (1–25) |
| category | no | e.g. `Systems`, `Scenes` |

### sbox_get_doc_page
| Param | Required | Default |
|-------|----------|---------|
| url | yes | docs.facepunch.com URL |
| startIndex | no | 0 |
| maxLength | no | 5000 (100–20000) |

### sbox_list_doc_categories
No params.

### sbox_search_api
| Param | Required | Default |
|-------|----------|---------|
| query | yes | Type/method/keyword |
| limit | no | 8 (1–20) |

### sbox_get_api_type
| Param | Required | Default |
|-------|----------|---------|
| name | yes | e.g. `Component`, `Sandbox.Component` |
| startIndex | no | 0 |
| maxLength | no | 5000 |

### sbox_cache_status
No params. Docs/API index status.

## Example calls

**List scene hierarchy:**
```json
{ "name": "scene_get_hierarchy", "arguments": {} }
```

**Find barriers:**
```json
{ "name": "scene_find_objects", "arguments": { "query": "*Barrier*" } }
```

**Add Barrier component:**
```json
{
  "name": "component_add",
  "arguments": { "objectId": "<uuid>", "componentType": "Barrier" }
}
```

**Read game code:**
```json
{ "name": "file_read", "arguments": { "path": "Code/Entities/Barrier.cs" } }
```

**Search s&box networking docs:**
```json
{ "name": "sbox_search_docs", "arguments": { "query": "Sync Rpc networking" } }
```
