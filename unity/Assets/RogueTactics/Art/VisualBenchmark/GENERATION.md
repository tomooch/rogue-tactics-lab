# Original character generation

Built-in imagegen tool; no CLI/API-key fallback. Final retained assets are
`PartyAtlas.png` (edited layout variant) and `EnemyAtlas.png`. Both preserve RGBA
transparency. Unity imports/slices the atlas using alpha bounds per quadrant;
no reference original or absolute local path is embedded in the project.

## Party generation prompt

Use case: stylized-concept. Asset type: ONE transparent sprite atlas for an original indie mobile fantasy RPG. Create a 2 by 2 evenly spaced atlas of FOUR distinct full-body chibi adventurers, each wholly inside its quadrant with generous transparent margins. No background, floor, shadows, text, UI, frames, labels or watermark. TOP LEFT: adorable ivory-haired blue-eyed young witch, oversized floppy blue wizard hat with little gold moon, blue cape with gold trim, wooden staff with luminous pale blue orb. TOP RIGHT: adorable blond amber-eyed armored young knight with blue cape, ivory steel shoulder guards, short silver sword and blue/gold kite shield. BOTTOM LEFT: adorable golden-haired green-eyed fox-eared ranger, moss-green cape, little recurve bow, fox tail. BOTTOM RIGHT: adorable rose-haired pink-eyed healer with cream beret and rose ribbon, cream-and-rose cape, small gold heart staff. All ORIGINAL characters, no existing franchise designs. Consistent soft pastel/pop painted 2.5D anime game-sprite finish, precious expressive large eyes, very oversized heads, tiny feet, soft velvety shading, intricate but legible costumes, warm daylight, fine colored outlines. Slight three-quarter isometric camera from above, characters looking toward upper-right while faces remain visible. Each character similar height and grounded feet aligned within its own quadrant. Elegant professional fantasy game illustration, not primitive spheres/capsules or low-poly toy models. Output genuinely transparent RGBA.

## Party layout edit prompt (final retained variant)

Edit this original transparent RPG sprite atlas for safe Unity slicing. Keep the SAME FOUR characters, same beautiful costumes, faces, style, full bodies, color and poses. Change ONLY placement/scale: shrink all four characters to 75% of current size and center each inside its own exact 2x2 quadrant. TOP LEFT blue witch, TOP RIGHT knight, BOTTOM LEFT fox ranger, BOTTOM RIGHT pink healer. Leave at least 12% completely transparent empty margin on ALL FOUR SIDES of each quadrant. No part of any character, staff, cape, sword, ear or tail may touch or cross the horizontal or vertical centerlines. No background, no floor, no shadows, no text, no drawn grid. All four full characters intact and separated on genuinely transparent RGBA.

## Enemy generation prompt

Use case: stylized-concept. Asset type: one original RPG monster sprite atlas, genuinely transparent RGBA background. A 2 by 2 square atlas with EXACTLY THREE small cute monsters; FOURTH BOTTOM RIGHT quadrant completely empty transparent. Each monster wholly centered inside its own quadrant with at least 15% clear margin on every side, no sprite crosses a quadrant boundary. Top-left: charming plump cream mushroom guardian with giant rounded salmon-red cap, ivory spots, tender black bead eyes, rosy cheeks, leafy moss collar and a little ivory flower on the cap; top-right: adorable lime-green sprout slime with jelly body, shiny eyes, tiny feet and two leaves; bottom-left: mysterious adorable plum-purple bat with chubby kitten-like body, amber luminous eyes, little fangs, soft berry-colored scalloped wings, long pointy ears. No text, no UI, no grid drawn, no background scenery, no floor, no cast shadows. Soft professional painted 2.5D chibi anime fantasy mobile game art, high-quality expressive charming faces, original designs only, soft warm daylight and colored outlines, consistent gentle isometric camera from above, full figures completely visible. Keep each monster compact and separate. Transparent background.

## SHA256 of retained assets

- PartyAtlas.png: `2e1b05b9a9189bbdf00dda69b7cde0ec767b222b67507c2663629c75697c6467`
- EnemyAtlas.png: `1dd9816fbad989020bb980c281260c02fba7ab2b6bfccad4e0901c86987e65e6`
