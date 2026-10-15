// Adapted from moorowl/ItemBrowser SearchResults and VirtualObjectList (MIT).
// Copyright (c) 2026 moorowl. See LICENSE in this directory.
// Storage-specific addresses, deterministic sorting and viewport bounds added.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ConnectedStorage.Browser
{
    public enum BrowserLayout { Grid, List }
    public enum BrowserSort { Physical, Name, Amount }

    // Deliberately independent of Unity. No item writes or spawning occur here.
    public sealed class StorageBrowserModel<TAddress>
    {
        public sealed class Entry
        {
            public TAddress Address;
            public string Name;
            public int Amount;
            public bool Empty;
            internal int PhysicalOrder;
        }

        private readonly List<Entry> source = new List<Entry>();
        private readonly List<Entry> visible = new List<Entry>();
        public IReadOnlyList<Entry> Entries => visible;
        public BrowserLayout Layout { get; private set; }
        public BrowserSort Sort { get; private set; }
        public bool Descending { get; private set; }
        public string Search { get; private set; } = "";
        public int FirstRow { get; private set; }
        public int GridColumns { get; }
        public int ViewportRows { get; }
        public int Columns => Layout == BrowserLayout.Grid ? GridColumns : 1;
        public int TotalRows => (visible.Count + Columns - 1) / Columns;

        public StorageBrowserModel(int columns, int rows)
        {
            if(columns < 1 || rows < 1) throw new ArgumentOutOfRangeException();
            GridColumns = columns; ViewportRows = rows;
        }

        public void SetEntries(IEnumerable<Entry> entries)
        {
            source.Clear();
            foreach(var entry in entries)
            {
                // Own the presentation record; never modify the caller's record.
                source.Add(new Entry { Address = entry.Address, Name = entry.Name ?? "",
                    Amount = entry.Amount, Empty = entry.Empty, PhysicalOrder = source.Count });
            }
            Refresh();
        }

        public void Configure(string search, BrowserSort sort, bool descending, BrowserLayout layout)
        {
            Search = search ?? ""; Sort = sort; Descending = descending; Layout = layout;
            FirstRow = 0; Refresh();
        }

        private void Refresh()
        {
            visible.Clear();
            string term = Normalize(Search);
            foreach(var entry in source)
                if(term.Length == 0 || (!entry.Empty && Normalize(entry.Name).Contains(term)))
                    visible.Add(entry);
            if(Sort != BrowserSort.Physical)
                visible.Sort((a,b) => {
                    // Empty insertion targets stay last, including descending mode.
                    if(a.Empty != b.Empty) return a.Empty ? 1 : -1;
                    int comparison = Sort == BrowserSort.Name
                        ? StringComparer.OrdinalIgnoreCase.Compare(a.Name,b.Name)
                        : a.Amount.CompareTo(b.Amount);
                    if(Descending) comparison = -comparison;
                    return comparison != 0 ? comparison : a.PhysicalOrder.CompareTo(b.PhysicalOrder);
                });
            ScrollToRow(FirstRow);
        }

        public void ScrollToRow(int row)
        {
            FirstRow = Math.Max(0,Math.Min(row,Math.Max(0,TotalRows-ViewportRows)));
        }

        public bool TryGetVisible(int slot, out Entry entry)
        {
            int index = FirstRow * Columns + slot;
            if(slot < 0 || slot >= Columns*ViewportRows || index >= visible.Count)
            { entry = null; return false; }
            entry = visible[index]; return true;
        }

        // Item Browser's normalization: case, accents and punctuation don't
        // prevent a match. Avoid a permanent cache of user-entered search text.
        public static string Normalize(string term)
        {
            var sb = new StringBuilder();
            foreach(char character in (term ?? "").ToLowerInvariant().Normalize(NormalizationForm.FormD))
            {
                switch(character)
                {
                    case '\'': case ' ': case '-': case '.': case ',': case '\n': case '"': continue;
                }
                if(CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                    sb.Append(character);
            }
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
