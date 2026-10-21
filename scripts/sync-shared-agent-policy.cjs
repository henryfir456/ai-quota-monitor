'use strict';

const fs = require('node:fs');
const path = require('node:path');

const ROOT = path.resolve(__dirname, '..');
const MIRRORS = [
  'docs/policies/shared-agent-policy.md',
  'docs/agent-adapters/README.md'
];

function regularFile(filePath) {
  try {
    const stats = fs.lstatSync(filePath);
    return stats.isFile() && !stats.isSymbolicLink();
  } catch (error) {
    if (error.code === 'ENOENT') return false;
    throw error;
  }
}

function pathEntryExists(filePath) {
  try {
    fs.lstatSync(filePath);
    return true;
  } catch (error) {
    if (error.code === 'ENOENT') return false;
    throw error;
  }
}

function targetFiles(targetRoot) {
  const manifestPath = path.join(targetRoot, 'package.json');
  if (!regularFile(manifestPath)) throw new Error('target must be the ADL repository root with a regular package.json');
  const manifest = JSON.parse(fs.readFileSync(manifestPath, 'utf8'));
  if (manifest.name !== 'agent-development-loop') throw new Error('target repository identity does not match agent-development-loop');
  return MIRRORS.map((relativePath) => {
    const destination = path.resolve(targetRoot, relativePath);
    if (!destination.startsWith(targetRoot + path.sep)) throw new Error('mirror destination escaped target root');
    return {relativePath, source: path.join(ROOT, relativePath), destination};
  });
}

function ensureNoSymlinkParents(targetRoot, destination) {
  let current = targetRoot;
  const relativeDirectory = path.relative(targetRoot, path.dirname(destination));
  for (const part of relativeDirectory.split(path.sep).filter(Boolean)) {
    current = path.join(current, part);
    try {
      if (fs.lstatSync(current).isSymbolicLink()) throw new Error('mirror path cannot traverse symbolic links');
    } catch (error) {
      if (error.message === 'mirror path cannot traverse symbolic links') throw error;
      if (error.code !== 'ENOENT') throw error;
    }
  }
}

function syncSharedAgentPolicy({targetRoot, write = false, replaceExisting = false} = {}) {
  if (typeof targetRoot !== 'string' || targetRoot.trim() === '') throw new Error('target repository path is required');
  const resolvedTarget = fs.realpathSync(path.resolve(targetRoot));
  if (resolvedTarget === ROOT) throw new Error('source and target repository must be different');
  const files = targetFiles(resolvedTarget);
  const observed = files.map((file) => {
    const expected = fs.readFileSync(file.source);
    ensureNoSymlinkParents(resolvedTarget, file.destination);
    if (pathEntryExists(file.destination) && !regularFile(file.destination)) {
      throw new Error(`mirror destination must be a regular file: ${file.relativePath}`);
    }
    const exists = regularFile(file.destination);
    const same = exists && fs.readFileSync(file.destination).equals(expected);
    return {...file, expected, exists, same};
  });
  const differences = observed.filter((file) => !file.same).map((file) => file.relativePath);
  if (!write || differences.length === 0) {
    return {status: differences.length === 0 ? 'PASS' : 'DIFFERENT', differences};
  }
  if (!replaceExisting && observed.some((file) => file.exists && !file.same)) {
    const existing = observed.filter((file) => file.exists && !file.same).map((file) => file.relativePath);
    throw new Error(`existing mirrors differ; rerun with --replace-existing after reviewing: ${existing.join(', ')}`);
  }

  const staged = [];
  try {
    for (const file of observed.filter((item) => !item.same)) {
      fs.mkdirSync(path.dirname(file.destination), {recursive: true});
      ensureNoSymlinkParents(resolvedTarget, file.destination);
      const temporary = `${file.destination}.tmp-${process.pid}`;
      fs.writeFileSync(temporary, file.expected, {flag: 'wx', mode: 0o644});
      staged.push({temporary, destination: file.destination});
    }
    for (const file of staged) fs.renameSync(file.temporary, file.destination);
  } finally {
    for (const file of staged) {
      try { fs.unlinkSync(file.temporary); } catch (error) { if (error.code !== 'ENOENT') throw error; }
    }
  }
  return {status: 'SYNCED', differences};
}

function main(argv) {
  const args = [...argv];
  const targetRoot = args.shift();
  const write = args.includes('--write');
  const replaceExisting = args.includes('--replace-existing');
  if (!targetRoot || args.some((arg) => !['--write', '--replace-existing'].includes(arg)) ||
      (replaceExisting && !write) || new Set(args).size !== args.length) {
    throw new Error('usage: node scripts/sync-shared-agent-policy.cjs <adl-root> [--write [--replace-existing]]');
  }
  const result = syncSharedAgentPolicy({targetRoot, write, replaceExisting});
  process.stdout.write(JSON.stringify(result) + '\n');
  if (result.status === 'DIFFERENT') process.exitCode = 1;
}

if (require.main === module) {
  try {
    main(process.argv.slice(2));
  } catch (error) {
    process.stderr.write(`${error.message}\n`);
    process.exitCode = 2;
  }
}

module.exports = {syncSharedAgentPolicy};
