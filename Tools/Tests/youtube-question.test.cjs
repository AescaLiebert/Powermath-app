const { test } = require('node:test');
const assert = require('node:assert/strict');
const vm = require('node:vm');
const fs = require('node:fs');
const path = require('node:path');
const bridge = fs.readFileSync(path.join(__dirname,
  '../../Assets/Plugins/WebGL/PowerMathYouTube.jslib'), 'utf8');

function setup({ ready = true } = {}) {
  const nodes = new Map(), listeners = new Map();
  const players = [], messages = [], scripts = [];
  const createElement = tag => ({
    tag, style: {}, children: [],
    appendChild(child) { this.children.push(child); nodes.set(child.id, child); }
  });
  const document = {
    createElement, getElementById: id => nodes.get(id),
    body: createElement('body'), head: { appendChild: tag => scripts.push(tag) }
  };
  const YT = {
    PlayerState: { ENDED: 0, PLAYING: 1 },
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
  vm.runInNewContext(bridge, {
    LibraryManager: { library }, mergeInto: Object.assign,
    UTF8ToString: value => value, window, document, YT,
    Module: { canvas: { getBoundingClientRect: () =>
      ({ left: 0, top: 0, width: 1280, height: 720 }) } },
    SendMessage: (...args) => messages.push(args)
  });
  const show = (generation, videoId = 'question') =>
    library.PowerMathYouTubeShow('receiver', videoId, generation);
  const end = player => player.options.events.onStateChange({ data: YT.PlayerState.ENDED });
  return { library, window, YT, nodes, players, messages, scripts, listeners, show, end };
}

test('requests documented hidden controls, disabled shortcuts/fullscreen and inline playback', () => {
  const c = setup(); c.show(1);
  const vars = c.players[0].options.playerVars;
  for (const [key, value] of Object.entries({ controls: 0, disablekb: 1, fs: 0, playsinline: 1 }))
    assert.equal(vars[key], value);
  assert.equal(vars.origin, 'https://example.test');
  assert.equal(c.nodes.get('powermath-youtube-overlay').style.pointerEvents, 'none');
  assert.equal(c.nodes.get('powermath-youtube-overlay').style.left, '0px');
  assert.equal(c.nodes.get('powermath-youtube-overlay').style.top, '0px');
  assert.equal(c.nodes.get('powermath-youtube-overlay').style.width, '1280px');
  assert.equal(c.nodes.get('powermath-youtube-overlay').style.height, '720px');
  assert.equal(c.nodes.get('powermath-youtube-overlay').style.userSelect, 'none');
  assert.equal(c.nodes.get('powermath-youtube-overlay').style.webkitTapHighlightColor, 'transparent');
});

test('ready iframe cannot receive browser focus, selection, or pointer highlight', () => {
  const c = setup(); c.show(3); const player = c.players[0];
  const iframe = { style: {} };
  player.options.events.onReady({
    target: {
      getIframe: () => iframe,
      playVideo: () => {}
    }
  });
  assert.equal(iframe.tabIndex, -1);
  assert.equal(iframe.draggable, false);
  assert.equal(iframe.style.pointerEvents, 'none');
  assert.equal(iframe.style.userSelect, 'none');
  assert.equal(iframe.style.webkitTapHighlightColor, 'transparent');
  assert.equal(iframe.style.outline, 'none');
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

test('blocked autoplay fails closed instead of leaving an opaque overlay', () => {
  const c = setup(); c.show(4); const player = c.players[0];
  player.options.events.onAutoplayBlocked();
  assert.deepEqual(c.messages, [['receiver', 'OnYouTubeError', '4|autoplay-blocked']]);
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
  assert.equal(c.listeners.size, 0);
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
