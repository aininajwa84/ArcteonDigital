# Transparent logo assets

Arcteon Nexus: added a simple text wordmark (`arcteon-nexus-wordmark.svg` and its light banner variant) based on the product name on the supplied presentation cover. It is a website typography treatment rather than a claim to an official supplied logo. Its detail page covers all five modules from the source PDF and offers the unchanged PDF and a rendered cover preview. ArcteonMed replaces Klinik Noble Medic in the shared product catalogue while retaining the `clinic` URL key and existing supplied logo and brochure.

Card refinement: SEP's card symbol is now 126x94 rather than 146x110. Card body padding, heading reserve and description/action gaps are reduced while retaining the existing grid and aligned action rows. Tender is displayed at 70x70 and RappoVena at 138x48 to reduce enlargement of the available small originals. No higher-resolution source was found in the supplied attachments, local Downloads filename search, or public web search; the client logos remain source-limited and have not been represented as HD replacements.

ByondQ replaces the former QMS placeholder name and logo. `byondq-logo.png` is a transparent crop of the supplied banking brochure's original wordmark; `byondq-logo-light.png` changes neutral lettering to white for the dark detail banner while retaining the orange accent. Shared catalogue data updates the product card, sidebar, breadcrumb, heading and enquiry selection together. SEP copy now describes software licensing and computer management.

Current product-card cleanup: following approval to recreate the simple marks, WorkVibes now uses `workvibes-symbol.svg` (gradient square and white clock) and QMS uses `qms-wordmark.svg` (clean grey lettering). These are code-native vector approximations of the supplied small artwork, not recovered original vector masters. Both remain sharp at any display size. Tender and RappoVena now use individual, transparent crops from `product-reference.png` rather than rectangular sprite backgrounds. Card logo sizes are optically balanced in `glass.css`; product titles and the four-product/two-client grouping are retained. Earlier raster-export notes below describe superseded attempts.

WorkVibes now displays `products/workvibes-symbol-hd.png` (506x516), prepared with `scripts/prepare-workvibes-logo.py`: direct source symbol crop, Lanczos enlargement and mild unsharp masking, with transparent outer padding. Embedded caption remains excluded. This is best-effort raster enhancement of a 49x50 source, not recovered HD detail or a vector redraw. Display size is unchanged.

SEP replacement: product cards and the details banner now use `products/sep-symbol-transparent.png` (518x390 RGBA). It is cropped from the supplied `products/sep-logo-dark.png` (1774x887) using the symbol region (620,130)-(1160,530), trimmed to visible bounds and padded by eight transparent pixels. Original symbol pixels are preserved. Both embedded text lines and the dark card backing are removed. Page product titles remain unchanged. The older screenshot-derived SEP export is no longer displayed.

Header/footer cleanup: `arcteon-header-footer.png` now uses the larger original tagline artwork (1258x432 trimmed export), with a consistent #FF8A24 orange and colour-derived alpha to remove the white matte/outline. The faint A remains translucent. Regenerate with `scripts/prepare-clean-brand.py` using Pillow. The screenshot with a baked checkerboard is retained only as `arcteon-header-footer-supplied.png`; it is not served in the header/footer. CSS sizes and effects remain unchanged.

Final processing: original pixel-based background removal, explicitly approved by the user. No generated logo artwork is deployed. Original foreground RGB colours are preserved. White backgrounds become alpha transparency; blank margins are trimmed and four transparent pixels of padding are added. WorkVibes retains its intentional white clock inside the orange square.

| Final asset under `wwwroot/assets/img` | Original source | Usage |
| --- | --- | --- |
| `arcteon-digital-tagline-transparent.png` | `arcteon-logo-tagline.png` | Header and footer, with tagline |
| `products/arcteonmed-transparent.png` | `products/arcteonmed-logo.png` | Clinic product card and detail banner |
| `products/workvibes-symbol-transparent.png` | WorkVibes symbol in `products/product-reference.png` | HR product card and detail banner; embedded caption excluded |
| `products/sep-transparent.png` | SEP logo in `products/product-reference.png` | Endpoint product card and detail banner |
| `products/qms-transparent.png` | QMS letters in `products/product-reference.png` | Queue product card and detail banner |

Rebuild the final assets on Windows with `scripts/prepare-transparent-logos.ps1`. Background removal preserves foreground RGB values. The export step then uses high-quality bicubic resampling for small assets to provide three-times display density without redrawing or generating artwork.

Company artwork remains at its native 1258x433 and ArcteonMed at 1101x584. WorkVibes exports at 264x269, SEP at 540x352 and QMS at 420x210. These last three remain limited by the small reference image: larger pixel dimensions do not recover missing detail, especially QMS lettering. True HD replacements require original vector or larger raster sources.

The Malaysia Digital logo was recovered from the original `Arcteon Digital-SEP.pptx` embedded `ppt/media/image4.png` (225x110), stored as `malaysia-digital-source.png`, and replaces the previous small badge. Its trimmed, padded export is 233x118. Website display sizes and effects remain unchanged.

## Image-editing trials

Built-in `image_gen` mode was tried for each of the five assets, with transparency enabled. Trial prompts requested background extraction, tight framing, preservation of the original colours/lettering/proportions, and no redesign. Subjects were: the complete Arcteon Digital logo and tagline; ArcteonMed; the WorkVibes clock symbol without its caption; SEP including its lettering; and QMS without its white rectangle. Cleanup prompts requested removal of coloured speckles while preserving the artwork. Those trial results were rejected because of edge artifacts. The user then approved deterministic pixel-based processing, which produced all final files listed above.
