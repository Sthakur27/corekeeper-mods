// Steam Workshop uploader for Sid's Core Keeper mods.
//
// Uses the game's own Facepunch.Steamworks + steam_api64 against the Steam client that is already
// running and logged in (no password involved). Build + run through release/workshop_publish.py.
//
//   dotnet WorkshopUploader.dll <items.json> <ids.json>
//
// items.json: [{ "key", "title", "description", "content", "preview", "gallery": [...], "tags": [...], "changelog",
//               "dependencies": ["<workshop id>" or "<key of another item in ids.json>"] }]
// "{overhaul}" in a description is replaced with the SidsOverhaul item id.
// ids.json:   { "<key>": <published file id> }  (read and updated; existing ids are updated in place)
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Steamworks;
using Steamworks.Data;
using Steamworks.Ugc;

public static class WorkshopUploader
{
    private const uint CoreKeeperAppId = 1621690;

    private sealed class WorkItem
    {
        public string key { get; set; }
        public string title { get; set; }
        public string description { get; set; }
        public string content { get; set; }
        public string preview { get; set; }
        public string[] tags { get; set; }
        public string changelog { get; set; }
        public string[] dependencies { get; set; }
        public string[] removeDependencies { get; set; }
        /// <summary>A Steam collection: no content, "dependencies" are its member items.</summary>
        public bool collection { get; set; }
        /// <summary>Update an existing item's cover without changing its metadata or gameplay files.</summary>
        public bool previewOnly { get; set; }
        /// <summary>Extra images under the cover; replaces the item's whole gallery. Null = leave it alone.</summary>
        public string[] gallery { get; set; }
    }

    /// <summary>
    /// Replaces an item's additional preview images. Facepunch's Editor has no API for these, so this
    /// drives the same internal ISteamUGC calls it uses (StartItemUpdate, RemoveItemPreview,
    /// AddItemPreviewFile, SubmitItemUpdate) through reflection.
    /// </summary>
    private static async Task<bool> SetGallery(PublishedFileId fileId, string[] files)
    {
        int existing = 0;
        ResultPage? page = await Query.All.WithFileId(fileId).WithAdditionalPreviews(true).GetPageAsync(1);
        if (page.HasValue)
        {
            foreach (Item entry in page.Value.Entries) existing = entry.AdditionalPreviews?.Length ?? 0;
            page.Value.Dispose();
        }

        const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        object ugc = typeof(SteamUGC).GetProperty("Internal", Any).GetValue(null);
        Type t = ugc.GetType();
        AppId app = CoreKeeperAppId;
        object handle = t.GetMethod("StartItemUpdate", Any).Invoke(ugc, new object[] { app, fileId });
        MethodInfo remove = t.GetMethod("RemoveItemPreview", Any);
        for (uint i = (uint)existing; i-- > 0;)
            remove.Invoke(ugc, new object[] { handle, i });
        MethodInfo add = t.GetMethod("AddItemPreviewFile", Any);
        foreach (string f in files)
        {
            if (!(bool)add.Invoke(ugc, new object[] { handle, Path.GetFullPath(f), ItemPreviewType.Image }))
            {
                Console.WriteLine($"  gallery: Steam refused {f}");
                return false;
            }
        }
        object call = t.GetMethod("SubmitItemUpdate", Any).Invoke(ugc, new object[] { handle, "Gallery images" });
        Type ct = call.GetType();
        var deadline = DateTime.UtcNow.AddMinutes(5);
        while (!(bool)ct.GetProperty("IsCompleted", Any).GetValue(call))
        {
            if (DateTime.UtcNow > deadline) return false;
            await Task.Delay(200);
        }
        object res = ct.GetMethod("GetResult", Any).Invoke(call, null);
        if (res == null) return false;
        var r = (Result)res.GetType().GetField("Result", Any).GetValue(res);
        if (r != Result.OK) Console.WriteLine($"  gallery submit: {r}");
        return r == Result.OK;
    }

    public static int Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("usage: WorkshopUploader <items.json> <ids.json>");
            return 2;
        }
        return Run(args[0], args[1]).GetAwaiter().GetResult();
    }

    private static async Task<int> Run(string itemsPath, string idsPath)
    {
        var items = JsonSerializer.Deserialize<List<WorkItem>>(File.ReadAllText(itemsPath));
        var ids = File.Exists(idsPath)
            ? JsonSerializer.Deserialize<Dictionary<string, ulong>>(File.ReadAllText(idsPath))
            : new Dictionary<string, ulong>();

        try
        {
            SteamClient.Init(CoreKeeperAppId, true);
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"Steam init failed (is Steam running and logged in?): {e.Message}");
            return 1;
        }
        Console.WriteLine($"Steam user: {SteamClient.Name} ({SteamClient.SteamId})");

        int failures = 0;
        bool needsAgreement = false;
        foreach (var item in items)
        {
            bool isNew = !ids.TryGetValue(item.key, out ulong existing);
            if (item.previewOnly && (isNew || string.IsNullOrEmpty(item.preview)))
            {
                Console.Error.WriteLine($"Cannot update artwork for {item.key}: existing item and preview required.");
                failures++;
                continue;
            }
            if (isNew && !item.collection && string.IsNullOrEmpty(item.content))
            {
                Console.WriteLine($"Skipping {item.key}: not on the Workshop yet and no content to upload (description-only run).");
                continue;
            }
            Editor editor = !isNew ? new Editor(new PublishedFileId { Value = existing })
                : item.collection ? Editor.NewCollection : Editor.NewCommunityFile;
            editor = editor.ForAppId(CoreKeeperAppId);
            string description = (item.description ?? "")
                .Replace("{overhaul}", ids.TryGetValue("SidsOverhaul", out ulong oh) ? oh.ToString() : "")
                .Replace("{collection}", ids.TryGetValue("Collection", out ulong col) ? col.ToString() : "");
            if (!item.previewOnly) editor = editor
                .WithTitle(item.title)
                .WithDescription(description)
                .WithPublicVisibility()
                .WithChangeLog(item.changelog ?? "");
            // No content folder = keep the item's current files (collections, description-only updates).
            if (!item.previewOnly && !string.IsNullOrEmpty(item.content)) editor = editor.WithContent(Path.GetFullPath(item.content));
            if (!string.IsNullOrEmpty(item.preview)) editor = editor.WithPreviewFile(Path.GetFullPath(item.preview));
            if (!item.previewOnly)
                foreach (var tag in item.tags ?? Array.Empty<string>()) editor = editor.WithTag(tag);

            Console.WriteLine($"{(isNew ? "Creating" : "Updating")} {item.key} ...");
            PublishResult result = await editor.SubmitAsync(null, created =>
            {
                // Save the id as soon as the item exists, so a failed upload is retried as an update.
                if (created.FileId.Value != 0)
                {
                    ids[item.key] = created.FileId.Value;
                    File.WriteAllText(idsPath, JsonSerializer.Serialize(ids, new JsonSerializerOptions { WriteIndented = true }));
                }
            });

            if (result.NeedsWorkshopAgreement) needsAgreement = true;
            if (result.Success)
            {
                ids[item.key] = result.FileId.Value;
                File.WriteAllText(idsPath, JsonSerializer.Serialize(ids, new JsonSerializerOptions { WriteIndented = true }));
                Console.WriteLine($"  OK https://steamcommunity.com/sharedfiles/filedetails/?id={result.FileId.Value}");
                if (item.gallery != null)
                {
                    bool ok = await SetGallery(result.FileId, item.gallery);
                    Console.WriteLine($"  gallery ({item.gallery.Length} images): {(ok ? "ok" : "FAILED")}");
                    if (!ok) failures++;
                }
                if (item.previewOnly) continue;
                foreach (string old in item.removeDependencies ?? Array.Empty<string>())
                {
                    if (!ulong.TryParse(old, out ulong oldId)) continue;
                    bool removed = await new Item(result.FileId).RemoveDependency(new PublishedFileId { Value = oldId });
                    if (removed) Console.WriteLine($"  removed old required item {oldId}");
                }
                foreach (string depRef in item.dependencies ?? Array.Empty<string>())
                {
                    // Steam's "Required items" list; re-adding an existing one is harmless.
                    if (!ulong.TryParse(depRef, out ulong dep) && !ids.TryGetValue(depRef, out dep))
                    {
                        Console.WriteLine($"  required item {depRef}: no workshop id yet, skipped");
                        continue;
                    }
                    bool ok = await new Item(result.FileId).AddDependency(new PublishedFileId { Value = dep });
                    Console.WriteLine($"  {(item.collection ? "member" : "required item")} {depRef} ({dep}): {(ok ? "ok" : "not added (maybe already listed)")}");
                }
            }
            else
            {
                failures++;
                Console.WriteLine($"  FAILED: {result.Result}");
            }
        }

        if (needsAgreement)
            Console.WriteLine("NEEDS_AGREEMENT: accept the Steam Workshop legal agreement at https://steamcommunity.com/sharedfiles/workshoplegalagreement (items stay hidden until then).");
        SteamClient.Shutdown();
        Console.WriteLine($"Done: {items.Count - failures}/{items.Count} uploaded.");
        return failures == 0 ? 0 : 1;
    }
}
