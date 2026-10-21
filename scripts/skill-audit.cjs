#!/usr/bin/env node
// Balanced Skill Audit. No network or model calls. Node.js built-ins only.
const fs = require('node:fs');
const path = require('node:path');
const crypto = require('node:crypto');
const root = process.cwd();
const args = process.argv.slice(2);
const option = (name) => { const i = args.indexOf(name); return i >= 0 ? args[i + 1] : null; };
const out = option('--out') || 'reports/skill-audit';
const basePath = option('--baseline');
const manifestName = option('--manifest') || 'agent.yaml';
const realRoot = fs.realpathSync(root);
const safeFile = (p) => {
  if (!p || /[\\\0*?<>|]/.test(p) || path.isAbsolute(p)) return null;
  const absolute = path.resolve(root, p);
  return absolute.startsWith(root + path.sep) ? absolute : null;
};
const sha = (p) => crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
const withinRoot = (p) => {
  try {
    const resolved = fs.realpathSync(p);
    return resolved === realRoot || resolved.startsWith(realRoot + path.sep);
  } catch { return false; }
};
const outputPathWithinRoot = (p) => {
  let current = p;
  while (true) {
    try { fs.lstatSync(current); }
    catch (error) {
      if (error.code !== 'ENOENT') return false;
      const parent = path.dirname(current);
      if (parent === current) return false;
      current = parent;
      continue;
    }
    return withinRoot(current);
  }
};

const findings = [];
const skills = [];
const manifest = safeFile(manifestName);
if (!manifest) { findings.push({code:'INVALID_MANIFEST_PATH',status:'FAIL'}); }
else if (!fs.existsSync(manifest)) { findings.push({code:'MANIFEST_ABSENT',status:'NOT_APPLICABLE'}); }
else if (!withinRoot(manifest)) { findings.push({code:'MANIFEST_SYMLINK_ESCAPE',status:'FAIL'}); }
else {
  let raw;
  try {
    if (!fs.statSync(manifest).isFile()) throw new Error('manifest is not a file');
    raw = fs.readFileSync(manifest, 'utf8');
  } catch {
    findings.push({code:'MANIFEST_UNREADABLE',status:'UNVERIFIED'});
  }
  if (raw !== undefined) {
    // Conservative parsing: support literal paths under skill entries; complex YAML requires a declared parser.
    const lines = raw.split(/\r?\n/);
    const references = lines.filter(x => /^\s*(?:-\s*)?(?:path|skill_path|file):\s*[^#]+/.test(x))
      .map(x=>x.replace(/^\s*(?:-\s*)?(?:path|skill_path|file):\s*/, '').trim().replace(/^['"]|['"]$/g,''));
    if (!references.length) findings.push({code:'MANIFEST_PATHS_UNVERIFIED',status:'UNVERIFIED',reason:'No literal path/skill_path/file entries; structured YAML parser may be needed'});
    for (const ref of [...new Set(references)]) {
      const target = safeFile(ref);
      if (!target) { findings.push({code:'UNSAFE_SKILL_PATH',status:'FAIL',path:ref}); continue; }
      const skillFile = fs.existsSync(target) && fs.statSync(target).isDirectory() ? path.join(target,'SKILL.md') : target;
      if (!fs.existsSync(skillFile) || !fs.statSync(skillFile).isFile()) findings.push({code:'SKILL_MISSING',status:'FAIL',path:ref});
      else if (!withinRoot(skillFile)) findings.push({code:'SKILL_SYMLINK_ESCAPE',status:'FAIL',path:ref});
      else {
        try { skills.push({path:ref,sha256:sha(skillFile),usageEvidence:'UNVERIFIED'}); }
        catch { findings.push({code:'SKILL_UNREADABLE',status:'UNVERIFIED',path:ref}); }
      }
    }
  }
}
let baseline = null;
let baselineParsed = false;
if (basePath) {
  const file = safeFile(basePath);
  if (!file || !fs.existsSync(file)) findings.push({code:'BASELINE_UNAVAILABLE',status:'UNVERIFIED'});
  else if (!withinRoot(file)) findings.push({code:'BASELINE_SYMLINK_ESCAPE',status:'FAIL'});
  else try { baseline = JSON.parse(fs.readFileSync(file,'utf8')); baselineParsed = true; } catch { findings.push({code:'BASELINE_INVALID',status:'UNVERIFIED'}); }
}
const changed = [];
const validBaseline = baselineParsed && baseline && typeof baseline === 'object' && !Array.isArray(baseline) &&
  Array.isArray(baseline.skills) && baseline.skills.every(x => x && typeof x === 'object' && !Array.isArray(x) &&
    typeof x.path === 'string' && x.path.length > 0 && typeof x.sha256 === 'string' && /^[a-f0-9]{64}$/.test(x.sha256));
if (baselineParsed && !validBaseline) findings.push({code:'BASELINE_SCHEMA_INVALID',status:'UNVERIFIED'});
if (validBaseline) {
  const older = new Map(baseline.skills.map(x => [x.path,x.sha256]));
  const current = new Map(skills.map(x => [x.path,x.sha256]));
  for (const [skillPath, digest] of current) if (older.get(skillPath)!==digest) changed.push(skillPath);
  for (const skillPath of older.keys()) if (!current.has(skillPath)) changed.push(skillPath);
}
const needsReview = changed.length > 0 || findings.some(f=>['FAIL','UNVERIFIED'].includes(f.status));
const report = {
 schemaVersion:1, mode:'balanced', manifest:manifestName,
 skills, findings, changedSkills:changed,
 aiReview:{requested:false, recommendation:needsReview?'CONDITIONAL_REVIEW':'NONE', reason:needsReview?'Changed skill or unresolved audit finding':'No detected trigger'},
 telemetry:{additionalModelCalls:0, measuredExtraTokens:null, measuredExtraCredits:null, tokenStatus:'UNKNOWN', creditsStatus:'UNKNOWN', outcomeEffect:'UNVERIFIED'},
 policy:{autoModelInvocation:false, autoMerge:false, safetyGateBypass:false},
 note:'This script performs static checks only; it cannot prove skill loading, compliance, freshness, or effectiveness.'
};
const rows=findings.map(x=>'| '+x.code+' | '+x.status+' | '+(x.path||x.reason||'—').replace(/[|\r\n]/g,' ')+' |');
const markdown=['# Agent Skill Audit — Balanced','', 'Static audit only; no model invocation.','',
'| Finding | Status | Detail |','| --- | --- | --- |',...rows,'',
'Skill references: '+skills.length,
'Conditional AI review: '+report.aiReview.recommendation,
'Additional model calls: 0 (this scanner only)',
'Additional tokens/credits: UNKNOWN (not instrumented)',
'Skill usage and outcome effectiveness: UNVERIFIED',
'', 'Safety and independent review gates remain unchanged.',''];
const outFile=safeFile(out);
const jsonFile=safeFile(out+'.json');
const markdownFile=safeFile(out+'.md');
if(!outFile || !jsonFile || !markdownFile || !outputPathWithinRoot(jsonFile) || !outputPathWithinRoot(markdownFile)) {
  process.stderr.write('Unsafe output path\n');
  process.exit(2);
}
try {
  fs.mkdirSync(path.dirname(outFile),{recursive:true});
  if (!outputPathWithinRoot(jsonFile) || !outputPathWithinRoot(markdownFile)) throw new Error('unsafe output path');
  const writeOutput = (file, content) => {
    const flags = fs.constants.O_WRONLY | fs.constants.O_CREAT | fs.constants.O_TRUNC | (fs.constants.O_NOFOLLOW || 0);
    const descriptor = fs.openSync(file,flags,0o666);
    try { fs.writeFileSync(descriptor,content); } finally { fs.closeSync(descriptor); }
  };
  writeOutput(jsonFile,JSON.stringify(report,null,2)+'\n');
  writeOutput(markdownFile,markdown.join('\n'));
} catch {
  process.stderr.write('Unable to write audit output safely\n');
  process.exit(2);
}
process.stdout.write('Audit generated: '+jsonFile+' / '+markdownFile+'\n');
if(findings.some(x=>x.status==='FAIL')) process.exitCode=1;
