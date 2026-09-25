// SPDX-License-Identifier: GPL-3.0-or-later
/*
 * Copyright 2024-2026 Jiamu Sun <barroit@linux.com>
 */

using System;
using System.Text;
using UnityEngine;

using PugMod;

using mikufish; namespace mikufish {

[Serializable]
public struct spoof {
	public bool __enabled;
	public float hold;
}

public class state {

public static void save(in spoof spoof)
{
	string json = JsonUtility.ToJson(spoof, true);
	byte[] raw = Encoding.UTF8.GetBytes(json);

	API.ConfigFilesystem.Write("mikufish.json", raw);
}

public static spoof load()
{
	if (!API.ConfigFilesystem.FileExists("mikufish.json"))
		return default;

	byte[] buf = API.ConfigFilesystem.Read("mikufish.json");
	string json = Encoding.UTF8.GetString(buf);

	try {
		return JsonUtility.FromJson<spoof>(json);
	} catch (Exception) {
		return default;
	}
}

} /* class state */

}
