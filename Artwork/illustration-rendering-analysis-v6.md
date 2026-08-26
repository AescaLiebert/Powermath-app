# Math:World Illustration and Logo Direction v6

Status: visual-direction concept for human review. No Unity implementation or asset replacement is authorized by this document.

## Reference analysis

### Perspective

- The references commit to one readable camera idea per illustration: eye-level activity, elevated world overlook, or dynamic wide-angle flight.
- Foreground characters use overlap and modest foreshortening to establish depth.
- Distant scenery is not equally resolved. Scale, atmospheric color, softer edges, and fewer internal lines carry distance.
- The horizon is deliberately placed to protect either character silhouettes or headline/logo space.

### Composition

- Character groups form simple triangles, diagonals, or radial arcs.
- A large foreground pet or character often anchors one edge while the human lead occupies the center or opposite third.
- Wide scenic images reserve broad sky or water as visual rest.
- Background landmarks are large directional masses, not collections of equally important objects.
- Movement is communicated through body direction, hair/clothing flow, pet trajectories, and converging scenery.

### Text placement

- Promotional illustrations normally carry one logo or one headline, not several UI blocks.
- Text sits in negative space with stable contrast and does not cover faces or gesture lines.
- Logos rely on a strong outer silhouette and large letter shapes. Small internal decoration is avoided because it disappears in banners and thumbnails.

### Lineart

- Exterior contours are smooth and slightly stronger than interior construction lines.
- Warm forms use dark brown lineart; cool forms use indigo or blue-violet.
- Interior lines are omitted when a shadow boundary, color boundary, or overlap already explains the form.
- Bright sun-facing and rim-lit edges may lose the contour entirely.
- Background line density decreases with distance.
- Black uniform outlines and rough pencil wobble are not part of this direction.

### Shading and material rendering

- Primary shadow: readable hard-edged cel shape that explains the main planes.
- Secondary shadow: softer, lower-saturation blue or brown that connects forms to the environment.
- AO: restrained and local to contact points, joints, hair overlaps, collars, feet, and creature undersides.
- Gradients: limited to skin warmth, hair light rolloff, cloth curvature, glossy pets, crystals, and metal.
- Texture: selective rather than global; cloth weave, stone grain, fur softness, and crystal clarity appear only near focal areas.
- The result resembles a clean stylized 3D game model rendered into an illustration.

### Lighting integration

- One key-light direction affects characters, pets, terrain, and architecture.
- Cool sky fill prevents shadows from becoming muddy or black.
- Ground bounce and a restrained rim light place characters inside the scene.
- Lighting is compositional: it connects the group and guides the eye, rather than adding sparkle everywhere.

## Detail budget

### Highest detail

- faces and eyes;
- hair silhouette and focal strands;
- hands and important gesture;
- equipped weapon;
- pet face and defining feature;
- logo letter silhouette.

### Medium detail

- main clothing shapes;
- foreground ground contact;
- one important landmark;
- selected UI icons.

### Lowest detail

- distant buildings;
- repeated windows and masonry;
- individual leaves and grass;
- cloud interiors;
- secondary props;
- decorative particles.

## V6 Main Menu changes

- UI information and placement remain unchanged from v5.
- The meadow is reduced to broad grass planes, a few flower color spots, large tree masses, one rock group, and limited ruin silhouettes.
- The fighter and golem use smoother colored contours, broad cel planes, soft secondary shadows, and limited surface gradients.
- One warm upper-left key and cool sky fill affect the entire scene.

## V6 Authentication changes

- Many floating islands, light routes, constellations, and tiny castles are replaced by one coastal academy mass.
- The adventurer and pets form one foreground triangle on the left.
- Large sky space supports the logo; the sign-in panel remains isolated at right.
- The authentication flow and wording remain unchanged.

## V6 Loading changes

- The scene uses one character movement arc, one floating meadow edge, two broad cloud masses, and one distant academy destination.
- The numbered road, many islands, waterfalls, and dense environmental props are removed.
- The character group carries the strongest outlines, gradients, and saturation.
- Only one progress indicator remains.

## V6 Logo changes

- Removed miniature castles, clouds, multiple math buttons, jewels, dense orbit paths, and decorative star fields.
- Reduced the mark to one world circle, one orbit, and one compass star.
- Limited the palette to indigo, cyan, gold, and ivory.
- Preserved a clearly readable colon.
- Uses one slight gradient and shallow shadow for depth while remaining reusable on game UI, ads, websites, and small thumbnails.

## Validation checklist

1. Thumbnail test: logo and main character silhouette remain identifiable at small size.
2. Blur test: the character group remains the strongest mass; background merges into broad shapes.
3. Grayscale test: focal order survives without color.
4. Edge test: exterior, interior, rim-lit, and distant edges visibly use different strength.
5. Lighting test: character, pet, ground, and landmark share one key direction.
6. Text test: logo/form/progress never overlaps faces or primary gestures.

## Final built-in image-generation prompt set

- Logo: simplify v5 to one world-orbit compass emblem and one-line `MATH:WORLD` wordmark, maximum four colors, transparent background, shallow depth only.
- Main Menu: preserve all v5 UI while reducing environment detail and applying smooth colored lineart, hard primary cel shadow, soft secondary shadow, restrained AO, and material-specific gradients to the fighter and golem.
- Authentication: preserve the sign-in flow, use the simplified logo, one elevated coastal academy vista, a compact foreground character triangle, large sky negative space, and reduced distant detail.
- Loading: preserve one progress indicator, replace the detailed world map with one movement arc, one meadow plane, broad clouds, and one distant destination.
