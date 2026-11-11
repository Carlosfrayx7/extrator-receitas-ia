// Grava docs/demo.gif com a API rodando em DEMO_URL (padrão http://localhost:5081) e credenciais configuradas.
// Sem ffmpeg: capturas PNG do Edge instalado, codificadas com gifenc.
// As dependências (playwright-core, gifenc, pngjs) ficam fora do repositório:
//   mkdir <pasta> && cd <pasta> && npm init -y && npm install playwright-core gifenc pngjs
// DEMO_TOOLS é obrigatório.
//   DEMO_TOOLS=<pasta> node docs/record-demo.mjs
import { createRequire } from 'node:module';
import { writeFileSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const tools = process.env.DEMO_TOOLS;
if (!tools) {
  console.error([
    'Defina DEMO_TOOLS com a pasta que contém playwright-core, gifenc e pngjs:',
    '  mkdir <pasta> && cd <pasta> && npm init -y && npm install playwright-core gifenc pngjs',
    '  DEMO_TOOLS=<pasta> node docs/record-demo.mjs',
  ].join('\n'));
  process.exit(1);
}
const require = createRequire(path.join(tools, 'package.json'));
const { chromium } = require('playwright-core');
const { PNG } = require('pngjs');
const { GIFEncoder, quantize, applyPalette } = require('gifenc');

const OUT = path.join(path.dirname(fileURLToPath(import.meta.url)), 'demo.gif');
const VW = 1280, VH = 1000;
const W = 960, H = Math.round(960 * VH / VW); // reduzido a 75%
const FRAME_MS = 200;

const browser = await chromium.launch({ channel: 'msedge', headless: true });
const context = await browser.newContext({ viewport: { width: VW, height: VH } });
const page = await context.newPage();

await page.goto(process.env.DEMO_URL ?? 'http://localhost:5081');
if (process.env.DEMO_DUMP) (await import('node:fs')).mkdirSync('docs/video-tmp', { recursive: true });
const frames = []; // { t, buf }
let recording = true;
const t0 = Date.now();
const capture = (async () => {
  while (recording) {
    const start = Date.now();
    frames.push({ t: start - t0, buf: await page.screenshot({ type: 'png' }) });
    const wait = FRAME_MS - (Date.now() - start);
    if (wait > 0) await new Promise((r) => setTimeout(r, wait));
  }
})();


await page.waitForTimeout(1500);
await page.getByRole('button', { name: 'rx-21-handwritten.png' }).click();
await page.locator('#usage').filter({ hasText: 'Opus' }).waitFor({ timeout: 90000 });
if (process.env.DEMO_DUMP) await page.screenshot({ path: 'docs/video-tmp/opus.png' });
await page.waitForTimeout(3000);
await page.selectOption('#model', 'haiku');
await page.waitForTimeout(800);
await page.getByRole('button', { name: 'rx-21-handwritten.png' }).click();
await page.locator('#usage').filter({ hasText: 'Haiku' }).waitFor({ timeout: 90000 });
if (process.env.DEMO_DUMP) await page.screenshot({ path: 'docs/video-tmp/haiku.png' });
await page.waitForTimeout(3000);
await page.getByRole('button', { name: 'other-33.png' }).click();
await page.locator('#status.bad').waitFor({ timeout: 90000 });
await page.waitForTimeout(2500);

recording = false;
await capture;
await context.close();
await browser.close();

function downscale(src, sw, sh) {
  const out = new Uint8Array(W * H * 4);
  for (let y = 0; y < H; y++) {
    const fy = (y + 0.5) * sh / H - 0.5, y0 = Math.max(0, Math.floor(fy)), y1 = Math.min(sh - 1, y0 + 1), ay = fy - y0;
    for (let x = 0; x < W; x++) {
      const fx = (x + 0.5) * sw / W - 0.5, x0 = Math.max(0, Math.floor(fx)), x1 = Math.min(sw - 1, x0 + 1), ax = fx - x0;
      for (let c = 0; c < 4; c++) {
        const p = (xx, yy) => src[(yy * sw + xx) * 4 + c];
        out[(y * W + x) * 4 + c] = (p(x0, y0) * (1 - ax) + p(x1, y0) * ax) * (1 - ay) + (p(x0, y1) * (1 - ax) + p(x1, y1) * ax) * ay;
      }
    }
  }
  return out;
}

const gif = GIFEncoder();
let prev = null, prevIndex = null, count = 0;
for (let i = 0; i < frames.length; i++) {
  const png = PNG.sync.read(frames[i].buf);
  const rgba = downscale(png.data, png.width, png.height);
  const delay = Math.max(20, (i + 1 < frames.length ? frames[i + 1].t - frames[i].t : 1000));
  if (prev && Buffer.compare(Buffer.from(rgba), prev) === 0) { // quadro repetido: só estende o anterior
    prevIndex.delay += delay;
    continue;
  }
  const palette = quantize(rgba, 256);
  const index = applyPalette(rgba, palette);
  if (prevIndex) gif.writeFrame(prevIndex.index, W, H, { palette: prevIndex.palette, delay: prevIndex.delay });
  prevIndex = { index, palette, delay };
  prev = Buffer.from(rgba);
  count++;
}
gif.writeFrame(prevIndex.index, W, H, { palette: prevIndex.palette, delay: prevIndex.delay });
gif.finish();
writeFileSync(OUT, gif.bytes());
console.log(`${frames.length} capturas, ${count} quadros distintos, ${(gif.bytes().length / 1e6).toFixed(2)} MB`);
