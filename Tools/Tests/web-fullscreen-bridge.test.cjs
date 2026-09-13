const { test } = require('node:test');
const assert = require('node:assert/strict');
const vm = require('node:vm');
const fs = require('node:fs');
const path = require('node:path');

const bridge = fs.readFileSync(path.join(
  __dirname,
  '../../Assets/Plugins/WebGL/PowerMathWebBridge.jslib'), 'utf8');
const authenticationView = fs.readFileSync(path.join(
  __dirname,
  '../../Assets/Project/Script/UI/Authentication/AuthenticationView.cs'), 'utf8');
const actorPresentation = fs.readFileSync(path.join(
  __dirname,
  '../../Assets/Project/Script/Gameplay/Combat/Unity/Presentation/ActorPresentationController.cs'), 'utf8');

function setup({ fullscreen = false, standalone = false } = {}) {
  const library = {};
  const requests = [];
  const orientationLocks = [];
  let resolveFullscreen;
  const fullscreenPromise = new Promise(resolve => {
    resolveFullscreen = resolve;
  });
  const canvas = {};
  const container = {
    requestFullscreen() {
      requests.push('container-fullscreen');
      return fullscreenPromise;
    }
  };
  const document = {
    fullscreenElement: fullscreen ? container : null,
    webkitFullscreenElement: null,
    getElementById: id => id === 'unity-canvas'
      ? canvas
      : id === 'unity-container' ? container : null
  };
  const window = {
    navigator: { standalone: false },
    matchMedia: query => ({
      matches: standalone &&
        (query.includes('standalone') || query.includes('fullscreen'))
    }),
    screen: {
      orientation: {
        lock(value) {
          orientationLocks.push(value);
          return Promise.resolve();
        }
      }
    }
  };
  vm.runInNewContext(bridge, {
    LibraryManager: { library },
    mergeInto: Object.assign,
    UTF8ToString: value => value,
    Module: { canvas },
    window,
    document,
    screen: window.screen,
    Promise,
    URL,
    setTimeout: () => {}
  });
  return {
    library,
    requests,
    orientationLocks,
    resolveFullscreen
  };
}

const flush = () => new Promise(resolve => setImmediate(resolve));

test('requests fullscreen once and locks landscape after success', async () => {
  const context = setup();

  assert.equal(context.library.PowerMathTryEnterLandscapeFullscreen(), 1);
  assert.equal(context.library.PowerMathTryEnterLandscapeFullscreen(), 0);
  assert.deepEqual(context.requests, ['container-fullscreen']);

  context.resolveFullscreen();
  await flush();
  assert.deepEqual(context.orientationLocks, ['landscape']);
});

test('rejects the request when browser fullscreen is already active', () => {
  const context = setup({ fullscreen: true });

  assert.equal(context.library.PowerMathTryEnterLandscapeFullscreen(), 0);
  assert.deepEqual(context.requests, []);
});

test('rejects the request when running as an installed PWA', () => {
  const context = setup({ standalone: true });

  assert.equal(context.library.PowerMathTryEnterLandscapeFullscreen(), 0);
  assert.deepEqual(context.requests, []);
});

test('login and actor input never request fullscreen implicitly', () => {
  assert.doesNotMatch(authenticationView, /TryEnterLandscapeFullscreen/);
  assert.doesNotMatch(actorPresentation, /TryEnterLandscapeFullscreen/);
});
