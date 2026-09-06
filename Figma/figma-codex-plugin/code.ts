type BridgeJob = {
  id: string;
  command: BridgeCommand;
};

type BridgeCommand = {
  type: string;
  [key: string]: unknown;
};

figma.showUI(__html__, { width: 340, height: 250, themeColors: true });

figma.ui.onmessage = async (message: { type?: string; job?: BridgeJob }) => {
  if (message.type !== 'execute-job' || !message.job) return;
  const { job } = message;
  try {
    const data = await executeCommand(job.command);
    figma.ui.postMessage({
      type: 'job-result',
      id: job.id,
      result: { ok: true, data },
    });
  } catch (error) {
    figma.ui.postMessage({
      type: 'job-result',
      id: job.id,
      result: {
        ok: false,
        error: error instanceof Error ? error.message : String(error),
      },
    });
  }
};

async function executeCommand(command: BridgeCommand): Promise<unknown> {
  switch (command.type) {
    case 'ping':
      return {
        fileKey: figma.fileKey,
        pageId: figma.currentPage.id,
        pageName: figma.currentPage.name,
        selectionCount: figma.currentPage.selection.length,
      };
    case 'inspect-selection':
      return inspectSelection();
    case 'inspect-node':
      return inspectNode(command);
    case 'create-frame':
      return createFrame(command);
    case 'create-text':
      return createText(command);
    case 'set-progress':
      return setProgress(command);
    case 'set-properties':
      return setProperties(command);
    case 'set-all-text-fonts':
      return setAllTextFonts(command);
    case 'rebuild-main-menu-ui':
      return rebuildMainMenuUi(command);
    case 'polish-main-menu-icons':
      return polishMainMenuIcons(command);
    case 'flatten-main-menu-icons':
      return flattenMainMenuIcons(command);
    case 'design-main-menu-vector-icons':
      return designMainMenuVectorIcons(command);
    case 'auto-layout-main-menu-panels':
      return autoLayoutMainMenuPanels(command);
    case 'normalize-main-menu-auto-layout':
      return normalizeMainMenuAutoLayout(command);
    case 'find-nodes':
      return findNodes(command);
    case 'build-player-hub-ui':
      return buildPlayerHubUi(command);
    case 'export-node-preview':
      return exportNodePreview(command);
    case 'restyle-player-hub-wireframe':
      return restylePlayerHubWireframe();
    case 'recompose-player-hub-reference-v16':
      return recomposePlayerHubReferenceV16();
    case 'build-rebirth-wireframe':
      return buildRebirthWireframe();
    case 'finalize-player-hub-interactions':
      return finalizePlayerHubInteractions();
    default:
      throw new Error(`Unsupported command: ${command.type}`);
  }
}

function findNodes(command: BridgeCommand) {
  const query = optionalString(command.query)?.toLowerCase();
  const pages = figma.root.children;
  const results: any[] = [];
  for (const page of pages) {
    const nodes = page.findAll((n) => !query || n.name.toLowerCase().includes(query));
    for (const n of nodes) {
      results.push({
        pageId: page.id,
        pageName: page.name,
        id: n.id,
        name: n.name,
        type: n.type,
        x: n.x,
        y: n.y,
        width: n.width,
        height: n.height,
      });
      if (results.length >= 200) break;
    }
  }
  return results;
}

async function normalizeMainMenuAutoLayout(command: BridgeCommand) {
  const stage = await requireFrameNode(command.stageNodeId ?? '34:23');
  const loadout = await requireFrameNode(command.loadoutNodeId ?? '34:58');
  const mutatedNodeIds: string[] = [stage.id, loadout.id];

  stage.primaryAxisSizingMode = 'FIXED';
  stage.counterAxisSizingMode = 'FIXED';
  stage.resizeWithoutConstraints(540, 184);

  const background = loadout.children.find((node) => node.name === 'Round_Panel_outer');
  if (background && 'layoutPositioning' in background) {
    background.layoutPositioning = 'ABSOLUTE';
    background.resizeWithoutConstraints(685, 219);
    background.x = 11;
    background.y = 9;
    mutatedNodeIds.push(background.id);
  }

  const healthRow = loadout.children.find((node) => node.name === 'Player Loadout / Health Row');
  const inventoryRow = loadout.children.find((node) => node.name === 'Player Loadout / Inventory Row');
  if (!healthRow || healthRow.type !== 'FRAME') throw new Error('Missing Player Loadout / Health Row');
  if (!inventoryRow || inventoryRow.type !== 'FRAME') throw new Error('Missing Player Loadout / Inventory Row');

  healthRow.paddingTop = 0;
  healthRow.paddingRight = 0;
  healthRow.paddingBottom = 0;
  healthRow.paddingLeft = 0;
  healthRow.primaryAxisSizingMode = 'FIXED';
  healthRow.counterAxisSizingMode = 'FIXED';
  healthRow.resizeWithoutConstraints(662, 29);

  for (const child of healthRow.children) {
    if ('layoutPositioning' in child) child.layoutPositioning = 'AUTO';
    mutatedNodeIds.push(child.id);
  }
  for (const slot of inventoryRow.children) {
    if ('layoutPositioning' in slot) slot.layoutPositioning = 'AUTO';
    if (slot.type === 'FRAME') {
      for (const child of slot.children) {
        if ('layoutPositioning' in child) child.layoutPositioning = 'AUTO';
        mutatedNodeIds.push(child.id);
      }
    }
    mutatedNodeIds.push(slot.id);
  }

  loadout.primaryAxisSizingMode = 'FIXED';
  loadout.counterAxisSizingMode = 'FIXED';
  loadout.resizeWithoutConstraints(707, 237);
  figma.currentPage.selection = [stage, loadout];
  figma.viewport.scrollAndZoomIntoView([stage, loadout]);
  return { mutatedNodeIds };
}

async function autoLayoutMainMenuPanels(command: BridgeCommand) {
  const stage = await requireFrameNode(command.stageNodeId ?? '34:23');
  const loadout = await requireFrameNode(command.loadoutNodeId ?? '34:58');
  const mutatedNodeIds: string[] = [stage.id, loadout.id];
  const createdNodeIds: string[] = [];

  await loadFontsInSubtree(stage);
  await loadFontsInSubtree(loadout);

  const stageTitle = requireDirectChild(stage, 'Stage Title');
  const divider = requireDirectChild(stage, 'Divider');
  const enemyName = requireDirectChild(stage, 'Enemy Name');
  const enemyTrack = requireDirectChild(stage, 'Enemy HP Track');
  const enemyFill = requireDirectChild(stage, 'Enemy HP Fill');
  const enemyValue = requireDirectChild(stage, 'Enemy HP Value');
  const abilities = ['Ability / MOVE', 'Ability / DASH', 'Ability / GUARD', 'Ability / FIGHT']
    .map((name) => requireDirectChild(stage, name));

  const enemyRow = figma.createFrame();
  enemyRow.name = 'Stage Info / Enemy Row';
  enemyRow.fills = [];
  enemyRow.clipsContent = false;
  enemyRow.resize(492, 28);
  enemyRow.layoutMode = 'HORIZONTAL';
  enemyRow.primaryAxisSizingMode = 'FIXED';
  enemyRow.counterAxisSizingMode = 'FIXED';
  enemyRow.primaryAxisAlignItems = 'MIN';
  enemyRow.counterAxisAlignItems = 'CENTER';
  enemyRow.itemSpacing = 12;
  createdNodeIds.push(enemyRow.id);

  const enemyBar = figma.createFrame();
  enemyBar.name = 'Stage Info / Enemy HP Bar';
  enemyBar.fills = [];
  enemyBar.clipsContent = false;
  enemyBar.resize(250, 24);
  createdNodeIds.push(enemyBar.id);
  enemyRow.appendChild(enemyName);
  enemyRow.appendChild(enemyBar);
  enemyRow.appendChild(enemyValue);
  enemyBar.appendChild(enemyTrack);
  enemyBar.appendChild(enemyFill);
  enemyTrack.x = 0;
  enemyTrack.y = 0;
  enemyFill.x = 0;
  enemyFill.y = 0;

  const actionRow = figma.createFrame();
  actionRow.name = 'Stage Info / Ability Row';
  actionRow.fills = [];
  actionRow.clipsContent = false;
  actionRow.resize(314, 50);
  actionRow.layoutMode = 'HORIZONTAL';
  actionRow.primaryAxisSizingMode = 'FIXED';
  actionRow.counterAxisSizingMode = 'FIXED';
  actionRow.primaryAxisAlignItems = 'MIN';
  actionRow.counterAxisAlignItems = 'CENTER';
  actionRow.itemSpacing = 14;
  for (const ability of abilities) actionRow.appendChild(ability);
  createdNodeIds.push(actionRow.id);

  stage.layoutMode = 'VERTICAL';
  stage.primaryAxisSizingMode = 'FIXED';
  stage.counterAxisSizingMode = 'FIXED';
  stage.primaryAxisAlignItems = 'MIN';
  stage.counterAxisAlignItems = 'CENTER';
  stage.paddingTop = 16;
  stage.paddingRight = 24;
  stage.paddingBottom = 14;
  stage.paddingLeft = 24;
  stage.itemSpacing = 12;
  stage.appendChild(stageTitle);
  stage.appendChild(divider);
  stage.appendChild(enemyRow);
  stage.appendChild(actionRow);
  if ('layoutSizingHorizontal' in stageTitle && 'layoutSizingVertical' in stageTitle) {
    stageTitle.layoutSizingHorizontal = 'HUG';
    stageTitle.layoutSizingVertical = 'HUG';
  }
  if ('layoutSizingHorizontal' in divider && 'layoutSizingVertical' in divider) {
    divider.layoutSizingHorizontal = 'FIXED';
    divider.layoutSizingVertical = 'FIXED';
  }
  enemyRow.layoutSizingHorizontal = 'FIXED';
  enemyRow.layoutSizingVertical = 'FIXED';
  actionRow.layoutSizingHorizontal = 'FIXED';
  actionRow.layoutSizingVertical = 'FIXED';

  const panelBackground = loadout.children.find((node) => node.name === 'Round_Panel_outer');
  const heart = requireDirectChild(loadout, 'Heart');
  const inventoryRow = requireDirectChild(loadout, 'Frame 3');
  const playerSlider = requireDirectChild(loadout, 'Player_Slider');
  if (inventoryRow.type !== 'FRAME') throw new Error('Player Loadout inventory row must be a FRAME');

  const healthRow = figma.createFrame();
  healthRow.name = 'Player Loadout / Health Row';
  healthRow.fills = [];
  healthRow.clipsContent = false;
  healthRow.resize(662, 29);
  healthRow.layoutMode = 'HORIZONTAL';
  healthRow.primaryAxisSizingMode = 'FIXED';
  healthRow.counterAxisSizingMode = 'FIXED';
  healthRow.primaryAxisAlignItems = 'MIN';
  healthRow.counterAxisAlignItems = 'CENTER';
  healthRow.itemSpacing = 17;
  healthRow.appendChild(heart);
  healthRow.appendChild(playerSlider);
  createdNodeIds.push(healthRow.id);

  inventoryRow.name = 'Player Loadout / Inventory Row';
  inventoryRow.layoutMode = 'HORIZONTAL';
  inventoryRow.primaryAxisSizingMode = 'FIXED';
  inventoryRow.counterAxisSizingMode = 'FIXED';
  inventoryRow.primaryAxisAlignItems = 'MIN';
  inventoryRow.counterAxisAlignItems = 'MAX';
  inventoryRow.itemSpacing = 10;
  inventoryRow.paddingTop = 0;
  inventoryRow.paddingRight = 0;
  inventoryRow.paddingBottom = 0;
  inventoryRow.paddingLeft = 0;
  inventoryRow.resize(662, 146);

  const slots = inventoryRow.children.filter((node): node is FrameNode => node.type === 'FRAME');
  for (const slot of slots) {
    slot.layoutMode = 'VERTICAL';
    slot.primaryAxisSizingMode = 'FIXED';
    slot.counterAxisSizingMode = 'FIXED';
    slot.primaryAxisAlignItems = 'MIN';
    slot.counterAxisAlignItems = 'CENTER';
    slot.itemSpacing = 8;
    slot.paddingTop = 13;
    slot.paddingRight = 10;
    slot.paddingBottom = 10;
    slot.paddingLeft = 10;
    const isSelected = slot.name === 'Loadout Slot 1';
    slot.resize(isSelected ? 102 : 102, isSelected ? 146 : 113);
    const placeholder = slot.children.find((node) => node.name === 'Item Placeholder');
    const label = slot.children.find((node) => node.name === 'Item Label');
    if (placeholder) slot.appendChild(placeholder);
    if (label) slot.appendChild(label);
    mutatedNodeIds.push(slot.id);
  }

  loadout.layoutMode = 'VERTICAL';
  loadout.primaryAxisSizingMode = 'FIXED';
  loadout.counterAxisSizingMode = 'FIXED';
  loadout.primaryAxisAlignItems = 'MIN';
  loadout.counterAxisAlignItems = 'CENTER';
  loadout.paddingTop = 30;
  loadout.paddingRight = 21;
  loadout.paddingBottom = 23;
  loadout.paddingLeft = 24;
  loadout.itemSpacing = 9;
  if (panelBackground && 'layoutPositioning' in panelBackground) {
    panelBackground.layoutPositioning = 'ABSOLUTE';
    panelBackground.x = 11;
    panelBackground.y = 9;
  }
  loadout.appendChild(healthRow);
  loadout.appendChild(inventoryRow);
  healthRow.layoutSizingHorizontal = 'FIXED';
  healthRow.layoutSizingVertical = 'FIXED';
  inventoryRow.layoutSizingHorizontal = 'FIXED';
  inventoryRow.layoutSizingVertical = 'FIXED';

  mutatedNodeIds.push(
    stageTitle.id, divider.id, enemyName.id, enemyTrack.id, enemyFill.id, enemyValue.id,
    ...abilities.map((node) => node.id), heart.id, playerSlider.id, inventoryRow.id,
  );
  figma.currentPage.selection = [stage, loadout];
  figma.viewport.scrollAndZoomIntoView([stage, loadout]);
  return { createdNodeIds, mutatedNodeIds };
}

function requireDirectChild(parent: FrameNode, name: string): SceneNode {
  const node = parent.children.find((child) => child.name === name);
  if (!node) throw new Error(`Missing direct child "${name}" in "${parent.name}"`);
  return node;
}

async function requireFrameNode(value: unknown): Promise<FrameNode> {
  const node = await requireSceneNode(value);
  if (node.type !== 'FRAME') throw new Error(`Expected FRAME, received ${node.type}`);
  return node;
}

async function loadFontsInSubtree(root: FrameNode) {
  const textNodes: TextNode[] = [];
  const visit = (node: SceneNode) => {
    if (node.type === 'TEXT') textNodes.push(node);
    if ('children' in node) {
      for (const child of node.children) visit(child);
    }
  };
  visit(root);
  const loaded = new Set<string>();
  for (const text of textNodes) {
    const segments = text.getStyledTextSegments(['fontName']);
    for (const segment of segments) {
      const font = segment.fontName;
      const key = `${font.family}\u0000${font.style}`;
      if (!loaded.has(key)) {
        await figma.loadFontAsync(font);
        loaded.add(key);
      }
    }
  }
}

function inspectSelection() {
  return figma.currentPage.selection.map((node) => ({
    id: node.id,
    name: node.name,
    type: node.type,
    x: node.x,
    y: node.y,
    width: node.width,
    height: node.height,
  }));
}

async function inspectNode(command: BridgeCommand) {
  const root = await requireSceneNode(command.nodeId);
  const maxDepth = Math.max(0, Math.min(10, finiteNumber(command.maxDepth, 6)));
  const maxNodes = Math.max(1, Math.min(1000, finiteNumber(command.maxNodes, 500)));
  let visited = 0;

  function serialize(node: SceneNode, depth: number): Record<string, unknown> {
    visited += 1;
    const result: Record<string, unknown> = {
      id: node.id,
      name: node.name,
      type: node.type,
      x: node.x,
      y: node.y,
      width: node.width,
      height: node.height,
      visible: node.visible,
      opacity: 'opacity' in node ? node.opacity : 1,
    };

    if ('constraints' in node) {
      result.constraints = node.constraints;
    }

    if ('layoutMode' in node) {
      result.layoutMode = node.layoutMode;
      result.primaryAxisSizingMode = node.primaryAxisSizingMode;
      result.counterAxisSizingMode = node.counterAxisSizingMode;
      result.primaryAxisAlignItems = node.primaryAxisAlignItems;
      result.counterAxisAlignItems = node.counterAxisAlignItems;
      result.paddingTop = node.paddingTop;
      result.paddingRight = node.paddingRight;
      result.paddingBottom = node.paddingBottom;
      result.paddingLeft = node.paddingLeft;
      result.itemSpacing = node.itemSpacing;
    }

    if ('layoutPositioning' in node) {
      result.layoutPositioning = node.layoutPositioning;
    }
    if ('layoutAlign' in node) {
      result.layoutAlign = node.layoutAlign;
    }
    if ('layoutGrow' in node) {
      result.layoutGrow = node.layoutGrow;
    }

    if ('fills' in node && Array.isArray(node.fills)) {
      result.fills = node.fills;
    }
    if ('strokes' in node && Array.isArray(node.strokes)) {
      result.strokes = node.strokes;
      result.strokeWeight = 'strokeWeight' in node ? node.strokeWeight : undefined;
      result.strokeAlign = 'strokeAlign' in node ? node.strokeAlign : undefined;
    }
    if ('cornerRadius' in node) {
      result.cornerRadius = node.cornerRadius;
    }
    if ('effects' in node && Array.isArray(node.effects)) {
      result.effects = node.effects;
    }

    if (node.type === 'TEXT') {
      const textNode = node as TextNode;
      result.characters = textNode.characters;
      result.fontSize = textNode.fontSize;
      result.fontName = textNode.fontName;
      result.textAlignHorizontal = textNode.textAlignHorizontal;
      result.textAlignVertical = textNode.textAlignVertical;
      result.letterSpacing = textNode.letterSpacing;
      result.lineHeight = textNode.lineHeight;
    }

    if ('boundVariables' in node) {
      result.boundVariables = node.boundVariables;
    }

    if (depth < maxDepth && visited < maxNodes && 'children' in node) {
      result.children = node.children
        .slice(0, maxNodes - visited)
        .map((child) => serialize(child, depth + 1));
    }
    return result;
  }

  const rootData = JSON.parse(JSON.stringify(
    serialize(root, 0),
    (_key, value) => typeof value === 'symbol' ? 'MIXED' : value,
  ));
  return { root: rootData, visited, truncated: visited >= maxNodes };
}

function createFrame(command: BridgeCommand) {
  const frame = figma.createFrame();
  frame.name = optionalString(command.name) ?? 'AI Frame';
  frame.resize(dimension(command.width, 400), dimension(command.height, 300));
  frame.x = finiteNumber(command.x, 0);
  frame.y = finiteNumber(command.y, 0);
  frame.fills = [{
    type: 'SOLID',
    color: parseColor(optionalString(command.fill) ?? '#FFFFFF'),
  }];
  figma.currentPage.selection = [frame];
  figma.viewport.scrollAndZoomIntoView([frame]);
  return { createdNodeIds: [frame.id], rootNodeId: frame.id };
}

async function createText(command: BridgeCommand) {
  const font: FontName = {
    family: optionalString(command.fontFamily) ?? 'Inter',
    style: optionalString(command.fontStyle) ?? 'Regular',
  };
  await figma.loadFontAsync(font);
  const text = figma.createText();
  text.name = optionalString(command.name) ?? 'AI Text';
  text.fontName = font;
  text.fontSize = dimension(command.fontSize, 16);
  text.characters = optionalString(command.text) ?? '';
  text.x = finiteNumber(command.x, 0);
  text.y = finiteNumber(command.y, 0);
  text.fills = [{
    type: 'SOLID',
    color: parseColor(optionalString(command.fill) ?? '#111827'),
  }];
  figma.currentPage.selection = [text];
  figma.viewport.scrollAndZoomIntoView([text]);
  return { createdNodeIds: [text.id] };
}

async function setProgress(command: BridgeCommand) {
  const fill = await requireSceneNode(command.fillNodeId, ['RECTANGLE', 'FRAME']);
  const indicator = await requireSceneNode(command.indicatorNodeId, [
    'FRAME', 'INSTANCE', 'VECTOR', 'COMPONENT', 'ELLIPSE', 'STAR',
  ]);
  const progress = Math.max(0, Math.min(1, finiteNumber(command.value, 0)));
  const totalWidth = dimension(command.totalWidth, fill.width);
  if (!('resize' in fill)) {
    throw new Error(`Node ${fill.id} cannot be resized`);
  }
  fill.resize(Math.max(1, totalWidth * progress), fill.height);
  const trackX = finiteNumber(command.trackX, fill.x);
  indicator.x = trackX + totalWidth * progress - indicator.width / 2;
  return { progress, mutatedNodeIds: [fill.id, indicator.id] };
}

async function setProperties(command: BridgeCommand) {
  const node = await requireSceneNode(command.nodeId);
  const name = optionalString(command.name);
  if (name !== undefined) node.name = name;
  if (command.x !== undefined) node.x = finiteNumber(command.x, node.x);
  if (command.y !== undefined) node.y = finiteNumber(command.y, node.y);
  if (command.visible !== undefined) node.visible = Boolean(command.visible);
  if (command.opacity !== undefined) {
    if (!('opacity' in node)) {
      throw new Error(`Node ${node.id} does not support opacity`);
    }
    node.opacity = Math.max(0, Math.min(1, finiteNumber(command.opacity, node.opacity)));
  }
  return { mutatedNodeIds: [node.id] };
}

async function setAllTextFonts(command: BridgeCommand) {
  const requestedFamily = optionalString(command.fontFamily) ?? 'HYWenHei';
  const requestedStyle = optionalString(command.fontStyle);
  await figma.loadAllPagesAsync();

  const availableFonts = await figma.listAvailableFontsAsync();
  const familyFonts = availableFonts
    .map((font) => font.fontName)
    .filter((font) => font.family.toLowerCase() === requestedFamily.toLowerCase());

  if (familyFonts.length === 0) {
    const similarFamilies = [...new Set(
      availableFonts
        .map((font) => font.fontName.family)
        .filter((family) => family.toLowerCase().includes('hywen')),
    )];
    throw new Error(
      `Font family "${requestedFamily}" is not available in Figma.` +
      (similarFamilies.length > 0 ? ` Similar families: ${similarFamilies.join(', ')}` : ''),
    );
  }

  const byStyle = new Map(familyFonts.map((font) => [font.style.toLowerCase(), font]));
  const requestedFont = requestedStyle
    ? byStyle.get(requestedStyle.toLowerCase())
    : undefined;
  if (requestedStyle && !requestedFont) {
    throw new Error(
      `Font style "${requestedStyle}" is not available for ${familyFonts[0].family}. ` +
      `Available styles: ${familyFonts.map((font) => font.style).join(', ')}`,
    );
  }
  const fallbackFont = requestedFont ?? byStyle.get('regular') ?? familyFonts[0];

  await Promise.all(familyFonts.map((font) => figma.loadFontAsync(font)));

  const mutatedNodeIds: string[] = [];
  const failures: Array<{ id: string; name: string; error: string }> = [];
  const pages: Array<{ id: string; name: string; textNodeCount: number }> = [];

  for (const page of figma.root.children) {
    const textNodes = page.findAllWithCriteria({ types: ['TEXT'] });
    pages.push({ id: page.id, name: page.name, textNodeCount: textNodes.length });

    for (const textNode of textNodes) {
      try {
        if (textNode.characters.length === 0) {
          textNode.fontName = fallbackFont;
        } else {
          const segments = textNode.getStyledTextSegments(['fontName']);
          for (const segment of segments) {
            const sourceFont = segment.fontName as FontName;
            const targetFont = requestedFont ?? byStyle.get(sourceFont.style.toLowerCase()) ?? fallbackFont;
            textNode.setRangeFontName(segment.start, segment.end, targetFont);
          }
        }
        mutatedNodeIds.push(textNode.id);
      } catch (error) {
        failures.push({
          id: textNode.id,
          name: textNode.name,
          error: error instanceof Error ? error.message : String(error),
        });
      }
    }
  }

  return {
    fontFamily: familyFonts[0].family,
    availableStyles: familyFonts.map((font) => font.style),
    mutatedNodeIds,
    mutatedCount: mutatedNodeIds.length,
    failureCount: failures.length,
    failures: failures.slice(0, 50),
    pages,
  };
}

async function exportNodePreview(command: BridgeCommand) {
  const node = await requireSceneNode(command.nodeId);
  const width = Math.max(320, Math.min(1800, finiteNumber(command.width, 1400)));
  const bytes = await node.exportAsync({
    format: 'JPG',
    constraint: { type: 'WIDTH', value: width },
  });
  return {
    nodeId: node.id,
    name: node.name,
    mimeType: 'image/jpeg',
    base64: figma.base64Encode(bytes),
  };
}

async function restylePlayerHubWireframe() {
  await Promise.all([
    figma.loadFontAsync({ family: 'Inter', style: 'Regular' }),
    figma.loadFontAsync({ family: 'Inter', style: 'Bold' }),
  ]);

  const section = figma.currentPage.children.find(
    (node) => node.type === 'SECTION' && (
      node.name === 'Generated / Complete Player Hub UI' || node.name === 'Player Hub UI'
    ),
  );
  if (!section || section.type !== 'SECTION') {
    throw new Error('Generated Player Hub section was not found.');
  }

  const palette = {
    navy: '#12396B',
    blue: '#397CC2',
    cyan: '#4AAFC6',
    paper: '#FFF8EB',
    panel: '#FFFDF8',
    canvas: '#EDF5FB',
    softBlue: '#E8F2F8',
    warm: '#F4EBDD',
    line: '#9EB4C8',
    warmLine: '#D7C2A5',
    gold: '#E9B84B',
    orange: '#F6A44B',
    orangeLine: '#D8752D',
    muted: '#60758A',
    coral: '#D96252',
  };

  const mutated = new Set<string>();
  const created: string[] = [];
  const hidden: string[] = [];

  function remember(node: SceneNode) {
    mutated.add(node.id);
    return node;
  }

  function solidPaint(hex: string, opacity = 1): SolidPaint {
    return { type: 'SOLID', color: parseColor(hex), opacity };
  }

  function shadow(radius = 14, offsetY = 6, alpha = 0.18): DropShadowEffect {
    return {
      type: 'DROP_SHADOW',
      color: { r: 0.04, g: 0.12, b: 0.22, a: alpha },
      offset: { x: 0, y: offsetY },
      radius,
      spread: 0,
      visible: true,
      blendMode: 'NORMAL',
    };
  }

  function findOne(root: ChildrenMixin, name: string): SceneNode | null {
    return root.findOne((node) => node.name === name);
  }

  function requireFrame(root: ChildrenMixin, name: string): FrameNode {
    const node = root.findOne((candidate) => candidate.type === 'FRAME' && candidate.name === name);
    if (!node || node.type !== 'FRAME') throw new Error(`Player Hub frame not found: ${name}`);
    return node;
  }

  function styleFrame(
    node: FrameNode | ComponentNode,
    fill: string | null,
    stroke: string | null,
    radius: number,
    withShadow = false,
  ) {
    node.fills = fill ? [solidPaint(fill)] : [];
    node.strokes = stroke ? [solidPaint(stroke)] : [];
    node.strokeWeight = stroke ? 2 : 0;
    node.cornerRadius = radius;
    node.effects = withShadow ? [shadow()] : [];
    remember(node);
  }

  function makeText(
    parent: ChildrenMixin,
    name: string,
    value: string,
    size: number,
    color: string,
    style: 'Regular' | 'Bold' = 'Bold',
  ) {
    const node = figma.createText();
    node.name = name;
    node.fontName = { family: 'Inter', style };
    node.fontSize = size;
    node.characters = value;
    node.fills = [solidPaint(color)];
    node.textAutoResize = 'WIDTH_AND_HEIGHT';
    parent.appendChild(node);
    created.push(node.id);
    return node;
  }

  function makePlaceholder(
    parent: ChildrenMixin,
    name: string,
    width: number,
    height: number,
    label: string,
  ) {
    const existing = parent.findOne((node) => node.name === name);
    if (existing && existing.type === 'FRAME') return existing;
    const node = figma.createFrame();
    node.name = name;
    node.resize(width, height);
    node.layoutMode = 'HORIZONTAL';
    node.primaryAxisSizingMode = 'FIXED';
    node.counterAxisSizingMode = 'FIXED';
    node.primaryAxisAlignItems = 'CENTER';
    node.counterAxisAlignItems = 'CENTER';
    node.fills = [solidPaint(palette.softBlue, 0.72)];
    node.strokes = [solidPaint(palette.line)];
    node.strokeWeight = 2;
    node.dashPattern = [10, 8];
    node.cornerRadius = Math.min(20, height / 4);
    parent.appendChild(node);
    created.push(node.id);
    makeText(node, `${name} / Label`, label, 12, palette.muted, 'Bold');
    return node;
  }

  function hideByName(root: ChildrenMixin, names: string[]) {
    for (const name of names) {
      for (const node of root.findAll((candidate) => candidate.name === name)) {
        node.visible = false;
        hidden.push(node.id);
        remember(node);
      }
    }
  }

  function restyleText(root: ChildrenMixin) {
    for (const node of root.findAll((candidate) => candidate.type === 'TEXT')) {
      if (node.type !== 'TEXT') continue;
      const wasRegular = node.fontName !== figma.mixed && node.fontName.style === 'Regular';
      node.fontName = { family: 'Inter', style: wasRegular ? 'Regular' : 'Bold' };
      const value = node.characters.toUpperCase();
      if (value.includes('★') || node.name === 'Stars') {
        node.fills = [solidPaint(palette.gold)];
      } else if (node.name === 'Subtitle' || node.name === 'Requirement' || node.name === 'Copy') {
        node.fills = [solidPaint(node.name === 'Requirement' ? palette.coral : palette.muted)];
      } else if (node.name === 'Kicker' || node.name === 'Brand') {
        node.fills = [solidPaint(palette.blue)];
      } else {
        node.fills = [solidPaint(palette.navy)];
      }
      remember(node);
    }
  }

  const sectionTitle = findOne(section, 'Section Title');
  if (sectionTitle && sectionTitle.type === 'TEXT') {
    sectionTitle.fills = [solidPaint(palette.paper)];
    sectionTitle.fontName = { family: 'Inter', style: 'Bold' };
    remember(sectionTitle);
  }
  const sectionNote = findOne(section, 'Section Note');
  if (sectionNote && sectionNote.type === 'TEXT') {
    sectionNote.characters = 'Graphics-light wireframe • Reference styling from Main Menu UI • Auto layout + horizontal scroll + prototype actions';
    sectionNote.fills = [solidPaint('#B9D8ED')];
    remember(sectionNote);
  }
  section.fills = [solidPaint(palette.navy)];

  const petCardSet = section.findOne(
    (node) => node.type === 'COMPONENT_SET' && node.name === 'Component / Pet Card',
  );
  if (petCardSet && petCardSet.type === 'COMPONENT_SET') {
    for (const component of petCardSet.children) {
      if (component.type !== 'COMPONENT') continue;
      styleFrame(component, palette.panel, palette.line, 18, false);
      for (const child of component.children) {
        if (child.name.startsWith('Vector / ') && child.name.endsWith(' Art')) {
          child.visible = false;
          hidden.push(child.id);
          remember(child);
        }
      }
      const petName = component.children.find(
        (child) => child.type === 'TEXT' && child.name === 'Pet Name',
      );
      const label = petName && petName.type === 'TEXT' ? `${petName.characters} PREVIEW` : 'PET PREVIEW';
      const placeholder = makePlaceholder(component, 'Wireframe / Pet Artwork', 104, 104, label);
      component.insertChild(0, placeholder);
    }
  }

  const screenNames = ['Player Hub / Pet State', 'Player Hub / Weapon State'];
  const screens: FrameNode[] = [];
  for (const screenName of screenNames) {
    const screen = requireFrame(section, screenName);
    screens.push(screen);
    screen.fills = [solidPaint(palette.canvas)];
    remember(screen);

    const background = requireFrame(screen, '1. Background & Player Stance');
    styleFrame(background, palette.canvas, null, 0, false);
    hideByName(background, [
      'Environment / Academy Room',
      'Environment / Window Glow',
      'Environment / Floor',
      'Player / Pedestal',
      'Vector / Player Stance Placeholder',
      'Vector / Equipped Pet',
      'Vector / Pet Quick Icon',
      'Vector / Weapon Quick Icon',
    ]);

    const stage = makePlaceholder(background, 'Wireframe / Player Stance', 520, 610, 'PLAYER STANCE PLACEHOLDER');
    stage.x = 56;
    stage.y = 156;
    stage.cornerRadius = 28;
    stage.dashPattern = [14, 10];
    stage.fills = [solidPaint(palette.paper, 0.78)];
    stage.effects = [shadow(18, 8, 0.12)];
    remember(stage);

    const quickRow = requireFrame(background, 'Player / Equipped Quick Slots');
    quickRow.fills = [];
    quickRow.effects = [];
    remember(quickRow);
    const quickPet = requireFrame(quickRow, 'Quick Slot / Pet');
    const quickWeapon = requireFrame(quickRow, 'Quick Slot / Weapon');
    styleFrame(quickPet, palette.panel, palette.line, 18, false);
    styleFrame(quickWeapon, palette.panel, palette.line, 18, false);
    const petSlotPlaceholder = makePlaceholder(quickPet, 'Wireframe / Pet Slot', 62, 62, 'PET');
    const weaponSlotPlaceholder = makePlaceholder(quickWeapon, 'Wireframe / Weapon Slot', 62, 62, 'WEAPON');
    quickPet.insertChild(0, petSlotPlaceholder);
    quickWeapon.insertChild(0, weaponSlotPlaceholder);
    const compactStats = requireFrame(quickRow, 'Player / Compact Stats');
    styleFrame(compactStats, palette.panel, palette.line, 18, false);

    const menu = requireFrame(screen, '2. UI Menu (Pet / Weapon)');
    menu.fills = [];
    menu.strokes = [];
    remember(menu);
    const workspaceName = screenName.endsWith('Pet State') ? 'Workspace / Pet' : 'Workspace / Weapon';
    const workspace = requireFrame(menu, workspaceName);
    styleFrame(workspace, palette.paper, palette.warmLine, 30, true);
    workspace.strokeWeight = 2;

    const tabs = requireFrame(workspace, 'Menu / Tabs');
    tabs.fills = [];
    tabs.strokes = [];
    remember(tabs);
    const petTab = requireFrame(tabs, 'Button / Pet Tab');
    const weaponTab = requireFrame(tabs, 'Button / Weapon Tab');
    const isPetScreen = screenName.endsWith('Pet State');
    styleFrame(petTab, isPetScreen ? palette.softBlue : palette.panel, isPetScreen ? palette.blue : palette.line, 14, false);
    styleFrame(weaponTab, isPetScreen ? palette.panel : palette.softBlue, isPetScreen ? palette.line : palette.blue, 14, false);

    if (isPetScreen) {
      const scroller = requireFrame(workspace, 'Pet / Horizontal Scroller');
      styleFrame(scroller, palette.warm, palette.warmLine, 20, false);
      scroller.clipsContent = true;
      scroller.overflowDirection = 'HORIZONTAL';
      const detail = requireFrame(workspace, 'Pet / Detail Preview');
      styleFrame(detail, palette.panel, palette.line, 22, true);
      const hero = requireFrame(detail, 'Pet / Hero Preview');
      styleFrame(hero, palette.softBlue, palette.line, 18, false);
      hideByName(hero, ['Pet / Aura', 'Vector / Tearay Hero']);
      const preview = makePlaceholder(hero, 'Wireframe / Selected Pet Preview', 360, 218, 'SELECTED PET PREVIEW');
      preview.x = 92;
      preview.y = 40;
      const equip = requireFrame(detail, 'Button / Equip Pet');
      styleFrame(equip, palette.orange, palette.orangeLine, 15, false);
      equip.effects = [shadow(8, 4, 0.16)];
    } else {
      const showcase = requireFrame(workspace, 'Weapon / Showcase');
      styleFrame(showcase, palette.panel, palette.line, 22, true);
      const heroSword = findOne(showcase, 'Vector / Skyedge Hero');
      let heroIndex = 3;
      if (heroSword) {
        heroIndex = showcase.children.indexOf(heroSword);
        heroSword.visible = false;
        hidden.push(heroSword.id);
        remember(heroSword);
      }
      const weaponPreview = makePlaceholder(showcase, 'Wireframe / Weapon Preview', 240, 236, 'WEAPON PREVIEW');
      showcase.insertChild(Math.max(0, heroIndex), weaponPreview);
      hideByName(showcase, ['Vector / Current Attack Icon', 'Vector / Next Attack Icon']);
      const progress = requireFrame(showcase, 'Weapon / Awakening Progress');
      styleFrame(progress, palette.softBlue, palette.line, 14, false);
      const compare = requireFrame(showcase, 'Weapon / Stat Comparison');
      styleFrame(compare, palette.paper, palette.warmLine, 16, false);
      const ascendPanel = requireFrame(workspace, 'Weapon / Ascend Panel');
      styleFrame(ascendPanel, palette.panel, palette.warmLine, 22, false);
      const materials = requireFrame(ascendPanel, 'Weapon / Materials');
      styleFrame(materials, palette.warm, palette.warmLine, 16, false);
      const ascendButton = requireFrame(ascendPanel, 'Button / Ascend Weapon');
      styleFrame(ascendButton, palette.orange, palette.orangeLine, 15, false);
      ascendButton.effects = [shadow(8, 4, 0.16)];
    }

    for (const node of workspace.findAll((candidate) => candidate.type === 'FRAME')) {
      if (node.type !== 'FRAME') continue;
      if (node.name.startsWith('Stat / ')) styleFrame(node, palette.softBlue, palette.line, 14, false);
      if (node.name === 'Power Coin' || node.name === 'Owned Count' || node.name === 'Cost') {
        styleFrame(node, palette.panel, palette.warmLine, 14, false);
      }
    }

    const utilities = requireFrame(screen, '3. Utilities Top Menu (Power Coin / Exit)');
    utilities.fills = [];
    utilities.strokes = [];
    remember(utilities);
    const topBar = requireFrame(utilities, 'Utilities / Top Bar');
    styleFrame(topBar, palette.paper, palette.warmLine, 0, true);
    topBar.effects = [shadow(10, 6, 0.14)];
    const exit = requireFrame(topBar, 'Button / Exit');
    styleFrame(exit, palette.panel, palette.warmLine, 27, false);
    const coin = requireFrame(topBar, 'Power Coin');
    styleFrame(coin, palette.panel, palette.warmLine, 16, false);

    restyleText(screen);
  }

  const toast = requireFrame(section, 'Prototype Overlay / Action Confirmed');
  styleFrame(toast, palette.paper, palette.gold, 20, true);
  restyleText(toast);

  figma.currentPage.selection = screens;
  figma.viewport.scrollAndZoomIntoView(screens);
  return {
    sectionNodeId: section.id,
    screenNodeIds: screens.map((node) => node.id),
    mutatedNodeIds: Array.from(mutated),
    createdNodeIds: created,
    hiddenGraphicNodeIds: hidden,
    styleSourceNodeIds: ['22:12', '4:2'],
    preservedBehaviors: ['auto-layout', 'horizontal-scroll', 'tab-navigation', 'button-overlays', 'exit-back'],
  };
}

async function recomposePlayerHubReferenceV16() {
  await Promise.all([
    figma.loadFontAsync({ family: 'Inter', style: 'Regular' }),
    figma.loadFontAsync({ family: 'Inter', style: 'Bold' }),
    figma.loadFontAsync({ family: 'Inter', style: 'Extra Bold' }),
  ]);

  const section = figma.currentPage.children.find(
    (node) => node.type === 'SECTION' && (
      node.name === 'Generated / Complete Player Hub UI' || node.name === 'Player Hub UI'
    ),
  );
  if (!section || section.type !== 'SECTION') throw new Error('Player Hub UI section was not found.');
  const hubSection: SectionNode = section;

  const petScreen = section.findOne((node) => node.type === 'FRAME' && node.name === 'Player Hub / Pet State');
  const weaponScreen = section.findOne((node) => node.type === 'FRAME' && node.name === 'Player Hub / Weapon State');
  if (!petScreen || petScreen.type !== 'FRAME' || !weaponScreen || weaponScreen.type !== 'FRAME') {
    throw new Error('Player Hub Pet/Weapon screens were not found.');
  }

  const colors = {
    navy: '#0C376A',
    blue: '#126FE8',
    cream: '#FFF4E4',
    creamTop: '#FFF9EE',
    creamDeep: '#F5DFC5',
    beige: '#D6B893',
    beigeDark: '#B58D64',
    green: '#4E9B4D',
    gold: '#F2A91B',
    paleBlue: '#EAF4FA',
    leftWarm: '#CBB8A6',
    white: '#FFFFFF',
  };
  const created: string[] = [];
  const mutated = new Set<string>();
  const hidden: string[] = [];
  const interactions: string[] = [];

  function track<T extends SceneNode>(node: T): T {
    created.push(node.id);
    return node;
  }
  function mark(node: SceneNode) {
    mutated.add(node.id);
    return node;
  }
  function paint(hex: string, opacity = 1): SolidPaint {
    return { type: 'SOLID', color: parseColor(hex), opacity };
  }
  function rgba(hex: string, a: number): RGBA {
    return { ...parseColor(hex), a };
  }
  function creamGradient(): GradientPaint {
    return {
      type: 'GRADIENT_LINEAR',
      gradientTransform: [[0.84, 0.16, 0], [-0.16, 0.84, 0.08]],
      gradientStops: [
        { position: 0, color: rgba(colors.creamTop, 1) },
        { position: 0.58, color: rgba(colors.cream, 1) },
        { position: 1, color: rgba(colors.creamDeep, 1) },
      ],
    };
  }
  function warmGradient(): GradientPaint {
    return {
      type: 'GRADIENT_LINEAR',
      gradientTransform: [[0.72, 0.28, 0], [-0.28, 0.72, 0.08]],
      gradientStops: [
        { position: 0, color: rgba('#D8C9BA', 1) },
        { position: 1, color: rgba('#BDA28C', 1) },
      ],
    };
  }
  function panelEffects(strong = false): Effect[] {
    return [
      {
        type: 'DROP_SHADOW',
        color: rgba(colors.navy, strong ? 0.22 : 0.12),
        offset: { x: 0, y: strong ? 8 : 4 },
        radius: strong ? 14 : 8,
        spread: 0,
        visible: true,
        blendMode: 'NORMAL',
      },
      {
        type: 'INNER_SHADOW',
        color: rgba(colors.white, 0.82),
        offset: { x: 0, y: 2 },
        radius: 4,
        spread: 0,
        visible: true,
        blendMode: 'NORMAL',
      },
      {
        type: 'INNER_SHADOW',
        color: rgba(colors.beigeDark, 0.16),
        offset: { x: 0, y: -2 },
        radius: 4,
        spread: 0,
        visible: true,
        blendMode: 'NORMAL',
      },
    ];
  }
  function frame(
    parent: ChildrenMixin,
    name: string,
    width: number,
    height: number,
    x: number,
    y: number,
    fill: Paint[] = [],
    radius = 0,
    stroke?: string,
    strokeWeight = 0,
  ) {
    const node = track(figma.createFrame());
    node.name = name;
    node.resize(width, height);
    node.x = x;
    node.y = y;
    node.fills = fill;
    node.cornerRadius = radius;
    node.strokes = stroke ? [paint(stroke)] : [];
    node.strokeWeight = stroke ? strokeWeight : 0;
    node.clipsContent = true;
    parent.appendChild(node);
    return node;
  }
  function text(
    parent: ChildrenMixin,
    name: string,
    value: string,
    size: number,
    x: number,
    y: number,
    color = colors.navy,
    style: 'Regular' | 'Bold' | 'Extra Bold' = 'Bold',
    width?: number,
    align: 'LEFT' | 'CENTER' | 'RIGHT' = 'LEFT',
  ) {
    const node = track(figma.createText());
    node.name = name;
    node.fontName = { family: 'Inter', style };
    node.fontSize = size;
    node.characters = value;
    node.fills = [paint(color)];
    node.textAlignHorizontal = align;
    if (width) {
      node.textAutoResize = 'HEIGHT';
      node.resize(width, Math.max(size * 1.35, 20));
    } else {
      node.textAutoResize = 'WIDTH_AND_HEIGHT';
    }
    node.x = x;
    node.y = y;
    parent.appendChild(node);
    return node;
  }
  function icon(parent: ChildrenMixin, name: string, svg: string, size: number, x: number, y: number, opacity = 1) {
    const node = track(figma.createNodeFromSvg(svg));
    node.name = `Vector / ${name}`;
    node.resize(size, size);
    node.x = x;
    node.y = y;
    node.opacity = opacity;
    parent.appendChild(node);
    return node;
  }
  function hideChildren(parent: ChildrenMixin) {
    for (const child of parent.children) {
      child.visible = false;
      hidden.push(child.id);
      mark(child);
    }
  }
  function removeGenerated(parent: ChildrenMixin, name: string) {
    const prior = parent.children.find((node) => node.name === name);
    if (prior) prior.remove();
  }
  function decorateBox(node: FrameNode | ComponentNode, strong = false) {
    node.effects = panelEffects(strong);
    node.cornerSmoothing = 0.62;
  }
  function addArtworkSlot(parent: ChildrenMixin, name: string, x: number, y: number, width: number, height: number, svg: string) {
    const slot = frame(parent, name, width, height, x, y, [paint(colors.paleBlue, 0.55)], 18, colors.beige, 1.5);
    slot.dashPattern = [8, 7];
    icon(slot, `${name} Watermark`, svg, Math.min(width, height) * 0.54, (width - Math.min(width, height) * 0.54) / 2, (height - Math.min(width, height) * 0.54) / 2, 0.09);
    return slot;
  }
  function addStars(parent: ChildrenMixin, x: number, y: number, width: number, size = 24) {
    return text(parent, 'Stars', '★  ★  ★  ★', size, x, y, colors.gold, 'Extra Bold', width, 'CENTER');
  }
  function addBadge(parent: ChildrenMixin, accent: string, svg: string) {
    const badge = track(figma.createEllipse());
    badge.name = 'Element Badge';
    badge.resize(38, 38);
    badge.x = 14;
    badge.y = 14;
    badge.fills = [paint(accent)];
    badge.strokes = [paint(colors.white, 0.85)];
    badge.strokeWeight = 2;
    parent.appendChild(badge);
    icon(parent, 'Element Glyph', svg, 24, 21, 21);
  }

  const oldPetSet = section.findOne((node) => node.type === 'COMPONENT_SET' && node.name === 'Component / Pet Card');
  if (oldPetSet) {
    oldPetSet.visible = false;
    hidden.push(oldPetSet.id);
    mark(oldPetSet);
  }
  removeGenerated(section, 'Component / Reference V16 Pet Card');
  removeGenerated(section, 'Component / Reference V16 Weapon Card');

  function makeCardSet(kind: 'Pet' | 'Weapon') {
    const defs = kind === 'Pet'
      ? [
        ['Item=Tearay, State=Selected', colors.blue, PAW_SVG, true],
        ['Item=Emberfin, State=Owned', '#DC5A39', PAW_SVG, false],
        ['Item=Blockowl, State=Owned', '#2D86E8', PAW_SVG, false],
        ['Item=Amethyst, State=Owned', '#7B58B6', GEM_SVG, false],
        ['Item=Sunbit, State=Owned', colors.gold, GEM_SVG, false],
      ] as const
      : [
        ['Item=Skyedge, State=Selected', colors.blue, SWORD_SVG, true],
        ['Item=Emberblade, State=Owned', '#DC5A39', SWORD_SVG, false],
        ['Item=Tideguard, State=Owned', '#2D86E8', SHIELD_SVG, false],
        ['Item=Aetherstaff, State=Owned', '#7B58B6', GEM_SVG, false],
        ['Item=Sunlance, State=Owned', colors.gold, SWORD_SVG, false],
      ] as const;
    const components: ComponentNode[] = [];
    for (const [variant, accent, glyph, selected] of defs) {
      const card = track(figma.createComponent());
      card.name = variant;
      card.resize(205, 230);
      card.fills = [creamGradient()];
      card.strokes = [paint(selected ? colors.green : colors.beige)];
      card.strokeWeight = selected ? 4 : 1.5;
      card.cornerRadius = 20;
      card.cornerSmoothing = 0.62;
      card.effects = panelEffects(false);
      hubSection.appendChild(card);
      addBadge(card, accent, glyph);
      addArtworkSlot(card, 'Artwork Placeholder', 34, 34, 137, 148, glyph);
      addStars(card, 20, 190, 165, 21);
      if (selected) {
        const check = track(figma.createEllipse());
        check.name = 'Selected Check';
        check.resize(42, 42);
        check.x = 153;
        check.y = 10;
        check.fills = [paint(colors.green)];
        check.strokes = [paint(colors.white)];
        check.strokeWeight = 2;
        card.appendChild(check);
        icon(card, 'Selected Checkmark', CHECK_SVG, 28, 160, 17);
      }
      components.push(card);
    }
    const set = figma.combineAsVariants(components, hubSection);
    created.push(set.id);
    set.name = `Component / Reference V16 ${kind} Card`;
    set.description = `${kind} inventory card styled from player-hub-icon-driven-v16.png. Artwork remains a replaceable vector placeholder.`;
    set.x = kind === 'Pet' ? 900 : 2200;
    set.y = 1160;
    return { set, components };
  }

  const petCards = makeCardSet('Pet');
  const weaponCards = makeCardSet('Weapon');

  function cardRail(parent: ChildrenMixin, mode: 'Pet' | 'Weapon', x: number, y: number) {
    const rail = frame(parent, `${mode} / Reference Card Scroller`, 904, 230, x, y, [], 0);
    rail.layoutMode = 'HORIZONTAL';
    rail.primaryAxisSizingMode = 'FIXED';
    rail.counterAxisSizingMode = 'FIXED';
    rail.itemSpacing = 16;
    rail.paddingLeft = 0;
    rail.paddingRight = 0;
    rail.paddingTop = 0;
    rail.paddingBottom = 0;
    rail.overflowDirection = 'HORIZONTAL';
    rail.clipsContent = true;
    const source = mode === 'Pet' ? petCards.components : weaponCards.components;
    for (const component of source) {
      const instance = track(component.createInstance());
      rail.appendChild(instance);
    }
    return rail;
  }

  function currencyChip(parent: ChildrenMixin, name: string, value: string, svg: string, x: number, width: number) {
    const chip = frame(parent, name, width, 58, x, 36, [creamGradient()], 17, colors.beige, 1.5);
    decorateBox(chip, false);
    icon(chip, `${name} Icon`, svg, 42, 12, 8);
    text(chip, `${name} Value`, value, 25, 64, 13, colors.navy, 'Bold');
    return chip;
  }

  function quickCard(parent: ChildrenMixin, name: string, x: number, stroke: string, svg: string, selected: boolean) {
    const card = frame(parent, name, 150, 174, x, 724, [creamGradient()], 18, stroke, selected ? 4 : 2);
    decorateBox(card, false);
    addArtworkSlot(card, `${name} Artwork`, 30, 18, 90, 104, svg);
    addStars(card, 12, 136, 126, 17);
    if (selected) {
      const check = track(figma.createEllipse());
      check.name = `${name} Selected`;
      check.resize(34, 34);
      check.x = 108;
      check.y = 8;
      check.fills = [paint(colors.green)];
      card.appendChild(check);
      icon(card, `${name} Check`, CHECK_SVG, 22, 114, 14);
    }
    return card;
  }

  function statPill(parent: ChildrenMixin, name: string, value: string, svg: string, y: number) {
    const pill = frame(parent, name, 126, 44, 355, y, [creamGradient()], 12, colors.beige, 1.5);
    decorateBox(pill, false);
    icon(pill, `${name} Icon`, svg, 26, 10, 9);
    text(pill, `${name} Value`, value, 20, 46, 9, colors.navy, 'Bold');
    return pill;
  }

  type ScreenRefs = {
    petTab: FrameNode;
    weaponTab: FrameNode;
    petQuick: FrameNode;
    weaponQuick: FrameNode;
    exit: FrameNode;
    coin: FrameNode;
    gem: FrameNode;
  };
  const refs: ScreenRefs[] = [];

  function composeScreen(screen: FrameNode, mode: 'Pet' | 'Weapon') {
    const background = screen.findOne((node) => node.type === 'FRAME' && node.name === '1. Background & Player Stance');
    const menu = screen.findOne((node) => node.type === 'FRAME' && node.name === '2. UI Menu (Pet / Weapon)');
    const utilities = screen.findOne((node) => node.type === 'FRAME' && node.name === '3. Utilities Top Menu (Power Coin / Exit)');
    if (!background || background.type !== 'FRAME' || !menu || menu.type !== 'FRAME' || !utilities || utilities.type !== 'FRAME') {
      throw new Error(`Missing layer groups in ${screen.name}`);
    }

    removeGenerated(background, 'Reference V16 / Background Composition');
    removeGenerated(menu, 'Reference V16 / UI Composition');
    removeGenerated(utilities, 'Reference V16 / Utilities Composition');
    hideChildren(background);
    hideChildren(menu);
    hideChildren(utilities);

    screen.fills = [paint(colors.leftWarm)];
    mark(screen);

    const bg = frame(background, 'Reference V16 / Background Composition', 1680, 945, 0, 0, [], 0);
    bg.clipsContent = true;
    const left = frame(bg, 'Background / Artwork Placeholder', 634, 945, 0, 0, [warmGradient()], 0);
    left.effects = [{
      type: 'INNER_SHADOW', color: rgba(colors.navy, 0.1), offset: { x: -6, y: 0 }, radius: 16,
      spread: 0, visible: true, blendMode: 'NORMAL',
    }];
    const floorHalo = track(figma.createEllipse());
    floorHalo.name = 'Background / Player Grounding Halo';
    floorHalo.resize(470, 118);
    floorHalo.x = 76;
    floorHalo.y = 574;
    floorHalo.fills = [paint(colors.navy, 0.055)];
    floorHalo.strokes = [paint(colors.creamTop, 0.28)];
    floorHalo.strokeWeight = 2;
    left.appendChild(floorHalo);
    icon(left, 'Player Stance Watermark', PAW_SVG, 260, 182, 236, 0.055);

    const petQuick = quickCard(bg, 'Quick Slot / Pet', 38, mode === 'Pet' ? colors.green : colors.beige, PAW_SVG, mode === 'Pet');
    const weaponQuick = quickCard(bg, 'Quick Slot / Weapon', 196, mode === 'Weapon' ? colors.blue : colors.navy, SWORD_SVG, mode === 'Weapon');
    statPill(bg, 'Compact Stat / Attack', '325', SWORD_SVG, 758);
    statPill(bg, 'Compact Stat / Defense', '18%', SHIELD_SVG, 808);
    statPill(bg, 'Compact Stat / Cooldown', '150%', COIN_SVG, 858);

    const ui = frame(menu, 'Reference V16 / UI Composition', 1680, 945, 0, 0, [], 0);
    const rightSheet = frame(ui, 'UI / Right Cream Sheet', 1046, 945, 634, 0, [creamGradient()], 36);
    rightSheet.topLeftRadius = 36;
    rightSheet.bottomLeftRadius = 0;
    rightSheet.topRightRadius = 0;
    rightSheet.bottomRightRadius = 0;
    rightSheet.effects = [
      { type: 'DROP_SHADOW', color: rgba(colors.navy, 0.18), offset: { x: -7, y: 0 }, radius: 12, spread: 0, visible: true, blendMode: 'NORMAL' },
      { type: 'INNER_SHADOW', color: rgba(colors.white, 0.8), offset: { x: 2, y: 0 }, radius: 4, spread: 0, visible: true, blendMode: 'NORMAL' },
    ];
    text(ui, 'Screen Title', 'PLAYER HUB', 46, 686, 34, colors.navy, 'Extra Bold');

    const petTab = frame(ui, 'Button / Pet Tab Reference', 172, 78, 685, 105, [creamGradient()], 20, mode === 'Pet' ? colors.blue : colors.beige, mode === 'Pet' ? 3 : 1.5);
    petTab.bottomLeftRadius = 0;
    petTab.bottomRightRadius = 0;
    decorateBox(petTab, false);
    icon(petTab, 'Pet Tab Paw', PAW_SVG, 48, 62, 15);
    if (mode === 'Pet') {
      const underline = frame(petTab, 'Active Underline', 164, 5, 2, 71, [paint(colors.blue)], 2);
      underline.effects = [];
    }
    const weaponTab = frame(ui, 'Button / Weapon Tab Reference', 172, 78, 870, 105, [creamGradient()], 20, mode === 'Weapon' ? colors.blue : colors.beige, mode === 'Weapon' ? 3 : 1.5);
    weaponTab.bottomLeftRadius = 0;
    weaponTab.bottomRightRadius = 0;
    decorateBox(weaponTab, false);
    icon(weaponTab, 'Weapon Tab Sword', SWORD_SVG, 46, 63, 16);
    if (mode === 'Weapon') {
      const underline = frame(weaponTab, 'Active Underline', 164, 5, 2, 71, [paint(colors.blue)], 2);
      underline.effects = [];
    }

    const panel = frame(ui, `${mode} / Main Reference Panel`, 990, 724, 655, 180, [creamGradient()], 30, colors.navy, 2);
    decorateBox(panel, true);
    cardRail(panel, mode, 38, 32);
    const detail = frame(panel, `${mode} / Reference Detail Panel`, 904, 398, 38, 290, [creamGradient()], 22, colors.beige, 1.5);
    decorateBox(detail, false);
    const titleValue = mode === 'Pet' ? 'TEARAY' : 'SKYEDGE';
    const visualSvg = mode === 'Pet' ? PAW_SVG : SWORD_SVG;
    text(detail, 'Item Name', titleValue, 36, 40, 34, colors.navy, 'Extra Bold');
    addStars(detail, 35, 90, 250, 25);
    const primaryStat = frame(detail, 'Detail Stat / Primary', 226, 84, 28, 166, [creamGradient()], 16, colors.beige, 1.5);
    decorateBox(primaryStat, false);
    icon(primaryStat, 'Primary Stat Icon', mode === 'Pet' ? SHIELD_SVG : SWORD_SVG, 54, 20, 15);
    text(primaryStat, 'Primary Stat Value', mode === 'Pet' ? '18%' : '325', 28, 88, 23, colors.navy, 'Bold');
    const secondaryStat = frame(detail, 'Detail Stat / Secondary', 226, 84, 28, 266, [creamGradient()], 16, colors.beige, 1.5);
    decorateBox(secondaryStat, false);
    icon(secondaryStat, 'Secondary Stat Icon', mode === 'Pet' ? SNOW_SVG : GEM_SVG, 54, 20, 15);
    if (mode === 'Weapon') text(secondaryStat, 'Secondary Stat Value', '3 / 4', 26, 88, 24, colors.navy, 'Bold');
    const artwork = addArtworkSlot(detail, `${mode} / Large Artwork Placeholder`, 318, 28, 550, 342, visualSvg);
    artwork.fills = [paint(colors.creamTop, 0.34)];
    artwork.strokes = [paint(colors.beige, 0.44)];
    artwork.dashPattern = [12, 10];

    const util = frame(utilities, 'Reference V16 / Utilities Composition', 1680, 110, 0, 0, [], 0);
    const coin = currencyChip(util, 'Power Coin', '25,480', COIN_SVG, 1138, 196);
    const gem = currencyChip(util, 'Power Crystal', '1,235', GEM_SVG, 1352, 196);
    const exit = frame(util, 'Button / Exit Reference', 64, 64, 1577, 33, [creamGradient()], 16, colors.beigeDark, 1.5);
    decorateBox(exit, false);
    icon(exit, 'Close Reference', CLOSE_SVG, 38, 13, 13);

    refs.push({ petTab, weaponTab, petQuick, weaponQuick, exit, coin, gem });
    return { panel, detail };
  }

  const petLayout = composeScreen(petScreen, 'Pet');
  const weaponLayout = composeScreen(weaponScreen, 'Weapon');

  const smart: Transition = { type: 'SMART_ANIMATE', easing: { type: 'EASE_OUT_BACK' }, duration: 0.3 };
  const fade: Transition = { type: 'DISSOLVE', easing: { type: 'EASE_OUT' }, duration: 0.18 };
  const toast = section.findOne((node) => node.type === 'FRAME' && node.name === 'Prototype Overlay / Action Confirmed');
  const toPet: Reaction[] = [{ trigger: { type: 'ON_CLICK' }, actions: [{ type: 'NODE', destinationId: petScreen.id, navigation: 'NAVIGATE', transition: smart, resetScrollPosition: false }] }];
  const toWeapon: Reaction[] = [{ trigger: { type: 'ON_CLICK' }, actions: [{ type: 'NODE', destinationId: weaponScreen.id, navigation: 'NAVIGATE', transition: smart, resetScrollPosition: false }] }];
  const overlay: Reaction[] = toast && toast.type === 'FRAME' ? [{ trigger: { type: 'ON_CLICK' }, actions: [{ type: 'NODE', destinationId: toast.id, navigation: 'OVERLAY', transition: fade }] }] : [];

  await Promise.all([
    refs[0].petTab.setReactionsAsync([]),
    refs[0].weaponTab.setReactionsAsync(toWeapon),
    refs[1].petTab.setReactionsAsync(toPet),
    refs[1].weaponTab.setReactionsAsync([]),
    refs[0].petQuick.setReactionsAsync([]),
    refs[0].weaponQuick.setReactionsAsync(toWeapon),
    refs[1].petQuick.setReactionsAsync(toPet),
    refs[1].weaponQuick.setReactionsAsync([]),
    refs[0].exit.setReactionsAsync([{ trigger: { type: 'ON_CLICK' }, actions: [{ type: 'BACK' }] }]),
    refs[1].exit.setReactionsAsync([{ trigger: { type: 'ON_CLICK' }, actions: [{ type: 'BACK' }] }]),
    refs[0].coin.setReactionsAsync(overlay),
    refs[0].gem.setReactionsAsync(overlay),
    refs[1].coin.setReactionsAsync(overlay),
    refs[1].gem.setReactionsAsync(overlay),
  ]);
  for (const ref of refs) interactions.push(ref.petTab.id, ref.weaponTab.id, ref.petQuick.id, ref.weaponQuick.id, ref.exit.id, ref.coin.id, ref.gem.id);

  const note = section.findOne((node) => node.type === 'TEXT' && node.name === 'Section Note');
  if (note && note.type === 'TEXT') {
    note.fontName = { family: 'Inter', style: 'Regular' };
    note.characters = 'Reference v16 layout • icon-led navigation • minimal copy • horizontal card scroll • replaceable artwork slots';
    mark(note);
  }

  figma.currentPage.selection = [petScreen, weaponScreen];
  figma.viewport.scrollAndZoomIntoView([petScreen, weaponScreen]);
  return {
    sectionNodeId: section.id,
    screenNodeIds: [petScreen.id, weaponScreen.id],
    panelNodeIds: [petLayout.panel.id, weaponLayout.panel.id],
    detailNodeIds: [petLayout.detail.id, weaponLayout.detail.id],
    componentSetNodeIds: [petCards.set.id, weaponCards.set.id],
    createdNodeIds: created,
    mutatedNodeIds: Array.from(mutated),
    hiddenPreviousNodeIds: hidden,
    interactiveNodeIds: interactions,
  };
}

async function buildRebirthWireframe() {
  await Promise.all([
    figma.loadFontAsync({ family: 'Inter', style: 'Regular' }),
    figma.loadFontAsync({ family: 'Inter', style: 'Bold' }),
    figma.loadFontAsync({ family: 'Inter', style: 'Extra Bold' }),
  ]);

  const existing = figma.currentPage.children.find(
    (node) => node.type === 'SECTION' && node.name === 'Generated / Rebirth UI Window',
  );
  if (existing && existing.type === 'SECTION') {
    existing.remove();
  }

  const created: string[] = [];
  const interactive: string[] = [];
  const variableIds: string[] = [];
  const styleIds: string[] = [];
  const colors = {
    navy: '#0A2E5F',
    navyDeep: '#071F43',
    blue: '#215EC2',
    blueLight: '#4A91F0',
    blueSurface: '#EAF4FC',
    blueSurfaceDeep: '#CFE2F2',
    cream: '#FFF8E8',
    creamDeep: '#F4E4C5',
    white: '#FFFFFF',
    gold: '#FFC83D',
    goldDeep: '#D58B13',
    orange: '#ED633D',
    orangeDeep: '#A83D25',
    warning: '#E85D35',
    muted: '#6B7F98',
  };

  function track<T extends SceneNode>(node: T): T {
    created.push(node.id);
    return node;
  }
  function rgb(hex: string): RGB {
    return parseColor(hex);
  }
  function rgba(hex: string, a: number): RGBA {
    return { ...parseColor(hex), a };
  }
  function solidPaint(hex: string, opacity = 1): SolidPaint {
    return { type: 'SOLID', color: rgb(hex), opacity };
  }
  function gradient(top: string, bottom: string): GradientPaint {
    return {
      type: 'GRADIENT_LINEAR',
      gradientTransform: [[0, 1, 0], [-1, 0, 1]],
      gradientStops: [
        { position: 0, color: rgba(top, 1) },
        { position: 1, color: rgba(bottom, 1) },
      ],
    };
  }
  function icon(parent: ChildrenMixin, name: string, svg: string, size: number) {
    const node = track(figma.createNodeFromSvg(svg));
    node.name = `Vector / ${name}`;
    node.resize(size, size);
    parent.appendChild(node);
    return node;
  }

  const collections = await figma.variables.getLocalVariableCollectionsAsync();
  let collection = collections.find((item) => item.name === 'Rebirth / Theme');
  if (!collection) {
    collection = figma.variables.createVariableCollection('Rebirth / Theme');
    collection.renameMode(collection.defaultModeId, 'Light Fantasy');
  }
  const modeId = collection.defaultModeId;
  const localVariables = await figma.variables.getLocalVariablesAsync();
  function colorVariable(name: string, value: string) {
    let variable = localVariables.find(
      (item) => item.name === name && item.variableCollectionId === collection!.id,
    );
    if (!variable) variable = figma.variables.createVariable(name, collection!, 'COLOR');
    variable.scopes = ['FRAME_FILL', 'SHAPE_FILL', 'TEXT_FILL', 'STROKE_COLOR'];
    variable.setValueForMode(modeId, rgb(value));
    variable.setVariableCodeSyntax('WEB', `--${name.replace(/\//g, '-').toLowerCase()}`);
    variableIds.push(variable.id);
    return variable;
  }
  function floatVariable(name: string, value: number, scopes: VariableScope[]) {
    let variable = localVariables.find(
      (item) => item.name === name && item.variableCollectionId === collection!.id,
    );
    if (!variable) variable = figma.variables.createVariable(name, collection!, 'FLOAT');
    variable.scopes = scopes;
    variable.setValueForMode(modeId, value);
    variableIds.push(variable.id);
    return variable;
  }
  const vars = {
    navy: colorVariable('color/navy', colors.navy),
    blue: colorVariable('color/header-blue', colors.blue),
    surface: colorVariable('color/surface-blue', colors.blueSurface),
    cream: colorVariable('color/surface-cream', colors.cream),
    gold: colorVariable('color/action-gold', colors.gold),
    orange: colorVariable('color/close-orange', colors.orange),
    white: colorVariable('color/text-inverse', colors.white),
    warning: colorVariable('color/warning', colors.warning),
    gap12: floatVariable('spacing/12', 12, ['GAP']),
    gap18: floatVariable('spacing/18', 18, ['GAP']),
    radius16: floatVariable('radius/16', 16, ['CORNER_RADIUS']),
    radius28: floatVariable('radius/28', 28, ['CORNER_RADIUS']),
  };
  function boundPaint(variable: Variable, fallback: string): SolidPaint {
    return figma.variables.setBoundVariableForPaint(solidPaint(fallback), 'color', variable);
  }

  const localEffects = await figma.getLocalEffectStylesAsync();
  function ensureEffectStyle(name: string, effects: Effect[]) {
    let style = localEffects.find((item) => item.name === name);
    if (!style) {
      style = figma.createEffectStyle();
      style.name = name;
    }
    style.effects = effects;
    styleIds.push(style.id);
    return style;
  }
  const modalShadow = ensureEffectStyle('Rebirth / Modal Shadow', [
    { type: 'DROP_SHADOW', color: rgba(colors.navyDeep, 0.42), offset: { x: 0, y: 12 }, radius: 18, spread: 0, visible: true, blendMode: 'NORMAL' },
    { type: 'INNER_SHADOW', color: rgba(colors.white, 0.6), offset: { x: 0, y: 3 }, radius: 4, spread: 0, visible: true, blendMode: 'NORMAL' },
  ]);
  const raisedShadow = ensureEffectStyle('Rebirth / Raised Surface', [
    { type: 'DROP_SHADOW', color: rgba(colors.navy, 0.18), offset: { x: 0, y: 5 }, radius: 8, spread: 0, visible: true, blendMode: 'NORMAL' },
    { type: 'INNER_SHADOW', color: rgba(colors.white, 0.76), offset: { x: 0, y: 2 }, radius: 3, spread: 0, visible: true, blendMode: 'NORMAL' },
    { type: 'INNER_SHADOW', color: rgba(colors.navy, 0.12), offset: { x: 0, y: -2 }, radius: 3, spread: 0, visible: true, blendMode: 'NORMAL' },
  ]);

  const localTextStyles = await figma.getLocalTextStylesAsync();
  function ensureTextStyle(name: string, size: number, styleName: 'Bold' | 'Extra Bold') {
    let style = localTextStyles.find((item) => item.name === name);
    if (!style) {
      style = figma.createTextStyle();
      style.name = name;
    }
    style.fontName = { family: 'Inter', style: styleName };
    style.fontSize = size;
    style.lineHeight = { unit: 'AUTO' };
    styleIds.push(style.id);
    return style;
  }
  const displayStyle = ensureTextStyle('Rebirth / Display', 56, 'Extra Bold');
  const numberStyle = ensureTextStyle('Rebirth / Number', 36, 'Extra Bold');
  const buttonStyle = ensureTextStyle('Rebirth / Button', 40, 'Extra Bold');

  function auto(
    parent: ChildrenMixin,
    name: string,
    width: number,
    height: number,
    direction: 'HORIZONTAL' | 'VERTICAL',
    gap: number,
    padding: number,
    fills: Paint[],
    radius: number,
    stroke?: Paint,
    strokeWeight = 0,
  ) {
    const node = track(figma.createFrame());
    node.name = name;
    node.resize(width, height);
    node.layoutMode = direction;
    node.primaryAxisSizingMode = 'FIXED';
    node.counterAxisSizingMode = 'FIXED';
    node.itemSpacing = gap;
    node.paddingTop = padding;
    node.paddingRight = padding;
    node.paddingBottom = padding;
    node.paddingLeft = padding;
    node.fills = fills;
    node.cornerRadius = radius;
    node.cornerSmoothing = 0.62;
    node.strokes = stroke ? [stroke] : [];
    node.strokeWeight = stroke ? strokeWeight : 0;
    parent.appendChild(node);
    return node;
  }
  function text(
    parent: ChildrenMixin,
    name: string,
    value: string,
    size: number,
    style: 'Regular' | 'Bold' | 'Extra Bold',
    colorVariable: Variable,
    width?: number,
    align: 'LEFT' | 'CENTER' | 'RIGHT' = 'LEFT',
  ) {
    const node = track(figma.createText());
    node.name = name;
    node.fontName = { family: 'Inter', style };
    node.fontSize = size;
    node.characters = value;
    node.fills = [boundPaint(colorVariable, colors.navy)];
    node.textAlignHorizontal = align;
    if (width) {
      node.textAutoResize = 'HEIGHT';
      node.resize(width, Math.max(24, size * 1.3));
    } else {
      node.textAutoResize = 'WIDTH_AND_HEIGHT';
    }
    parent.appendChild(node);
    return node;
  }
  function spacer(parent: ChildrenMixin, width: number, height: number) {
    const node = auto(parent, 'Spacer', width, height, 'HORIZONTAL', 0, 0, [], 0);
    return node;
  }

  let maxX = 0;
  for (const child of figma.currentPage.children) maxX = Math.max(maxX, child.x + child.width);
  const section = figma.createSection();
  section.name = 'Generated / Rebirth UI Window';
  section.resizeWithoutConstraints(2460, 1180);
  section.x = maxX + 240;
  section.y = 0;
  figma.currentPage.appendChild(section);
  created.push(section.id);
  section.fills = [solidPaint('#D9E2EC')];

  const sectionTitle = text(section, 'Section Title', 'REBIRTH — COMPLETE UI WINDOW', 28, 'Extra Bold', vars.navy);
  sectionTitle.x = 40;
  sectionTitle.y = 36;
  const sectionNote = text(section, 'Section Note', 'Window only • Auto Layout • editable vector icons • functional confirmation flow', 14, 'Regular', vars.navy);
  sectionNote.x = 40;
  sectionNote.y = 76;

  type CompareDef = { name: string; icon: string; before: string; after: string; warning?: boolean };
  const compareDefs: CompareDef[] = [
    { name: 'Level', icon: SWORD_SVG, before: '25', after: '31' },
    { name: 'Area', icon: MAP_SVG, before: '3', after: '1', warning: true },
    { name: 'Reward', icon: COIN_SVG, before: '—', after: '12' },
  ];
  const rowComponents: ComponentNode[] = [];
  for (const def of compareDefs) {
    const component = track(figma.createComponent());
    component.name = `Type=${def.name}`;
    component.resize(868, 112);
    component.layoutMode = 'HORIZONTAL';
    component.primaryAxisSizingMode = 'FIXED';
    component.counterAxisSizingMode = 'FIXED';
    component.counterAxisAlignItems = 'CENTER';
    component.primaryAxisAlignItems = 'SPACE_BETWEEN';
    component.itemSpacing = 22;
    component.fills = [];
    section.appendChild(component);

    const before = auto(component, 'Before Value', 340, 104, 'HORIZONTAL', 22, 14, [boundPaint(vars.surface, colors.blueSurface)], 18, solidPaint('#AFCBE2'), 2);
    before.counterAxisAlignItems = 'CENTER';
    await before.setEffectStyleIdAsync(raisedShadow.id);
    const beforeIcon = auto(before, 'Icon Slot', 80, 76, 'HORIZONTAL', 0, 8, [solidPaint(colors.white, 0.24)], 14);
    beforeIcon.primaryAxisAlignItems = 'CENTER';
    beforeIcon.counterAxisAlignItems = 'CENTER';
    icon(beforeIcon, `${def.name} Before`, def.icon, 62);
    const beforeText = text(before, 'Before Number', def.before, 36, 'Extra Bold', vars.navy, 178, 'CENTER');
    await beforeText.setTextStyleIdAsync(numberStyle.id);

    const arrowSlot = auto(component, 'Direction', 100, 72, 'HORIZONTAL', 0, 0, [], 0);
    arrowSlot.primaryAxisAlignItems = 'CENTER';
    arrowSlot.counterAxisAlignItems = 'CENTER';
    const arrow = text(arrowSlot, 'Arrow', '➜', 52, 'Extra Bold', vars.blue, 86, 'CENTER');
    arrow.effects = [{ type: 'DROP_SHADOW', color: rgba(colors.navy, 0.24), offset: { x: 0, y: 3 }, radius: 2, spread: 0, visible: true, blendMode: 'NORMAL' }];

    const after = auto(component, 'After Value', 340, 104, 'HORIZONTAL', 18, 14, [boundPaint(vars.surface, colors.blueSurface)], 18, solidPaint('#AFCBE2'), 2);
    after.counterAxisAlignItems = 'CENTER';
    await after.setEffectStyleIdAsync(raisedShadow.id);
    const afterIcon = auto(after, 'Icon Slot', 80, 76, 'HORIZONTAL', 0, 8, [solidPaint(colors.white, 0.24)], 14);
    afterIcon.primaryAxisAlignItems = 'CENTER';
    afterIcon.counterAxisAlignItems = 'CENTER';
    icon(afterIcon, `${def.name} After`, def.icon, 62);
    const afterText = text(after, 'After Number', def.after, 36, 'Extra Bold', vars.navy, def.warning ? 126 : 178, 'CENTER');
    await afterText.setTextStyleIdAsync(numberStyle.id);
    if (def.warning) {
      const warning = auto(after, 'Warning Badge', 46, 46, 'HORIZONTAL', 0, 0, [boundPaint(vars.warning, colors.warning)], 14, solidPaint(colors.orangeDeep), 2);
      warning.primaryAxisAlignItems = 'CENTER';
      warning.counterAxisAlignItems = 'CENTER';
      const warningText = text(warning, 'Warning', '!', 28, 'Extra Bold', vars.white, 30, 'CENTER');
      warningText.y = 2;
    }
    rowComponents.push(component);
  }
  const rowSet = figma.combineAsVariants(rowComponents, section);
  created.push(rowSet.id);
  rowSet.name = 'Component / Rebirth Comparison Row';
  rowSet.description = 'Before-and-after Rebirth comparison row. Variants cover Level, Area reset, and Reward.';
  rowSet.x = 1210;
  rowSet.y = 150;
  rowSet.layoutMode = 'VERTICAL';
  rowSet.primaryAxisSizingMode = 'AUTO';
  rowSet.counterAxisSizingMode = 'FIXED';
  rowSet.itemSpacing = 20;
  rowSet.paddingTop = 24;
  rowSet.paddingRight = 24;
  rowSet.paddingBottom = 24;
  rowSet.paddingLeft = 24;
  rowSet.resize(920, 420);

  async function makeButtonVariant(state: 'Default' | 'Disabled') {
    const component = track(figma.createComponent());
    component.name = `State=${state}`;
    component.resize(560, 104);
    component.layoutMode = 'HORIZONTAL';
    component.primaryAxisSizingMode = 'FIXED';
    component.counterAxisSizingMode = 'FIXED';
    component.primaryAxisAlignItems = 'CENTER';
    component.counterAxisAlignItems = 'CENTER';
    component.itemSpacing = 18;
    component.paddingTop = 12;
    component.paddingRight = 28;
    component.paddingBottom = 12;
    component.paddingLeft = 28;
    component.cornerRadius = 22;
    component.cornerSmoothing = 0.62;
    component.fills = state === 'Default'
      ? [gradient('#FFD957', colors.gold)]
      : [solidPaint('#B8C3CE')];
    component.strokes = [solidPaint(state === 'Default' ? colors.goldDeep : colors.muted)];
    component.strokeWeight = 3;
    await component.setEffectStyleIdAsync(raisedShadow.id);
    component.opacity = state === 'Default' ? 1 : 0.68;
    section.appendChild(component);
    icon(component, 'Rebirth Action', REBIRTH_SVG, 78);
    const label = text(component, 'Label', 'REBIRTH', 40, 'Extra Bold', vars.navy);
    await label.setTextStyleIdAsync(buttonStyle.id);
    return component;
  }
  const buttonDefault = await makeButtonVariant('Default');
  const buttonDisabled = await makeButtonVariant('Disabled');
  const buttonSet = figma.combineAsVariants([buttonDefault, buttonDisabled], section);
  created.push(buttonSet.id);
  buttonSet.name = 'Component / Rebirth Primary Button';
  buttonSet.description = 'Primary Rebirth action with enabled and disabled resource states.';
  buttonSet.x = 1210;
  buttonSet.y = 620;
  buttonSet.layoutMode = 'VERTICAL';
  buttonSet.primaryAxisSizingMode = 'AUTO';
  buttonSet.counterAxisSizingMode = 'FIXED';
  buttonSet.itemSpacing = 18;
  buttonSet.paddingTop = 24;
  buttonSet.paddingRight = 24;
  buttonSet.paddingBottom = 24;
  buttonSet.paddingLeft = 24;
  buttonSet.resize(610, 270);

  const windowFrame = auto(section, 'Rebirth / Window', 980, 820, 'VERTICAL', 0, 0, [], 32, solidPaint(colors.navyDeep), 6);
  windowFrame.x = 80;
  windowFrame.y = 130;
  windowFrame.clipsContent = true;
  await windowFrame.setEffectStyleIdAsync(modalShadow.id);
  windowFrame.setBoundVariable('cornerRadius', vars.radius28);

  const header = auto(windowFrame, 'Rebirth / Header', 968, 130, 'HORIZONTAL', 18, 22, [gradient(colors.blueLight, colors.blue)], 0);
  header.primaryAxisAlignItems = 'SPACE_BETWEEN';
  header.counterAxisAlignItems = 'CENTER';
  header.layoutSizingHorizontal = 'FILL';
  header.setBoundVariable('itemSpacing', vars.gap18);
  spacer(header, 72, 72);
  const titleGroup = auto(header, 'Rebirth / Title Group', 630, 98, 'HORIZONTAL', 18, 0, [], 0);
  titleGroup.primaryAxisAlignItems = 'CENTER';
  titleGroup.counterAxisAlignItems = 'CENTER';
  icon(titleGroup, 'Rebirth Mark', REBIRTH_SVG, 96);
  const title = text(titleGroup, 'Title', 'REBIRTH', 56, 'Extra Bold', vars.white);
  await title.setTextStyleIdAsync(displayStyle.id);
  title.effects = [
    { type: 'DROP_SHADOW', color: rgba(colors.navyDeep, 0.74), offset: { x: 0, y: 5 }, radius: 2, spread: 0, visible: true, blendMode: 'NORMAL' },
  ];
  const close = auto(header, 'Button / Close', 72, 72, 'HORIZONTAL', 0, 8, [gradient('#FF835C', colors.orange)], 18, solidPaint(colors.orangeDeep), 3);
  close.primaryAxisAlignItems = 'CENTER';
  close.counterAxisAlignItems = 'CENTER';
  await close.setEffectStyleIdAsync(raisedShadow.id);
  icon(close, 'Close', CLOSE_SVG, 48);
  interactive.push(close.id);

  const body = auto(windowFrame, 'Rebirth / Body', 968, 678, 'VERTICAL', 16, 28, [gradient('#FFFDF2', colors.cream)], 0);
  body.counterAxisAlignItems = 'CENTER';
  body.layoutSizingHorizontal = 'FILL';
  body.layoutSizingVertical = 'FILL';

  const comparisonList = auto(body, 'Rebirth / Comparison List', 868, 364, 'VERTICAL', 14, 0, [], 0);
  comparisonList.counterAxisAlignItems = 'CENTER';
  comparisonList.setBoundVariable('itemSpacing', vars.gap12);
  for (const component of rowComponents) {
    const instance = track(component.createInstance());
    comparisonList.appendChild(instance);
  }

  const progress = auto(body, 'Rebirth / Progress', 868, 70, 'HORIZONTAL', 0, 0, [gradient('#234D8B', colors.navy)], 16, solidPaint(colors.navyDeep), 3);
  progress.counterAxisAlignItems = 'CENTER';
  await progress.setEffectStyleIdAsync(raisedShadow.id);
  const progressIcon = auto(progress, 'Progress Icon', 108, 64, 'HORIZONTAL', 0, 6, [gradient(colors.blueLight, '#2E7DE0')], 12);
  progressIcon.primaryAxisAlignItems = 'CENTER';
  progressIcon.counterAxisAlignItems = 'CENTER';
  icon(progressIcon, 'Progress Paw', PAW_SVG, 46);
  const progressValue = text(progress, 'Progress Value', '3 / 50', 34, 'Extra Bold', vars.white, 724, 'CENTER');
  progressValue.y = 8;

  const rebirthButton = track(buttonDefault.createInstance());
  rebirthButton.name = 'Button / Rebirth';
  body.appendChild(rebirthButton);
  interactive.push(rebirthButton.id);

  async function overlayFrame(name: string, width: number, height: number, x: number, y: number) {
    const node = auto(section, name, width, height, 'VERTICAL', 16, 24, [gradient('#FFFDF2', colors.cream)], 26, solidPaint(colors.navyDeep), 5);
    node.x = x;
    node.y = y;
    node.counterAxisAlignItems = 'CENTER';
    await node.setEffectStyleIdAsync(modalShadow.id);
    return node;
  }
  const confirmOverlay = await overlayFrame('Rebirth / Confirmation Overlay', 500, 300, 1840, 140);
  icon(confirmOverlay, 'Confirm Rebirth', REBIRTH_SVG, 76);
  text(confirmOverlay, 'Confirm Title', 'CONFIRM REBIRTH?', 28, 'Extra Bold', vars.navy, 420, 'CENTER');
  text(confirmOverlay, 'Confirm Copy', 'Area progress resets to 1.', 15, 'Regular', vars.navy, 420, 'CENTER');
  const confirmActions = auto(confirmOverlay, 'Confirmation Actions', 420, 62, 'HORIZONTAL', 14, 0, [], 0);
  confirmActions.primaryAxisAlignItems = 'CENTER';
  confirmActions.counterAxisAlignItems = 'CENTER';
  const cancel = auto(confirmActions, 'Button / Cancel', 180, 58, 'HORIZONTAL', 0, 10, [boundPaint(vars.surface, colors.blueSurface)], 16, solidPaint('#AFCBE2'), 2);
  cancel.primaryAxisAlignItems = 'CENTER';
  cancel.counterAxisAlignItems = 'CENTER';
  text(cancel, 'Label', 'CANCEL', 18, 'Extra Bold', vars.navy);
  const confirm = auto(confirmActions, 'Button / Confirm', 210, 58, 'HORIZONTAL', 0, 10, [gradient('#FFD957', colors.gold)], 16, solidPaint(colors.goldDeep), 2);
  confirm.primaryAxisAlignItems = 'CENTER';
  confirm.counterAxisAlignItems = 'CENTER';
  text(confirm, 'Label', 'REBIRTH', 18, 'Extra Bold', vars.navy);
  interactive.push(cancel.id, confirm.id);

  const successOverlay = await overlayFrame('Rebirth / Success Overlay', 440, 220, 1840, 520);
  icon(successOverlay, 'Success', CHECK_SVG, 72);
  text(successOverlay, 'Success Title', 'REBIRTH READY', 26, 'Extra Bold', vars.navy, 360, 'CENTER');
  text(successOverlay, 'Success Copy', 'New progression values applied.', 14, 'Regular', vars.navy, 360, 'CENTER');

  const openTransition: Transition = { type: 'MOVE_IN', direction: 'TOP', matchLayers: false, easing: { type: 'EASE_OUT_BACK' }, duration: 0.3 };
  const dissolve: Transition = { type: 'DISSOLVE', easing: { type: 'EASE_OUT' }, duration: 0.18 };
  await Promise.all([
    close.setReactionsAsync([{ trigger: { type: 'ON_CLICK' }, actions: [{ type: 'BACK' }] }]),
    rebirthButton.setReactionsAsync([{ trigger: { type: 'ON_CLICK' }, actions: [{ type: 'NODE', destinationId: confirmOverlay.id, navigation: 'OVERLAY', transition: openTransition }] }]),
    cancel.setReactionsAsync([{ trigger: { type: 'ON_CLICK' }, actions: [{ type: 'CLOSE' }] }]),
    confirm.setReactionsAsync([{ trigger: { type: 'ON_CLICK' }, actions: [{ type: 'NODE', destinationId: successOverlay.id, navigation: 'SWAP', transition: dissolve }] }]),
    successOverlay.setReactionsAsync([{ trigger: { type: 'AFTER_TIMEOUT', timeout: 1.2 }, actions: [{ type: 'CLOSE' }] }]),
  ]);

  figma.currentPage.selection = [windowFrame];
  figma.viewport.scrollAndZoomIntoView([windowFrame]);
  return {
    reused: false,
    sectionNodeId: section.id,
    windowNodeId: windowFrame.id,
    comparisonComponentSetNodeId: rowSet.id,
    buttonComponentSetNodeId: buttonSet.id,
    confirmationOverlayNodeId: confirmOverlay.id,
    successOverlayNodeId: successOverlay.id,
    variableCollectionId: collection.id,
    variableIds,
    styleIds,
    createdNodeIds: created,
    interactiveNodeIds: interactive,
  };
}

async function finalizePlayerHubInteractions() {
  const section = figma.currentPage.children.find(
    (node) => node.type === 'SECTION' && (
      node.name === 'Generated / Complete Player Hub UI' || node.name === 'Player Hub UI'
    ),
  );
  if (!section || section.type !== 'SECTION') {
    throw new Error('Generated Player Hub section was not found.');
  }

  function namedFrame(root: ChildrenMixin, name: string): FrameNode {
    const node = root.findOne((candidate) => candidate.type === 'FRAME' && candidate.name === name);
    if (!node || node.type !== 'FRAME') throw new Error(`Player Hub frame not found: ${name}`);
    return node;
  }

  const petScreen = namedFrame(section, 'Player Hub / Pet State');
  const weaponScreen = namedFrame(section, 'Player Hub / Weapon State');
  const toast = namedFrame(section, 'Prototype Overlay / Action Confirmed');
  const petTabOnPet = namedFrame(petScreen, 'Button / Pet Tab');
  const weaponTabOnPet = namedFrame(petScreen, 'Button / Weapon Tab');
  const petTabOnWeapon = namedFrame(weaponScreen, 'Button / Pet Tab');
  const weaponTabOnWeapon = namedFrame(weaponScreen, 'Button / Weapon Tab');
  const exitOnPet = namedFrame(petScreen, 'Button / Exit');
  const exitOnWeapon = namedFrame(weaponScreen, 'Button / Exit');
  const equip = namedFrame(petScreen, 'Button / Equip Pet');
  const ascend = namedFrame(weaponScreen, 'Button / Ascend Weapon');
  const petQuickOnPet = namedFrame(petScreen, 'Quick Slot / Pet');
  const weaponQuickOnPet = namedFrame(petScreen, 'Quick Slot / Weapon');
  const petQuickOnWeapon = namedFrame(weaponScreen, 'Quick Slot / Pet');
  const weaponQuickOnWeapon = namedFrame(weaponScreen, 'Quick Slot / Weapon');
  const coinOnPet = namedFrame(petScreen, 'Power Coin');
  const coinOnWeapon = namedFrame(weaponScreen, 'Power Coin');

  const smart: Transition = {
    type: 'SMART_ANIMATE',
    easing: { type: 'EASE_OUT_BACK' },
    duration: 0.3,
  };
  const dissolve: Transition = {
    type: 'DISSOLVE',
    easing: { type: 'EASE_OUT' },
    duration: 0.18,
  };
  const toPet: Reaction[] = [{
    trigger: { type: 'ON_CLICK' },
    actions: [{ type: 'NODE', destinationId: petScreen.id, navigation: 'NAVIGATE', transition: smart, resetScrollPosition: false }],
  }];
  const toWeapon: Reaction[] = [{
    trigger: { type: 'ON_CLICK' },
    actions: [{ type: 'NODE', destinationId: weaponScreen.id, navigation: 'NAVIGATE', transition: smart, resetScrollPosition: false }],
  }];
  const overlay: Reaction[] = [{
    trigger: { type: 'ON_CLICK' },
    actions: [{ type: 'NODE', destinationId: toast.id, navigation: 'OVERLAY', transition: dissolve }],
  }];

  await weaponTabOnPet.setReactionsAsync(toWeapon);
  await petTabOnWeapon.setReactionsAsync(toPet);
  await weaponQuickOnPet.setReactionsAsync(toWeapon);
  await petQuickOnWeapon.setReactionsAsync(toPet);
  await petTabOnPet.setReactionsAsync([]);
  await weaponTabOnWeapon.setReactionsAsync([]);
  await petQuickOnPet.setReactionsAsync([]);
  await weaponQuickOnWeapon.setReactionsAsync([]);
  await exitOnPet.setReactionsAsync([{ trigger: { type: 'ON_CLICK' }, actions: [{ type: 'BACK' }] }]);
  await exitOnWeapon.setReactionsAsync([{ trigger: { type: 'ON_CLICK' }, actions: [{ type: 'BACK' }] }]);
  await equip.setReactionsAsync(overlay);
  await ascend.setReactionsAsync(overlay);
  await coinOnPet.setReactionsAsync(overlay);
  await coinOnWeapon.setReactionsAsync(overlay);
  await toast.setReactionsAsync([{ trigger: { type: 'AFTER_TIMEOUT', timeout: 1.2 }, actions: [{ type: 'CLOSE' }] }]);

  const mutatedNodeIds = [
    petTabOnPet, weaponTabOnPet, petTabOnWeapon, weaponTabOnWeapon,
    exitOnPet, exitOnWeapon, equip, ascend,
    petQuickOnPet, weaponQuickOnPet, petQuickOnWeapon, weaponQuickOnWeapon,
    coinOnPet, coinOnWeapon, toast,
  ].map((node) => node.id);
  figma.currentPage.selection = [petScreen, weaponScreen];
  figma.viewport.scrollAndZoomIntoView([petScreen, weaponScreen]);
  return { sectionNodeId: section.id, petScreenNodeId: petScreen.id, weaponScreenNodeId: weaponScreen.id, mutatedNodeIds };
}

async function buildPlayerHubUi(command: BridgeCommand) {
  await Promise.all([
    figma.loadFontAsync({ family: 'Inter', style: 'Regular' }),
    figma.loadFontAsync({ family: 'Inter', style: 'Bold' }),
    figma.loadFontAsync({ family: 'Inter', style: 'Extra Bold' }),
  ]);

  const existing = figma.currentPage.children.find(
    (node) => node.type === 'SECTION' && (
      node.name === 'Generated / Complete Player Hub UI' || node.name === 'Player Hub UI'
    ),
  );
  if (existing) {
    figma.currentPage.selection = [existing];
    figma.viewport.scrollAndZoomIntoView([existing]);
    return {
      reused: true,
      sectionNodeId: existing.id,
      createdNodeIds: [],
      message: 'Existing generated Player Hub section selected; no duplicate created.',
    };
  }

  const created: string[] = [];
  const interactive: string[] = [];
  const screenWidth = 1680;
  const screenHeight = 945;
  const navy = '#12396B';
  const blue = '#2878D5';
  const cyan = '#56C9DF';
  const cream = '#FFF8EC';
  const panelCream = '#FFFDF7';
  const muted = '#EEF3F6';
  const border = '#C9B9A2';
  const orange = '#FF9E45';
  const coral = '#F36B59';
  const green = '#5DA65A';
  const gold = '#F3BD43';

  function track<T extends SceneNode>(node: T): T {
    created.push(node.id);
    return node;
  }

  function frame(
    parent: ChildrenMixin,
    name: string,
    width: number,
    height: number,
    fill?: string,
    radius = 0,
    stroke?: string,
  ) {
    const node = track(figma.createFrame());
    node.name = name;
    node.resize(width, height);
    node.cornerRadius = radius;
    node.fills = fill ? [solid(fill)] : [];
    node.strokes = stroke ? [solid(stroke)] : [];
    node.strokeWeight = stroke ? 2 : 0;
    parent.appendChild(node);
    return node;
  }

  function auto(
    parent: ChildrenMixin,
    name: string,
    width: number,
    height: number,
    direction: 'HORIZONTAL' | 'VERTICAL',
    gap: number,
    padding: number,
    fill?: string,
    radius = 0,
    stroke?: string,
  ) {
    const node = frame(parent, name, width, height, fill, radius, stroke);
    node.layoutMode = direction;
    node.primaryAxisSizingMode = 'FIXED';
    node.counterAxisSizingMode = 'FIXED';
    node.itemSpacing = gap;
    node.paddingTop = padding;
    node.paddingRight = padding;
    node.paddingBottom = padding;
    node.paddingLeft = padding;
    return node;
  }

  function text(
    parent: ChildrenMixin,
    name: string,
    value: string,
    size: number,
    style: 'Regular' | 'Bold' | 'Extra Bold' = 'Regular',
    color = navy,
    align: 'LEFT' | 'CENTER' | 'RIGHT' = 'LEFT',
  ) {
    const node = track(figma.createText());
    node.name = name;
    node.fontName = { family: 'Inter', style };
    node.fontSize = size;
    node.characters = value;
    node.fills = [solid(color)];
    node.textAlignHorizontal = align;
    node.textAutoResize = 'WIDTH_AND_HEIGHT';
    parent.appendChild(node);
    return node;
  }

  function icon(parent: ChildrenMixin, name: string, svg: string, size: number) {
    const node = track(figma.createNodeFromSvg(svg));
    node.name = `Vector / ${name}`;
    node.resize(size, size);
    parent.appendChild(node);
    return node;
  }

  function place(node: SceneNode, x: number, y: number) {
    node.x = x;
    node.y = y;
    return node;
  }

  function decoratePanel(node: FrameNode, shadow = true) {
    node.effects = shadow ? [{
      type: 'DROP_SHADOW',
      color: { r: 0.04, g: 0.12, b: 0.22, a: 0.18 },
      offset: { x: 0, y: 8 },
      radius: 22,
      spread: 0,
      visible: true,
      blendMode: 'NORMAL',
    }] : [];
  }

  function chip(parent: ChildrenMixin, name: string, iconSvg: string, value: string, width = 170) {
    const node = auto(parent, name, width, 54, 'HORIZONTAL', 10, 10, '#FFF9EF', 16, '#D9C8AF');
    node.counterAxisAlignItems = 'CENTER';
    icon(node, `${name} Icon`, iconSvg, 32);
    text(node, `${name} Value`, value, 19, 'Extra Bold');
    return node;
  }

  function statRow(parent: ChildrenMixin, iconSvg: string, labelValue: string, value: string, width = 314) {
    const row = auto(parent, `Stat / ${labelValue}`, width, 62, 'HORIZONTAL', 12, 10, muted, 15, '#D7DFE8');
    row.counterAxisAlignItems = 'CENTER';
    icon(row, `${labelValue} Icon`, iconSvg, 38);
    const labels = auto(row, `${labelValue} Text`, 188, 42, 'VERTICAL', 2, 0);
    text(labels, 'Label', labelValue, 11, 'Bold', '#64748B');
    text(labels, 'Value', value, 20, 'Extra Bold');
    return row;
  }

  let maxX = 0;
  for (const child of figma.currentPage.children) {
    maxX = Math.max(maxX, child.x + child.width);
  }

  const section = figma.createSection();
  section.name = 'Generated / Complete Player Hub UI';
  section.resizeWithoutConstraints(3540, 1420);
  section.x = maxX + 240;
  section.y = 0;
  figma.currentPage.appendChild(section);
  created.push(section.id);

  const heading = text(section, 'Section Title', 'COMPLETE PLAYER HUB UI', 28, 'Extra Bold', cream);
  place(heading, 0, 24);
  const note = text(
    section,
    'Section Note',
    'Interactive prototype: Pet ↔ Weapon tabs • horizontal pet scroller • Equip/Ascend confirmation • Exit/Back',
    14,
    'Regular',
    '#B9D8ED',
  );
  place(note, 0, 64);

  const petComponents: ComponentNode[] = [];
  const petDefs = [
    ['Pet=Tearay, State=Equipped', 'TEARAY', 'WATER • 4 STAR', cyan, PET_DROP_SVG],
    ['Pet=Emberfin, State=Owned', 'EMBERFIN', 'FIRE • 4 STAR', coral, PET_FISH_SVG],
    ['Pet=Blockowl, State=Owned', 'BLOCKOWL', 'WATER • 4 STAR', blue, PET_OWL_SVG],
    ['Pet=Amethyst, State=Owned', 'AMETHYST', 'ARCANE • 4 STAR', '#8D62C7', PET_GEM_SVG],
    ['Pet=Sunbit, State=Owned', 'SUNBIT', 'LIGHT • 3 STAR', gold, PET_ORB_SVG],
    ['Pet=Mossling, State=Owned', 'MOSSLING', 'NATURE • 3 STAR', green, PET_DROP_SVG],
  ] as const;

  for (const [variant, petName, rarity, accent, petSvg] of petDefs) {
    const component = track(figma.createComponent());
    component.name = variant;
    component.resize(192, 186);
    component.layoutMode = 'VERTICAL';
    component.primaryAxisSizingMode = 'FIXED';
    component.counterAxisSizingMode = 'FIXED';
    component.counterAxisAlignItems = 'CENTER';
    component.itemSpacing = 5;
    component.paddingTop = 12;
    component.paddingRight = 10;
    component.paddingBottom = 10;
    component.paddingLeft = 10;
    component.cornerRadius = 20;
    component.fills = [solid(panelCream)];
    component.strokes = [solid(variant.includes('Equipped') ? green : border)];
    component.strokeWeight = variant.includes('Equipped') ? 4 : 2;
    icon(component, `${petName} Art`, petSvg.split('#56C9DF').join(accent), 104);
    text(component, 'Pet Name', petName, 15, 'Extra Bold', navy, 'CENTER');
    text(component, 'Rarity', rarity, 10, 'Bold', accent, 'CENTER');
    petComponents.push(component);
    section.appendChild(component);
  }
  const petCardSet = figma.combineAsVariants(petComponents, section);
  petCardSet.name = 'Component / Pet Card';
  petCardSet.description = 'Player Hub pet inventory card. Variants represent pet identity and ownership/equipped state.';
  petCardSet.x = 0;
  petCardSet.y = 1160;
  created.push(petCardSet.id);

  const screens: FrameNode[] = [];
  const tabRefs: Array<{ pet: FrameNode; weapon: FrameNode; exit: FrameNode; action: FrameNode }> = [];

  function makeScreen(mode: 'Pet' | 'Weapon', x: number) {
    const screen = frame(section, `Player Hub / ${mode} State`, screenWidth, screenHeight, navy, 0);
    screen.x = x;
    screen.y = 120;
    screen.clipsContent = true;
    screens.push(screen);

    const background = frame(screen, '1. Background & Player Stance', screenWidth, screenHeight);
    background.x = 0;
    background.y = 0;
    const backdrop = frame(background, 'Environment / Academy Room', screenWidth, screenHeight, '#CBE7F2');
    backdrop.x = 0;
    backdrop.y = 0;
    backdrop.fills = [{
      type: 'GRADIENT_LINEAR',
      gradientTransform: [[1, 0, 0], [0, 1, 0]],
      gradientStops: [
        { position: 0, color: { r: 0.45, g: 0.76, b: 0.91, a: 1 } },
        { position: 1, color: { r: 0.96, g: 0.86, b: 0.70, a: 1 } },
      ],
    }];
    const wallGlow = frame(background, 'Environment / Window Glow', 520, 610, '#DFF6FF', 260);
    place(wallGlow, 32, 140);
    wallGlow.opacity = 0.82;
    const floor = frame(background, 'Environment / Floor', 650, 280, '#B98961', 0);
    place(floor, 0, 665);
    const pedestal = frame(background, 'Player / Pedestal', 430, 88, '#3D72B3', 44, '#F0C970');
    place(pedestal, 102, 760);
    pedestal.strokeWeight = 8;
    decoratePanel(pedestal);
    const player = icon(background, 'Player Stance Placeholder', PLAYER_SVG, 420);
    player.resize(330, 560);
    place(player, 150, 220);
    const equippedPet = icon(background, 'Equipped Pet', PET_DROP_SVG, 190);
    place(equippedPet, 390, 590);
    const quickRow = auto(background, 'Player / Equipped Quick Slots', 450, 138, 'HORIZONTAL', 16, 12);
    place(quickRow, 82, 795);
    quickRow.fills = [];
    const petQuick = auto(quickRow, 'Quick Slot / Pet', 132, 114, 'VERTICAL', 4, 8, '#FFF9EF', 20, mode === 'Pet' ? green : border);
    petQuick.counterAxisAlignItems = 'CENTER';
    icon(petQuick, 'Pet Quick Icon', PET_DROP_SVG, 72);
    text(petQuick, 'Pet Quick Label', 'PET', 11, 'Extra Bold', navy, 'CENTER');
    const weaponQuick = auto(quickRow, 'Quick Slot / Weapon', 132, 114, 'VERTICAL', 4, 8, '#FFF9EF', 20, mode === 'Weapon' ? blue : border);
    weaponQuick.counterAxisAlignItems = 'CENTER';
    icon(weaponQuick, 'Weapon Quick Icon', SWORD_SVG, 72);
    text(weaponQuick, 'Weapon Quick Label', 'WEAPON', 11, 'Extra Bold', navy, 'CENTER');
    const stats = auto(quickRow, 'Player / Compact Stats', 154, 114, 'VERTICAL', 6, 8, '#0B2E55', 18, '#4B7FAE');
    text(stats, 'ATK', '⚔ 325 ATK', 13, 'Extra Bold', cream);
    text(stats, 'CR', '◈ 18% CR', 13, 'Bold', cream);
    text(stats, 'CD', '◉ 150% CD', 13, 'Bold', cream);

    const menu = frame(screen, '2. UI Menu (Pet / Weapon)', screenWidth, screenHeight);
    menu.x = 0;
    menu.y = 0;
    const workspace = frame(menu, `Workspace / ${mode}`, 1010, 744, cream, 32, '#E2C59D');
    place(workspace, 630, 166);
    workspace.strokeWeight = 3;
    decoratePanel(workspace);

    const tabs = auto(workspace, 'Menu / Tabs', 950, 58, 'HORIZONTAL', 12, 6);
    place(tabs, 30, 18);
    const petTab = auto(tabs, 'Button / Pet Tab', 220, 46, 'HORIZONTAL', 10, 10, mode === 'Pet' ? '#DFF4E2' : '#ECE7DD', 16, mode === 'Pet' ? green : border);
    petTab.counterAxisAlignItems = 'CENTER';
    icon(petTab, 'Paw', PAW_SVG, 28);
    text(petTab, 'Label', 'PET COLLECTION', 14, 'Extra Bold', mode === 'Pet' ? '#2F7D48' : '#6B7280');
    const weaponTab = auto(tabs, 'Button / Weapon Tab', 220, 46, 'HORIZONTAL', 10, 10, mode === 'Weapon' ? '#DDEBFF' : '#ECE7DD', 16, mode === 'Weapon' ? blue : border);
    weaponTab.counterAxisAlignItems = 'CENTER';
    icon(weaponTab, 'Sword', SWORD_SVG, 28);
    text(weaponTab, 'Label', 'WEAPON ASCEND', 14, 'Extra Bold', mode === 'Weapon' ? blue : '#6B7280');

    let actionButton: FrameNode;
    if (mode === 'Pet') {
      const header = auto(workspace, 'Pet / Header', 930, 54, 'HORIZONTAL', 10, 0);
      place(header, 40, 92);
      header.primaryAxisAlignItems = 'SPACE_BETWEEN';
      const titleStack = auto(header, 'Pet / Title Stack', 520, 54, 'VERTICAL', 2, 0);
      text(titleStack, 'Title', 'YOUR PETS', 20, 'Extra Bold');
      text(titleStack, 'Subtitle', 'Swipe or scroll to browse • select a pet to preview', 12, 'Regular', '#60758A');
      chip(header, 'Owned Count', PAW_SVG, '12 / 24', 160);

      const scroll = auto(workspace, 'Pet / Horizontal Scroller', 930, 212, 'HORIZONTAL', 16, 10, '#F4EBDD', 22, '#DDCCB4');
      place(scroll, 40, 154);
      scroll.clipsContent = true;
      scroll.overflowDirection = 'HORIZONTAL';
      for (const component of petComponents) {
        const instance = track(component.createInstance());
        scroll.appendChild(instance);
      }

      const detail = auto(workspace, 'Pet / Detail Preview', 930, 334, 'HORIZONTAL', 18, 18, '#173F72', 24, '#4D7DAA');
      place(detail, 40, 386);
      const info = auto(detail, 'Pet / Selected Info', 330, 298, 'VERTICAL', 10, 0);
      text(info, 'Kicker', 'EQUIPPED • WATER', 12, 'Extra Bold', cyan);
      text(info, 'Name', 'TEARAY', 30, 'Extra Bold', cream);
      text(info, 'Stars', '★ ★ ★ ★', 22, 'Extra Bold', gold);
      statRow(info, SHIELD_SVG, 'DAMAGE REDUCTION', '18%');
      statRow(info, SNOW_SVG, 'PASSIVE', 'FROST WARD');
      actionButton = auto(info, 'Button / Equip Pet', 314, 54, 'HORIZONTAL', 8, 12, orange, 17, '#D8752D');
      actionButton.primaryAxisAlignItems = 'CENTER';
      actionButton.counterAxisAlignItems = 'CENTER';
      text(actionButton, 'Label', 'EQUIP PET', 17, 'Extra Bold', navy, 'CENTER');
      const preview = frame(detail, 'Pet / Hero Preview', 544, 298, '#E8F8F7', 20, '#8DDDDD');
      const aura = figma.createEllipse();
      track(aura);
      aura.name = 'Pet / Aura';
      aura.resize(260, 260);
      aura.fills = [solid('#BDEFEF', 0.65)];
      preview.appendChild(aura);
      aura.x = 142;
      aura.y = 18;
      const heroPet = icon(preview, 'Tearay Hero', PET_DROP_SVG, 250);
      place(heroPet, 147, 20);
    } else {
      const weaponBody = auto(workspace, 'Weapon / Ascend Layout', 930, 624, 'HORIZONTAL', 18, 0);
      place(weaponBody, 40, 92);
      const showcase = auto(weaponBody, 'Weapon / Showcase', 570, 624, 'VERTICAL', 10, 18, '#173F72', 24, '#4D7DAA');
      showcase.counterAxisAlignItems = 'CENTER';
      text(showcase, 'Kicker', 'CURRENT WEAPON', 12, 'Extra Bold', cyan, 'CENTER');
      text(showcase, 'Name', 'SKYEDGE', 30, 'Extra Bold', cream, 'CENTER');
      text(showcase, 'Stars', '★ ★ ★ ★', 22, 'Extra Bold', gold, 'CENTER');
      const swordHero = icon(showcase, 'Skyedge Hero', SWORD_SVG, 260);
      swordHero.resize(240, 260);
      const progress = auto(showcase, 'Weapon / Awakening Progress', 520, 52, 'HORIZONTAL', 14, 10, '#0D315C', 15, '#4D7DAA');
      progress.primaryAxisAlignItems = 'CENTER';
      progress.counterAxisAlignItems = 'CENTER';
      text(progress, 'Progress Label', '◆  ◆  ◆  ◇', 23, 'Extra Bold', cyan, 'CENTER');
      const compare = auto(showcase, 'Weapon / Stat Comparison', 520, 96, 'HORIZONTAL', 16, 12, cream, 18, '#D9C7AD');
      compare.counterAxisAlignItems = 'CENTER';
      chip(compare, 'Current Attack', SWORD_SVG, '325', 180);
      text(compare, 'Arrow', '→', 32, 'Extra Bold', coral, 'CENTER');
      chip(compare, 'Next Attack', SWORD_SVG, '372', 180);

      const ascend = auto(weaponBody, 'Weapon / Ascend Panel', 342, 624, 'VERTICAL', 14, 18, panelCream, 24, '#E1C8A4');
      ascend.counterAxisAlignItems = 'CENTER';
      text(ascend, 'Title', 'ASCEND', 22, 'Extra Bold', navy, 'CENTER');
      text(ascend, 'Subtitle', 'AWAKEN THE NEXT FORM', 11, 'Bold', '#60758A', 'CENTER');
      const materials = auto(ascend, 'Weapon / Materials', 306, 250, 'VERTICAL', 12, 10, '#F4EBDD', 18, '#DDCCB4');
      statRow(materials, CRYSTAL_SVG, 'SKY CRYSTAL', '3 / 3', 280);
      statRow(materials, GEM_SVG, 'VERDANT GEM', '2 / 4', 280);
      statRow(materials, COIN_SVG, 'POWER COIN', '120', 280);
      text(ascend, 'Cost Label', 'ASCENSION COST', 11, 'Bold', '#60758A', 'CENTER');
      chip(ascend, 'Cost', COIN_SVG, '120', 250);
      actionButton = auto(ascend, 'Button / Ascend Weapon', 306, 64, 'HORIZONTAL', 10, 12, orange, 18, '#D8752D');
      actionButton.primaryAxisAlignItems = 'CENTER';
      actionButton.counterAxisAlignItems = 'CENTER';
      icon(actionButton, 'Ascend Arrow', UP_SVG, 30);
      text(actionButton, 'Label', 'ASCEND', 21, 'Extra Bold', navy, 'CENTER');
      text(ascend, 'Requirement', 'Requires 2 more Verdant Gems', 11, 'Bold', coral, 'CENTER');
    }

    const utilities = frame(screen, '3. Utilities Top Menu (Power Coin / Exit)', screenWidth, screenHeight);
    utilities.x = 0;
    utilities.y = 0;
    const topBar = auto(utilities, 'Utilities / Top Bar', 1680, 112, 'HORIZONTAL', 20, 22, '#FFF9EF', 0, '#D7C2A5');
    topBar.primaryAxisAlignItems = 'SPACE_BETWEEN';
    topBar.counterAxisAlignItems = 'CENTER';
    text(topBar, 'Brand', '✦ MATH WORLD', 18, 'Extra Bold', blue);
    text(topBar, 'Screen Title', 'PLAYER HUB', 34, 'Extra Bold', navy, 'CENTER');
    const actions = auto(topBar, 'Utilities / Actions', 470, 68, 'HORIZONTAL', 14, 7);
    actions.counterAxisAlignItems = 'CENTER';
    const coins = chip(actions, 'Power Coin', COIN_SVG, '25,480', 228);
    const exit = auto(actions, 'Button / Exit', 68, 54, 'HORIZONTAL', 0, 10, '#FFFDF8', 16, '#D7C2A5');
    exit.primaryAxisAlignItems = 'CENTER';
    exit.counterAxisAlignItems = 'CENTER';
    icon(exit, 'Close', CLOSE_SVG, 30);
    interactive.push(petTab.id, weaponTab.id, exit.id, actionButton.id, coins.id, petQuick.id, weaponQuick.id);
    tabRefs.push({ pet: petTab, weapon: weaponTab, exit, action: actionButton });
    return screen;
  }

  const petScreen = makeScreen('Pet', 0);
  const weaponScreen = makeScreen('Weapon', 1780);

  const toast = frame(section, 'Prototype Overlay / Action Confirmed', 420, 112, '#113A68', 24, '#F2BE55');
  toast.x = 1470;
  toast.y = 1190;
  toast.strokeWeight = 3;
  decoratePanel(toast);
  const toastIcon = icon(toast, 'Success', CHECK_SVG, 42);
  place(toastIcon, 26, 34);
  const toastTitle = text(toast, 'Title', 'ACTION CONFIRMED', 18, 'Extra Bold', cream);
  place(toastTitle, 88, 28);
  const toastCopy = text(toast, 'Copy', 'Your Player Hub selection was updated.', 12, 'Regular', '#B9D8ED');
  place(toastCopy, 88, 58);

  const smart: Transition = {
    type: 'SMART_ANIMATE',
    easing: { type: 'EASE_OUT_BACK' },
    duration: 0.3,
  };
  const dissolve: Transition = {
    type: 'DISSOLVE',
    easing: { type: 'EASE_OUT' },
    duration: 0.18,
  };
  await Promise.all([
    tabRefs[0].weapon.setReactionsAsync([{ trigger: { type: 'ON_CLICK' }, actions: [{ type: 'NODE', destinationId: weaponScreen.id, navigation: 'NAVIGATE', transition: smart, resetScrollPosition: false }] }]),
    tabRefs[1].pet.setReactionsAsync([{ trigger: { type: 'ON_CLICK' }, actions: [{ type: 'NODE', destinationId: petScreen.id, navigation: 'NAVIGATE', transition: smart, resetScrollPosition: false }] }]),
    tabRefs[0].exit.setReactionsAsync([{ trigger: { type: 'ON_CLICK' }, actions: [{ type: 'BACK' }] }]),
    tabRefs[1].exit.setReactionsAsync([{ trigger: { type: 'ON_CLICK' }, actions: [{ type: 'BACK' }] }]),
    tabRefs[0].action.setReactionsAsync([{ trigger: { type: 'ON_CLICK' }, actions: [{ type: 'NODE', destinationId: toast.id, navigation: 'OVERLAY', transition: dissolve }] }]),
    tabRefs[1].action.setReactionsAsync([{ trigger: { type: 'ON_CLICK' }, actions: [{ type: 'NODE', destinationId: toast.id, navigation: 'OVERLAY', transition: dissolve }] }]),
    toast.setReactionsAsync([{ trigger: { type: 'AFTER_TIMEOUT', timeout: 1.2 }, actions: [{ type: 'CLOSE' }] }]),
  ]);

  figma.currentPage.selection = [petScreen, weaponScreen];
  figma.viewport.scrollAndZoomIntoView([petScreen, weaponScreen]);
  return {
    reused: false,
    sectionNodeId: section.id,
    petScreenNodeId: petScreen.id,
    weaponScreenNodeId: weaponScreen.id,
    petCardComponentSetNodeId: petCardSet.id,
    toastNodeId: toast.id,
    createdNodeIds: created,
    interactiveNodeIds: interactive,
    scrollNodeName: 'Pet / Horizontal Scroller',
    layerGroups: [
      '1. Background & Player Stance',
      '2. UI Menu (Pet / Weapon)',
      '3. Utilities Top Menu (Power Coin / Exit)',
    ],
  };
}

async function rebuildMainMenuUi(command: BridgeCommand) {
  await Promise.all([
    figma.loadFontAsync({ family: 'Inter', style: 'Regular' }),
    figma.loadFontAsync({ family: 'Inter', style: 'Bold' }),
    figma.loadFontAsync({ family: 'Inter', style: 'Extra Bold' }),
  ]);

  const root = await requireSceneNode(command.rootNodeId, ['FRAME']);
  if (root.type !== 'FRAME') throw new Error('Main-menu root must be a frame');

  const hiddenNodeIds = Array.isArray(command.hideNodeIds)
    ? command.hideNodeIds.filter((id): id is string => typeof id === 'string')
    : [];
  const hiddenNodes = await Promise.all(hiddenNodeIds.map((id) => requireSceneNode(id)));
  for (const node of hiddenNodes) node.visible = false;

  const previous = root.children.find((node) => node.name === 'Main Menu UI / Reworked');
  if (previous) previous.remove();

  const created: string[] = [];
  const overlay = figma.createFrame();
  overlay.name = 'Main Menu UI / Reworked';
  overlay.resize(1680, 945);
  overlay.fills = [];
  root.appendChild(overlay);
  overlay.x = 0;
  overlay.y = 0;
  created.push(overlay.id);

  const navy = '#12396B';
  const blue = '#2B6CB0';
  const cream = '#FFF8EB';
  const gold = '#E9B95C';
  const border = '#9BB2C8';
  const muted = '#EAF2F8';

  function panel(name: string, x: number, y: number, width: number, height: number, radius = 24) {
    const node = figma.createFrame();
    node.name = name;
    node.resize(width, height);
    node.cornerRadius = radius;
    node.fills = [solid(cream, 0.94)];
    node.strokes = [solid(border, 0.95)];
    node.strokeWeight = 2;
    node.effects = [{
      type: 'DROP_SHADOW',
      color: { r: 0.04, g: 0.14, b: 0.24, a: 0.20 },
      offset: { x: 0, y: 8 },
      radius: 18,
      spread: 0,
      visible: true,
      blendMode: 'NORMAL',
    }];
    overlay.appendChild(node);
    node.x = x;
    node.y = y;
    created.push(node.id);
    return node;
  }

  function label(
    parent: FrameNode,
    name: string,
    value: string,
    x: number,
    y: number,
    size: number,
    style: 'Regular' | 'Bold' | 'Extra Bold' = 'Regular',
    color = navy,
  ) {
    const node = figma.createText();
    node.name = name;
    node.fontName = { family: 'Inter', style };
    node.fontSize = size;
    node.characters = value;
    node.fills = [solid(color)];
    parent.appendChild(node);
    node.x = x;
    node.y = y;
    created.push(node.id);
    return node;
  }

  function roundedRect(
    parent: FrameNode,
    name: string,
    x: number,
    y: number,
    width: number,
    height: number,
    fill: string,
    radius: number,
    stroke = border,
  ) {
    const node = figma.createRectangle();
    node.name = name;
    node.resize(width, height);
    node.cornerRadius = radius;
    node.fills = [solid(fill)];
    node.strokes = [solid(stroke)];
    node.strokeWeight = 2;
    parent.appendChild(node);
    node.x = x;
    node.y = y;
    created.push(node.id);
    return node;
  }

  const player = panel('Player Summary', 28, 26, 400, 118, 28);
  const avatar = figma.createEllipse();
  avatar.name = 'Avatar Placeholder';
  avatar.resize(92, 92);
  avatar.fills = [solid('#DDEEFF')];
  avatar.strokes = [solid(gold)];
  avatar.strokeWeight = 4;
  player.appendChild(avatar);
  avatar.x = 14;
  avatar.y = 13;
  created.push(avatar.id);
  label(player, 'Player Name', 'NOVA', 124, 18, 28, 'Extra Bold');
  label(player, 'Player Level', 'LV. 12', 126, 58, 16, 'Bold', blue);
  const statLabels = ['MEDAL  3', 'COIN  0', 'GEM  0'];
  statLabels.forEach((value, index) => {
    const stat = panelChild(
      player,
      `Stat ${index + 1}`,
      214 + index * 58,
      55,
      52,
      38,
      12,
      muted,
      border,
      created,
    );
    label(stat, 'Value', value.split('  ')[1], 18, 9, 15, 'Bold');
  });

  const stage = panel('Stage Status', 570, 24, 540, 184, 28);
  label(stage, 'Stage Title', 'STAGE 12', 188, 16, 30, 'Extra Bold');
  const divider = roundedRect(stage, 'Divider', 24, 58, 492, 2, '#D7C7AE', 1, '#D7C7AE');
  divider.strokeWeight = 0;
  label(stage, 'Enemy Name', 'STONEWARD', 24, 76, 17, 'Extra Bold');
  roundedRect(stage, 'Enemy HP Track', 144, 77, 250, 24, '#F4EEE6', 12);
  roundedRect(stage, 'Enemy HP Fill', 144, 77, 212, 24, '#E85C5C', 12, '#C43F3F');
  label(stage, 'Enemy HP Value', '850 / 1000', 412, 78, 17, 'Bold');
  ['MOVE', 'DASH', 'GUARD', 'FIGHT'].forEach((value, index) => {
    const action = panelChild(stage, `Ability / ${value}`, 108 + index * 82, 120, 68, 50, 14, muted, border, created);
    label(action, 'Label', value, 10, 16, 11, 'Extra Bold', index === 3 ? '#B83B3B' : blue);
  });

  const utility = figma.createFrame();
  utility.name = 'Utility Actions';
  utility.resize(330, 94);
  utility.fills = [];
  overlay.appendChild(utility);
  utility.x = 1318;
  utility.y = 28;
  created.push(utility.id);
  ['MAP', 'CUP', 'SET'].forEach((value, index) => {
    const button = panelChild(utility, `Utility / ${value}`, index * 110, 0, 92, 92, 46, '#E8F3FF', gold, created);
    label(button, 'Label', value, value === 'MAP' ? 27 : 25, 35, 16, 'Extra Bold');
  });

  const nav = panel('Primary Navigation', 30, 254, 210, 548, 30);
  label(nav, 'Navigation Label', 'MENU', 72, 20, 18, 'Extra Bold', blue);
  ['HUB', 'GACHA', 'REBIRTH'].forEach((value, index) => {
    const button = panelChild(nav, `Navigation / ${value}`, 24, 62 + index * 150, 162, 132, 22, index === 0 ? '#E5F3FF' : '#FFFDF8', index === 0 ? gold : border, created);
    roundedRect(button, 'Icon Placeholder', 49, 16, 64, 64, index === 0 ? '#BFE4FF' : muted, 18);
    label(button, 'Label', value, value === 'REBIRTH' ? 36 : 48, 92, 18, 'Extra Bold');
  });
  const collapse = panelChild(nav, 'Collapse Handle', 186, 230, 42, 88, 18, '#EDF5FB', border, created);
  label(collapse, 'Chevron', '‹', 12, 24, 32, 'Bold');

  const loadout = panel('Player Loadout', 920, 664, 728, 248, 30);
  label(loadout, 'Loadout Title', 'ACTIVE LOADOUT', 28, 18, 16, 'Extra Bold', blue);
  label(loadout, 'Heart', 'HP', 30, 57, 18, 'Extra Bold', '#C83E4D');
  roundedRect(loadout, 'Player HP Track', 78, 52, 370, 34, '#F5F0E9', 17);
  roundedRect(loadout, 'Player HP Fill', 78, 52, 288, 34, '#25C7C8', 17, '#0B8F9B');
  label(loadout, 'Player HP Value', '780 / 1000', 214, 58, 18, 'Extra Bold', '#FFFFFF');
  const slotLabels = ['PET', 'SWORD', 'POTION ×3', 'LEAF ×2', 'EMPTY', 'EMPTY'];
  slotLabels.forEach((value, index) => {
    const selected = index === 0;
    const slot = panelChild(loadout, `Loadout Slot ${index + 1}`, 24 + index * 112, 106, 98, 112, 18, selected ? '#F2FAE8' : '#FFFDF8', selected ? '#7CA73A' : border, created);
    roundedRect(slot, 'Item Placeholder', 17, 14, 64, 64, selected ? '#CDEFF4' : muted, 16);
    label(slot, 'Item Label', value, value.length > 7 ? 12 : 20, 86, value.length > 7 ? 10 : 12, 'Bold');
  });

  figma.currentPage.selection = [overlay];
  figma.viewport.scrollAndZoomIntoView([overlay]);
  return {
    createdNodeIds: created,
    mutatedNodeIds: hiddenNodes.map((node) => node.id),
    overlayNodeId: overlay.id,
    preservedBackgroundNodeId: optionalString(command.backgroundNodeId),
  };
}

function panelChild(
  parent: FrameNode,
  name: string,
  x: number,
  y: number,
  width: number,
  height: number,
  radius: number,
  fill: string,
  stroke: string,
  created: string[],
) {
  const node = figma.createFrame();
  node.name = name;
  node.resize(width, height);
  node.cornerRadius = radius;
  node.fills = [solid(fill, 0.96)];
  node.strokes = [solid(stroke)];
  node.strokeWeight = 2;
  parent.appendChild(node);
  node.x = x;
  node.y = y;
  created.push(node.id);
  return node;
}

function solid(hex: string, opacity = 1): SolidPaint {
  return { type: 'SOLID', color: parseColor(hex), opacity };
}

async function polishMainMenuIcons(command: BridgeCommand) {
  const overlay = await requireSceneNode(command.overlayNodeId, ['FRAME']);
  if (overlay.type !== 'FRAME') throw new Error('Overlay must be a frame');
  const overlayFrame: FrameNode = overlay;
  const frames: FrameNode[] = [];
  const stack: SceneNode[] = [...overlayFrame.children];
  while (stack.length > 0) {
    const candidate = stack.pop();
    if (!candidate) continue;
    if (candidate.type === 'FRAME') frames.push(candidate);
    if ('children' in candidate) stack.push(...candidate.children);
  }
  const created: string[] = [];
  const mutated: string[] = [];

  function frameNamed(name: string): FrameNode {
    for (const candidate of frames) {
      if (candidate.name === name) return candidate;
    }
    throw new Error(`Frame not found: ${name}`);
  }

  function clearGeneratedIcon(frame: FrameNode) {
    const children = [...frame.children];
    for (const child of children) {
      if (child.name === 'Icon Placeholder' || child.name === 'Label' || child.name.startsWith('Boolean Icon /')) {
        child.remove();
      }
    }
    mutated.push(frame.id);
  }

  function rect(parent: FrameNode, x: number, y: number, w: number, h: number, radius = 0) {
    const node = figma.createRectangle();
    node.resize(w, h);
    node.cornerRadius = radius;
    parent.appendChild(node);
    node.x = x;
    node.y = y;
    return node;
  }

  function ellipse(parent: FrameNode, x: number, y: number, w: number, h: number) {
    const node = figma.createEllipse();
    node.resize(w, h);
    parent.appendChild(node);
    node.x = x;
    node.y = y;
    return node;
  }

  function polygon(parent: FrameNode, x: number, y: number, w: number, h: number, count = 3) {
    const node = figma.createPolygon();
    node.pointCount = count;
    node.resize(w, h);
    parent.appendChild(node);
    node.x = x;
    node.y = y;
    return node;
  }

  function finish(node: BooleanOperationNode, name: string, fill: string, stroke = '#163E70') {
    node.name = `Boolean Icon / ${name}`;
    node.fills = [solid(fill)];
    node.strokes = [solid(stroke)];
    node.strokeWeight = 2;
    node.effects = [{
      type: 'DROP_SHADOW',
      color: { r: 0.03, g: 0.12, b: 0.22, a: 0.22 },
      offset: { x: 0, y: 3 },
      radius: 4,
      spread: 0,
      visible: true,
      blendMode: 'NORMAL',
    }];
    created.push(node.id);
  }

  const hub = frameNamed('Navigation / HUB');
  clearGeneratedIcon(hub);
  const houseBody = rect(hub, 52, 42, 58, 38, 5);
  const roof = polygon(hub, 46, 14, 70, 50);
  const house = figma.union([houseBody, roof], hub);
  const door = rect(hub, 72, 56, 17, 25, 5);
  const windowCut = rect(hub, 94, 49, 10, 10, 2);
  const houseCut = figma.subtract([house, door, windowCut], hub);
  finish(houseCut, 'Hub House', '#49A8F2');

  const gacha = frameNamed('Navigation / GACHA');
  clearGeneratedIcon(gacha);
  const face = ellipse(gacha, 49, 28, 64, 56);
  const earLeft = polygon(gacha, 47, 14, 30, 34);
  earLeft.rotation = -12;
  const earRight = polygon(gacha, 87, 14, 30, 34);
  earRight.rotation = 12;
  const catHead = figma.union([face, earLeft, earRight], gacha);
  const eyeLeft = ellipse(gacha, 66, 48, 9, 14);
  const eyeRight = ellipse(gacha, 88, 48, 9, 14);
  const catCut = figma.subtract([catHead, eyeLeft, eyeRight], gacha);
  finish(catCut, 'Gacha Cat', '#75D8EE');

  const rebirth = frameNamed('Navigation / REBIRTH');
  clearGeneratedIcon(rebirth);
  const outer = ellipse(rebirth, 48, 20, 66, 66);
  const inner = ellipse(rebirth, 60, 32, 42, 42);
  const ring = figma.subtract([outer, inner], rebirth);
  const arrowA = polygon(rebirth, 93, 13, 26, 28);
  arrowA.rotation = 25;
  const arrowB = polygon(rebirth, 43, 65, 26, 28);
  arrowB.rotation = 205;
  const arrows = figma.union([ring, arrowA, arrowB], rebirth);
  finish(arrows, 'Rebirth Arrows', '#EF5C83');

  const map = frameNamed('Utility / MAP');
  clearGeneratedIcon(map);
  const pinHead = ellipse(map, 25, 15, 42, 42);
  const pinPoint = polygon(map, 31, 39, 30, 38);
  pinPoint.rotation = 180;
  const pinBase = figma.union([pinHead, pinPoint], map);
  const pinHole = ellipse(map, 38, 27, 16, 16);
  const pin = figma.subtract([pinBase, pinHole], map);
  finish(pin, 'Map Pin', '#F3B63C');

  const cup = frameNamed('Utility / CUP');
  clearGeneratedIcon(cup);
  const bowl = rect(cup, 25, 18, 42, 30, 8);
  const stem = rect(cup, 42, 44, 8, 22, 3);
  const base = rect(cup, 30, 63, 32, 9, 4);
  const trophy = figma.union([bowl, stem, base], cup);
  const bowlCut = rect(cup, 34, 18, 24, 10, 4);
  const trophyCut = figma.subtract([trophy, bowlCut], cup);
  finish(trophyCut, 'Trophy', '#F4F7FA');

  const settings = frameNamed('Utility / SET');
  clearGeneratedIcon(settings);
  const gearCore = ellipse(settings, 24, 14, 44, 44);
  const teeth: RectangleNode[] = [];
  for (let index = 0; index < 8; index++) {
    const tooth = rect(settings, 42, 7, 8, 20, 3);
    tooth.rotation = index * 45;
    teeth.push(tooth);
  }
  const gearOuter = figma.union([gearCore, ...teeth], settings);
  const gearHole = ellipse(settings, 37, 27, 18, 18);
  const gear = figma.subtract([gearOuter, gearHole], settings);
  finish(gear, 'Settings Gear', '#E8EEF3');

  figma.currentPage.selection = [hub, gacha, rebirth, map, cup, settings];
  return { createdNodeIds: created, mutatedNodeIds: mutated };
}

async function flattenMainMenuIcons(command: BridgeCommand) {
  const ids = Array.isArray(command.nodeIds)
    ? command.nodeIds.filter((id): id is string => typeof id === 'string')
    : [];
  if (ids.length === 0 || ids.length > 20) {
    throw new Error('Provide between 1 and 20 Boolean icon node IDs');
  }

  const nodes = await Promise.all(ids.map((id) => requireSceneNode(id, ['BOOLEAN_OPERATION'])));
  const createdNodeIds: string[] = [];
  const replacedNodeIds: string[] = [];

  for (const node of nodes) {
    if (node.type !== 'BOOLEAN_OPERATION' || !node.parent || !('children' in node.parent)) {
      throw new Error(`Boolean icon cannot be flattened: ${node.id}`);
    }
    const parent = node.parent;
    const index = parent.children.indexOf(node);
    const originalName = node.name.replace('Boolean Icon / ', '');
    const vector = figma.flatten([node], parent, index);
    vector.name = `Vector Icon / ${originalName}`;
    createdNodeIds.push(vector.id);
    replacedNodeIds.push(node.id);
  }

  const vectors = await Promise.all(createdNodeIds.map((id) => figma.getNodeByIdAsync(id)));
  figma.currentPage.selection = vectors.filter((node): node is VectorNode => node?.type === 'VECTOR');
  return { createdNodeIds, replacedNodeIds };
}

async function designMainMenuVectorIcons(command: BridgeCommand) {
  const overlay = await requireSceneNode(command.overlayNodeId, ['FRAME']);
  if (overlay.type !== 'FRAME') throw new Error('Overlay must be a frame');

  const frames: FrameNode[] = [];
  const stack: SceneNode[] = [...overlay.children];
  while (stack.length > 0) {
    const candidate = stack.pop();
    if (!candidate) continue;
    if (candidate.type === 'FRAME') frames.push(candidate);
    if ('children' in candidate) stack.push(...candidate.children);
  }

  function target(name: string): FrameNode {
    for (const frame of frames) {
      if (frame.name === name) return frame;
    }
    throw new Error(`Icon target not found: ${name}`);
  }

  function placeVector(
    parent: FrameNode,
    name: string,
    svg: string,
    x: number,
    y: number,
    size: number,
  ) {
    for (const child of [...parent.children]) {
      if (child.name.startsWith('Vector Icon /') || child.name === 'Icon Placeholder' || child.name === 'Label') {
        child.remove();
      }
    }
    const imported = figma.createNodeFromSvg(svg);
    const vector = figma.flatten([...imported.children], imported);
    vector.name = `Vector Icon / ${name}`;
    parent.appendChild(vector);
    vector.resize(size, size);
    vector.x = x;
    vector.y = y;
    imported.remove();
    return vector;
  }

  const icons = [
    placeVector(target('Navigation / HUB'), 'Hub House', HOUSE_SVG, 45, 12, 72),
    placeVector(target('Navigation / GACHA'), 'Gacha Pet', GACHA_SVG, 45, 12, 72),
    placeVector(target('Navigation / REBIRTH'), 'Rebirth', REBIRTH_SVG, 45, 12, 72),
    placeVector(target('Utility / MAP'), 'Map', MAP_SVG, 14, 14, 64),
    placeVector(target('Utility / CUP'), 'Trophy', TROPHY_SVG, 14, 14, 64),
    placeVector(target('Utility / SET'), 'Settings', SETTINGS_SVG, 14, 14, 64),
  ];

  figma.currentPage.selection = icons;
  return { createdNodeIds: icons.map((icon) => icon.id) };
}

const HOUSE_SVG = `<svg width="72" height="72" viewBox="0 0 72 72" xmlns="http://www.w3.org/2000/svg">
<path d="M7 34 36 9l29 25-6 7-4-3v25H17V38l-4 3-6-7Z" fill="#3AA7E8" stroke="#12396B" stroke-width="4" stroke-linejoin="round"/>
<path d="M21 31 36 18l15 13v28H21V31Z" fill="#FFF5DE" stroke="#12396B" stroke-width="3"/>
<path d="M29 63V44c0-5 3-8 7-8s7 3 7 8v19" fill="#D79C50" stroke="#12396B" stroke-width="3"/>
<path d="M46 20V9h10v20" fill="#E7A85A" stroke="#12396B" stroke-width="3" stroke-linejoin="round"/>
<path d="M25 27h22" stroke="#8ED8FF" stroke-width="3" stroke-linecap="round"/>
</svg>`;

const GACHA_SVG = `<svg width="72" height="72" viewBox="0 0 72 72" xmlns="http://www.w3.org/2000/svg">
<path d="M13 28 9 8l17 12c3-2 7-3 10-3s7 1 10 3L63 8l-4 20c5 5 8 11 8 18 0 13-14 21-31 21S5 59 5 46c0-7 3-13 8-18Z" fill="#EAFBFF" stroke="#12396B" stroke-width="4" stroke-linejoin="round"/>
<path d="m15 16 10 8-9 5-1-13Zm42 0-10 8 9 5 1-13Z" fill="#44C7E9"/>
<path d="M17 42c5-8 12-12 19-12s14 4 19 12l-5 15H22l-5-15Z" fill="#43BCE5" stroke="#12396B" stroke-width="3"/>
<ellipse cx="26" cy="43" rx="5" ry="7" fill="#FFF" stroke="#12396B" stroke-width="2"/>
<ellipse cx="46" cy="43" rx="5" ry="7" fill="#FFF" stroke="#12396B" stroke-width="2"/>
<path d="M32 54c3 3 5 3 8 0" fill="none" stroke="#12396B" stroke-width="3" stroke-linecap="round"/>
</svg>`;

const REBIRTH_SVG = `<svg width="72" height="72" viewBox="0 0 72 72" xmlns="http://www.w3.org/2000/svg">
<path d="M13 31C16 16 31 8 45 13l4-7 15 15-20 3 5-7C38 13 26 18 21 28l-8 3Z" fill="#EF5C83" stroke="#12396B" stroke-width="3" stroke-linejoin="round"/>
<path d="M59 41C56 56 41 64 27 59l-4 7L8 51l20-3-5 7c11 4 23-1 28-11l8-3Z" fill="#39A9F0" stroke="#12396B" stroke-width="3" stroke-linejoin="round"/>
<path d="m55 46 3 7 8 1-6 5 2 8-7-4-7 4 2-8-6-5 8-1 3-7Z" fill="#FFD65A" stroke="#12396B" stroke-width="2"/>
</svg>`;

const MAP_SVG = `<svg width="64" height="64" viewBox="0 0 64 64" xmlns="http://www.w3.org/2000/svg">
<path d="m5 20 17-7 20 7 17-7v38l-17 7-20-7-17 7V20Z" fill="#7FCB70" stroke="#12396B" stroke-width="3" stroke-linejoin="round"/>
<path d="M22 13v38m20-31v38" stroke="#FFF2B2" stroke-width="3"/>
<path d="M32 5c-8 0-14 6-14 14 0 11 14 24 14 24s14-13 14-24c0-8-6-14-14-14Z" fill="#F4B942" stroke="#12396B" stroke-width="3"/>
<circle cx="32" cy="19" r="5" fill="#FFF8DE" stroke="#12396B" stroke-width="2"/>
</svg>`;

const TROPHY_SVG = `<svg width="64" height="64" viewBox="0 0 64 64" xmlns="http://www.w3.org/2000/svg">
<path d="M18 8h28v14c0 11-6 19-14 19S18 33 18 22V8Z" fill="#F7F8FA" stroke="#12396B" stroke-width="3"/>
<path d="M18 14H8v7c0 9 6 14 14 14M46 14h10v7c0 9-6 14-14 14" fill="none" stroke="#12396B" stroke-width="4" stroke-linecap="round"/>
<path d="M32 41v10m-12 5h24" stroke="#12396B" stroke-width="5" stroke-linecap="round"/>
<path d="M24 14h16" stroke="#B7D7F3" stroke-width="4" stroke-linecap="round"/>
</svg>`;

const SETTINGS_SVG = `<svg width="64" height="64" viewBox="0 0 64 64" xmlns="http://www.w3.org/2000/svg">
<path d="m27 4 10 0 2 8c2 1 4 2 6 4l8-3 5 9-6 6v8l6 6-5 9-8-3c-2 2-4 3-6 4l-2 8H27l-2-8c-2-1-4-2-6-4l-8 3-5-9 6-6v-8l-6-6 5-9 8 3c2-2 4-3 6-4l2-8Z" fill="#F4F6F8" stroke="#12396B" stroke-width="3" stroke-linejoin="round"/>
<circle cx="32" cy="32" r="11" fill="#58A7DD" stroke="#12396B" stroke-width="3"/>
<circle cx="32" cy="32" r="4" fill="#FFF"/>
</svg>`;

const PLAYER_SVG = `<svg width="330" height="560" viewBox="0 0 330 560" xmlns="http://www.w3.org/2000/svg">
<ellipse cx="165" cy="525" rx="112" ry="25" fill="#12396B" opacity=".22"/>
<path d="M114 472h55v54H91c1-23 8-42 23-54Zm102 0h-55v54h78c-1-23-8-42-23-54Z" fill="#244B79" stroke="#12396B" stroke-width="8" stroke-linejoin="round"/>
<path d="m111 278 52 8 56-8 20 190-64 14-66-14 2-190Z" fill="#183A67" stroke="#12396B" stroke-width="9"/>
<path d="m116 279 49 18 50-18 37 45-30 36-19-22-4 78H132l-4-78-19 22-30-36 37-45Z" fill="#2B78C5" stroke="#12396B" stroke-width="9" stroke-linejoin="round"/>
<path d="M132 306h66l-11 55h-44l-11-55Z" fill="#FFF5DF"/>
<path d="M148 303h34l-8 48h-18l-8-48Z" fill="#F3BD43"/>
<circle cx="165" cy="188" r="86" fill="#FFD2A6" stroke="#12396B" stroke-width="9"/>
<path d="M82 176c3-80 50-121 94-111 50 10 79 48 76 115-19-20-35-34-56-51-25 30-64 46-114 47Z" fill="#5B3A2A" stroke="#12396B" stroke-width="9" stroke-linejoin="round"/>
<circle cx="132" cy="190" r="10" fill="#12396B"/><circle cx="198" cy="190" r="10" fill="#12396B"/>
<path d="M142 225c14 12 32 12 46 0" fill="none" stroke="#C56F65" stroke-width="7" stroke-linecap="round"/>
<path d="m68 322 41 30-22 106-33-11 14-125Zm194 0-41 30 22 106 33-11-14-125Z" fill="#FFD2A6" stroke="#12396B" stroke-width="9" stroke-linejoin="round"/>
</svg>`;

const PET_DROP_SVG = `<svg width="128" height="128" viewBox="0 0 128 128" xmlns="http://www.w3.org/2000/svg">
<path d="M64 8c19 27 39 48 39 75 0 23-17 37-39 37S25 106 25 83C25 56 45 35 64 8Z" fill="#56C9DF" stroke="#12396B" stroke-width="6" stroke-linejoin="round"/>
<path d="m25 71-19 15 24 9m73-24 19 15-24 9" fill="#7CE2E7" stroke="#12396B" stroke-width="6" stroke-linecap="round" stroke-linejoin="round"/>
<path d="m64 25 12 25-12 13-12-13 12-25Z" fill="#FFF1C7"/>
<circle cx="50" cy="82" r="6" fill="#12396B"/><circle cx="78" cy="82" r="6" fill="#12396B"/>
<path d="M56 96c5 4 11 4 16 0" fill="none" stroke="#12396B" stroke-width="4" stroke-linecap="round"/>
</svg>`;

const PET_FISH_SVG = `<svg width="128" height="128" viewBox="0 0 128 128" xmlns="http://www.w3.org/2000/svg">
<path d="M20 67c11-30 36-44 62-36 21 7 34 23 34 43 0 22-18 38-44 38-25 0-47-17-52-45Z" fill="#56C9DF" stroke="#12396B" stroke-width="6"/>
<path d="M23 60 5 43l5 29-5 24 25-18" fill="#FFB070" stroke="#12396B" stroke-width="6" stroke-linejoin="round"/>
<circle cx="82" cy="66" r="6" fill="#12396B"/><path d="M91 82c5 3 10 3 14 0" fill="none" stroke="#12396B" stroke-width="4" stroke-linecap="round"/>
<path d="M48 38c8 9 13 19 14 30" fill="none" stroke="#FFF1C7" stroke-width="7" stroke-linecap="round"/>
</svg>`;

const PET_OWL_SVG = `<svg width="128" height="128" viewBox="0 0 128 128" xmlns="http://www.w3.org/2000/svg">
<rect x="18" y="20" width="92" height="94" rx="24" fill="#56C9DF" stroke="#12396B" stroke-width="6"/>
<path d="m24 32 23-18 17 23 17-23 23 18" fill="#2D77D0" stroke="#12396B" stroke-width="6" stroke-linejoin="round"/>
<circle cx="45" cy="62" r="19" fill="#FFF8EC" stroke="#12396B" stroke-width="5"/><circle cx="83" cy="62" r="19" fill="#FFF8EC" stroke="#12396B" stroke-width="5"/>
<circle cx="45" cy="62" r="7" fill="#12396B"/><circle cx="83" cy="62" r="7" fill="#12396B"/>
<path d="m64 68 9 10-9 8-9-8 9-10Z" fill="#F3BD43" stroke="#12396B" stroke-width="3"/>
</svg>`;

const PET_GEM_SVG = `<svg width="128" height="128" viewBox="0 0 128 128" xmlns="http://www.w3.org/2000/svg">
<path d="m64 10 47 36-18 66H35L17 46 64 10Z" fill="#56C9DF" stroke="#12396B" stroke-width="6" stroke-linejoin="round"/>
<path d="M64 10 47 52l17 60 17-60-17-42ZM17 46l30 6m64-6-30 6" fill="none" stroke="#FFF1C7" stroke-width="5"/>
<circle cx="50" cy="75" r="5" fill="#12396B"/><circle cx="78" cy="75" r="5" fill="#12396B"/>
<path d="M56 89c5 4 11 4 16 0" fill="none" stroke="#12396B" stroke-width="4" stroke-linecap="round"/>
</svg>`;

const PET_ORB_SVG = `<svg width="128" height="128" viewBox="0 0 128 128" xmlns="http://www.w3.org/2000/svg">
<circle cx="64" cy="68" r="46" fill="#56C9DF" stroke="#12396B" stroke-width="6"/>
<path d="m64 8 9 18 20 3-15 14 4 20-18-10-18 10 4-20-15-14 20-3 9-18Z" fill="#F3BD43" stroke="#12396B" stroke-width="5" stroke-linejoin="round"/>
<circle cx="49" cy="76" r="6" fill="#12396B"/><circle cx="79" cy="76" r="6" fill="#12396B"/>
</svg>`;

const SWORD_SVG = `<svg width="96" height="96" viewBox="0 0 96 96" xmlns="http://www.w3.org/2000/svg">
<path d="m74 8 14 0-5 20-41 42-16-16 42-41 6-5Z" fill="#DDE7F0" stroke="#12396B" stroke-width="5" stroke-linejoin="round"/>
<path d="m67 15 13 13-38 39-13-13 38-39Z" fill="#FFFFFF" opacity=".72"/>
<path d="m20 50 26 26-8 9-27-27 9-8Z" fill="#F3BD43" stroke="#12396B" stroke-width="5"/>
<path d="m28 72-17 17" stroke="#12396B" stroke-width="9" stroke-linecap="round"/>
</svg>`;

const PAW_SVG = `<svg width="72" height="72" viewBox="0 0 72 72" xmlns="http://www.w3.org/2000/svg"><ellipse cx="36" cy="45" rx="20" ry="17" fill="#56C9DF" stroke="#12396B" stroke-width="4"/><ellipse cx="16" cy="27" rx="7" ry="10" fill="#56C9DF" stroke="#12396B" stroke-width="4"/><ellipse cx="31" cy="18" rx="7" ry="11" fill="#56C9DF" stroke="#12396B" stroke-width="4"/><ellipse cx="47" cy="18" rx="7" ry="11" fill="#56C9DF" stroke="#12396B" stroke-width="4"/><ellipse cx="59" cy="30" rx="7" ry="10" fill="#56C9DF" stroke="#12396B" stroke-width="4"/></svg>`;
const SHIELD_SVG = `<svg width="72" height="72" viewBox="0 0 72 72" xmlns="http://www.w3.org/2000/svg"><path d="M36 6 62 16v19c0 16-10 25-26 31C20 60 10 51 10 35V16L36 6Z" fill="#56C9DF" stroke="#12396B" stroke-width="5"/><path d="M36 16v38c10-5 16-11 16-21V23l-16-7Z" fill="#2D77D0"/></svg>`;
const SNOW_SVG = `<svg width="72" height="72" viewBox="0 0 72 72" xmlns="http://www.w3.org/2000/svg"><path d="M36 7v58M11 22l50 28M61 22 11 50M28 12l8 8 8-8M28 60l8-8 8 8M13 33l11 3-3 11M59 33l-11 3 3 11" fill="none" stroke="#2878D5" stroke-width="5" stroke-linecap="round" stroke-linejoin="round"/></svg>`;
const CRYSTAL_SVG = `<svg width="72" height="72" viewBox="0 0 72 72" xmlns="http://www.w3.org/2000/svg"><path d="m13 57 9-36 16-12 21 17-5 31H13Z" fill="#56C9DF" stroke="#12396B" stroke-width="4"/><path d="m22 21 16 36m0-48v48m21-31L38 57" fill="none" stroke="#E7FFFF" stroke-width="4"/></svg>`;
const GEM_SVG = `<svg width="72" height="72" viewBox="0 0 72 72" xmlns="http://www.w3.org/2000/svg"><path d="m18 13 35 2 11 19-26 31L8 39l10-26Z" fill="#70C95C" stroke="#12396B" stroke-width="4"/><path d="m18 13 20 12 15-10M8 39l30-14 26 9M38 25v40" fill="none" stroke="#DFF5B8" stroke-width="3"/></svg>`;
const COIN_SVG = `<svg width="72" height="72" viewBox="0 0 72 72" xmlns="http://www.w3.org/2000/svg"><circle cx="36" cy="36" r="29" fill="#F3BD43" stroke="#12396B" stroke-width="4"/><circle cx="36" cy="36" r="22" fill="#FFD967" stroke="#E18B2D" stroke-width="3"/><path d="m36 20 5 10 12 2-9 8 2 12-10-6-10 6 2-12-9-8 12-2 5-10Z" fill="#FFF1C7" stroke="#E18B2D" stroke-width="2"/></svg>`;
const UP_SVG = `<svg width="72" height="72" viewBox="0 0 72 72" xmlns="http://www.w3.org/2000/svg"><path d="M36 8 64 39H48v25H24V39H8L36 8Z" fill="#FFF8EC" stroke="#12396B" stroke-width="5" stroke-linejoin="round"/></svg>`;
const CLOSE_SVG = `<svg width="72" height="72" viewBox="0 0 72 72" xmlns="http://www.w3.org/2000/svg"><path d="m17 17 38 38M55 17 17 55" fill="none" stroke="#12396B" stroke-width="10" stroke-linecap="round"/></svg>`;
const CHECK_SVG = `<svg width="72" height="72" viewBox="0 0 72 72" xmlns="http://www.w3.org/2000/svg"><circle cx="36" cy="36" r="29" fill="#67B85F" stroke="#FFF8EC" stroke-width="4"/><path d="m20 37 10 10 23-25" fill="none" stroke="#FFF8EC" stroke-width="7" stroke-linecap="round" stroke-linejoin="round"/></svg>`;

async function requireSceneNode(
  idValue: unknown,
  allowedTypes?: readonly SceneNode['type'][],
): Promise<SceneNode> {
  if (typeof idValue !== 'string' || idValue.length === 0) {
    throw new Error('A valid node ID is required.');
  }
  const node = await figma.getNodeByIdAsync(idValue);
  if (!node || node.type === 'DOCUMENT' || node.type === 'PAGE') {
    throw new Error(`Scene node not found: ${idValue}`);
  }
  const sceneNode = node as SceneNode;
  if (allowedTypes && !allowedTypes.includes(sceneNode.type)) {
    throw new Error(
      `Node ${idValue} is ${sceneNode.type}; expected ${allowedTypes.join(', ')}`,
    );
  }
  return sceneNode;
}

function optionalString(value: unknown): string | undefined {
  return typeof value === 'string' ? value : undefined;
}

function finiteNumber(value: unknown, fallback: number): number {
  const number = Number(value);
  return Number.isFinite(number) ? number : fallback;
}

function dimension(value: unknown, fallback: number): number {
  return Math.min(16384, Math.max(1, finiteNumber(value, fallback)));
}

function parseColor(hex: string): RGB {
  const normalized = hex.replace('#', '');
  if (!/^[0-9a-fA-F]{6}$/.test(normalized)) {
    throw new Error(`Invalid six-digit hex color: ${hex}`);
  }
  return {
    r: parseInt(normalized.slice(0, 2), 16) / 255,
    g: parseInt(normalized.slice(2, 4), 16) / 255,
    b: parseInt(normalized.slice(4, 6), 16) / 255,
  };
}
