const { test } = require('node:test');
const assert = require('node:assert/strict');
const vm = require('node:vm');
const fs = require('node:fs');
const path = require('node:path');
const bridge = fs.readFileSync(path.join(__dirname,
  '../../Assets/Plugins/WebGL/PowerMathYouTube.jslib'), 'utf8');
const webTemplate = fs.readFileSync(path.join(__dirname,
  '../../Assets/WebGLTemplates/MathWorldPWA/index.html'), 'utf8');
const cloudflareHeaders = fs.readFileSync(path.join(__dirname,
  '../../Cloudflare/public/_headers'), 'utf8');

function setup({ ready = true, sendThrows = false } = {}) {
  const nodes = new Map(), listeners = new Map();
  const players = [], messages = [], scripts = [];
  let canvasFocusCount = 0;
  const createElement = tag => ({
    tag, style: {}, children: [],
    attributes: {},
    setAttribute(name, value) { this.attributes[name] = value; },
    appendChild(child) {
      child.parentNode = this;
      this.children.push(child);
      nodes.set(child.id, child);
    }
  });
  const container = createElement('div');
  container.id = 'unity-container';
  nodes.set(container.id, container);
  const document = {
    createElement, getElementById: id => nodes.get(id),
    body: createElement('body'), head: { appendChild: tag => scripts.push(tag) }
  };
  const YT = {
    PlayerState: { ENDED: 0, PLAYING: 1, PAUSED: 2 },
    Player: function (id, options) {
      this.options = options;
      this.destroy = () => { this.destroyed = true; };
      players.push(this);
    }
  };
  const window = {
    location: { origin: 'https://example.test' },
    setTimeout: callback => { window.pendingTimeout = callback; return 1; },
    clearTimeout: () => { window.pendingTimeout = null; },
    addEventListener: (type, handler) => listeners.set(type, handler),
    removeEventListener: (type, handler) => {
      if (listeners.get(type) === handler) listeners.delete(type);
    }
  };
  if (ready) window.YT = YT;
  const library = {};
  const canvas = {
    getBoundingClientRect: () =>
      ({ left: 0, top: 0, width: 1280, height: 720 }),
    focus: () => { canvasFocusCount++; }
  };
  vm.runInNewContext(bridge, {
    LibraryManager: { library }, mergeInto: Object.assign,
    UTF8ToString: value => value, window, document, YT,
    Module: { canvas },
    SendMessage: (...args) => {
      if (sendThrows) throw new WebAssembly.RuntimeError('memory access out of bounds');
      messages.push(args);
    }
  });
  const show = (generation, videoId = 'question') =>
    library.PowerMathYouTubeShow('receiver', videoId, generation);
  const end = player => player.options.events.onStateChange({ data: YT.PlayerState.ENDED });
  return {
    library, window, YT, nodes, players, messages, scripts, listeners, show, end,
    get canvasFocusCount() { return canvasFocusCount; }
  };
}

test('shell identifies the production origin to YouTube embeds', () => {
  assert.match(webTemplate,
    /<meta name="referrer" content="strict-origin-when-cross-origin">/);
  assert.match(cloudflareHeaders,
    /Referrer-Policy: strict-origin-when-cross-origin/);
});

test('keeps native playback available until the video starts', () => {
  const c = setup(); c.show(1);
  const vars = c.players[0].options.playerVars;
  for (const [key, value] of Object.entries({ controls: 1, disablekb: 1, fs: 0, playsinline: 1 }))
    assert.equal(vars[key], value);
  assert.equal(vars.mute, 0);
  assert.equal(vars.origin, 'https://example.test');
  assert.equal(vars.vq, 'hd720');
  assert.equal(c.nodes.get('powermath-youtube-overlay').style.pointerEvents, 'auto');
  assert.equal(c.nodes.get('powermath-youtube-overlay').style.left, '0px');
  assert.equal(c.nodes.get('powermath-youtube-overlay').style.top, '0px');
  assert.equal(c.nodes.get('powermath-youtube-overlay').style.width, '1280px');
  assert.equal(c.nodes.get('powermath-youtube-overlay').style.height, '720px');
  assert.equal(c.nodes.get('powermath-youtube-overlay').style.userSelect, 'none');
  assert.equal(c.nodes.get('powermath-youtube-overlay').style.webkitTapHighlightColor, 'transparent');
  assert.equal(c.nodes.get('powermath-youtube-scaler').style.width, '1280px');
  assert.equal(c.nodes.get('powermath-youtube-scaler').style.height, '720px');
  assert.equal(c.nodes.get('powermath-youtube-scaler').style.transform, 'scale(1)');
});

test('ready iframe un-mutes and ensures full volume playback', () => {
  const c = setup(); c.show(3); const player = c.players[0];
  let unmuted = false, volume = 0, played = false;
  const iframe = {
    style: {}, attributes: {},
    setAttribute(name, value) { this.attributes[name] = value; }
  };
  player.options.events.onReady({
    target: {
      getIframe: () => iframe,
      unMute: () => { unmuted = true; },
      setVolume: v => { volume = v; },
      playVideo: () => { played = true; }
    }
  });
  assert.equal(iframe.tabIndex, -1);
  assert.equal(iframe.draggable, false);
  assert.equal(iframe.style.pointerEvents, 'auto');
  assert.equal(iframe.style.userSelect, 'none');
  assert.equal(iframe.style.webkitTapHighlightColor, 'transparent');
  assert.equal(iframe.style.outline, 'none');
  assert.equal(iframe.attributes.allow, 'autoplay; encrypted-media; picture-in-picture');
  assert.equal(iframe.attributes.referrerpolicy, 'strict-origin-when-cross-origin');
  assert.equal(unmuted, true);
  assert.equal(volume, 100);
  assert.equal(played, true);
});

test('a replaced iframe cannot complete or fail the new question', () => {
  const c = setup(); c.show(1, 'old'); const old = c.players[0];
  c.show(2, 'new');
  assert.equal(old.destroyed, true);
  c.end(old); old.options.events.onError({ data: 100 });
  assert.equal(c.messages.length, 0);
  c.end(c.players[1]);
  assert.deepEqual(c.messages, [['receiver', 'OnYouTubeEnded', '2']]);
});

test('completion is delivered once and late error cannot dismiss retained question', () => {
  const c = setup(); c.show(1); const player = c.players[0];
  c.end(player); c.end(player); player.options.events.onError({ data: 100 });
  assert.equal(c.messages.length, 1);
  c.library.PowerMathYouTubeSetAnswerMode();
  assert.equal(c.window.PowerMathYouTubeState.answerMode, true);
  assert.equal(c.nodes.get('powermath-youtube-overlay').style.display, 'none');
});

test('a trapped late Wasm callback cannot escape the browser event handler', () => {
  const c = setup({ sendThrows: true }); c.show(9); const player = c.players[0];
  assert.doesNotThrow(() => c.end(player));
  assert.equal(c.nodes.get('powermath-youtube-overlay').style.display, 'none');
});

test('blocked autoplay leaves the native player tappable on iOS', () => {
  const c = setup(); c.show(4); const player = c.players[0];
  const iframe = { style: {} };
  player.options.events.onReady({
    target: { getIframe: () => iframe, playVideo: () => {} }
  });
  player.options.events.onAutoplayBlocked();
  assert.deepEqual(c.messages, []);
  assert.equal(typeof c.window.pendingTimeout, 'function');
  assert.equal(c.window.PowerMathYouTubeState.awaitingGesture, true);
  assert.equal(c.nodes.get('powermath-youtube-overlay').style.pointerEvents, 'auto');
  assert.equal(iframe.style.pointerEvents, 'auto');

  player.options.events.onStateChange({ data: c.YT.PlayerState.PLAYING });
  assert.equal(c.window.PowerMathYouTubeState.awaitingGesture, false);
  assert.equal(c.nodes.get('powermath-youtube-overlay').style.pointerEvents, 'none');
  assert.equal(iframe.style.pointerEvents, 'none');
  assert.ok(c.canvasFocusCount > 0);
});

test('a paused video is restarted and fails closed if playback stays stalled', () => {
  const c = setup(); c.show(6); const player = c.players[0];
  let restarts = 0;
  player.options.events.onStateChange({
    data: c.YT.PlayerState.PAUSED,
    target: { playVideo: () => { restarts++; } }
  });
  assert.equal(restarts, 1);
  c.window.pendingTimeout();
  assert.deepEqual(c.messages, [['receiver', 'OnYouTubeError', '6|playback-stalled']]);
  assert.equal(c.nodes.get('powermath-youtube-overlay').style.display, 'none');
});

test('ready playback has a bounded startup watchdog', () => {
  const c = setup(); c.show(5); const player = c.players[0];
  let played = false;
  player.options.events.onReady({ target: { playVideo: () => { played = true; } } });
  assert.equal(played, true);
  c.window.pendingTimeout();
  assert.deepEqual(c.messages, [['receiver', 'OnYouTubeError', '5|start-timeout']]);
});

test('hiding invalidates events and releases player and resize handler', () => {
  const c = setup(); c.show(1); const player = c.players[0];
  c.library.PowerMathYouTubeHide(); c.end(player);
  assert.equal(c.messages.length, 0);
  assert.equal(player.destroyed, true);
  assert.equal(c.listeners.has('resize'), false);
  assert.equal(c.nodes.get('powermath-youtube-overlay').style.display, 'none');
});

test('cancelled API load cannot create a hidden playing iframe', () => {
  const c = setup({ ready: false }); c.show(1);
  c.library.PowerMathYouTubeHide(); c.window.YT = c.YT;
  c.window.onYouTubeIframeAPIReady();
  assert.equal(c.players.length, 0);
});

test('only newest pending question is mounted when API arrives', () => {
  const c = setup({ ready: false }); c.show(1, 'old'); c.show(2, 'new');
  c.window.YT = c.YT; c.window.onYouTubeIframeAPIReady();
  assert.equal(c.scripts.length, 1);
  assert.equal(c.players.length, 1);
  assert.equal(c.players[0].options.videoId, 'new');
});

test('API failure reports unavailable once for current question and permits retry', () => {
  const c = setup({ ready: false }); c.show(1); c.show(2);
  c.scripts[0].onerror(); c.scripts[0].onerror();
  assert.deepEqual(c.messages, [['receiver', 'OnYouTubeError', '2|api-load']]);
  c.show(3);
  assert.equal(c.scripts.length, 2);
});
