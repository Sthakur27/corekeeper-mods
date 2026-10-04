// Steam Workshop uploader for Sid's Core Keeper mods.
//
// Uses the game's own Facepunch.Steamworks + steam_api64 against the Steam client that is already
// running and logged in (no password involved). Build + run through release/workshop_publish.py.
//
//   dotnet WorkshopUploader.dll <items.json> <ids.json>
//
// items.json: [{ "key", "title", "description", "content", "preview", "tags": [...], "changelog",
//               "dependencies": ["<workshop id>" or "<key of another item in ids.json>"] }]
// "{overhaul}" in a description is replaced with the SidsOverhaul item id.
// ids.json:   { "<key>": <published file id> }  (read and updated; existing ids are updated in place)
using System;
using System.Collections.Generic;
using System.IO;
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
            Editor editor = isNew ? Editor.NewCommunityFile : new Editor(new PublishedFileId { Value = existing });
            editor = editor
                .ForAppId(CoreKeeperAppId)
                .WithTitle(item.title)
                .WithDescription(item.description.Replace("{overhaul}", ids.TryGetValue("SidsOverhaul", out ulong oh) ? oh.ToString() : ""))
                .WithContent(Path.GetFullPath(item.content))
                .WithPublicVisibility()
                .WithChangeLog(item.changelog ?? "");
            if (!string.IsNullOrEmpty(item.preview)) editor = editor.WithPreviewFile(Path.GetFullPath(item.preview));
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
                    Console.WriteLine($"  required item {depRef} ({dep}): {(ok ? "ok" : "not added (maybe already listed)")}");
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
