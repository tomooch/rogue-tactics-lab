import { mkdir, copyFile, writeFile, rm } from 'node:fs/promises';
import { execFileSync } from 'node:child_process';

// Publish only the runtime, never the repository root or local configuration.
const destination = new URL('../_site/', import.meta.url);
const files = ['index.html', 'style.css', 'app.mjs', 'core.mjs', 'timeline.mjs'];
await rm(destination, { recursive: true, force: true });
await mkdir(destination, { recursive: true });
for (const file of files) {
  await copyFile(new URL(`../${file}`, import.meta.url), new URL(file, destination));
}
let commit = process.env.GITHUB_SHA || null;
if (!commit) {
  try { commit = execFileSync('git', ['rev-parse', 'HEAD'], { encoding: 'utf8', stdio: ['ignore', 'pipe', 'ignore'] }).trim(); }
  catch { /* A local preview can be built before the initial commit. */ }
}
const repository = process.env.GITHUB_REPOSITORY || 'tomooch/rogue-tactics-lab';
await writeFile(new URL('revision.json', destination), JSON.stringify({
  commit,
  verification: commit ? `https://github.com/${repository}/blob/${commit}/legacy/web-v0/VERIFICATION.md` : null,
}, null, 2) + '\n');
await writeFile(new URL('.nojekyll', destination), '');
console.log(`Published files prepared in legacy/web-v0/_site/ (${files.length} runtime files).`);
