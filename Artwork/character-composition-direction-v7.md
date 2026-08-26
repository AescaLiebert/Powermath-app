# Math:World Character and Composition Direction v7

Status: visual-direction concept for human review. No Unity implementation or asset replacement is authorized by this document.

## Core correction

The environment now communicates location through a small number of large shapes. Character identity, gesture, and interaction carry the narrative and rendering detail.

## Shape-driven environment rule

- Ground: three or four interlocking color planes plus one path and one cast-shadow mass.
- Trees: rounded canopy silhouettes; no individual leaves.
- Rocks: two or three large facets.
- Architecture: one readable silhouette; almost no windows, bricks, or ornaments.
- Clouds: broad soft masses.
- Flowers and grass: sparse color dots, not individually rendered plants.
- Distance: fewer lines, lower contrast, cooler color, softer edges.

The environment can remain colorful without becoming realistic or texturally busy.

## Original Wayfinder heroine

The heroine is an original Math:World design. The supplied character reference informed only high-level design priorities: distinctive facial rhythm, readable hair silhouette, elegant asymmetry, controlled palette, and strong costume masses.

Identity anchors:

- gently angular oval face;
- amber eyes with a subtle teal inner ring;
- small notch in the left eyebrow;
- warm honey-blonde asymmetrical bob;
- long left braided comet-tail;
- crescent cowlick and cyan compass pin;
- cropped cream travel coat;
- azure left shoulder cape;
- deep-indigo underlayer and practical shorts;
- asymmetric cream and cyan compass-shaped coat tails;
- compass buckle and restrained gold piping;
- silver-blue sword in a low back scabbard.

Her front and rear silhouettes retain the same braid, cape, compass tails, buckle, and weapon landmarks.

## Main Menu composition

- The UI architecture remains unchanged.
- Wayfinder occupies the foreground lower-left/center.
- Sword direction and torso angle lead to the golem's face.
- The golem remains the only midground focal subject.
- Ground, trees, rocks, ruins, and clouds are reduced to large flat or softly graded shapes.

## Authentication composition

- Small Math:World logo is centered above the form.
- Authentication panel is centered on the main axis.
- Wayfinder frames the left third with face and costume visible.
- Ice pet frames the right foreground; leaf pet bridges the lower center-right.
- Characters form a triangle around the form without covering it.
- The academy garden uses only ground, path, tree masses, arch silhouette, sky, and clouds.

## Loading narrative

The loading scene presents a short readable event:

1. A glowing compass-number glyph escapes toward the lower center.
2. Ice pet leaps from the extreme foreground to catch it.
3. Wayfinder reaches from the opposite diagonal.
4. Leaf pet skids between them with the dropped map ribbon.
5. Star companion reacts above.

The low wide lens enlarges the foreground pet and reaching hand. Characters occupy most of the image; scenery is only spatial context.

## Rendering hierarchy

- Characters and pets: smooth colored contour, hard primary cel shadow, soft low-saturation second shadow, restrained AO, material-specific gradients.
- Foreground ground: broad light and shadow shapes with little lineart.
- Background: almost no internal contours and minimal texture.
- Highest detail remains on face, hair, hands, compass, sword, and pet-defining features.

## Validation checklist

1. Rear-view test: identify Wayfinder from braid, cape, coat tails, buckle, and sword without seeing her face.
2. Blur test: Authentication still reads as heroine–form–pet; Loading still reads as pet–glyph–heroine.
3. Background test: no individual grass blade, leaf, window, or brick should attract attention.
4. Narrative test: a viewer describes the Loading action as a chase or near-catch without explanation.
5. Form test: centered Authentication controls remain the first actionable UI despite surrounding characters.

## Final built-in image-generation prompt set

- Wayfinder: original full-body front/back heroine concept sheet with distinct face, honey asymmetrical hair and braid, cream/azure/indigo compass-themed travel costume, and reusable silhouette. The generated checkerboard is baked into the raster and is not production alpha.
- Main Menu: preserve all v6 UI, replace the fighter with Wayfinder, and reduce nature to interlocking ground, path, tree, rock, ruin, and cloud shapes.
- Authentication: center a smaller logo and centered sign-in form; frame them with Wayfinder at left and pets at right over a simplified academy garden.
- Loading: low wide-lens character narrative about catching a runaway compass-number glyph; characters occupy roughly seventy percent of the frame and scenery remains minimal.
