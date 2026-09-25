// SPDX-License-Identifier: GPL-3.0-or-later
/*
 * Copyright 2024-2026 Jiamu Sun <barroit@linux.com>
 */

using PugMod;
using UnityEngine;

using mikufish; namespace mikufish {

public class init : IMod {

public void Init()
{
	var mikufish = ((ModAPIModLoader)API.ModLoader).GetMod("mikufish");
	var assets = mikufish.AssetBundles[0];

	var prefab = assets.LoadAsset<GameObject>("Assets/mikufish/switch.prefab");
	var parent = ((InventoryUI)Manager.ui.playerInventoryUI).transform.GetChild(0);
	var btn = Object.Instantiate(prefab, parent, false);

	/*
	 * base: -7.625 -2.375
	 * per icon
	 *   size: 1
	 *   gap:  0.0625
	 */
	var pos = new Vector3(-8.6875f, -2.375f, 0);

	btn.transform.localPosition = pos;
	btn.SetActive(true);
}

public void Update() {}

public void EarlyInit() {}

public void ModObjectLoaded(Object _) {}

public void Shutdown() {}

} /* class mikufish */

}
