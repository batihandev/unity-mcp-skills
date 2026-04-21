# xr recipes

Recipe path rule: `../../recipes/xr/<command>.md`

| Command | File | Description |
|---------|------|-------------|
| `xr_check_setup` | [xr_check_setup.md](xr_check_setup.md) | Validate XR package, rig, managers, and scene prerequisites |
| `xr_setup_rig` | [xr_setup_rig.md](xr_setup_rig.md) | Create XR Origin with camera, camera offset, and controllers |
| `xr_setup_interaction_manager` | [xr_setup_interaction_manager.md](xr_setup_interaction_manager.md) | Add or find XRInteractionManager in the scene |
| `xr_setup_event_system` | [xr_setup_event_system.md](xr_setup_event_system.md) | Replace StandaloneInputModule with XRUIInputModule |
| `xr_get_scene_report` | [xr_get_scene_report.md](xr_get_scene_report.md) | Report all XR components in the scene with counts and paths |
| `xr_add_ray_interactor` | [xr_add_ray_interactor.md](xr_add_ray_interactor.md) | Add XRRayInteractor for remote pointing and ray interaction |
| `xr_add_direct_interactor` | [xr_add_direct_interactor.md](xr_add_direct_interactor.md) | Add XRDirectInteractor for close-range hand grab |
| `xr_add_socket_interactor` | [xr_add_socket_interactor.md](xr_add_socket_interactor.md) | Add XRSocketInteractor for snap-to-slot object placement |
| `xr_list_interactors` | [xr_list_interactors.md](xr_list_interactors.md) | List all XR interactors in the scene |
| `xr_add_grab_interactable` | [xr_add_grab_interactable.md](xr_add_grab_interactable.md) | Make a GameObject grabbable with XRGrabInteractable |
| `xr_add_simple_interactable` | [xr_add_simple_interactable.md](xr_add_simple_interactable.md) | Add hover/select events without grab physics |
| `xr_configure_interactable` | [xr_configure_interactable.md](xr_configure_interactable.md) | Fine-tune properties on an existing XR interactable |
| `xr_list_interactables` | [xr_list_interactables.md](xr_list_interactables.md) | List all XR interactables in the scene |
| `xr_setup_teleportation` | [xr_setup_teleportation.md](xr_setup_teleportation.md) | Add TeleportationProvider to XR Origin |
| `xr_add_teleport_area` | [xr_add_teleport_area.md](xr_add_teleport_area.md) | Mark a surface as a teleport destination |
| `xr_add_teleport_anchor` | [xr_add_teleport_anchor.md](xr_add_teleport_anchor.md) | Create a stationary teleport destination at a fixed position |
| `xr_setup_continuous_move` | [xr_setup_continuous_move.md](xr_setup_continuous_move.md) | Add stick-based continuous locomotion to XR Origin |
| `xr_setup_turn_provider` | [xr_setup_turn_provider.md](xr_setup_turn_provider.md) | Add snap or smooth turn provider to XR Origin |
| `xr_setup_ui_canvas` | [xr_setup_ui_canvas.md](xr_setup_ui_canvas.md) | Convert a Canvas to WorldSpace with TrackedDeviceGraphicRaycaster |
| `xr_configure_haptics` | [xr_configure_haptics.md](xr_configure_haptics.md) | Set hover and select vibration on an XR interactor |
| `xr_add_interaction_event` | [xr_add_interaction_event.md](xr_add_interaction_event.md) | Wire an interaction event to a target MonoBehaviour method |
| `xr_configure_interaction_layers` | [xr_configure_interaction_layers.md](xr_configure_interaction_layers.md) | Set InteractionLayerMask on an interactor or interactable |
