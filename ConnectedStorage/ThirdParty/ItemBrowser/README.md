# Item Browser UI migration source

Upstream: https://github.com/moorowl/ItemBrowser

Pinned revision: `9b9df228428f212681873e875409fc33003cb3f7`

Copyright (c) 2026 moorowl. MIT license; see LICENSE.

`Upstream/` preserves selected unmodified source, the authored browser prefab,
and the classic UI atlas, including its sprite slicing metadata. These are
development inputs, not a complete Unity package or runtime mod. The prefab has
additional dependencies that must be resolved during the port. Do not add these
scripts directly to the mod manifest: they depend on Item Browser's catalogue,
options, localization, and other UI classes.

The new storage browser will adapt the authored UI and virtual list instead of
continuing the runtime-drawn header from the native-chest prototype. Search,
grid/list switching, sort selection and ascending/descending order belong to
this browser. Real storage addresses must survive filtering and sorting; an
entry is a unit entity plus its physical slot, not just an item type.

`StorageBrowserModel.cs` is the first adapted layer. It retains the upstream
search normalization and viewport indexing approach, replacing catalogue-only
entries with stable generic addresses and deterministic sorting. It is not yet
connected to the shipped UI. This separation allows the new visual layer to be
developed without repairing the superseded screen's deposit route.

Do not include spawning, recipes, discoveries, catalogue tabs, or Item Browser's
global options in the storage interface. No changes are made to the installed
Item Browser mod. Keep its license and source attribution with adapted code.

Checkpoint before migration: `5b458ea`. The installed build is still that
prototype; the new browser is not yet ready for live testing.

Migration update: the runtime implementation is now in Scripts/StorageBrowserUI.cs and Scripts/StorageBrowserModel.cs. Unity/build_browser.py exports an independent visual prefab, stripping upstream catalogue behaviours. See Tests/BROWSER-UI-2026-10-14.md for the first-pass controls and remaining live verification. The earlier notes above describe the initial source checkpoint.
