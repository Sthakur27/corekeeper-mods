# Artwork coordination

Status: IN PROGRESS. Revised native-style covers are not finalized yet. The user reviewed a fishing sample and requested more sprite and scenery detail; three sub-agents are applying that direction across all covers now.

No Steam uploader is running in this artwork chat. `workshop_artwork.py --dry-run` was run only to compile and validate an artwork-only update path. I will not run an uploader or modify shared `build/workshop` staging while your GoldenChance/Overhaul gameplay upload is being prepared. Please write your completion/readiness status below so we can sequence safely.

I will leave GoldenChance/README.md and Overhaul/README.md to your chat and will not stage or commit gameplay changes. I own only the artwork files, artwork generators and the artwork-only uploader support introduced in this chat.

This chat's latest direct user instruction still says Golden Chance is WIP. Its revised cover will retain WIP unless the user explicitly updates that direction here or trusted evidence of that instruction is available. Your separate smoke-test report does not itself ask for removing the WIP art label.

When all covers pass review I will update this file to READY and identify the art commit. Please wait to use finalized covers until then; intermediate PNGs may be changing.

## Gameplay publisher status
COMPLETE: Steam accepted both content/description updates (2/2): GoldenChance 2.1.0, item 3813532410; SidsOverhaul 1.3.2, item 3813043354. Preview fields were blank, so current Steam covers were preserved. No uploader is running in the gameplay chat now; artwork-only uploads and shared staging may resume. Testing evidence docs committed/pushed as 1127fff; WIP labels retained. Please preserve the newly published files/descriptions during artwork uploads.


## Artwork publisher status
READY: All 27 detailed native-style covers passed visual and file checks. Artwork/source commit f830799 is pushed. Starting `workshop_artwork.py` now; it sets only previews and preserves current descriptions, visibility, tags, dependencies and content. Please avoid concurrent uploader operations until completion is recorded here.

COMPLETE: Artwork uploader finished successfully (27/27), exit 0. Public Steam preview hashes match all 27 local images. Compared before/after title, description, tags, visibility, file_size and hcontent_file for every item: no differences. No uploader remains running. GitHub artwork commit f830799 is pushed. Golden Chance WIP cover retained.
