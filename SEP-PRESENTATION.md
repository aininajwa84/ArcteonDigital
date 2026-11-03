# SEP presentation viewer

## Current website version: brochures

The SEP detail page now displays eight brochure spreads in place of the 22 PowerPoint slides. The catalogue is `SepPresentation.Brochures`: platform overview, Endpoint Management, IT Service Desk, System Monitoring, Smart Analytics, IT Asset Management, Workflow Automation/Preventive Maintenance, and Device Deployment/Tagging/Checklists. Together they retain coverage of all 22 source slides.

The active viewer assets are under `wwwroot/assets/brochures/sep`: 2400x1600 WebP previews, 360x240 thumbnails, eight individual PDFs and the complete brochure PDF. The viewer uses a 3:2 aspect ratio, has a current-brochure PDF download that follows the selection, and offers a complete PDF download. The former PowerPoint download and slide links are no longer displayed. Enlargement, keyboard controls and touch swipes are retained. The historical deck exports below are retained as source assets, not the active viewer content.

Brochure source files are in `output/pdf/SEP-Brochures`; regenerate them with `scripts/build-sep-brochures.py` and validate with `scripts/verify-sep-brochures.py`. To refresh the website copies, convert the eight source PNGs to the matching WebP files, generate thumbnails, and copy the PDFs into the public brochure directory.

## Original presentation preparation

The SEP details route (`/service-details?solution=assets`) includes an inline viewer of all 22 slides from the supplied `Arcteon Digital-SEP.pptx`. No slide artwork or content is rewritten. PowerPoint exports preserve the deck layout; WebP previews are 1600x900, with separate 240x135 thumbnails. The downloadable PPTX is an unchanged copy of the attachment. A PowerPoint-exported PDF is also provided.

The viewer supports previous/next controls, slide selection, topic shortcuts, thumbnails, arrow keys, touch swipes and a native enlarged dialog with full-resolution image links. No autoplay or external Office viewer is required. Without JavaScript, thumbnails open individual slides and PDF/PPTX links remain available. Assets and scripts are loaded only by the SEP detail page.

To update from the original file:

1. Run `scripts/export-sep-presentation.ps1` using installed PowerPoint. This opens an independent read-only copy, exports the slides and PDF, and closes that copy without quitting the user's PowerPoint session.
2. Run `scripts/prepare-sep-previews.py` with Pillow installed.
3. Update `Pages/SepPresentation.cs` if the deck's titles or slide count change. Topic shortcuts in `_SepPresentation.cshtml` refer to the original slide order.

The native slide exports used for visual review are private build files under `bin/SEP-presentation`; the web assets live under `wwwroot/assets/presentations/sep`. Presentation controls are in `sep-presentation.js` with scoped styles in `sep-presentation.css`.
