"""Publish/update Sid's Core Keeper mods on mod.io via the REST API.

Usage:
  python publish.py create <ModFolder> <logo.png> <version> [dep_id ...]   # new mod page + file + tags
  python publish.py file   <mod_id> <ModFolder> <version> "<changelog>"     # upload new file version
  python publish.py setup  <mod_id> <ModFolder> <version> [dep_id ...]      # page made in browser: desc+tags+deps+file
  python publish.py show   <mod_id>
NOTE: personal access tokens get 403 on POST /games/5289/mods (create). Create the page in the
browser (name, summary, logo, tags, Public), then run `setup`. Non-file writes must be urlencoded.

Reads the OAuth token from C:\\Users\\Sid\\CoreKeeperMods\\.modio_token. Summary = README first
paragraph (<=250 chars); description = README converted to simple HTML. Tags: 1.3.0 / Quality of Life /
Client / Server / Script. Zips are built in release/ with ModManifest.json at the zip root.
"""
import sys, os, json, re, zipfile, subprocess, html

ROOT = r"C:\Users\Sid\CoreKeeperMods"
API = "https://g-5289.modapi.io/v1"; GAME = 5289
TOK = open(os.path.join(ROOT, ".modio_token")).read().strip()
TAGS = ["1.3.0", "Quality of Life", "Client", "Server", "Script"]

def call(method, path, fields=None, files=None):
    cmd = ["curl", "-s", "-w", "\n%{http_code}", "-X", method, f"{API}{path}",
           "-H", f"Authorization: Bearer {TOK}", "-H", "Accept: application/json"]
    import tempfile
    tmpdir = tempfile.mkdtemp()
    if files:
        # multipart (only endpoints that take a file accept it)
        for i, (k, v) in enumerate((fields or {}).items()):
            fp = os.path.join(tmpdir, f"f{i}.txt")
            open(fp, "w", encoding="utf-8", newline="").write(str(v))
            cmd += ["-F", f"{k}=<{fp}"]   # '<' = read value from file; safe for ; " and newlines
        for k, p in files.items():
            cmd += ["-F", f"{k}=@{p}"]
    elif fields:
        # every non-file write must be application/x-www-form-urlencoded
        cmd += ["-H", "Content-Type: application/x-www-form-urlencoded"]
        for i, (k, v) in enumerate(fields.items()):
            fp = os.path.join(tmpdir, f"f{i}.txt")
            open(fp, "w", encoding="utf-8", newline="").write(str(v))
            cmd += ["--data-urlencode", f"{k}@{fp}"]
    out = subprocess.run(cmd, capture_output=True, text=True, encoding="utf-8").stdout
    body, _, code = out.rpartition("\n")
    try:
        d = json.loads(body)
    except Exception:
        raise SystemExit(f"HTTP {code} non-JSON reply for {method} {path}: {body[:300]}")
    if isinstance(d, dict) and "error" in d:
        raise SystemExit(f"{method} {path} -> {d['error']}")
    return d

def build_zip(folder, version):
    name = os.path.basename(folder)
    out = os.path.join(ROOT, "release", f"{name}-{version}.zip")
    with zipfile.ZipFile(out, "w", zipfile.ZIP_DEFLATED) as z:
        for r, dirs, fs in os.walk(folder):
            dirs[:] = [d for d in dirs if d not in ("release", ".git", "__pycache__")]
            for f in fs:
                if f.endswith((".pugbackup", ".patch.md")): continue
                p = os.path.join(r, f); z.write(p, os.path.relpath(p, folder))
    return out

def readme_parts(folder):
    md = open(os.path.join(folder, "README.md"), encoding="utf-8-sig").read().replace("\r\n", "\n")
    lines = md.split("\n")
    title = re.sub(r"\s*\(.*?\)\s*$", "", lines[0].lstrip("# ").strip())
    body = "\n".join(lines[1:]).strip()
    paras = [p.strip() for p in re.split(r"\n\s*\n", body)
             if p.strip() and not p.strip().startswith(("![", "#", "|"))]
    first = re.sub(r"\[([^\]]+)\]\([^)]+\)", r"\1", re.sub(r"[*`_]", "", paras[0]))
    first = re.sub(r"\s+", " ", first)
    summary = first if len(first) <= 250 else first[:247].rsplit(" ", 1)[0] + "..."
    return title, summary, to_html(body)

LIST_RE = re.compile(r"^\s*([-*]|\d+\.)\s+")

def to_html(md):
    """README Markdown -> the HTML mod.io renders: headings, paragraphs (hard-wrapped lines joined), tables,
    bullet/numbered lists, code blocks, bold/italic/code/links. Hero images are dropped (mod.io shows the
    logo). Mirrors workshop_publish.to_bbcode; after changing either, look at a live page with a table."""
    lines, out, para, i = md.split("\n"), [], [], 0

    def flush():
        if para:
            out.append("<p>" + inline(" ".join(l.strip() for l in para)) + "</p>")
            para.clear()

    while i < len(lines):
        line = lines[i]
        if not line.strip() or re.match(r"^!\[[^\]]*\]\([^)]*\)$", line.strip()):
            flush(); i += 1; continue
        if line.startswith("```"):
            flush(); block = []; i += 1
            while i < len(lines) and not lines[i].startswith("```"):
                block.append(html.escape(lines[i])); i += 1
            out.append("<pre><code>" + "\n".join(block) + "</code></pre>"); i += 1; continue
        m = re.match(r"^(#{1,6})\s+(.*)", line)
        if m:
            flush(); n = min(len(m.group(1)) + 1, 4)
            out.append(f"<h{n}>{inline(m.group(2).strip())}</h{n}>"); i += 1; continue
        if line.lstrip().startswith("|"):
            flush(); rows = []
            while i < len(lines) and lines[i].lstrip().startswith("|"):
                cells = [c.strip() for c in lines[i].strip().strip("|").split("|")]
                if not all(re.fullmatch(r":?-{2,}:?", c) for c in cells):
                    rows.append(cells)
                i += 1
            t = "".join("<tr>" + "".join(f"<{'th' if r == 0 else 'td'}>{inline(c)}</{'th' if r == 0 else 'td'}>"
                                         for c in cells) + "</tr>" for r, cells in enumerate(rows))
            out.append(f"<table>{t}</table>"); continue
        if LIST_RE.match(line):
            flush(); ordered = bool(re.match(r"^\s*\d+\.", line)); items = []
            while i < len(lines) and (LIST_RE.match(lines[i])
                                      or (items and lines[i][:1] in (" ", "\t") and lines[i].strip())):
                if LIST_RE.match(lines[i]):
                    items.append(LIST_RE.sub("", lines[i]))
                else:
                    items[-1] += " " + lines[i].strip()
                i += 1
            tag = "ol" if ordered else "ul"
            out.append(f"<{tag}>" + "".join(f"<li>{inline(x)}</li>" for x in items) + f"</{tag}>"); continue
        para.append(line); i += 1
    flush()
    # mod.io's Cloudflare WAF rejects a PUT containing "python -I <path>" (HTTP 403 HTML page); the
    # developer commands still read fine without the flag (Solarite Drill, Potion of Return, 2026-11-12).
    return "\n".join(out).replace("python -I ", "python ")

def inline(s):
    s = html.escape(s, quote=False).replace("\\*", "&#42;")
    s = re.sub(r"`([^`]+)`", r"<code>\1</code>", s)
    s = re.sub(r"\*\*(.+?)\*\*", r"<strong>\1</strong>", s)
    s = re.sub(r"(?<![\w*])\*(?!\s)(.+?)(?<!\s)\*(?![\w*])", r"<em>\1</em>", s)
    s = re.sub(r"\[([^\]]+)\]\((https?://[^)\s]+)\)", r'<a href="\2">\1</a>', s)
    s = re.sub(r"\[([^\]]+)\]\(([^)]+)\)", r"\1", s)  # relative links: keep the text
    return s

def setup(mid, folder, version, deps, changelog="Initial release."):
    """Finish a page created in the browser: description, tags, dependencies, file."""
    folder = os.path.join(ROOT, folder) if not os.path.isabs(folder) else folder
    title, summary, desc = readme_parts(folder)
    # community_options 132097 = comments + allow collections (pages made in the browser start at 0, and
    # mod.io refuses to put them in a collection). Collections also reject mods with mod.io dependencies.
    call("PUT", f"/games/{GAME}/mods/{mid}", {"summary": summary, "description": desc, "visible": "1",
                                             "community_options": "132097"})
    call("POST", f"/games/{GAME}/mods/{mid}/tags", {f"tags[{i}]": t for i, t in enumerate(TAGS)})
    if deps:
        call("POST", f"/games/{GAME}/mods/{mid}/dependencies", {f"dependencies[{i}]": d for i, d in enumerate(deps)})
    upload(mid, folder, version, changelog)

def create(folder, logo, version, deps):
    folder = os.path.join(ROOT, folder) if not os.path.isabs(folder) else folder
    title, summary, desc = readme_parts(folder)
    mod = call("POST", f"/games/{GAME}/mods",
               {"name": title, "summary": summary, "description": desc, "visible": "1"},
               {"logo": logo})
    mid = mod["id"]; print("created", mid, mod["name"], mod.get("profile_url"))
    call("POST", f"/games/{GAME}/mods/{mid}/tags", {f"tags[{i}]": t for i, t in enumerate(TAGS)})
    if deps:
        call("POST", f"/games/{GAME}/mods/{mid}/dependencies", {f"dependencies[{i}]": d for i, d in enumerate(deps)})
    upload(mid, folder, version, "Initial release.")
    return mid

def upload(mid, folder, version, changelog):
    folder = os.path.join(ROOT, folder) if not os.path.isabs(folder) else folder
    z = build_zip(folder, version)
    f = call("POST", f"/games/{GAME}/mods/{mid}/files",
             {"version": version, "changelog": changelog, "active": "1"}, {"filedata": z})
    print("file", f["id"], f["version"], f["filesize"], "bytes, virus:", f.get("virus_status"))
    show(mid)

def show(mid):
    m = call("GET", f"/games/{GAME}/mods/{mid}")
    print(f"  {m['id']} {m['name']!r} status={m['status']} visible={m['visible']} "
          f"file={ (m.get('modfile') or {}).get('version') } tags={[t['name'] for t in m['tags']]} url={m['profile_url']}")

if __name__ == "__main__":
    a = sys.argv[1:]
    if a[0] == "setup": setup(int(a[1]), a[2], a[3], [int(x) for x in a[4:]])
    elif a[0] == "create": create(a[1], a[2], a[3], [int(x) for x in a[4:]])
    elif a[0] == "file": upload(int(a[1]), a[2], a[3], a[4])
    elif a[0] == "show": show(int(a[1]))
